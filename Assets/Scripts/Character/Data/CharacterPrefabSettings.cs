using UnityEngine;

/// <summary>Character生成と配置能力で共有するPrefab参照を設定します。</summary>
[CreateAssetMenu(fileName = "CharacterPrefabSettings", menuName = "3D Grid Bomber/Settings/Character Prefabs")]
public class CharacterPrefabSettings : ScriptableObject
{
    [SerializeField] private PlayerCharacter _playerPrefab;
    [SerializeField] private EnemyCharacter _enemyPrefab;
    [SerializeField] private Block _placeableBlockPrefab;
    [SerializeField] private Bomb _bombPrefab;

    public PlayerCharacter PlayerPrefab => _playerPrefab;
    public EnemyCharacter EnemyPrefab => _enemyPrefab;
    public Block PlaceableBlockPrefab => _placeableBlockPrefab;
    public Bomb BombPrefab => _bombPrefab;
}
