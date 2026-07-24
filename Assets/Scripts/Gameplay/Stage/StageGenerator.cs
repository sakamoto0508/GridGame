using UnityEngine;

/// <summary>
/// ステージを生成するクラス
/// </summary>
public class StageGenerator : MonoBehaviour
{
    [SerializeField] private GridManager _gridManager;
    [SerializeField] private Block _unbreakableBlockPrefab;

    private void Start()
    {
        GenerateStage();
    }

    private void GenerateStage()
    {
        GenerateFloor();
        GenerateOuterWalls();
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
                SpawnBlock(new Vector3Int(x, 0, z),_unbreakableBlockPrefab);
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
            SpawnBlock(new Vector3Int(x, wallY, 0),_unbreakableBlockPrefab);

            SpawnBlock(new Vector3Int(x, wallY, size.z - 1),_unbreakableBlockPrefab);
        }

        for (int z = 1; z < size.z - 1; z++)
        {
            SpawnBlock(new Vector3Int(0, wallY, z),_unbreakableBlockPrefab);

            SpawnBlock(new Vector3Int(size.x - 1, wallY, z),_unbreakableBlockPrefab);
        }
    }

    /// <summary>
    /// 指定された位置にブロックを生成する
    /// </summary>
    /// <param name="position"></param>
    /// <param name="prefab"></param>
    /// <returns></returns>
    private bool SpawnBlock(Vector3Int position,Block prefab)
    {
        if (!_gridManager.Contains(position) || prefab == null)
            return false;

        Block block = Instantiate(prefab,_gridManager.GetWorldPosition(position),
            Quaternion.identity,transform);

        if (!_gridManager.TryRegisterBlock(position, block))
        {
            Destroy(block.gameObject);
            return false;
        }

        block.Initialize(position);
        return true;
    }
}
