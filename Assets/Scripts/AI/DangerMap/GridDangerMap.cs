using System.Collections.Generic;
using UnityEngine;

/// <summary>Bombの連鎖爆発を含め、各セルが何秒後に危険になるかを予測します。</summary>
public sealed class GridDangerMap
{
    private sealed class BombPrediction
    {
        public Vector3Int Position;
        public float ExplosionTime;
        public IReadOnlyList<Vector3Int> AffectedCells;
    }

    private readonly Dictionary<Vector3Int, float> _dangerTimes = new();
    public IReadOnlyDictionary<Vector3Int, float> DangerTimes => _dangerTimes;

    public void Rebuild(GridManager gridManager) => Rebuild(gridManager, null, 0, 0f);

    /// <summary>未設置のBombを加え、設置後に逃げられるか判断するMapを作ります。</summary>
    public void RebuildWithVirtualBomb(
        GridManager gridManager, Vector3Int position, int power, float fuseTime)
        => Rebuild(gridManager, position, power, fuseTime);

    public bool IsDangerous(Vector3Int position) => _dangerTimes.ContainsKey(position);

    /// <summary>
    /// 指定したセルが何秒後に危険になるかを取得します。
    /// </summary>
    /// <param name="position"></param>
    /// <param name="seconds"></param>
    /// <returns></returns>
    public bool TryGetDangerTime(Vector3Int position, out float seconds)
        => _dangerTimes.TryGetValue(position, out seconds);

    /// <summary>
    /// Bombの爆発と連鎖爆発を含め、各セルが何秒後に危険になるかを予測します。
    /// </summary>
    /// <param name="gridManager"></param>
    /// <param name="virtualPosition"></param>
    /// <param name="virtualPower"></param>
    /// <param name="virtualFuseTime"></param>
    private void Rebuild(
        GridManager gridManager, Vector3Int? virtualPosition, int virtualPower, float virtualFuseTime)
    {
        _dangerTimes.Clear();
        if (gridManager == null)
            return;

        List<BombPrediction> bombs = CollectBombs(gridManager);
        if (virtualPosition.HasValue && gridManager.Contains(virtualPosition.Value))
        {
            bombs.Add(CreatePrediction(gridManager, virtualPosition.Value,
                Mathf.Max(1, virtualPower), Mathf.Max(0f, virtualFuseTime)));
        }

        PropagateChainTimes(bombs);
        foreach (BombPrediction bomb in bombs)
            RegisterDangerCells(bomb);
    }

    /// <summary>
    /// GridManagerからBombを収集し、爆発予測を作ります。
    /// </summary>
    /// <param name="gridManager"></param>
    /// <returns></returns>
    private static List<BombPrediction> CollectBombs(GridManager gridManager)
    {
        List<BombPrediction> bombs = new();
        Vector3Int size = gridManager.Size;
        for (int x = 0; x < size.x; x++)
        for (int y = 0; y < size.y; y++)
        for (int z = 0; z < size.z; z++)
        {
            Bomb bomb = gridManager.GetBomb(new Vector3Int(x, y, z));
            if (bomb != null && bomb.State != BombState.Exploded)
            {
                bombs.Add(CreatePrediction(gridManager, bomb.GridPosition,
                    bomb.ExplosionPower, bomb.RemainingFuseTime));
            }
        }
        return bombs;
    }

    /// <summary>
    /// 指定した位置のBombの爆発予測を作ります。
    /// </summary>
    /// <param name="gridManager"></param>
    /// <param name="position"></param>
    /// <param name="power"></param>
    /// <param name="time"></param>
    /// <returns></returns>
    private static BombPrediction CreatePrediction(
        GridManager gridManager, Vector3Int position, int power, float time)
    {
        return new BombPrediction
        {
            Position = position,
            ExplosionTime = time,
            AffectedCells = ExplosionSystem.CalculateAffectedCells(gridManager, position, power)
        };
    }

    /// <summary>爆風が別のBombへ届く関係を、変化がなくなるまで時刻へ反映します。</summary>
    private static void PropagateChainTimes(List<BombPrediction> bombs)
    {
        bool changed;
        do
        {
            changed = false;
            for (int sourceIndex = 0; sourceIndex < bombs.Count; sourceIndex++)
            for (int targetIndex = 0; targetIndex < bombs.Count; targetIndex++)
            {
                if (sourceIndex == targetIndex)
                    continue;

                BombPrediction source = bombs[sourceIndex];
                BombPrediction target = bombs[targetIndex];
                if (source.ExplosionTime < target.ExplosionTime &&
                    Contains(source.AffectedCells, target.Position))
                {
                    target.ExplosionTime = source.ExplosionTime;
                    changed = true;
                }
            }
        } while (changed);
    }

    /// <summary>
    /// Bombの爆風が届くセルを危険として登録します。
    /// </summary>
    /// <param name="bomb"></param>
    private void RegisterDangerCells(BombPrediction bomb)
    {
        foreach (Vector3Int position in bomb.AffectedCells)
        {
            if (!_dangerTimes.TryGetValue(position, out float oldTime) ||
                bomb.ExplosionTime < oldTime)
                _dangerTimes[position] = bomb.ExplosionTime;
        }
    }

    /// <summary>
    /// 指定したセルが、指定したセルのリストに含まれるかを判定します。
    /// </summary>
    /// <param name="cells"></param>
    /// <param name="position"></param>
    /// <returns></returns>
    private static bool Contains(IReadOnlyList<Vector3Int> cells, Vector3Int position)
    {
        for (int i = 0; i < cells.Count; i++)
            if (cells[i] == position)
                return true;
        return false;
    }
}
