using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// ステージを生成するクラス
/// </summary>
public class StageGenerator : MonoBehaviour
{
    public Vector3Int PlayerSpawnPosition => _settings.PlayerSpawnPosition;
    public Vector3Int EnemySpawnPosition => _settings.EnemySpawnPosition;

    [SerializeField] private GridManager _gridManager;
    [SerializeField] private StageSettings _settings;

    /// <summary>通常グリッドの外に床・四方の壁・天井を作り、内部に破壊可能Blockを生成します。</summary>
    public void GenerateStage()
    {
        if (_gridManager == null || _settings == null)
        {
            Debug.LogError("StageGeneratorのGridManagerまたはStage Settingsが未設定です。", this);
            return;
        }

        Random.InitState(_settings.RandomSeed);
        if (!GenerateBoundary()) return;
        GenerateBreakableBlocks();
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
        // 再生成要求でも、登録済み外殻の重複生成はしません。
        if (_gridManager.GetBlock(position) != null) return;
        Block block = GridObjectPool.For(_gridManager).Rent(prefab,
            _gridManager.GetWorldPosition(position), Quaternion.identity);
        if (!_gridManager.TryRegisterBoundaryBlock(position, block))
        {
            block.Despawn();
            Debug.LogWarning($"外殻Blockの登録に失敗しました: {position}", this);
            return;
        }
        block.Initialize(_gridManager, position);
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
}
