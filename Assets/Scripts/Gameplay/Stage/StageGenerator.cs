using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// ステージを生成するクラス
/// </summary>
public class StageGenerator : MonoBehaviour
{
    public Vector3Int PlayerSpawnPosition => _playerSpawnPosition;
    public Vector3Int EnemySpawnPosition => _enemySpawnPosition;

    [SerializeField] private GridManager _gridManager;
    [SerializeField] private Block _unbreakableBlockPrefab;
    [SerializeField] private Block _breakableBlockPrefab;
    [SerializeField] private Vector3Int _playerSpawnPosition = new Vector3Int(1, 1, 1);
    [SerializeField] private Vector3Int _enemySpawnPosition = new Vector3Int(5, 1, 5);
    [SerializeField] private float _breakableBlockRate = 0.4f;

    [SerializeField] private int _randomSeed = 12345;

    /// <summary>固定床、外壁、破壊可能Blockの順にステージを生成します。</summary>
    public void GenerateStage()
    {
        Random.InitState(_randomSeed);
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
                SpawnBlock(new Vector3Int(x, 0, z), _unbreakableBlockPrefab);
            }
        }
    }

    /// <summary>
    /// ステージの外壁を生成する
    /// </summary>
    private void GenerateOuterWalls()
    {
        Vector3Int size = _gridManager.Size;
        const int wallY = 1;

        for (int x = 0; x < size.x; x++)
        {
            SpawnBlock(new Vector3Int(x, wallY, 0), _unbreakableBlockPrefab);

            SpawnBlock(new Vector3Int(x, wallY, size.z - 1), _unbreakableBlockPrefab);
        }

        for (int z = 1; z < size.z - 1; z++)
        {
            SpawnBlock(new Vector3Int(0, wallY, z), _unbreakableBlockPrefab);

            SpawnBlock(new Vector3Int(size.x - 1, wallY, z), _unbreakableBlockPrefab);
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
        const int blockY = 1;

        for (int x = 1; x < size.x - 1; x++)
        {
            for (int z = 1; z < size.z - 1; z++)
            {
                Vector3Int position = new Vector3Int(x, blockY, z);

                if (IsSpawnSafeCell(position))
                    continue;

                if (Random.value > _breakableBlockRate)
                    continue;

                SpawnBlock(position, _breakableBlockPrefab);
            }
        }
    }

    /// <summary>指定セルがPlayerの開始地点または脱出用セルかを判定します。</summary>
    private bool IsSpawnSafeCell(Vector3Int position)
    {
        return position == _playerSpawnPosition ||
               position == _enemySpawnPosition ||
               position == _playerSpawnPosition + Vector3Int.right ||
               position == _playerSpawnPosition + Vector3Int.forward ||
               position == _enemySpawnPosition + Vector3Int.left ||
               position == _enemySpawnPosition + Vector3Int.back;
    }
}
