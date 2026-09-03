using UnityEngine;

/// <summary>Playerを生成し、Scene固有の参照を各Componentへ注入します。</summary>
public class CharacterSpawner : MonoBehaviour
{
    [SerializeField] private GridManager _gridManager;
    [SerializeField] private PlayerCharacter _playerPrefab;
    [SerializeField] private EnemyCharacter _testEnemyPrefab;
    [SerializeField] private Camera _gameCamera;
    [SerializeField] private Block _placeableBlockPrefab;
    [SerializeField] private Bomb _bombPrefab;

    /// <summary>指定座標へPlayerを生成し、必要なScene参照を注入します。</summary>
    public PlayerCharacter SpawnPlayer(Vector3Int gridPosition)
    {
        if (_gridManager == null || _playerPrefab == null)
        {
            Debug.LogError("Playerを生成できません: GridManagerまたはPlayer Prefabが未設定です。", this);
            return null;
        }

        Vector3 worldPosition = _gridManager.GetWorldPosition(gridPosition);

        PlayerCharacter player = Instantiate(_playerPrefab, worldPosition, Quaternion.identity);
        MovementComponent movement = player.GetComponent<MovementComponent>();
        PlayerController controller=player.GetComponent<PlayerController>();
        BlockPlacementComponent blockPlacement = player.GetComponent<BlockPlacementComponent>();
        BombComponent bombComponent = player.GetComponent<BombComponent>();

        if (movement == null || !movement.Init(_gridManager, gridPosition))
        {
            Debug.LogError($"Playerのグリッド登録に失敗しました: Cell={gridPosition}", player);
            Destroy(player.gameObject);
            return null;
        }

        if (controller != null)
            controller.Init(_gameCamera);

        if (blockPlacement == null)
        {
            Debug.LogError(
                "生成したPlayerにBlockPlacementComponentがありません。Player Prefabを確認してください。",
                player);
        }
        else
        {
            blockPlacement.Init(_gridManager, _placeableBlockPrefab);
        }

        if (bombComponent == null)
        {
            Debug.LogError(
                "生成したPlayerにBombComponentがありません。Player Prefabを確認してください。",
                player);
        }
        else
        {
            bombComponent.Init(_gridManager, _bombPrefab);
        }

        return player;
    }

    /// <summary>
    /// AIをまだ持たない動作確認用Enemyを生成し、開始セルへ登録します。
    /// MovementComponentとLifeComponentはPlayerと同じ共通実装を使用します。
    /// </summary>
    public EnemyCharacter SpawnTestEnemy(Vector3Int gridPosition)
    {
        if (_gridManager == null || _testEnemyPrefab == null)
        {
            Debug.LogError(
                "Test Enemyを生成できません: GridManagerまたはTest Enemy Prefabが未設定です。",
                this);
            return null;
        }

        EnemyCharacter enemy = Instantiate(
            _testEnemyPrefab,
            _gridManager.GetWorldPosition(gridPosition),
            Quaternion.identity);
        MovementComponent movement = enemy.GetComponent<MovementComponent>();

        if (movement == null || !movement.Init(_gridManager, gridPosition))
        {
            Debug.LogError($"Test Enemyのグリッド登録に失敗しました: Cell={gridPosition}", enemy);
            Destroy(enemy.gameObject);
            return null;
        }

        Debug.Log($"Test Enemyを生成しました: Cell={gridPosition}", enemy);
        return enemy;
    }
}
