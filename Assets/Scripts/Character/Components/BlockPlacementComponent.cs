using UnityEngine;

/// <summary>
/// キャラクターによるBlock配置を管理します。
/// 地上では正面、ジャンプ中では直下のセルを配置対象にします。
/// </summary>
[RequireComponent(typeof(MovementComponent))]
public class BlockPlacementComponent : MonoBehaviour
{
    private GridManager _gridManager;
    private MovementComponent _movement;
    private Block _blockPrefab;

    private void Awake()
    {
        _movement = GetComponent<MovementComponent>();
    }

    /// <summary>Spawnerからグリッドと配置用Block Prefabを受け取ります。</summary>
    public void Init(GridManager gridManager, Block blockPrefab)
    {
        _gridManager = gridManager;
        _blockPrefab = blockPrefab;

        if (_gridManager == null)
            Debug.LogError("Block配置の初期化に失敗しました: GridManagerが未設定です。", this);

        if (_blockPrefab == null)
        {
            Debug.LogError(
                "Block配置の初期化に失敗しました: CharacterSpawnerのPlaceable Block Prefabが未設定です。",
                this);
        }
    }

    /// <summary>現在の移動状態に応じたセルへBlockの配置を試みます。</summary>
    public bool TryPlaceBlock()
    {
        if (_gridManager == null)
        {
            Debug.LogWarning("Blockを配置できません: GridManagerが初期化されていません。", this);
            return false;
        }

        if (_movement == null)
        {
            Debug.LogWarning("Blockを配置できません: MovementComponentがありません。", this);
            return false;
        }

        if (_blockPrefab == null)
        {
            Debug.LogWarning(
                "Blockを配置できません: 配置用Block Prefabが設定されていません。CharacterSpawnerを確認してください。",
                this);
            return false;
        }

        Vector3Int targetPosition;

        if (_movement.State == CharacterMoveState.Jumping)
        {
            // その場ジャンプ中はPlayer直下の空いたセルへ配置します。
            targetPosition = _movement.CurrentGridPosition + Vector3Int.down;
        }
        else if (_movement.State == CharacterMoveState.Grounded)
        {
            // 地上では最後に向いていた方向の隣接セルへ配置します。
            targetPosition =
                _movement.CurrentGridPosition + _movement.FacingDirection;
        }
        else
        {
            Debug.LogWarning(
                $"Blockを配置できません: 現在の移動状態は {_movement.State} です。GroundedまたはJumpingのときだけ配置できます。",
                this);
            return false;
        }

        if (!_gridManager.CanPlaceBlock(targetPosition, out string failureReason))
        {
            Debug.LogWarning(
                $"Blockを配置できません: {failureReason} " +
                $"現在位置={_movement.CurrentGridPosition}, 向き={_movement.FacingDirection}, 状態={_movement.State}",
                this);
            return false;
        }

        Block block = Instantiate(
            _blockPrefab,
            _gridManager.GetWorldPosition(targetPosition),
            Quaternion.identity);

        if (!_gridManager.TryRegisterBlock(targetPosition, block))
        {
            Destroy(block.gameObject);
            Debug.LogWarning(
                $"Blockを生成しましたが、セル {targetPosition} への登録に失敗したため破棄しました。",
                this);
            return false;
        }

        block.Initialize(_gridManager, targetPosition);
        return true;
    }
}
