using UnityEngine;

/// <summary>ステージ生成座標、生成率、Seed、Block Prefabを設定します。</summary>
[CreateAssetMenu(fileName = "StageSettings", menuName = "3D Grid Bomber/Settings/Stage")]
public class StageSettings : ScriptableObject
{
    [Header("Prefabs")]
    [SerializeField] private Block _unbreakableBlockPrefab;
    [SerializeField] private Block _breakableBlockPrefab;

    [Header("Spawn Positions")]
    [SerializeField] private Vector3Int _playerSpawnPosition = new Vector3Int(1, 1, 1);
    [SerializeField] private Vector3Int _enemySpawnPosition = new Vector3Int(5, 1, 5);

    [Header("Generation")]
    [SerializeField, Range(0f, 1f)] private float _breakableBlockRate = 0.4f;
    [SerializeField] private int _randomSeed = 12345;
    [SerializeField, Min(0)] private int _floorY;
    [SerializeField, Min(0)] private int _wallY = 1;
    [SerializeField, Min(0)] private int _breakableBlockY = 1;

    public Block UnbreakableBlockPrefab => _unbreakableBlockPrefab;
    public Block BreakableBlockPrefab => _breakableBlockPrefab;
    public Vector3Int PlayerSpawnPosition => _playerSpawnPosition;
    public Vector3Int EnemySpawnPosition => _enemySpawnPosition;
    public float BreakableBlockRate => _breakableBlockRate;
    public int RandomSeed => _randomSeed;
    public int FloorY => _floorY;
    public int WallY => _wallY;
    public int BreakableBlockY => _breakableBlockY;
}
