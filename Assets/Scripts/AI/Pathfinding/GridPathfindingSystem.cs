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
/// Enemyの逃走経路は幅優先探索（BFS）、Playerへの攻撃位置まではA*で計算します。
///
/// このクラス自身はCharacterを動かしません。GridManagerへ移動可能性を問い合わせ、
/// GridDangerMapへ爆発時刻を問い合わせたうえで、通過するセルの一覧だけを返します。
/// 実際にMoveまたはJumpを実行するのは、結果を受け取ったEnemyBrainです。
///
/// 現在探索できる行動:
/// ・同じ高さにある水平4方向への通常移動
/// ・水平に隣接する1段高いBlock上へのジャンプ
///
/// Escapeで現在探索しない行動:
/// ・斜め移動
/// ・高い場所から低い場所への意図的な降下
/// ・探索途中でのBlock設置
/// ・Bomb設置
///
/// 注意:
/// 通常移動とジャンプでは所要時間が異なりますが、探索順はBFSです。
/// そのため「通過セル数が少ない経路」は見つけやすい一方、異なる移動コストを含む
/// 厳密な最短時間経路は保証しません。将来的に時間最短を保証するなら、
/// Dijkstra法またはA*へ置き換える必要があります。
/// </summary>
public static class GridPathfindingSystem
{
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

    // UnityではYが高さです。この配列には高さを変えないX/Z方向だけを定義します。
    // 配列の順番は、同じ距離に複数の安全セルがある場合の探索優先順にもなります。
    private static readonly Vector3Int[] HorizontalDirections =
    {
        Vector3Int.right,
        Vector3Int.left,
        Vector3Int.forward,
        Vector3Int.back
    };

    /// <summary>
    /// PlayerへBombの爆風を届かせられるセルまで、A*で行動列を探索します。
    /// 通常移動、既存Blockへのジャンプ、経路中1回までのBlock設置後ジャンプを扱います。
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
        float actionInterval)
    {
        List<GridPathStep> empty = new List<GridPathStep>();
        if (gridManager == null || dangerMap == null || !gridManager.Contains(start))
            return empty;

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
        const int maxExpandedNodes = 5000;

        while (open.Count > 0 && expandedNodeCount < maxExpandedNodes)
        {
            expandedNodeCount++;
            int bestIndex = 0;
            for (int i = 1; i < open.Count; i++)
                if (open[i].Score < open[bestIndex].Score)
                    bestIndex = i;

            AttackNode current = open[bestIndex];
            open.RemoveAt(bestIndex);

            if (current.Position != start &&
                !dangerMap.IsDangerous(current.Position) &&
                ManhattanDistance(current.Position, playerPosition) <= bombDistance &&
                CanExplosionReach(gridManager, current.Position, playerPosition, explosionPower))
                return BuildAttackPath(current);

            for (int i = 0; i < HorizontalDirections.Length; i++)
            {
                Vector3Int direction = HorizontalDirections[i];
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

    private static bool CanExplosionReach(
        GridManager gridManager, Vector3Int origin, Vector3Int target, int power)
    {
        IReadOnlyList<Vector3Int> cells =
            ExplosionSystem.CalculateAffectedCells(gridManager, origin, power);
        for (int i = 0; i < cells.Count; i++)
            if (cells[i] == target)
                return true;
        return false;
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

    private static float AttackHeuristic(
        Vector3Int position, Vector3Int target, float moveDuration)
    {
        Vector3Int difference = position - target;
        int distance = Mathf.Abs(difference.x) + Mathf.Abs(difference.y) +
                       Mathf.Abs(difference.z);
        return distance * Mathf.Max(0.01f, moveDuration);
    }

    private static int ManhattanDistance(Vector3Int from, Vector3Int to)
    {
        Vector3Int difference = from - to;
        return Mathf.Abs(difference.x) + Mathf.Abs(difference.y) +
               Mathf.Abs(difference.z);
    }

    private static string AttackKey(Vector3Int position, Vector3Int? virtualBlock)
        => $"{position.x},{position.y},{position.z}|" +
           (virtualBlock.HasValue ? virtualBlock.Value.ToString() : "none");

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

    /// <summary>
    /// startから到達できる「爆風予定に一度も含まれない最寄りセル」を探します。
    ///
    /// 戻り値の例:
    /// start=(1,1,1)、安全セル=(3,1,1)の場合
    /// [(1,1,1), (2,1,1), (3,1,1)]
    ///
    /// 戻り値にはstartを含みます。EnemyBrainは通常、index 1を次の移動先として使います。
    /// 安全セルへ到達できない場合、引数が不正な場合は空のListを返します。
    ///
    /// moveDuration   : 通常移動1回の表示・行動時間
    /// jumpDuration   : 1段ジャンプ1回の時間
    /// actionInterval : Enemyが次の判断を行うまでの間隔
    /// </summary>
    public static List<Vector3Int> FindPathToNearestSafeCell(
        GridManager gridManager,
        GridDangerMap dangerMap,
        Vector3Int start,
        float moveDuration = 0f,
        float jumpDuration = 0f,
        float actionInterval = 0f)
    {
        // 失敗時にnullを返すと呼び出し側でnull判定が必要になるため、空Listで統一します。
        List<Vector3Int> emptyPath = new List<Vector3Int>();

        // 探索に必要な情報がなければ、盤面へアクセスせず終了します。
        if (gridManager == null || dangerMap == null || !gridManager.Contains(start))
            return emptyPath;

        // open:
        // これから隣接セルを調べる「探索待ちセル」のFIFOキューです。
        // FIFOなので、startから1手、2手、3手の順に近いセルから展開されます。
        Queue<Vector3Int> open = new Queue<Vector3Int>();

        // visited:
        // すでに発見したセルです。同じセルを何度もキューへ入れて無限ループすることを防ぎます。
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();

        // previous:
        // 「そのセルへどのセルから来たか」を記録します。
        // ゴール発見後、goalからstartまで逆向きにたどって経路を復元するために使います。
        Dictionary<Vector3Int, Vector3Int> previous =
            new Dictionary<Vector3Int, Vector3Int>();

        // arrivalTimes:
        // startから各セルへ到着する予想時刻（探索開始からの経過秒）です。
        // GridDangerMapの爆発時刻と比較し、到着前後に爆発するセルを除外します。
        Dictionary<Vector3Int, float> arrivalTimes = new Dictionary<Vector3Int, float>();

        // 探索開始セルを最初の探索待ちセルとして登録します。
        open.Enqueue(start);
        visited.Add(start);
        arrivalTimes[start] = 0f;

        // 調べるセルがなくなるか、安全セルが見つかるまで探索を続けます。
        while (open.Count > 0)
        {
            // 最も早くキューへ追加されたセルを1つ取り出します。
            Vector3Int current = open.Dequeue();

            // start自身はEnemyが現在立っている危険セルなので、ゴールにはしません。
            // IsDangerous=falseは「どのBombの爆風予定にも含まれない」ことを意味します。
            if (current != start && !dangerMap.IsDangerous(current))
                return BuildPath(previous, start, current);

            // currentから水平4方向へ進めるか、1方向ずつ調べます。
            for (int i = 0; i < HorizontalDirections.Length; i++)
            {
                Vector3Int direction = HorizontalDirections[i];

                // 通常移動した場合の、同じ高さにある隣接セルです。
                Vector3Int next = current + direction;

                // currentへ到着するまでに必要だった累積時間です。
                float currentArrival = arrivalTimes[current];

                // AIは移動が早く完了しても、次の思考タイミングまでは新しい行動をしません。
                // そのため1手の所要時間は、移動時間と行動間隔の大きい方として扱います。
                float moveArrival = currentArrival + Mathf.Max(moveDuration, actionInterval);

                // 1. nextへCharacterが入れる
                // 2. nextの直下にBlock（足場）がある
                // 3. 到着・滞在中に爆発しない
                // 以上を満たす場合だけ探索候補へ追加します。
                if (CanStandAt(gridManager, next) && CanArriveBeforeExplosion(
                        dangerMap, next, moveArrival, actionInterval))
                {
                    TryEnqueue(next, current, moveArrival, visited, previous, arrivalTimes, open);
                }

                // 通常移動とは別に、同じdirectionへ1段上るジャンプも調べます。
                // CanJumpUpは、方向先にBlockがあり、そのBlock上が空いているかを判定します。
                if (gridManager.CanJumpUp(current, direction, out Vector3Int jumpLanding) &&
                    gridManager.CanCharacterEnter(jumpLanding))
                {
                    // ジャンプについても、ジャンプ時間とAI行動間隔の大きい方を1手の時間にします。
                    float jumpArrival = currentArrival + Mathf.Max(jumpDuration, actionInterval);

                    // ジャンプ先へ到着し、次の行動を待つ間に爆発しない場合だけ追加します。
                    if (CanArriveBeforeExplosion(
                            dangerMap, jumpLanding, jumpArrival, actionInterval))
                    {
                        TryEnqueue(jumpLanding, current, jumpArrival,
                            visited, previous, arrivalTimes, open);
                    }
                }
            }
        }

        // キューが空になった場合、現在の移動ルールでは安全セルへ到達できません。
        return emptyPath;
    }

    /// <summary>
    /// 通常移動後に、そのセルへ立っていられるかを判定します。
    /// CanCharacterEnterだけではセル内部が空かしか分からないため、直下の足場も確認します。
    /// </summary>
    private static bool CanStandAt(GridManager gridManager, Vector3Int position)
    {
        // Block、Bomb、別Character、予約があるセルには通常移動できません。
        if (!gridManager.CanCharacterEnter(position))
            return false;

        // Characterが立つセルの1段下を足場セルとして調べます。
        Vector3Int below = position + Vector3Int.down;

        // 外殻床(Y=-1)も足場として認識。立つセル自体は上で内部範囲を検証済みです。
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
        // HashSet.Addは新規追加できた場合だけtrueを返します。
        // falseなら別経路ですでに発見済みなので、重複して探索しません。
        if (!visited.Add(next))
            return;

        // nextへはcurrentから来たことを保存します。
        previous[next] = current;

        // nextへ到着するまでの累積時間を保存します。
        arrivalTimes[next] = arrivalTime;

        // 後でnextの隣接セルを調べるため、探索待ちキューへ追加します。
        open.Enqueue(next);
    }

    /// <summary>
    /// 指定セルへ到着し、次の思考タイミングまで滞在しても爆発前かを判定します。
    /// DangerMapに登録されていないセルは、現在存在するBombに対して安全です。
    /// </summary>
    private static bool CanArriveBeforeExplosion(
        GridDangerMap dangerMap, Vector3Int position, float arrivalTime, float actionInterval)
    {
        // 危険時刻が存在しないなら、どのBombの爆風にも含まれていません。
        if (!dangerMap.TryGetDangerTime(position, out float dangerTime))
            return true;

        // 計算誤差やフレーム時間のずれで爆発と同時到着にならないよう、少し余裕を持たせます。
        const float safetyMargin = 0.05f;

        // 到着時刻だけでなく、次の行動を開始できるまでの待ち時間も含めて比較します。
        return arrivalTime + actionInterval + safetyMargin < dangerTime;
    }

    /// <summary>
    /// previousを使ってgoalからstartまで逆向きにたどり、実行順の経路を作ります。
    /// </summary>
    private static List<Vector3Int> BuildPath(
        Dictionary<Vector3Int, Vector3Int> previous,
        Vector3Int start,
        Vector3Int goal)
    {
        // 最初はゴールだけを入れます。この段階では逆順の経路です。
        List<Vector3Int> path = new List<Vector3Int> { goal };
        Vector3Int current = goal;

        // goal → 親 → 親の親 → startという順番でたどります。
        while (current != start)
        {
            current = previous[current];
            path.Add(current);
        }

        // 現在はgoalからstartの逆順なので、startからgoalの実行順へ反転します。
        path.Reverse();
        return path;
    }
}
