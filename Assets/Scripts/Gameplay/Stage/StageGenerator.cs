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

    /// <summary>固定床、外壁、破壊可能Blockの順にステージを生成します。</summary>
    public void GenerateStage()
    {
        if (_gridManager == null || _settings == null)
        {
            Debug.LogError("StageGeneratorのGridManagerまたはStage Settingsが未設定です。", this);
            return;
        }

        Random.InitState(_settings.RandomSeed);
        GenerateFloor();
        GenerateOuterWalls();
        GenerateBreakableBlocks();
    }

    /// <summary>
    /// ステージの床を生成する
    /// </summary>
    private void GenerateFloor()
    {
        Vector3Int size = _gridManager.Size;

        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.z; z++)
            {
                SpawnBlock(new Vector3Int(x, _settings.FloorY, z), _settings.UnbreakableBlockPrefab);
            }
        }
    }

    /// <summary>
    /// ステージの外壁を生成する
    /// </summary>
    private void GenerateOuterWalls()
    {
        Vector3Int size = _gridManager.Size;
        int wallY = _settings.WallY;

        for (int x = 0; x < size.x; x++)
        {
            SpawnBlock(new Vector3Int(x, wallY, 0), _settings.UnbreakableBlockPrefab);

            SpawnBlock(new Vector3Int(x, wallY, size.z - 1), _settings.UnbreakableBlockPrefab);
        }

        for (int z = 1; z < size.z - 1; z++)
        {
            SpawnBlock(new Vector3Int(0, wallY, z), _settings.UnbreakableBlockPrefab);

            SpawnBlock(new Vector3Int(size.x - 1, wallY, z), _settings.UnbreakableBlockPrefab);
        }
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

        Block block = Instantiate(prefab, _gridManager.GetWorldPosition(position),
            Quaternion.identity, transform);

        if (!_gridManager.TryRegisterBlock(position, block))
        {
            Destroy(block.gameObject);
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

        for (int x = 1; x < size.x - 1; x++)
        {
            for (int z = 1; z < size.z - 1; z++)
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
