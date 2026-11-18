using UnityEngine;

/// <summary>
/// グリッド上で重力を受けるオブジェクトの落下先を計算します。
/// 表示アニメーションやセル占有の更新は、各オブジェクト側が担当します。
/// </summary>
public static class GridGravitySystem
{
    /// <summary>
    /// 水平へ1セル踏み出した後、足場がなければ着地するセルまで下方向を調べます。
    /// Character、Bomb、Blockなどで塞がれた列には降りません。
    /// </summary>
    public static bool TryGetStepAndFallDestination(
        GridManager gridManager,
        Vector3Int currentPosition,
        Vector3Int direction,
        out Vector3Int edgePosition,
        out Vector3Int landingPosition)
    {
        edgePosition = currentPosition + direction;
        landingPosition = edgePosition;

        if (gridManager == null || !gridManager.CanCharacterEnter(edgePosition))
            return false;

        Vector3Int cursor = edgePosition;
        while (gridManager.Contains(cursor))
        {
            Vector3Int below = cursor + Vector3Int.down;
            // Y=-1の外殻床も足場なので、範囲外チェックより先にBlockを確認します。
            if (gridManager.HasBlock(below))
            {
                landingPosition = cursor;

                // 踏み出した先に足場がある（落下しない）場合は通常移動なのでfalseにします。
                return landingPosition.y < edgePosition.y;
            }

            if (!gridManager.Contains(below))
                return false;

            if (!gridManager.CanCharacterEnter(below))
                return false;

            cursor = below;
        }

        return false;
    }

    /// <summary>
    /// Block専用の配置条件を使って、真下の着地可能セルを返します。
    /// Characterは落下を妨げず、通過時にBlock側で押し潰し判定を行います。
    /// </summary>
    public static bool TryGetBlockFallDestination(
        GridManager gridManager, Vector3Int startPosition, out Vector3Int destination)
        => TryGetDropDestination(gridManager, startPosition, (grid, p) => grid.CanFallingBlockEnter(p), out destination);

    /// <summary>Characterの落下先です。Bombのあるセルにも落ちられます（GridCell.IsWalkable）。</summary>
    public static bool TryGetFallDestination(
        GridManager gridManager, Vector3Int startPosition, out Vector3Int destination)
        => TryGetDropDestination(gridManager, startPosition, (grid, p) => grid.CanEnter(p), out destination);

    /// <summary>
    /// Bomb専用の配置条件を使って、真下の着地可能セルを返します。
    /// BombはCharacterと同じセルへ存在できますが、Blockや別Bombは通過できません。
    /// </summary>
    public static bool TryGetBombFallDestination(
        GridManager gridManager, Vector3Int startPosition, out Vector3Int destination)
        => TryGetDropDestination(gridManager, startPosition, (grid, p) => grid.CanPlaceBomb(p), out destination);

    /// <summary>
    /// 開始セルから真下へ、canEnterを満たす間だけ1セルずつ進み、最後に入れたセルを着地点とします。
    /// 1セル以上落下できる場合だけtrueを返します。グリッド外は各判定がfalseを返すため、そこで止まります。
    /// </summary>
    private static bool TryGetDropDestination(
        GridManager gridManager,
        Vector3Int startPosition,
        System.Func<GridManager, Vector3Int, bool> canEnter,
        out Vector3Int destination)
    {
        destination = startPosition;

        if (gridManager == null || !gridManager.Contains(startPosition))
            return false;

        Vector3Int nextPosition = startPosition + Vector3Int.down;

        while (canEnter(gridManager, nextPosition))
        {
            destination = nextPosition;
            nextPosition += Vector3Int.down;
        }

        return destination != startPosition;
    }
}
