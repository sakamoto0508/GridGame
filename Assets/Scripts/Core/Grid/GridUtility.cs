using UnityEngine;

/// <summary>
/// グリッド座標とワールド座標の変換や、グリッドの範囲判定などのユーティリティ関数を提供するクラス
/// </summary>
public static class GridUtility
{
    /// <summary>
    /// グリッド座標をワールド座標に変換する
    /// </summary>
    /// <param name="gridPosition"></param>
    /// <param name="cellSize"></param>
    /// <param name="origin"></param>
    /// <returns></returns>
    public static Vector3 GridToWorld(Vector3Int gridPosition,float cellSize,Vector3 origin)
    {
        return origin + (Vector3)gridPosition * cellSize;
    }

    /// <summary>
    /// グリッド座標をワールド座標に変換する
    /// </summary>
    /// <param name="worldPosition"></param>
    /// <param name="cellSize"></param>
    /// <param name="origin"></param>
    /// <returns></returns>
    public static Vector3Int WorldToGrid(Vector3 worldPosition,float cellSize,Vector3 origin)
    {
        Vector3 localPosition = worldPosition - origin;
        return new Vector3Int(
            Mathf.FloorToInt(localPosition.x / cellSize),
            Mathf.FloorToInt(localPosition.y / cellSize),
            Mathf.FloorToInt(localPosition.z / cellSize)
        );
    }

    /// <summary>
    /// 指定されたグリッド座標がグリッドの範囲内にあるかどうかを判定する
    /// </summary>
    /// <param name="gridPosition"></param>
    /// <param name="gridSize"></param>
    /// <returns></returns>
    public static bool IsInside(Vector3Int gridPosition, Vector3Int gridSize)
    {
        return gridPosition.x >= 0 && gridPosition.x < gridSize.x &&
               gridPosition.y >= 0 && gridPosition.y < gridSize.y &&
               gridPosition.z >= 0 && gridPosition.z < gridSize.z;
    }

    /// <summary>
    /// 六方向のベクトル配列
    /// </summary>
    public static readonly Vector3Int[] SixDirections =
    {
        new Vector3Int(1, 0, 0),   // Right
        new Vector3Int(-1, 0, 0),  // Left
        new Vector3Int(0, 1, 0),   // Up
        new Vector3Int(0, -1, 0),  // Down
        new Vector3Int(0, 0, 1),   // Forward
        new Vector3Int(0, 0, -1)   // Backward
    };
}
