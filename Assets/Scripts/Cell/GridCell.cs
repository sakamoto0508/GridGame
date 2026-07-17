using UnityEngine;

public class GridCell
{
    public GridCell(Vector3Int gridPos, bool isWalkable)
    {
        GridPos = gridPos;
        IsWalkable = isWalkable;
    }

    /// <summary> グリッド座標 </summary>
    public Vector3Int GridPos { get; private set; }

    /// <summary> このマスに乗れるか </summary>
    public bool IsWalkable { get; private set; }

    /// <summary> このマスにいるオブジェクト </summary>
    public GameObject OccupyingObject { get; private set; }

    /// <summary> ボム </summary>
    //GameObject Bomb;

    /// <summary> アイテム </summary>
    //GameObject Item;

    public void SetWalkable(bool isWalkable)
    {
        IsWalkable = isWalkable;
    }

    public void SetOccupyingObject(GameObject obj)
    {
        OccupyingObject = obj;
    }

    public void ResetCell()
    {
        OccupyingObject = null;
    }
}
