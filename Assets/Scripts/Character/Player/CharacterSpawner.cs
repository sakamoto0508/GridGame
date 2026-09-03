using UnityEngine;

/// <summary>Playerを生成し、Scene固有の参照を各Componentへ注入します。</summary>
public class CharacterSpawner : MonoBehaviour
{
    [SerializeField] private GridManager _gridManager;
    [SerializeField] private CharacterPrefabSettings _settings;
    [SerializeField] private Camera _gameCamera;

    /// <summary>指定座標へPlayerを生成し、必要なScene参照を注入します。</summary>
    public PlayerCharacter SpawnPlayer(Vector3Int gridPosition)
    {
        if (_gridManager == null || _settings == null || _settings.PlayerPrefab == null)
        {
            Debug.LogError("Playerを生成できません: GridManagerまたはPlayer Prefabが未設定です。", this);
            return null;
        }

        Vector3 worldPosition = _gridManager.GetWorldPosition(gridPosition);

        PlayerCharacter player = Instantiate(_settings.PlayerPrefab, worldPosition, Quaternion.identity);
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
            blockPlacement.Init(_gridManager, _settings.PlaceableBlockPrefab);
        }

        if (bombComponent == null)
        {
            Debug.LogError(
                "生成したPlayerにBombComponentがありません。Player Prefabを確認してください。",
                player);
        }
        else
        {
            bombComponent.Init(_gridManager, _settings.BombPrefab);
        }

        return player;
    }

    /// <summary>
    /// Enemyを生成して共通Componentと簡易AIを初期化します。
    /// </summary>
    public EnemyCharacter SpawnTestEnemy(
        Vector3Int gridPosition,
        CharacterBase player,
        EnemyDifficulty difficulty,
        EnemyAISettings enemyAISettings)
    {
        if (_gridManager == null || _settings == null || _settings.EnemyPrefab == null)
        {
            Debug.LogError(
                "Test Enemyを生成できません: GridManagerまたはTest Enemy Prefabが未設定です。",
                this);
            return null;
        }

        EnemyCharacter enemy = Instantiate(
            _settings.EnemyPrefab,
            _gridManager.GetWorldPosition(gridPosition),
            Quaternion.identity);
        MovementComponent movement = enemy.GetComponent<MovementComponent>();
        BombComponent bombComponent = enemy.GetComponent<BombComponent>();
        BlockPlacementComponent blockPlacement = enemy.GetComponent<BlockPlacementComponent>();
        EnemyBrain enemyBrain = enemy.GetComponent<EnemyBrain>();

        // 既存Enemy PrefabにはRequireComponent追加が遡って反映されないため補完します。
        if (blockPlacement == null)
        {
            blockPlacement = enemy.gameObject.AddComponent<BlockPlacementComponent>();
            Debug.LogWarning(
                "Enemy PrefabにBlockPlacementComponentがなかったため実行時に追加しました。Prefabへの追加を推奨します。",
                enemy);
        }

        if (movement == null || !movement.Init(_gridManager, gridPosition))
        {
            Debug.LogError($"Test Enemyのグリッド登録に失敗しました: Cell={gridPosition}", enemy);
            Destroy(enemy.gameObject);
            return null;
        }

        if (bombComponent == null || blockPlacement == null || enemyBrain == null)
        {
            Debug.LogError(
                "Test EnemyにBombComponent、BlockPlacementComponent、EnemyBrainのいずれかがありません。Enemy Prefabを確認してください。",
                enemy);
            movement.UnregisterFromGrid();
            Destroy(enemy.gameObject);
            return null;
        }

        bombComponent.Init(_gridManager, _settings.BombPrefab);
        blockPlacement.Init(_gridManager, _settings.PlaceableBlockPrefab);

        if (!enemyBrain.Init(_gridManager, player, difficulty, enemyAISettings))
        {
            movement.UnregisterFromGrid();
            Destroy(enemy.gameObject);
            return null;
        }

        Debug.Log(
            $"Test Enemyを生成しました: Cell={gridPosition}, Difficulty={difficulty}",
            enemy);
        return enemy;
    }
}
