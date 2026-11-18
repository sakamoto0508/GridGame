#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEngine;

public class GridUtilityTests
{
    [Test]
    public void GridToWorld_セル幅と原点を反映する()
    {
        Vector3 world = GridUtility.GridToWorld(new Vector3Int(1, 2, 3), 2f, new Vector3(10, 0, 0));

        Assert.AreEqual(new Vector3(12, 4, 6), world);
    }

    [Test]
    public void WorldToGrid_GridToWorldの逆変換になる()
    {
        Vector3Int cell = new Vector3Int(3, 0, 5);
        Vector3 origin = new Vector3(-1.5f, 0f, 2f);

        Vector3 world = GridUtility.GridToWorld(cell, 1.5f, origin);

        Assert.AreEqual(cell, GridUtility.WorldToGrid(world, 1.5f, origin));
    }

    [Test]
    public void WorldToGrid_負の座標は切り捨てで負のセルになる()
    {
        Assert.AreEqual(new Vector3Int(-1, 0, 0), GridUtility.WorldToGrid(new Vector3(-0.1f, 0f, 0f), 1f, Vector3.zero));
    }

    [Test]
    public void IsInside_範囲の端を正しく判定する()
    {
        Vector3Int size = new Vector3Int(3, 3, 3);

        Assert.IsTrue(GridUtility.IsInside(Vector3Int.zero, size));
        Assert.IsTrue(GridUtility.IsInside(new Vector3Int(2, 2, 2), size));
        Assert.IsFalse(GridUtility.IsInside(new Vector3Int(3, 0, 0), size));
        Assert.IsFalse(GridUtility.IsInside(new Vector3Int(0, -1, 0), size));
    }
}
#endif
