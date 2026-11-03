using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// EnemyBrainの危険予測・経路探索結果・状態をSceneビューへ描画します（Editor専用）。
///
/// ・EnemyBrainの読み取り専用アクセサ（EnemyBrain.Debug.cs）だけを参照し、AIの判断には関与しません。
/// ・[DrawGizmo]で描画するため、Enemy Prefabへのコンポーネント追加は不要です。
/// ・表示のON/OFFはシーン上のEnemyAIDebugVisualizerのbool値で切り替えます。
/// ・Editorフォルダ配下なのでビルドには含まれません。
/// ・グリッド座標→ワールド座標の変換はGridManager.GetWorldPosition（セル中心）を使用します。
///
/// 凡例:
///   危険セル     : 赤（間もなく爆発）→黄（猶予あり）の半透明キューブ。ラベルは残り秒数と高さ(y)
///   逃走経路     : シアン（BFS）
///   攻撃経路     : マゼンタ（A*）。終点に攻撃位置、点線の先に計画時のPlayer位置
///   Item取得経路 : 緑（Dijkstra）。終点にItem位置
///   行動の種類   : Move=実線 / JumpUp=上向きの弧 / MoveAndFall=水平実線+垂直点線
///                  PlaceBlockAndJump=設置予定Block（橙のワイヤーキューブ）+弧
///   高さ         : 高さが変わるノードに y 値を表示。Height Guides有効時はy=0の床まで点線を下ろします
/// </summary>
internal static class EnemyAIDebugGizmoDrawer
{
    private const float LineWidth = 4f;

    private static readonly Color EscapeColor = new Color(0.2f, 0.95f, 1f);
    private static readonly Color AttackColor = new Color(1f, 0.3f, 0.85f);
    private static readonly Color ItemColor = new Color(0.4f, 1f, 0.35f);
    private static readonly Color BlockColor = new Color(1f, 0.6f, 0.1f);
    private static readonly Color HeightGuideColor = new Color(1f, 1f, 1f, 0.35f);

    private static GUIStyle _timerStyle;
    private static GUIStyle _stateStyle;
    private static GUIStyle _stepStyle;

    // Play開始時のドメインリロードでfalseへ戻ります。
    private static bool _loggedFirstCall;

    // 危険予測はどのEnemyでも同じ盤面から作られるため、重複描画を避けて1体分だけ描きます。
    private static EnemyBrain _dangerOwner;

    /// <summary>グリッド座標⇔ワールド座標の変換に必要な情報をまとめます。</summary>
    internal readonly struct GridSpace
    {
        public readonly GridManager Grid;
        public readonly float CellSize;

        public GridSpace(GridManager grid)
        {
            Grid = grid;
            float size = Mathf.Abs(
                (grid.GetWorldPosition(Vector3Int.right) - grid.GetWorldPosition(Vector3Int.zero)).x);
            CellSize = size > 0f ? size : 1f;
        }

        public Vector3 Center(Vector3Int position) => Grid.GetWorldPosition(position);

        public Vector3 Bottom(Vector3Int position)
            => Center(position) + Vector3.down * (CellSize * 0.5f);

        /// <summary>同じXZでy=0セルの底面（盤面の床）です。</summary>
        public Vector3 Floor(Vector3Int position)
            => Bottom(new Vector3Int(position.x, 0, position.z));
    }

    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
    private static void DrawEnemyBrainGizmos(EnemyBrain brain, GizmoType gizmoType)
    {
        if (!EditorApplication.isPlaying)
            return;

        // 「表示されない」ときの切り分け用に、Play開始後の最初の呼び出しで状態を1度だけ出力します。
        // （このログ自体が出ない場合、UnityがSceneビューのGizmoを描画していません）
        if (!_loggedFirstCall)
        {
            _loggedFirstCall = true;
            Debug.Log($"[AI Debug] Scene gizmo drawer running: visualizerEnabled={EnemyAIDebugSettings.Enabled}, " +
                      $"enemyReady={brain != null && brain.DebugIsReady}, collecting={PathSearchDebug.IsCollecting}");
        }

        if (!EnemyAIDebugSettings.Enabled)
            return;
        if (brain == null || !brain.isActiveAndEnabled || !brain.DebugIsReady)
            return;

        bool isSelected = Selection.Contains(brain.gameObject);
        if (EnemyAIDebugSettings.SelectedOnly && !isSelected)
            return;

        var space = new GridSpace(brain.DebugGridManager);
        Vector3Int current = brain.DebugCurrentPosition;

        EnsureStyles();

        if ((EnemyAIDebugSettings.DangerCells || EnemyAIDebugSettings.DangerTimers) &&
            IsDangerOwner(brain))
            DrawDangerMap(brain, space, current);

        if (EnemyAIDebugSettings.EscapePath)
            DrawEscapePath(brain, space, current);

        if (EnemyAIDebugSettings.AttackPath)
            DrawAttackPath(brain, space, current);

        if (EnemyAIDebugSettings.ItemPath)
            DrawItemPath(brain, space, current);

        if (EnemyAIDebugSettings.StateLabel)
            DrawStateLabel(brain, space, current);
    }

    // ------------------------------------------------------------------
    // 危険予測
    // ------------------------------------------------------------------

    /// <summary>
    /// 危険予測を描画するEnemyを1体に決めます。選択中のEnemyがいればそれを優先し、
    /// 前回の担当が有効な間は担当を変えません（描画のちらつき防止）。
    /// </summary>
    private static bool IsDangerOwner(EnemyBrain brain)
    {
        bool anySelected = false;
        foreach (GameObject selected in Selection.gameObjects)
        {
            if (selected != null && selected.TryGetComponent(out EnemyBrain _))
            {
                anySelected = true;
                break;
            }
        }

        bool ownerValid = _dangerOwner != null && _dangerOwner.isActiveAndEnabled &&
                          _dangerOwner.DebugIsReady &&
                          (!anySelected || Selection.Contains(_dangerOwner.gameObject));

        if (!ownerValid)
        {
            bool candidateOk = !anySelected || Selection.Contains(brain.gameObject);
            if (!candidateOk)
                return false;
            _dangerOwner = brain;
        }

        return _dangerOwner == brain;
    }

    private static void DrawDangerMap(EnemyBrain brain, GridSpace space, Vector3Int current)
    {
        IReadOnlyDictionary<Vector3Int, float> dangerTimes = brain.DebugDangerTimes;
        if (dangerTimes.Count == 0)
            return;

        // DangerTimesはRebuild時点からの相対秒です。現在からの残り秒へ換算します。
        float elapsed = Time.time - brain.DebugDangerMapBuiltAt;
        Vector3 cubeSize = Vector3.one * (space.CellSize * 0.9f);

        foreach (KeyValuePair<Vector3Int, float> pair in dangerTimes)
        {
            Vector3Int cell = pair.Key;
            if (EnemyAIDebugSettings.NearHeightOnly && Mathf.Abs(cell.y - current.y) > 1)
                continue;

            float remaining = pair.Value - elapsed;
            Color color = GetDangerColor(remaining);
            Vector3 center = space.Center(cell);

            if (EnemyAIDebugSettings.DangerCells)
            {
                Gizmos.color = new Color(color.r, color.g, color.b, 0.22f);
                Gizmos.DrawCube(center, cubeSize);
                Gizmos.color = color;
                Gizmos.DrawWireCube(center, cubeSize);

                if (EnemyAIDebugSettings.HeightGuides && cell.y > 0)
                    DrawHeightGuide(space, cell);
            }

            if (EnemyAIDebugSettings.DangerTimers)
            {
                _timerStyle.normal.textColor = color;
                string time = remaining > 0f ? $"{remaining:0.0}s" : "NOW";
                Handles.Label(center, $"{time}\ny{cell.y}", _timerStyle);
            }
        }
    }

    /// <summary>残り時間が短いほど赤、長いほど黄色にします。</summary>
    private static Color GetDangerColor(float remainingSeconds)
    {
        if (remainingSeconds <= 0f)
            return new Color(0.8f, 0f, 0.2f);
        float t = Mathf.InverseLerp(0.3f, 3f, remainingSeconds);
        return Color.Lerp(new Color(1f, 0.1f, 0.05f), new Color(1f, 0.9f, 0.1f), t);
    }

    // ------------------------------------------------------------------
    // 経路
    // ------------------------------------------------------------------

    /// <summary>BFSの逃走経路。セル列のみなので、高さ差から行動種別を推定します。</summary>
    private static void DrawEscapePath(EnemyBrain brain, GridSpace space, Vector3Int current)
    {
        IReadOnlyList<Vector3Int> path = brain.DebugEscapePath;
        if (path.Count == 0)
            return;

        Vector3 offset = Vector3.up * (space.CellSize * 0.06f);
        Vector3Int from = current;
        bool drewAny = false;

        for (int i = 0; i < path.Count; i++)
        {
            Vector3Int to = path[i];
            if (to == from)
                continue;

            Vector3Int difference = to - from;
            Vector3Int direction = new Vector3Int(difference.x, 0, difference.z);
            GridPathActionType action = difference.y > 0 ? GridPathActionType.JumpUp :
                difference.y < 0 ? GridPathActionType.MoveAndFall : GridPathActionType.Move;

            DrawStep(space, from, to, direction, action, EscapeColor, offset);
            from = to;
            drewAny = true;
        }

        if (drewAny)
            DrawGoalMarker(space, from, EscapeColor, offset, "SAFE (BFS)");
    }

    private static void DrawAttackPath(EnemyBrain brain, GridSpace space, Vector3Int current)
    {
        IReadOnlyList<GridPathStep> path = brain.DebugAttackPath;
        Vector3 offset = Vector3.up * (space.CellSize * 0.12f);

        Vector3Int end = DrawStepPath(space, current, path, AttackColor, offset,
            brain.DebugIsWaitingForPlacedBlockJump);

        if (path.Count > 0)
            DrawGoalMarker(space, end, AttackColor, offset, "ATTACK POS (A*)");

        if (brain.DebugHasAttackPlan)
        {
            Vector3Int player = brain.DebugPlannedPlayerPosition;
            Vector3 playerCenter = space.Center(player) + offset;
            using (new Handles.DrawingScope(AttackColor))
            {
                if (path.Count > 0)
                    Handles.DrawDottedLine(space.Center(end) + offset, playerCenter, 3f);
                Handles.DrawWireDisc(space.Bottom(player) + Vector3.up * 0.02f, Vector3.up,
                    space.CellSize * 0.45f);
                Handles.DrawWireDisc(space.Bottom(player) + Vector3.up * 0.02f, Vector3.up,
                    space.CellSize * 0.3f);
            }
            Handles.Label(playerCenter + Vector3.up * (space.CellSize * 0.3f),
                $"TARGET y{player.y}", _stepStyle);
        }
    }

    private static void DrawItemPath(EnemyBrain brain, GridSpace space, Vector3Int current)
    {
        if (!brain.DebugHasItemTarget)
            return;

        IReadOnlyList<GridPathStep> path = brain.DebugItemPath;
        Vector3 offset = Vector3.up * (space.CellSize * 0.18f);

        DrawStepPath(space, current, path, ItemColor, offset, false);

        Vector3Int target = brain.DebugItemTargetPosition;
        DrawGoalMarker(space, target, ItemColor, offset, "ITEM (Dijkstra)");
    }

    /// <summary>GridPathStep列を描画し、終点セルを返します。</summary>
    private static Vector3Int DrawStepPath(GridSpace space, Vector3Int start,
        IReadOnlyList<GridPathStep> path, Color color, Vector3 offset, bool firstBlockPlaced)
    {
        Vector3Int from = start;
        for (int i = 0; i < path.Count; i++)
        {
            GridPathStep step = path[i];
            // EnemyBrainは到達済みの先頭ステップを次の思考時に取り除くため、ここでは描画を飛ばします。
            if (step.Position == from)
                continue;

            DrawStep(space, from, step.Position, step.Direction, step.Action, color, offset,
                i == 0 && firstBlockPlaced);
            from = step.Position;
        }
        return from;
    }

    /// <summary>1手分を行動種別ごとに描き分けます。</summary>
    internal static void DrawStep(GridSpace space, Vector3Int from, Vector3Int to,
        Vector3Int direction, GridPathActionType action, Color color, Vector3 offset,
        bool blockAlreadyPlaced = false)
    {
        Vector3 a = space.Center(from) + offset;
        Vector3 b = space.Center(to) + offset;
        float cell = space.CellSize;

        using (new Handles.DrawingScope(color))
        {
            switch (action)
            {
                case GridPathActionType.Move:
                    Handles.DrawAAPolyLine(LineWidth, a, b);
                    break;

                case GridPathActionType.JumpUp:
                    DrawJumpArc(a, b, color, cell);
                    Handles.Label(Vector3.Lerp(a, b, 0.5f) + Vector3.up * (cell * 0.7f),
                        "JUMP", _stepStyle);
                    break;

                case GridPathActionType.MoveAndFall:
                {
                    Vector3Int edgeCell = from + direction;
                    Vector3 edge = space.Center(edgeCell) + offset;
                    Handles.DrawAAPolyLine(LineWidth, a, edge);
                    Handles.DrawDottedLine(edge, b, 4f);
                    Handles.Label(Vector3.Lerp(edge, b, 0.5f),
                        $"FALL {edgeCell.y - to.y}", _stepStyle);
                    break;
                }

                case GridPathActionType.PlaceBlockAndJump:
                {
                    Vector3Int blockCell = from + direction;
                    Vector3 blockCenter = space.Center(blockCell);
                    using (new Handles.DrawingScope(BlockColor))
                    {
                        Handles.DrawWireCube(blockCenter, Vector3.one * (cell * 0.95f));
                        Handles.DrawWireCube(blockCenter, Vector3.one * (cell * 0.85f));
                    }
                    _stepStyle.normal.textColor = BlockColor;
                    Handles.Label(blockCenter,
                        blockAlreadyPlaced ? "BLOCK (placed)\nwaiting jump" : "PLACE BLOCK",
                        _stepStyle);
                    _stepStyle.normal.textColor = Color.white;
                    DrawJumpArc(a, b, color, cell);
                    break;
                }
            }

            Handles.SphereHandleCap(0, b, Quaternion.identity, cell * 0.12f, EventType.Repaint);
        }

        // 高さが変わったノードには到達後のyを表示し、3Dの段差を読み取りやすくします。
        if (to.y != from.y)
        {
            _stepStyle.normal.textColor = color;
            Handles.Label(b + Vector3.up * (cell * 0.2f), $"y{to.y}", _stepStyle);
            _stepStyle.normal.textColor = Color.white;
        }

        if (EnemyAIDebugSettings.HeightGuides && to.y > 0)
            DrawHeightGuide(space, to);
    }

    private static void DrawJumpArc(Vector3 a, Vector3 b, Color color, float cell)
    {
        Handles.DrawBezier(a, b,
            a + Vector3.up * (cell * 0.9f),
            b + Vector3.up * (cell * 0.6f),
            color, null, LineWidth);
    }

    internal static void DrawGoalMarker(GridSpace space, Vector3Int cell, Color color,
        Vector3 offset, string label)
    {
        Vector3 center = space.Center(cell) + offset;
        using (new Handles.DrawingScope(color))
        {
            Handles.DrawWireCube(space.Center(cell), Vector3.one * (space.CellSize * 0.7f));
            Handles.DrawWireDisc(space.Bottom(cell) + Vector3.up * 0.02f, Vector3.up,
                space.CellSize * 0.4f);
        }
        _stepStyle.normal.textColor = color;
        Handles.Label(center + Vector3.up * (space.CellSize * 0.45f),
            $"{label}\n({cell.x},{cell.y},{cell.z})", _stepStyle);
        _stepStyle.normal.textColor = Color.white;
    }

    /// <summary>セル底面からy=0の床まで点線を下ろし、空中のセルの高さを示します。</summary>
    internal static void DrawHeightGuide(GridSpace space, Vector3Int cell)
    {
        Vector3 bottom = space.Bottom(cell);
        Vector3 floor = space.Floor(cell);
        using (new Handles.DrawingScope(HeightGuideColor))
        {
            Handles.DrawDottedLine(bottom, floor, 2f);
            Handles.DrawWireDisc(floor + Vector3.up * 0.01f, Vector3.up, space.CellSize * 0.15f);
        }
    }

    // ------------------------------------------------------------------
    // 状態と探索目標
    // ------------------------------------------------------------------

    private static void DrawStateLabel(EnemyBrain brain, GridSpace space, Vector3Int current)
    {
        string goal = GetGoalDescription(brain);
        string extra = brain.DebugIsWaitingForPlacedBlockJump ? "\n<i>waiting placed-block jump</i>" : "";
        if (brain.DebugLastBombDecision != EnemyBrain.DebugBombDecision.None)
            extra += $"\nBomb: {brain.DebugLastBombDecision} " +
                     $"({Time.time - brain.DebugLastBombDecisionTime:0.0}s ago)";
        string text =
            $"<b>{brain.name}</b>  ({current.x},{current.y},{current.z})\n" +
            $"State: <b>{brain.CurrentState}</b>\n" +
            $"Goal: {goal}{extra}";

        Handles.Label(space.Center(current) + Vector3.up * (space.CellSize * 1.4f), text, _stateStyle);
    }

    private static string GetGoalDescription(EnemyBrain brain)
    {
        switch (brain.CurrentState)
        {
            case EnemyAIState.Escape:
            {
                IReadOnlyList<Vector3Int> path = brain.DebugEscapePath;
                return path.Count > 0
                    ? $"safe cell {Format(path[path.Count - 1])} [BFS, {path.Count} cells]"
                    : "none (waiting for safety / no BFS path)";
            }

            case EnemyAIState.MoveToAttackPosition:
            case EnemyAIState.Chase:
            case EnemyAIState.PlaceBomb:
            {
                IReadOnlyList<GridPathStep> path = brain.DebugAttackPath;
                if (path.Count > 0)
                    return $"attack pos {Format(path[path.Count - 1].Position)} [A*, {path.Count} steps]" +
                           $" → player {Format(brain.DebugPlannedPlayerPosition)}";
                return brain.DebugHasAttackPlan
                    ? $"player {Format(brain.DebugPlannedPlayerPosition)} (no A* path)"
                    : "bomb at current cell";
            }

            case EnemyAIState.CollectItem:
                return brain.DebugHasItemTarget
                    ? $"item {Format(brain.DebugItemTargetPosition)} [Dijkstra, {brain.DebugItemPath.Count} steps]"
                    : "item collected";

            default:
                return "none";
        }
    }

    private static string Format(Vector3Int p) => $"({p.x},{p.y},{p.z})";

    internal static void EnsureStyles()
    {
        if (_timerStyle == null)
        {
            _timerStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10
            };
        }

        if (_stepStyle == null)
        {
            _stepStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10
            };
            _stepStyle.normal.textColor = Color.white;
        }

        if (_stateStyle == null)
        {
            _stateStyle = new GUIStyle(EditorStyles.helpBox)
            {
                richText = true,
                fontSize = 11,
                alignment = TextAnchor.UpperLeft
            };
            _stateStyle.normal.textColor = Color.white;
        }
    }
}
