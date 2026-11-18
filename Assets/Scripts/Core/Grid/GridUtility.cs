using UnityEngine;

/// <summary>
/// グリッド座標とワールド座標の変換や、グリッドの範囲判定などのユーティリティ関数を提供するクラス
/// </summary>
public static class GridUtility
{
    /// <summary>
    /// UnityではY軸が高さです。並び順は爆風の探索順にもなります（右・左・上・下・前・後ろ）。
    /// </summary>
    public static readonly Vector3Int[] SixDirections =
    {
        Vector3Int.right,
        Vector3Int.left,
        Vector3Int.up,
        Vector3Int.down,
        Vector3Int.forward,
        Vector3Int.back
    };

    /// <summary>
    /// 水平4方向です。並び順は、経路探索で同じ距離の候補が複数あるときの優先順にもなります。
    /// </summary>
    public static readonly Vector3Int[] HorizontalDirections =
    {
        Vector3Int.right,
        Vector3Int.left,
        Vector3Int.forward,
        Vector3Int.back
    };

    /// <summary>X/Z平面上の単位4方向（上下を含まない1マス分）ならtrueです。</summary>
    public static bool IsHorizontalUnit(Vector3Int direction)
        => direction.y == 0 && Mathf.Abs(direction.x) + Mathf.Abs(direction.z) == 1;

    /// <summary>XYZ各軸の差を合計したグリッド上の距離です。</summary>
    public static int ManhattanDistance(Vector3Int from, Vector3Int to)
    {
        Vector3Int difference = from - to;
        return Mathf.Abs(difference.x) + Mathf.Abs(difference.y) + Mathf.Abs(difference.z);
    }

    /// <param name="origin">グリッド座標(0,0,0)のセル中心に対応するワールド座標</param>
    public static Vector3 GridToWorld(Vector3Int gridPosition, float cellSize, Vector3 origin)
    {
        return origin + (Vector3)gridPosition * cellSize;
    }

    /// <summary>切り捨てで、ワールド座標が属するグリッド座標を求めます。</summary>
    /// <param name="origin">グリッド座標(0,0,0)のセル中心に対応するワールド座標</param>
    public static Vector3Int WorldToGrid(Vector3 worldPosition, float cellSize, Vector3 origin)
    {
        Vector3 localPosition = worldPosition - origin;

        return new Vector3Int(
            Mathf.FloorToInt(localPosition.x / cellSize),
            Mathf.FloorToInt(localPosition.y / cellSize),
            Mathf.FloorToInt(localPosition.z / cellSize)
        );
    }

    public static bool IsInside(Vector3Int gridPosition, Vector3Int gridSize)
    {
        return gridPosition.x >= 0 && gridPosition.x < gridSize.x &&
               gridPosition.y >= 0 && gridPosition.y < gridSize.y &&
               gridPosition.z >= 0 && gridPosition.z < gridSize.z;
    }
}

/// <summary>
/// 立方体（直方体）の8頂点を0～7の番号で扱うための共通定義です。
/// 頂点番号のbit0がX、bit1がY、bit2がZを表し、ビットが立っていれば正側（最大側）の頂点です。
/// </summary>
public static class CubeCorner
{
    public const int Count = 8;

    public const int BitX = 1;
    public const int BitY = 2;
    public const int BitZ = 4;

    /// <summary>12辺を1本の連続線で通る頂点順。一部の辺は同じ位置を折り返します（LineRenderer用）。</summary>
    public static readonly int[] EdgePath = { 0, 1, 3, 2, 0, 4, 5, 1, 5, 7, 3, 7, 6, 2, 6, 4 };

    public const int FaceCount = 6;
    public const int VerticesPerFace = 4;

    /// <summary>4つずつで1面分の頂点番号です。面の向き（表裏）が揃う順に並べています。</summary>
    public static readonly int[] Faces =
    {
        0, 2, 3, 1, // -Z
        4, 5, 7, 6, // +Z
        0, 4, 6, 2, // -X
        1, 3, 7, 5, // +X
        2, 6, 7, 3, // +Y（上面）
        0, 1, 5, 4  // -Y（底面）
    };

    /// <summary>指定した軸のビットが立っている（正側の頂点）ならtrueです。</summary>
    public static bool IsPositive(int corner, int axisBit) => (corner & axisBit) != 0;

    /// <summary>頂点の各軸の符号（-1か1）を返します。</summary>
    public static Vector3 Sign(int corner) => new Vector3(
        IsPositive(corner, BitX) ? 1 : -1,
        IsPositive(corner, BitY) ? 1 : -1,
        IsPositive(corner, BitZ) ? 1 : -1);
}
