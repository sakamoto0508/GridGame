using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// グリッド上の水平移動と、隣接する1段高いBlockへのジャンプを含む経路を
/// 幅優先探索で計算します。
/// </summary>
public static class GridPathfindingSystem
{
    private static readonly Vector3Int[] HorizontalDirections =
    {
        Vector3Int.right,
        Vector3Int.left,
        Vector3Int.forward,
        Vector3Int.back
    };

    /// <summary>
    /// startから最も近い安全セルまでの経路を返します。
    /// 戻り値にはstartを含め、到達不能なら空のリストを返します。
    /// </summary>
    public static List<Vector3Int> FindPathToNearestSafeCell(
        GridManager gridManager,
        GridDangerMap dangerMap,
        Vector3Int start,
        float moveDuration = 0f,
        float jumpDuration = 0f,
        float actionInterval = 0f)
    {
        List<Vector3Int> emptyPath = new List<Vector3Int>();

        if (gridManager == null || dangerMap == null || !gridManager.Contains(start))
            return emptyPath;

        Queue<Vector3Int> open = new Queue<Vector3Int>();
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();
        Dictionary<Vector3Int, Vector3Int> previous =
            new Dictionary<Vector3Int, Vector3Int>();
        Dictionary<Vector3Int, float> arrivalTimes = new Dictionary<Vector3Int, float>();

        open.Enqueue(start);
        visited.Add(start);
        arrivalTimes[start] = 0f;

        while (open.Count > 0)
        {
            Vector3Int current = open.Dequeue();

            if (current != start && !dangerMap.IsDangerous(current))
                return BuildPath(previous, start, current);

            for (int i = 0; i < HorizontalDirections.Length; i++)
            {
                Vector3Int direction = HorizontalDirections[i];
                Vector3Int next = current + direction;
                float currentArrival = arrivalTimes[current];

                // まず同じ高さへの通常移動を候補にします。
                float moveArrival = currentArrival + Mathf.Max(moveDuration, actionInterval);
                if (CanStandAt(gridManager, next) && CanArriveBeforeExplosion(
                        dangerMap, next, moveArrival, actionInterval))
                {
                    TryEnqueue(next, current, moveArrival, visited, previous, arrivalTimes, open);
                }

                // 方向先のBlockが1段高く、その上が空いていればジャンプ先も候補にします。
                if (gridManager.CanJumpUp(current, direction, out Vector3Int jumpLanding) &&
                    gridManager.CanCharacterEnter(jumpLanding))
                {
                    float jumpArrival = currentArrival + Mathf.Max(jumpDuration, actionInterval);
                    if (CanArriveBeforeExplosion(
                            dangerMap, jumpLanding, jumpArrival, actionInterval))
                    {
                        TryEnqueue(jumpLanding, current, jumpArrival,
                            visited, previous, arrivalTimes, open);
                    }
                }
            }
        }

        return emptyPath;
    }

    /// <summary>Characterが進入でき、直下に足場があるセルだけを探索対象にします。</summary>
    private static bool CanStandAt(GridManager gridManager, Vector3Int position)
    {
        if (!gridManager.CanCharacterEnter(position))
            return false;

        Vector3Int below = position + Vector3Int.down;
        return gridManager.Contains(below) && gridManager.HasBlock(below);
    }

    /// <summary>未探索セルだけを探索待ちキューへ追加し、直前のセルを記録します。</summary>
    private static void TryEnqueue(
        Vector3Int next,
        Vector3Int current,
        float arrivalTime,
        HashSet<Vector3Int> visited,
        Dictionary<Vector3Int, Vector3Int> previous,
        Dictionary<Vector3Int, float> arrivalTimes,
        Queue<Vector3Int> open)
    {
        if (!visited.Add(next))
            return;

        previous[next] = current;
        arrivalTimes[next] = arrivalTime;
        open.Enqueue(next);
    }

    /// <summary>到着後、次の思考まで滞在しても爆発前に移動できるかを判定します。</summary>
    private static bool CanArriveBeforeExplosion(
        GridDangerMap dangerMap, Vector3Int position, float arrivalTime, float actionInterval)
    {
        if (!dangerMap.TryGetDangerTime(position, out float dangerTime))
            return true;

        const float safetyMargin = 0.05f;
        return arrivalTime + actionInterval + safetyMargin < dangerTime;
    }

    /// <summary>終点から親セルをたどり、startからgoalまでの順序へ戻します。</summary>
    private static List<Vector3Int> BuildPath(
        Dictionary<Vector3Int, Vector3Int> previous,
        Vector3Int start,
        Vector3Int goal)
    {
        List<Vector3Int> path = new List<Vector3Int> { goal };
        Vector3Int current = goal;

        while (current != start)
        {
            current = previous[current];
            path.Add(current);
        }

        path.Reverse();
        return path;
    }
}
