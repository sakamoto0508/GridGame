using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 試合時間→予告→落下→着地待ちを順に処理します。同時に扱う落下は1個です。
/// GridManagerと同じObjectに配置し、GridDangerMapから予告情報を参照します。
/// </summary>
[DisallowMultipleComponent]
public class EndPhaseManager : MonoBehaviour
{
    public bool IsEndPhase { get; private set; }
    private GridManager _grid;
    private GridBomberGameState _gameState;
    private EndPhaseSettings _settings;
    private EndPhaseSettings _runtimeSettings;
    private Block _prefab;
    private Block _activeBlock;
    private Vector3Int _column;
    private float _matchElapsed;
    private float _dropAt;
    private float _nextWarningAt;
    private bool _warning;
    private bool _initialized;
    private bool _spawning;
    private LineRenderer _marker;
    private Material _markerMaterial;
    private readonly List<Vector3Int> _candidates = new();
    private static readonly int[] BoxPath = { 0, 1, 3, 2, 0, 4, 5, 1, 5, 7, 3, 7, 6, 2, 6, 4 };

    public void Init(GridManager grid, GridBomberGameState gameState, EndPhaseSettings settings, Block fallbackPrefab)
    {
        if (_initialized) return;
        _grid = grid;
        _gameState = gameState;
        _settings = settings;
        if (_settings == null)
        {
            _runtimeSettings = ScriptableObject.CreateInstance<EndPhaseSettings>();
            _settings = _runtimeSettings;
        }
        _prefab = _settings.BlockPrefab != null ? _settings.BlockPrefab : fallbackPrefab;
        if (!_settings.Enabled) return;
        if (_grid == null || _gameState == null || _prefab == null ||
            !_prefab.HasSettings || _prefab.Type != BlockType.Unbreakable)
        {
            Debug.LogError("EndPhase: Grid、GameState、設定付きUnbreakable Block Prefabが必要です。", this);
            return;
        }
        // 見えない予告のまま落下させないため、マーカー生成失敗時はイベント自体を止めます。
        if (!CreateMarker()) return;
        _gameState.StateChanged += HandleStateChanged;
        _initialized = true;
    }

    private void Update()
    {
        if (!_initialized || _gameState == null || _gameState.State != MatchState.Playing) return;
        if (!_settings.Enabled) { CancelPending(); return; }
        _matchElapsed += Time.deltaTime;
        if (!IsEndPhase)
        {
            if (_matchElapsed < _settings.StartAfterSeconds) return;
            IsEndPhase = true;
            Debug.Log("End phase started: 落下ブロックに注意してください。", this);
        }
        if (_activeBlock != null)
        {
            if (_activeBlock.IsFalling) return;
            // 着地したBlockは盤面に残します。管理対象だけを手放します。
            _activeBlock = null;
            _marker.enabled = false;
            _nextWarningAt = Time.time + _settings.DropInterval;
            return;
        }
        if (_warning)
        {
            if (Time.time >= _dropAt) SpawnDrop();
            return;
        }
        if (Time.time < _nextWarningAt) return;
        BeginWarning();
    }

    private void BeginWarning()
    {
        _candidates.Clear();
        Vector3Int size = _grid.Size;
        for (int x = 0; x < size.x; x++)
        for (int z = 0; z < size.z; z++)
        {
            Vector3Int top = new Vector3Int(x, size.y - 1, z);
            if (_grid.CanFallingBlockEnter(top)) _candidates.Add(top);
        }
        _nextWarningAt = Time.time + _settings.DropInterval;
        // 全列が埋まっている場合は生成せず、間隔を空けて再確認します。
        if (_candidates.Count == 0) return;
        _column = _candidates[Random.Range(0, _candidates.Count)];
        _dropAt = Time.time + _settings.WarningSeconds;
        _warning = true;
        ShowMarker();
    }

    private void SpawnDrop()
    {
        _warning = false;
        // 予告中に設置されたBlock/Bomb/予約があれば、その予告はキャンセルします。
        // 別列へ無予告で落とすことはしません。
        if (!_grid.CanFallingBlockEnter(_column))
        {
            _marker.enabled = false;
            _nextWarningAt = Time.time + _settings.DropInterval;
            return;
        }
        _activeBlock = GridObjectPool.For(_grid).Rent(_prefab, _grid.GetWorldPosition(_column), Quaternion.identity);
        if (!_grid.TryRegisterFallingBlock(_column, _activeBlock))
        {
            _activeBlock.Despawn();
            _activeBlock = null;
            _marker.enabled = false;
            _nextWarningAt = Time.time + _settings.DropInterval;
            return;
        }
        // 最上段に留まったCharacterも、予告終了後の生成時に押し潰します。
        _spawning = true;
        try { _activeBlock.Initialize(_grid, _column, true); }
        finally { _spawning = false; }
    }

    /// <summary>
    /// 爆風予測へ落下予告を合流。地形が途中で壊れる可能性を考え、列全体を保守的に危険扱い。
    /// 時刻は全セルとも生成時刻を使い、実際の到達より遅い予測にならないようにします。
    /// </summary>
    public void AppendDanger(GridDangerMap map)
    {
        if (!_initialized || !isActiveAndEnabled || _gameState == null ||
            _gameState.State != MatchState.Playing || !_settings.Enabled) return;
        bool falling = _activeBlock != null && _activeBlock.IsFalling;
        if (!_warning && !falling) return;
        float seconds = _warning ? Mathf.Max(0f, _dropAt - Time.time) : 0f;
        for (int y = 0; y < _grid.Size.y; y++)
            map.RegisterDanger(new Vector3Int(_column.x, y, _column.z), seconds);
    }

    private bool CreateMarker()
    {
        Shader shader = Resources.Load<Shader>("BoundaryOutline");
        if (shader == null)
        {
            Debug.LogError("EndPhase: BoundaryOutline Shaderがないため予告を表示できません。", this);
            return false;
        }
        _markerMaterial = new Material(shader);
        GameObject markerObject = new GameObject("Falling Block Warning");
        markerObject.transform.SetParent(transform, false);
        _marker = markerObject.AddComponent<LineRenderer>();
        _marker.sharedMaterial = _markerMaterial;
        _marker.useWorldSpace = true;
        _marker.positionCount = BoxPath.Length;
        _marker.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _marker.receiveShadows = false;
        _marker.enabled = false;
        return true;
    }

    private void ShowMarker()
    {
        float cell = Vector3.Distance(_grid.GetWorldPosition(Vector3Int.zero), _grid.GetWorldPosition(Vector3Int.right));
        Vector3 bottom = _grid.GetWorldPosition(new Vector3Int(_column.x, 0, _column.z));
        Vector3 top = _grid.GetWorldPosition(_column);
        // 内部の柱全体を赤枠で示します。地形の上にある線も見えるよう列を丸ごと表示します。
        for (int i = 0; i < BoxPath.Length; i++)
        {
            int corner = BoxPath[i];
            _marker.SetPosition(i, new Vector3(bottom.x + ((corner & 1) == 0 ? -0.48f : 0.48f) * cell,
                (corner & 2) == 0 ? bottom.y - cell * 0.48f : top.y + cell * 0.48f,
                bottom.z + ((corner & 4) == 0 ? -0.48f : 0.48f) * cell));
        }
        _marker.widthMultiplier = cell * _settings.LineWidthInCells;
        _markerMaterial.color = _settings.WarningColor;
        _marker.enabled = true;
    }

    private void HandleStateChanged(MatchState state)
    {
        if (state != MatchState.Playing) CancelPending();
    }

    private void CancelPending()
    {
        _warning = false;
        if (_marker != null) _marker.enabled = false;
        // 着地済みは残し、落下中は返却して試合終了後の追加死亡を防ぎます。
        if (_activeBlock != null)
        {
            Block block = _activeBlock;
            _activeBlock = null;
            if (block.IsFalling || _spawning) block.Despawn();
        }
        _nextWarningAt = Time.time + (_settings != null ? _settings.DropInterval : 0f);
    }

    private void OnDisable() => CancelPending();

    private void OnDestroy()
    {
        if (_gameState != null) _gameState.StateChanged -= HandleStateChanged;
        if (_markerMaterial != null) Destroy(_markerMaterial);
        if (_marker != null) Destroy(_marker.gameObject);
        if (_runtimeSettings != null) Destroy(_runtimeSettings);
    }
}
