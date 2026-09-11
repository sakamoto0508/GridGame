using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// ステージを生成するクラス
/// </summary>
public class StageGenerator : MonoBehaviour
{
    public Vector3Int PlayerSpawnPosition => _settings.PlayerSpawnPosition;
    public Vector3Int EnemySpawnPosition => _settings.EnemySpawnPosition;
    public GridManager Grid => _gridManager;
    public Block UnbreakableBlockPrefab => _settings != null ? _settings.UnbreakableBlockPrefab : null;

    [SerializeField] private GridManager _gridManager;
    [SerializeField] private StageSettings _settings;
    [SerializeField] private StageLightingSettings _lightingSettings;
    [SerializeField] private RooftopBackgroundSettings _backgroundSettings;
    [Tooltip("設定するとランダム生成を行わず、この子に配置されたBlockを登録します。")]
    [SerializeField] private Transform _sceneBlocksRoot;
    public Transform SceneBlocksRoot => _sceneBlocksRoot;
    public RooftopBackgroundSettings BackgroundSettings => _backgroundSettings;
    private BoundaryVisibilityController _boundaryView;

    /// <summary>背景は難易度選択中にも表示するため、試合開始を待たずに生成します。</summary>
    private void Start()
    {
        RooftopBackgroundController background = GetComponent<RooftopBackgroundController>();
        if (background == null) background = gameObject.AddComponent<RooftopBackgroundController>();
        background.Init(_gridManager, _backgroundSettings);
    }

    /// <summary>通常グリッドの外に床・四方の壁・天井を作り、内部に破壊可能Blockを生成します。</summary>
    public bool GenerateStage()
    {
        if (_gridManager == null || _settings == null)
        {
            Debug.LogError("StageGeneratorのGridManagerまたはStage Settingsが未設定です。", this);
            return false;
        }

        Random.InitState(_settings.RandomSeed);
        _boundaryView = GetComponent<BoundaryVisibilityController>();
        if (_boundaryView == null) _boundaryView = gameObject.AddComponent<BoundaryVisibilityController>();
        _boundaryView.Init(_gridManager);
        if (_sceneBlocksRoot != null)
        {
            if (!RegisterSceneBlocks()) return false;
        }
        else if (!GenerateBoundary()) return false;
        StageLightingController lighting = GetComponent<StageLightingController>();
        if (lighting == null) 
            lighting = gameObject.AddComponent<StageLightingController>();
        lighting.Init(_gridManager, _lightingSettings);
        if (_sceneBlocksRoot == null) GenerateBreakableBlocks();
        return true;
    }

    /// <summary>全セルを検証→全Block登録→初期化の順で行い、未登録の足場へ落下しないようにします。</summary>
    private bool RegisterSceneBlocks()
    {
        Block[] blocks = _sceneBlocksRoot.GetComponentsInChildren<Block>(false);
        var positions = new System.Collections.Generic.Dictionary<Vector3Int, Block>();
        float tolerance = Vector3.Distance(_gridManager.GetWorldPosition(Vector3Int.zero), _gridManager.GetWorldPosition(Vector3Int.right)) * 0.01f;
        foreach (Block block in blocks)
        {
            Vector3Int p = _gridManager.GetGridPosition(block.transform.position);
            bool boundary = _gridManager.IsBoundaryPosition(p);
            if (!block.HasSettings || (!boundary && !_gridManager.Contains(p)) ||
                (boundary && block.Type != BlockType.Unbreakable) ||
                Vector3.Distance(block.transform.position, _gridManager.GetWorldPosition(p)) > tolerance ||
                positions.ContainsKey(p) || _gridManager.GetBlock(p) != null ||
                p == PlayerSpawnPosition || p == EnemySpawnPosition)
            {
                Debug.LogError($"Scene Blockの配置が不正です: {block.name}, {p}。セル中心・重複・範囲・Settings・開始地点を確認してください。", block);
                return false;
            }
            positions.Add(p, block);
        }
        if (blocks.Length == 0) { Debug.LogError("Scene Blocks Rootに有効なBlockがありません。", this); return false; }
        foreach (var entry in positions)
        {
            entry.Value.PrepareForRent(null); // Scene配置Blockの状態を初期化。破壊時は通常破棄します。
            bool ok = _gridManager.IsBoundaryPosition(entry.Key)
                ? _gridManager.TryRegisterBoundaryBlock(entry.Key, entry.Value)
                : _gridManager.TryRegisterBlock(entry.Key, entry.Value);
            if (!ok)
            {
                foreach (var registered in positions) _gridManager.TryUnregisterBlock(registered.Key, registered.Value);
                Debug.LogError($"Scene Block登録に失敗しました: {entry.Key}", this);
                return false;
            }
        }
        System.Array.Sort(blocks, (a, b) => a.transform.position.y.CompareTo(b.transform.position.y));
        foreach (Block block in blocks)
        {
            Vector3Int p = _gridManager.GetGridPosition(block.transform.position);
            block.Initialize(_gridManager, p);
            if (_gridManager.IsBoundaryPosition(p)) _boundaryView.Register(block, p);
        }
        return true;
    }

    /// <summary>
    /// 6面の外殻を生成します。面ごとの範囲を分け、辺と角の重複を避けます。
    /// </summary>
    private bool GenerateBoundary()
    {
        Block prefab = _settings.UnbreakableBlockPrefab;
        if (prefab == null || prefab.Type != BlockType.Unbreakable)
        {
            Debug.LogError("外殻生成にはUnbreakableのBlock Prefabが必要です。", this);
            return false;
        }
        Vector3Int size = _gridManager.Size;
        // 床と天井は角も含めて全面を覆います。
        for (int x = -1; x <= size.x; x++)
        for (int z = -1; z <= size.z; z++)
        {
            SpawnBoundaryBlock(new Vector3Int(x, -1, z), prefab);
            SpawnBoundaryBlock(new Vector3Int(x, size.y, z), prefab);
        }
        for (int y = 0; y < size.y; y++)
        {
            for (int z = -1; z <= size.z; z++)
            {
                SpawnBoundaryBlock(new Vector3Int(-1, y, z), prefab);
                SpawnBoundaryBlock(new Vector3Int(size.x, y, z), prefab);
            }
            // 四隅の列は上で生成済みなのでXは内部範囲のみ。
            for (int x = 0; x < size.x; x++)
            {
                SpawnBoundaryBlock(new Vector3Int(x, y, -1), prefab);
                SpawnBoundaryBlock(new Vector3Int(x, y, size.z), prefab);
            }
        }
        return true;
    }

    private void SpawnBoundaryBlock(Vector3Int position, Block prefab)
    {
#if UNITY_EDITOR
        if (_editorBlocksRoot != null) { CreateEditorBlock(position, prefab); return; }
#endif
        // 再生成要求でも、登録済み外殻の重複生成はしません。
        Block existing = _gridManager.GetBlock(position);
        if (existing != null)
        {
            _boundaryView.Register(existing, position);
            return;
        }
        Block block = GridObjectPool.For(_gridManager).Rent(prefab,
            _gridManager.GetWorldPosition(position), Quaternion.identity);
        if (!_gridManager.TryRegisterBoundaryBlock(position, block))
        {
            block.Despawn();
            Debug.LogWarning($"外殻Blockの登録に失敗しました: {position}", this);
            return;
        }
        block.Initialize(_gridManager, position);
        _boundaryView.Register(block, position);
    }

    /// <summary>
    /// 指定された位置にブロックを生成する
    /// </summary>
    /// <param name="position"></param>
    /// <param name="prefab"></param>
    /// <returns></returns>
    private bool SpawnBlock(Vector3Int position, Block prefab)
    {
        if (!_gridManager.Contains(position) || prefab == null)
            return false;

#if UNITY_EDITOR
        if (_editorBlocksRoot != null) { CreateEditorBlock(position, prefab); return true; }
#endif

        Block block = GridObjectPool.For(_gridManager).Rent(prefab, _gridManager.GetWorldPosition(position),
            Quaternion.identity);

        if (!_gridManager.TryRegisterBlock(position, block))
        {
            block.Despawn();
            return false;
        }

        block.Initialize(_gridManager, position);
        return true;
    }

    /// <summary>開始地点の安全地帯を除く内側セルへ破壊可能Blockをランダム配置します。</summary>
    private void GenerateBreakableBlocks()
    {
        Vector3Int size = _gridManager.Size;
        int blockY = _settings.BreakableBlockY;

        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.z; z++)
            {
                Vector3Int position = new Vector3Int(x, blockY, z);

                if (IsSpawnSafeCell(position))
                    continue;

                if (Random.value > _settings.BreakableBlockRate)
                    continue;

                SpawnBlock(position, _settings.BreakableBlockPrefab);
            }
        }
    }

    /// <summary>指定セルがPlayerの開始地点または脱出用セルかを判定します。</summary>
    private bool IsSpawnSafeCell(Vector3Int position)
    {
        return position == _settings.PlayerSpawnPosition ||
               position == _settings.EnemySpawnPosition ||
               position == _settings.PlayerSpawnPosition + Vector3Int.right ||
               position == _settings.PlayerSpawnPosition + Vector3Int.forward ||
               position == _settings.EnemySpawnPosition + Vector3Int.left ||
               position == _settings.EnemySpawnPosition + Vector3Int.back;
    }

#if UNITY_EDITOR
    private Transform _editorBlocksRoot;
    /// <summary>現在のSeed/Settingsと同じ配置をPrefabインスタンスとしてSceneへ作ります。</summary>
    public Transform BakeBlocksForScene()
    {
        if (Application.isPlaying || _gridManager == null || _settings == null ||
            _settings.UnbreakableBlockPrefab == null || _settings.BreakableBlockPrefab == null)
            throw new System.InvalidOperationException("Grid、StageSettings、2種類のBlock Prefabを設定してPlayを停止してください。");
        Random.State state = Random.state;
        GameObject root = new GameObject("Scene Stage Blocks");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, gameObject.scene);
        _editorBlocksRoot = root.transform;
        try
        {
            Random.InitState(_settings.RandomSeed);
            if (!GenerateBoundary()) throw new System.InvalidOperationException("外殻を生成できませんでした。");
            GenerateBreakableBlocks();
            UnityEditor.Undo.RecordObject(this, "Assign Scene Blocks");
            _sceneBlocksRoot = root.transform;
            UnityEditor.EditorUtility.SetDirty(this);
            return root.transform;
        }
        catch { DestroyImmediate(root); throw; }
        finally { _editorBlocksRoot = null; Random.state = state; }
    }

    private void CreateEditorBlock(Vector3Int position, Block prefab)
    {
        GameObject instance = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab.gameObject, _editorBlocksRoot);
        instance.name = $"{prefab.name} [{position.x},{position.y},{position.z}]";
        instance.transform.position = _gridManager.GetWorldPosition(position);
        UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
    }
#endif
}
