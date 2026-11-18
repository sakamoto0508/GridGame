using System.Collections.Generic;
using UnityEngine;

/// <summary>A*がEnemyへ返す1手の種類です。</summary>
public enum GridPathActionType
{
    Move,
    JumpUp,
    MoveAndFall,
    PlaceBlockAndJump
}

/// <summary>移動先だけでなく、そのセルへ到達するための行動も保持します。</summary>
public readonly struct GridPathStep
{
    public Vector3Int Position { get; }
    public Vector3Int Direction { get; }
    public GridPathActionType Action { get; }

    public GridPathStep(Vector3Int position, Vector3Int direction, GridPathActionType action)
    {
        Position = position;
        Direction = direction;
        Action = action;
    }
}

/// <summary>
/// Enemyの逃走経路は幅優先探索（BFS）、攻撃位置まではA*、Item取得はDijkstra法で計算します。
///
/// Escape（BFS）で探索できる行動:
/// ・同じ高さにある水平4方向への通常移動
/// ・水平に隣接する1段高いBlock上へのジャンプ
/// ・足場のない方向へ踏み出し、下の最初の足場まで飛び降りる移動
///
/// Escapeで現在探索しない行動:
/// ・斜め移動
/// ・探索途中でのBlock設置
/// ・Bomb設置
///
/// </summary>
public static class GridPathfindingSystem
{
    /// <summary>A*で展開するノード数の上限。経路がない場合でも探索が長引かないようにします。</summary>
    private const int MaxAttackExpandedNodes = 5000;

    /// <summary>A*のヒューリスティックで使う1手あたりの最小コスト（秒）。移動時間0でも距離を評価できるようにします。</summary>
    private const float MinimumMoveCostSeconds = 0.01f;

    /// <summary>爆発時刻に対する安全マージン（秒）。ぎりぎりの到着を危険とみなします。</summary>
    private const float ExplosionSafetyMargin = 0.05f;

    // 探索ノードと共通データ
    private sealed class AttackNode
    {
        public Vector3Int Position;
        public Vector3Int? VirtualBlock;
        public float Cost;
        public float Score;
        public float ArrivalTime;
        public AttackNode Parent;
        public GridPathStep Step;
    }

    // ===== Item: Dijkstra =====
    /// <summary>
    /// 歩行・段差ジャンプ・降下だけを使用し、地形を変更しません。
    /// 攻撃/逃走と違い、爆発前に通過できても予測危険セルへは寄り道しない方針です。
    /// </summary>
    public static List<GridPathStep> FindSafePathToItem(
        GridManager grid, GridDangerMap danger, Vector3Int start, Vector3Int goal,
        float moveDuration, float jumpDuration, float fallDuration, float interval,
        out float totalTime, PathSearchTrace trace = null)
    {
        totalTime = float.PositiveInfinity;
        var emptyPath = new List<GridPathStep>();
        trace?.BeginSearch(start, goal);

        if (grid == null || danger == null ||
            !grid.Contains(start) || !grid.Contains(goal) ||
            danger.IsDangerous(start) || danger.IsDangerous(goal))
        {
            trace?.CompleteWithoutPath();
            return emptyPath;
        }

        var open = new List<AttackNode>
        {
            new AttackNode { Position = start }
        };
        var costs = new Dictionary<Vector3Int, float> { [start] = 0f };

        while (open.Count > 0)
        {
            // 累積コストが最小のノードを取り出します（線形探索）。
            int best = 0;
            for (int i = 1; i < open.Count; i++)
            {
                if (open[i].Cost < open[best].Cost)
                    best = i;
            }

            AttackNode node = open[best];
            open.RemoveAt(best);

            if (node.Cost > costs[node.Position])
                continue;

            // 古いコストのまま残った重複エントリは上で読み飛ばすため、ここからが実際の展開です。
            trace?.RecordExpanded(
                node.Position,
                node.Parent?.Position,
                node.Cost,
                0f,
                node.Parent != null ? node.Step.Action : (GridPathActionType?)null);

            if (node.Position == goal)
            {
                totalTime = node.Cost;
                List<GridPathStep> itemPath = BuildAttackPath(node);
                trace?.CompleteWithSteps(itemPath, node.Cost);
                return itemPath;
            }

            foreach (Vector3Int direction in GridUtility.HorizontalDirections)
            {
                TryAdd(new GridPathStep(node.Position + direction, direction, GridPathActionType.Move));

                TryAdd(new GridPathStep(
                    node.Position + direction + Vector3Int.up,
                    direction,
                    GridPathActionType.JumpUp));

                if (GridGravitySystem.TryGetStepAndFallDestination(
                        grid, node.Position, direction, out _, out Vector3Int landing))
                {
                    TryAdd(new GridPathStep(landing, direction, GridPathActionType.MoveAndFall));
                }
            }

            void TryAdd(GridPathStep step)
            {
                if (!IsSafeItemStep(grid, danger, node.Position, step, moveDuration, jumpDuration,
                        fallDuration, interval, node.Cost, out float duration))
                    return;

                float cost = node.Cost + duration;
                if (costs.TryGetValue(step.Position, out float old) && old <= cost)
                    return;

                costs[step.Position] = cost;
                open.Add(new AttackNode
                {
                    Position = step.Position,
                    Cost = cost,
                    Parent = node,
                    Step = step
                });
            }
        }

        trace?.CompleteWithoutPath();
        return emptyPath;
    }

    /// <summary>保持した取得経路の再検証にも同じ判定を使い、探索と実行の条件を揃えます。</summary>
    public static bool IsSafeItemStep(
        GridManager grid,
        GridDangerMap danger,
        Vector3Int from,
        GridPathStep step,
        float moveDuration,
        float jumpDuration,
        float fallDuration,
        float interval,
        float elapsed,
        out float duration)
    {
        duration = Mathf.Max(moveDuration, interval);

        if (danger.IsDangerous(from) || danger.IsDangerous(step.Position))
            return false;

        Vector3Int direction = step.Direction;
        if (!GridUtility.IsHorizontalUnit(direction))
            return false;

        switch (step.Action)
        {
            case GridPathActionType.Move:
                if (step.Position != from + direction || !CanStandAt(grid, step.Position))
                    return false;
                break;

            case GridPathActionType.JumpUp:
                duration = Mathf.Max(jumpDuration, interval);

                // 着地点が一致し、着地点と真上（跳躍中に通るセル）が空いていて安全であること
                if (!grid.CanJumpUp(from, direction, out Vector3Int up) ||
                    up != step.Position ||
                    !grid.CanCharacterEnter(up) ||
                    !grid.CanCharacterEnter(from + Vector3Int.up) ||
                    danger.IsDangerous(from + Vector3Int.up))
                    return false;
                break;

            case GridPathActionType.MoveAndFall:
                if (!GridGravitySystem.TryGetStepAndFallDestination(
                        grid, from, direction, out Vector3Int edge, out Vector3Int down) ||
                    down != step.Position)
                    return false;

                duration += fallDuration * (edge.y - down.y);

                // 落下中に通過する列がすべて安全であること
                for (int y = edge.y; y >= down.y; y--)
                {
                    if (danger.IsDangerous(new Vector3Int(edge.x, y, edge.z)))
                        return false;
                }
                break;

            default:
                return false;
        }

        return CanArriveBeforeExplosion(danger, step.Position, elapsed + duration, interval);
    }

    // ===== Attack: A* =====
    /// <summary>
    /// PlayerへBombの爆風を届かせられるセルまで、A*で行動列を探索します。
    /// 通常移動、既存Blockへのジャンプ、経路中1回までのBlock設置後ジャンプを扱います。
    /// excludedGoalを指定した場合、そのセルは攻撃位置（ゴール）として選びません。
    /// </summary>
    public static List<GridPathStep> FindPathToAttackPosition(
        GridManager gridManager,
        GridDangerMap dangerMap,
        Vector3Int start,
        Vector3Int playerPosition,
        int explosionPower,
        int bombDistance,
        float moveDuration,
        float jumpDuration,
        float fallDurationPerCell,
        float actionInterval,
        Vector3Int? excludedGoal = null,
        PathSearchTrace trace = null)
    {
        List<GridPathStep> empty = new List<GridPathStep>();
        trace?.BeginSearch(start, playerPosition);
        if (gridManager == null || dangerMap == null || !gridManager.Contains(start))
        {
            trace?.CompleteWithoutPath();
            return empty;
        }

        List<AttackNode> open = new List<AttackNode>();
        Dictionary<string, float> bestCosts = new Dictionary<string, float>();
        AttackNode startNode = new AttackNode
        {
            Position = start,
            Cost = 0f,
            Score = AttackHeuristic(start, playerPosition, moveDuration),
            ArrivalTime = 0f
        };
        open.Add(startNode);
        bestCosts[AttackKey(start, null)] = 0f;

        int expandedNodeCount = 0;

        while (open.Count > 0 && expandedNodeCount < MaxAttackExpandedNodes)
        {
            expandedNodeCount++;

            // 評価値(f = g + h)が最小のノードを取り出します（線形探索）。
            int bestIndex = 0;
            for (int i = 1; i < open.Count; i++)
            {
                if (open[i].Score < open[bestIndex].Score)
                    bestIndex = i;
            }

            AttackNode current = open[bestIndex];
            open.RemoveAt(bestIndex);

            trace?.RecordExpanded(
                current.Position,
                current.Parent?.Position,
                current.Cost,
                current.Score - current.Cost,
                current.Parent != null ? current.Step.Action : (GridPathActionType?)null,
                current.VirtualBlock);

            // excludedGoalは「到達できてもBombを置けない（逃げ道がない）」と判明した攻撃位置です。
            // 通過は許可し、ゴールとしてだけ除外します。
            if (current.Position != start &&
                (!excludedGoal.HasValue || current.Position != excludedGoal.Value) &&
                !dangerMap.IsDangerous(current.Position) &&
                GridUtility.ManhattanDistance(current.Position, playerPosition) <= bombDistance &&
                ExplosionSystem.CanReach(gridManager, current.Position, playerPosition, explosionPower))
            {
                List<GridPathStep> attackPath = BuildAttackPath(current);
                trace?.CompleteWithSteps(attackPath, current.Cost);
                return attackPath;
            }

            for (int i = 0; i < GridUtility.HorizontalDirections.Length; i++)
            {
                Vector3Int direction = GridUtility.HorizontalDirections[i];
                Vector3Int sameLevel = current.Position + direction;
                float moveCost = Mathf.Max(moveDuration, actionInterval);

                if (CanStandAt(gridManager, sameLevel, current.VirtualBlock))
                    AddAttackNode(open, bestCosts, dangerMap, current, sameLevel,
                        current.VirtualBlock, direction, GridPathActionType.Move,
                        moveCost, actionInterval, playerPosition, moveDuration);

                // 水平先に足場がない場合、下にある最初の足場まで降りる経路も候補にします。
                if (GridGravitySystem.TryGetStepAndFallDestination(
                        gridManager, current.Position, direction,
                        out Vector3Int edge, out Vector3Int fallLanding))
                {
                    int fallDistance = edge.y - fallLanding.y;
                    float fallCost = moveCost +
                                     fallDurationPerCell * Mathf.Max(1, fallDistance);
                    if (CanFallBeforeExplosion(
                            dangerMap, edge, fallLanding,
                            current.ArrivalTime + fallCost, actionInterval))
                    {
                        AddAttackNode(open, bestCosts, dangerMap, current, fallLanding,
                            current.VirtualBlock, direction, GridPathActionType.MoveAndFall,
                            fallCost, actionInterval, playerPosition, moveDuration);
                    }
                }

                Vector3Int blockPosition = current.Position + direction;
                Vector3Int landing = blockPosition + Vector3Int.up;
                bool hasBlock = gridManager.HasBlock(blockPosition) ||
                                current.VirtualBlock == blockPosition;

                if (hasBlock && gridManager.CanCharacterEnter(landing))
                {
                    AddAttackNode(open, bestCosts, dangerMap, current, landing,
                        current.VirtualBlock, direction, GridPathActionType.JumpUp,
                        Mathf.Max(jumpDuration, actionInterval), actionInterval,
                        playerPosition, moveDuration);
                }

                // 探索量と盤面の過剰変更を抑えるため、仮想Blockは経路中1個までです。
                if (!current.VirtualBlock.HasValue &&
                    gridManager.CanPlaceBlock(blockPosition) &&
                    gridManager.HasBlock(blockPosition + Vector3Int.down) &&
                    gridManager.CanCharacterEnter(landing))
                {
                    float buildAndJumpCost = actionInterval +
                                             Mathf.Max(jumpDuration, actionInterval);
                    AddAttackNode(open, bestCosts, dangerMap, current, landing,
                        blockPosition, direction, GridPathActionType.PlaceBlockAndJump,
                        buildAndJumpCost, actionInterval, playerPosition, moveDuration);
                }
            }
        }

        trace?.CompleteWithoutPath();
        return empty;
    }

    private static void AddAttackNode(
        List<AttackNode> open,
        Dictionary<string, float> bestCosts,
        GridDangerMap dangerMap,
        AttackNode parent,
        Vector3Int position,
        Vector3Int? virtualBlock,
        Vector3Int direction,
        GridPathActionType action,
        float stepCost,
        float actionInterval,
        Vector3Int playerPosition,
        float moveDuration)
    {
        float arrival = parent.ArrivalTime + stepCost;
        if (!CanArriveBeforeExplosion(dangerMap, position, arrival, actionInterval))
            return;

        float cost = parent.Cost + stepCost;
        string key = AttackKey(position, virtualBlock);
        if (bestCosts.TryGetValue(key, out float oldCost) && oldCost <= cost)
            return;

        bestCosts[key] = cost;
        open.Add(new AttackNode
        {
            Position = position,
            VirtualBlock = virtualBlock,
            Cost = cost,
            Score = cost + AttackHeuristic(position, playerPosition, moveDuration),
            ArrivalTime = arrival,
            Parent = parent,
            Step = new GridPathStep(position, direction, action)
        });
    }

    private static bool CanStandAt(
        GridManager gridManager, Vector3Int position, Vector3Int? virtualBlock)
    {
        if (!gridManager.CanCharacterEnter(position))
            return false;

        Vector3Int below = position + Vector3Int.down;
        return gridManager.HasBlock(below) || virtualBlock == below;
    }

    /// <summary>踏み出しセルから着地セルまでの落下列が爆発前に通過可能か確認します。</summary>
    private static bool CanFallBeforeExplosion(
        GridDangerMap dangerMap,
        Vector3Int edge,
        Vector3Int landing,
        float arrivalTime,
        float actionInterval)
    {
        for (int y = edge.y; y >= landing.y; y--)
        {
            Vector3Int position = new Vector3Int(edge.x, y, edge.z);
            if (!CanArriveBeforeExplosion(dangerMap, position, arrivalTime, actionInterval))
                return false;
        }

        return true;
    }

    /// <summary>A*のヒューリスティック。目標までのマンハッタン距離 × 1手の最小コストです。</summary>
    private static float AttackHeuristic(
        Vector3Int position, Vector3Int target, float moveDuration)
        => GridUtility.ManhattanDistance(position, target) * Mathf.Max(MinimumMoveCostSeconds, moveDuration);

    /// <summary>「位置＋仮想Blockの有無・位置」で探索状態を区別するためのキーです。</summary>
    private static string AttackKey(Vector3Int position, Vector3Int? virtualBlock)
        => $"{position.x},{position.y},{position.z}|" +
           (virtualBlock.HasValue ? virtualBlock.Value.ToString() : "none");

    /// <summary>ゴールから親をたどって、実行順の行動列を作ります（開始ノードは含みません）。</summary>
    private static List<GridPathStep> BuildAttackPath(AttackNode goal)
    {
        List<GridPathStep> path = new List<GridPathStep>();
        AttackNode current = goal;

        while (current.Parent != null)
        {
            path.Add(current.Step);
            current = current.Parent;
        }

        path.Reverse();
        return path;
    }

    // ===== Escape: BFS =====
    /// <summary>
    /// startから到達できる「爆風予定に一度も含まれない最寄りセル」を探します。
    /// 飛び降りを含む経路では、着地セルのyが1つ前のセルより低くなります。
    /// </summary>
    public static List<Vector3Int> FindPathToNearestSafeCell(
        GridManager gridManager,
        GridDangerMap dangerMap,
        Vector3Int start,
        float moveDuration = 0f,
        float jumpDuration = 0f,
        float actionInterval = 0f,
        float fallDurationPerCell = 0f,
        PathSearchTrace trace = null)
    {
        List<Vector3Int> emptyPath = new List<Vector3Int>();
        trace?.BeginSearch(start, null);

        if (gridManager == null || dangerMap == null || !gridManager.Contains(start))
        {
            trace?.CompleteWithoutPath();
            return emptyPath;
        }

        Queue<Vector3Int> open = new Queue<Vector3Int>();

        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();

        Dictionary<Vector3Int, Vector3Int> previous = new Dictionary<Vector3Int, Vector3Int>();

        Dictionary<Vector3Int, float> arrivalTimes = new Dictionary<Vector3Int, float>();

        open.Enqueue(start);
        visited.Add(start);
        arrivalTimes[start] = 0f;

        while (open.Count > 0)
        {
            Vector3Int current = open.Dequeue();

            trace?.RecordExpanded(current,
                current == start ? (Vector3Int?)null : previous[current],
                arrivalTimes[current], 0f);

            if (current != start && !dangerMap.IsDangerous(current))
            {
                List<Vector3Int> safePath = BuildPath(previous, start, current);
                trace?.CompleteWithCells(safePath, arrivalTimes[current]);
                return safePath;
            }

            for (int i = 0; i < GridUtility.HorizontalDirections.Length; i++)
            {
                Vector3Int direction = GridUtility.HorizontalDirections[i];

                Vector3Int next = current + direction;

                float currentArrival = arrivalTimes[current];

                float moveArrival = currentArrival + Mathf.Max(moveDuration, actionInterval);

                if (CanStandAt(gridManager, next) && CanArriveBeforeExplosion(
                        dangerMap, next, moveArrival, actionInterval))
                {
                    TryEnqueue(next, current, moveArrival, visited, previous, arrivalTimes, open);
                }

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

                if (GridGravitySystem.TryGetStepAndFallDestination(
                        gridManager, current, direction,
                        out Vector3Int fallEdge, out Vector3Int fallLanding))
                {
                    int fallDistance = fallEdge.y - fallLanding.y;
                    float fallArrival = currentArrival + Mathf.Max(moveDuration, actionInterval) +
                                        fallDurationPerCell * Mathf.Max(1, fallDistance);

                    if (CanFallBeforeExplosion(
                            dangerMap, fallEdge, fallLanding, fallArrival, actionInterval))
                    {
                        TryEnqueue(fallLanding, current, fallArrival,
                            visited, previous, arrivalTimes, open);
                    }
                }
            }
        }

        trace?.CompleteWithoutPath();
        return emptyPath;
    }

    /// <summary>
    /// 通常移動後に、そのセルへ立っていられるかを判定します。
    /// CanCharacterEnterだけではセル内部が空かしか分からないため、直下の足場も確認します。
    /// </summary>
    private static bool CanStandAt(GridManager gridManager, Vector3Int position)
    {
        if (!gridManager.CanCharacterEnter(position))
            return false;

        Vector3Int below = position + Vector3Int.down;

        return gridManager.HasBlock(below);
    }

    /// <summary>
    /// 未探索セルをopenへ追加し、経路復元用の親セルと到着時刻を記録します。
    /// すでにvisitedへ登録されているセルは追加しません。
    /// </summary>
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

    /// <summary>
    /// 指定セルへ到着し、次の思考タイミングまで滞在しても爆発前かを判定します。
    /// DangerMapに登録されていないセルは、現在存在するBombに対して安全です。
    /// </summary>
    private static bool CanArriveBeforeExplosion(
        GridDangerMap dangerMap, Vector3Int position, float arrivalTime, float actionInterval)
    {
        if (!dangerMap.TryGetDangerTime(position, out float dangerTime))
            return true;

        return arrivalTime + actionInterval + ExplosionSafetyMargin < dangerTime;
    }

    /// <summary>
    /// previousを使ってgoalからstartまで逆向きにたどり、実行順の経路を作ります。
    /// </summary>
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
