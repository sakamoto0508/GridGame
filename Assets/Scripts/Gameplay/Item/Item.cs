using UnityEngine;

public class Item : PooledGridObject
{
    [SerializeField] private ItemSettings _settings;
    [Tooltip("Sceneへ手動配置するときに指定します。Spawner生成時はInitで設定されます。")]
    private GridManager _gridManager;
    public Vector3Int GridPosition { get; private set; }
    public bool IsFalling { get; private set; }
    // 同じプール個体でも再出現したItemは別の取得目標として扱います。
    public int SpawnVersion { get; private set; }
    public ItemSettings Settings => _settings;
    public bool IsAvailable => _initialized && !_removed && isActiveAndEnabled;
    private bool _initialized;
    private bool _removed;
    private Vector3 _fallStart;
    private float _fallElapsed;

    public bool Init(GridManager grid, Vector3Int position)
    {
        if (_initialized || _removed) return false;
        if (grid == null || _settings == null || !grid.TryRegisterItem(position, this))
        {
            Debug.LogError($"Item初期化失敗: Settingsまたは配置先を確認してください。Cell={position}", this);
            return false;
        }
        _gridManager = grid;
        GridPosition = position;
        transform.position = grid.GetWorldPosition(position);
        _initialized = true;
        SpawnVersion++;
        return true;
    }

    private void Update()
    {
        if (!_initialized || _removed || _gridManager == null) return;
        // 後からBlockが設置・落下した場合はItemを消します。ItemはCharacterを殺しません。
        if (_gridManager.HasBlock(GridPosition)) { Despawn(); return; }
        if (IsFalling)
        {
            _fallElapsed += Time.deltaTime;
            float rate = Mathf.Clamp01(_fallElapsed / _settings.FallDurationPerCell);
            transform.position = Vector3.Lerp(_fallStart, _gridManager.GetWorldPosition(GridPosition), rate);
            if (rate < 1f) return;
            IsFalling = false;
        }

        // 各セル到着時に取得判定します。Character側は既存の論理位置を基準にします。
        if (TryCollect(_gridManager.GetCharacter(GridPosition))) return;
        Vector3Int below = GridPosition + Vector3Int.down;
        if (_gridManager.TryMoveItem(GridPosition, below, this))
        {
            GridPosition = below;
            _fallStart = transform.position;
            _fallElapsed = 0f;
            IsFalling = true;
        }
    }

    /// <summary>Player・Enemy共通の取得処理です。二重取得を防いでから効果を付与します。</summary>
    public bool TryCollect(CharacterBase character)
    {
        if (!_initialized || _removed || IsFalling || character == null || !character.IsAlive) return false;
        InventoryComponent inventory = character.GetComponent<InventoryComponent>();
        if (inventory == null) inventory = character.gameObject.AddComponent<InventoryComponent>();
        _removed = true;
        _gridManager.TryUnregisterItem(GridPosition, this);
        inventory.Apply(_settings);
        // Pool返却でItemが消えても、Manager側で音を最後まで再生します。
        AudioManager.PlayAt(SoundId.ItemCollect, transform.position);
        Debug.Log($"Item取得: {character.name}, {_settings.Type} +{_settings.IncreaseAmount}", character);
        ReturnToPool();
        return true;
    }

    /// <summary>
    /// Itemをグリッドから解除してプールへ返却します。
    /// </summary>
    public void Despawn()
    {
        if (_removed) return;
        _removed = true;
        if (_initialized && _gridManager != null) _gridManager.TryUnregisterItem(GridPosition, this);
        ReturnToPool();
    }

    /// <summary>
    /// Itemをプールに返却する前に、グリッドから解除して状態をリセットします。
    /// </summary>
    internal override void ClearForPool()
    {
        if (_initialized && _gridManager != null) _gridManager.TryUnregisterItem(GridPosition, this);
        _gridManager = null;
        _initialized = false;
        _removed = true;
        IsFalling = false;
        _fallElapsed = 0f;
        _fallStart = default;
        GridPosition = default;
    }

    protected override void ResetForRent()
    {
        ClearForPool();
        _removed = false;
        enabled = true;
    }
}
