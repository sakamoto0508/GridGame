#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class ExplosionSystemTests
{
    private static readonly Vector3Int Origin = new Vector3Int(2, 2, 2);

    private GridTestUtility _world;

    [SetUp]
    public void SetUp() => _world = new GridTestUtility(new Vector3Int(5, 5, 5));

    [TearDown]
    public void TearDown() => _world.Dispose();

    [Test]
    public void 障害物がなければ中心と6方向へ届く()
    {
        IReadOnlyList<ExplosionCellData> cells = Calculate(1);

        Assert.AreEqual(7, cells.Count);
        Assert.AreEqual(ExplosionCellType.Center, Find(cells, Origin).Type);
        Assert.AreEqual(ExplosionCellType.End, Find(cells, Origin + Vector3Int.right).Type);
        Assert.AreEqual(ExplosionCellType.End, Find(cells, Origin + Vector3Int.up).Type);
    }

    [Test]
    public void グリッド端で止まり_端のセルはEndになる()
    {
        IReadOnlyList<ExplosionCellData> cells = Calculate(3);

        Assert.AreEqual(ExplosionCellType.Middle, Find(cells, new Vector3Int(3, 2, 2)).Type);
        Assert.AreEqual(ExplosionCellType.End, Find(cells, new Vector3Int(4, 2, 2)).Type);
        Assert.IsFalse(Contains(cells, new Vector3Int(5, 2, 2)));
    }

    [Test]
    public void 破壊可能Blockのセルまで届き_その先には進まない()
    {
        _world.AddBlock(new Vector3Int(3, 2, 2), BlockType.Breakable);

        IReadOnlyList<ExplosionCellData> cells = Calculate(3);

        Assert.AreEqual(ExplosionCellType.BlockedEnd, Find(cells, new Vector3Int(3, 2, 2)).Type);
        Assert.IsFalse(Contains(cells, new Vector3Int(4, 2, 2)));
    }

    [Test]
    public void 破壊不能Blockの手前で止まり_手前のセルがBlockedEndになる()
    {
        _world.AddBlock(new Vector3Int(4, 2, 2), BlockType.Unbreakable);

        IReadOnlyList<ExplosionCellData> cells = Calculate(3);

        Assert.AreEqual(ExplosionCellType.BlockedEnd, Find(cells, new Vector3Int(3, 2, 2)).Type);
        Assert.IsFalse(Contains(cells, new Vector3Int(4, 2, 2)));
    }

    [Test]
    public void 隣が破壊不能Blockなら_その方向には広がらない()
    {
        _world.AddBlock(new Vector3Int(3, 2, 2), BlockType.Unbreakable);

        IReadOnlyList<ExplosionCellData> cells = Calculate(3);

        Assert.IsFalse(cells.Any(cell => cell.Direction == Vector3Int.right));
    }

    [Test]
    public void 範囲外の起点では何も返さない()
    {
        // 範囲外ではWarningが出ますが、Warningはテストの失敗扱いにはなりません。
        IReadOnlyList<ExplosionCellData> cells =
            ExplosionSystem.CalculateExplosionCells(_world.Grid, new Vector3Int(-1, 0, 0), 2);

        Assert.AreEqual(0, cells.Count);
    }

    private IReadOnlyList<ExplosionCellData> Calculate(int power)
        => ExplosionSystem.CalculateExplosionCells(_world.Grid, Origin, power);

    private static ExplosionCellData Find(IReadOnlyList<ExplosionCellData> cells, Vector3Int position)
    {
        Assert.IsTrue(Contains(cells, position), $"{position} が爆風に含まれていません。");
        return cells.First(cell => cell.Position == position);
    }

    private static bool Contains(IReadOnlyList<ExplosionCellData> cells, Vector3Int position)
        => cells.Any(cell => cell.Position == position);
}
#endif
