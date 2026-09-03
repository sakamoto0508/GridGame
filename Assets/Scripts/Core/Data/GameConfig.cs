using UnityEngine;

/// <summary>
/// ジャンル別Settings Assetを一覧できる任意のカタログです。
/// 実行Componentは必要なSettingsだけを直接参照します。
/// </summary>
[CreateAssetMenu(fileName = "GameConfig", menuName = "3D Grid Bomber/Settings/Game Config Catalog")]
public class GameConfig : ScriptableObject
{
    [SerializeField] private GridSettings _grid;
    [SerializeField] private CharacterMovementSettings _characterMovement;
    [SerializeField] private CharacterPrefabSettings _characterPrefabs;
    [SerializeField] private BombSettings _bomb;
    [SerializeField] private StageSettings _stage;
    [SerializeField] private EnemyAISettings _enemyAI;
    [SerializeField] private ExplosionVisualSettings _explosionVisual;
    [SerializeField] private GameHudSettings _gameHud;

    public GridSettings Grid => _grid;
    public CharacterMovementSettings CharacterMovement => _characterMovement;
    public CharacterPrefabSettings CharacterPrefabs => _characterPrefabs;
    public BombSettings Bomb => _bomb;
    public StageSettings Stage => _stage;
    public EnemyAISettings EnemyAI => _enemyAI;
    public ExplosionVisualSettings ExplosionVisual => _explosionVisual;
    public GameHudSettings GameHud => _gameHud;
}
