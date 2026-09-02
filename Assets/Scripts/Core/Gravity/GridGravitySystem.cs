using UnityEngine;

/// <summary>
/// グリッド上で重力を受けるオブジェクトの落下先を計算します。
/// 表示アニメーションやセル占有の更新は、各オブジェクト側が担当します。
/// </summary>
public static class GridGravitySystem
{
    /// <summary>
    /// 開始セルから真下を探索し、最初の障害物の1セル上を着地点として返します。
    /// 1セル以上落下できる場合だけtrueを返します。
    /// </summary>
    public static bool TryGetFallDestination(GridManager gridManager,Vector3Int startPosition,
        out Vector3Int destination)
    {
        destination = startPosition;

        if (gridManager == null || !gridManager.Contains(startPosition))
            return false;

        Vector3Int nextPosition = startPosition + Vector3Int.down;

        // 下のセルが空いている間、グリッド最下部まで1セルずつ探索します。
        while (gridManager.Contains(nextPosition) && gridManager.CanEnter(nextPosition))
        {
            destination = nextPosition;
            nextPosition += Vector3Int.down;
        }

        return destination != startPosition;
    }
}
