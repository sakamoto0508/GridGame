using UnityEngine;
using UnityEngine.Rendering;

public struct GridCell
{
    /// <summary> グリッド座標 </summary>
    Vector3Int GridPos;

    /// <summary> このマスに乗れるか </summary>
    bool IsWalkable;

    /// <summary> このマスにいるオブジェクト </summary>
    GameObject OccupyingObject;

    /// <summary> ボム </summary>
    //GameObject Bomb;

    /// <summary> アイテム </summary>
    //GameObject Item;
}
