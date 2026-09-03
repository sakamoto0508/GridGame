using UnityEngine;

/// <summary>Playerを生成し、Scene固有の参照を各Componentへ注入します。</summary>
public class CharacterSpawner : MonoBehaviour
{
    [SerializeField] private GridManager _gridManager;
    [SerializeField] private PlayerCharacter _playerPrefab;
    [SerializeField] private Camera _gameCamera;
    [SerializeField] private Block _placeableBlockPrefab;
    [SerializeField] private Bomb _bombPrefab;

    /// <summary>指定座標へPlayerを生成し、必要なScene参照を注入します。</summary>
    public PlayerCharacter SpawnPlayer(Vector3Int gridPosition)
    {
        Vector3 worldPosition = _gridManager.GetWorldPosition(gridPosition);

        PlayerCharacter player = Instantiate(_playerPrefab, worldPosition, Quaternion.identity);
        MovementComponent movement = player.GetComponent<MovementComponent>();
        PlayerController controller=player.GetComponent<PlayerController>();
        BlockPlacementComponent blockPlacement = player.GetComponent<BlockPlacementComponent>();
        BombComponent bombComponent = player.GetComponent<BombComponent>();

        movement.Init(_gridManager, gridPosition);
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
}
