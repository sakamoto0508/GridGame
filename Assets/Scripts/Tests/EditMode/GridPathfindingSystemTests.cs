#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>逃走経路（BFS）のテストです。移動時間0・行動間隔0として、到着時刻の影響を受けない形で確認します。</summary>
public class GridPathfindingSystemTests
{
    /// <summary>どの経路でも間に合う、十分先の爆発時刻（秒）です。</summary>
    private const float LateDangerSeconds = 10f;

    private GridTestUtility _world;
    private GridDangerMap _danger;

    [TearDown]
    public void TearDown() => _world?.Dispose();

    [Test]
    public void 隣が安全なら1手で逃げる()
    {
        CreateFlatWorld(new Vector3Int(5, 2, 5));
        Vector3Int start = new Vector3Int(2, 0, 2);
        _danger.RegisterDanger(start, LateDangerSeconds);

        List<Vector3Int> path = FindEscape(start);

        Assert.AreEqual(2, path.Count);
        Assert.AreEqual(start, path[0]);
        // 方向の優先順は右(+X)が最初です。
        Assert.AreEqual(start + Vector3Int.right, path[1]);
    }

    [Test]
    public void 周囲が危険なら_危険でない最寄りセルまでの経路を返す()
    {
        CreateFlatWorld(new Vector3Int(5, 2, 5));
        Vector3Int start = new Vector3Int(2, 0, 2);
        _danger.RegisterDanger(start, LateDangerSeconds);
        foreach (Vector3Int direction in new[] { Vector3Int.right, Vector3Int.left, Vector3Int.forward, Vector3Int.back })
            _danger.RegisterDanger(start + direction, LateDangerSeconds);

        List<Vector3Int> path = FindEscape(start);

        Assert.AreEqual(3, path.Count);
        Assert.IsFalse(_danger.IsDangerous(path[path.Count - 1]));
    }

    [Test]
    public void 爆発に間に合わないセルは経路に使わない()
    {
        CreateFlatWorld(new Vector3Int(5, 2, 1));
        Vector3Int start = new Vector3Int(2, 0, 0);
        _danger.RegisterDanger(start, LateDangerSeconds);

        // 右隣はすぐ爆発するため通れず、左側から逃げることになります。
        _danger.RegisterDanger(start + Vector3Int.right, 0f);

        List<Vector3Int> path = FindEscape(start);

        Assert.AreEqual(start + Vector3Int.left, path[1]);
    }

    [Test]
    public void 四方をBlockで囲まれ上にも行けなければ空の経路を返す()
    {
        // 高さ1のため、隣のBlockへジャンプして上ることもできません。
        CreateFlatWorld(new Vector3Int(3, 1, 3));
        Vector3Int start = new Vector3Int(1, 0, 1);
        _world.AddBlock(start + Vector3Int.right, BlockType.Unbreakable);
        _world.AddBlock(start + Vector3Int.left, BlockType.Unbreakable);
        _world.AddBlock(start + Vector3Int.forward, BlockType.Unbreakable);
        _world.AddBlock(start + Vector3Int.back, BlockType.Unbreakable);
        _danger.RegisterDanger(start, LateDangerSeconds);

        Assert.AreEqual(0, FindEscape(start).Count);
    }

    [Test]
    public void 段差を飛び降りる経路も見つける()
    {
        // 高い足場(y=1)の上にいて、隣は足場がなく下(y=0)の床へ降りられる配置です。
        CreateFlatWorld(new Vector3Int(3, 3, 1));
        _world.AddBlock(new Vector3Int(0, 0, 0), BlockType.Unbreakable);
        Vector3Int start = new Vector3Int(0, 1, 0);
        _danger.RegisterDanger(start, LateDangerSeconds);

        List<Vector3Int> path = FindEscape(start);

        Assert.AreEqual(new Vector3Int(1, 0, 0), path[1]);
    }

    /// <summary>y=-1に床を敷いたグリッドと、空の危険マップを用意します。</summary>
    private void CreateFlatWorld(Vector3Int size)
    {
        _world = new GridTestUtility(size);
        _world.AddFloor();
        _danger = new GridDangerMap();
    }

    private List<Vector3Int> FindEscape(Vector3Int start)
        => GridPathfindingSystem.FindPathToNearestSafeCell(_world.Grid, _danger, start);
}
#endif
