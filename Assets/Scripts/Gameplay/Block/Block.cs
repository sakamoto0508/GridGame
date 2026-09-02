using UnityEngine;

/// <summary>
/// 破壊可能かどうかを示すブロックの種類
/// </summary>
public enum BlockType
{
    Breakable,
    Unbreakable
}

/// <summary>
/// グリッド上のブロックを表すクラス
/// </summary>
public class Block : MonoBehaviour
{
    [SerializeField] private BlockType _blockType;
    [SerializeField] private float _fallDuration = 0.5f;
    private GridManager _gridManager;
    private bool _isFalling;

    public BlockType Type => _blockType;
    public Vector3Int GridPosition { get; private set; }

    /// <summary>生成されたBlockへ論理グリッド座標を設定します。</summary>
    public void Initialize(GridManager gridManager, Vector3Int gridPosition)
    {
        _gridManager = gridManager;
        GridPosition = gridPosition;

        TryStartFall();
    }

    private bool TryStartFall()
    {
        if (_isFalling)
            return false;

        if (!GridGravitySystem.TryGetFallDestination(
            _gridManager,
            GridPosition,
            out Vector3Int destination))
        {
            return false;
        }

        Vector3Int startPosition = GridPosition;

        if (!_gridManager.TryMoveBlock(
            startPosition,
            destination,
            this))
        {
            return false;
        }

        GridPosition = destination;

        _ = FallAwaitable(
            _gridManager.GetWorldPosition(destination));

        return true;
    }

    private async Awaitable FallAwaitable(Vector3 targetWorldPosition)
    {
        _isFalling = true;
        float elapsedTime = 0f;
        Vector3 startWorldPosition = transform.position;
        while (elapsedTime < _fallDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / _fallDuration);
            transform.position = Vector3.Lerp(startWorldPosition, targetWorldPosition, t);
            await Awaitable.NextFrameAsync();
        }
        transform.position = targetWorldPosition;
        _isFalling = false;
        // 落下後に再度落下可能か確認
        TryStartFall();
    }
}
