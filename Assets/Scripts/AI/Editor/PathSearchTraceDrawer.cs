using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// PathSearchTrace（探索履歴）をSceneビューへ描画します（Editor専用・表示のみ）。
///
/// ・展開済みノード : 展開順で青→橙に変わる小さなキューブ。ラベルは展開順(#n)と高さ(y)
/// ・親リンク       : 展開ノードから親ノードへの細線（どこから到達したか）
/// ・現在のステップ : 白い枠と詳細ラベル（BFS=到着時刻 / Dijkstra=累積時間 / A*=g・h・f）
/// ・最終経路       : 金色の太線。行動種別の描き分けはPhase 1のDrawStepを再利用
/// ・Y階層          : 指定したyだけを表示、または他の階層を薄く表示
///
/// 描画するのは記録された展開ノードだけで、openへ追加されただけのノードは表示しません。
/// </summary>
internal static class PathSearchTraceDrawer
{
    internal sealed class Options
    {
        public bool ShowOrderLabels = true;
        public bool ShowCostLabels;
        public bool ShowParentLinks = true;
        public bool ShowFinalPathAlways;
        public bool FilterByLayer;
        public int Layer;
        public bool DimOtherLayers = true;
    }

    private static readonly Color FinalPathColor = new Color(1f, 0.82f, 0.15f);
    private static readonly Color StartColor = new Color(0.3f, 1f, 0.45f);
    private static readonly Color TargetColor = new Color(1f, 0.35f, 0.35f);

    private static GUIStyle _nodeStyle;
    private static GUIStyle _detailStyle;

    public static void Draw(EnemyBrain brain, PathSearchTrace trace, int visibleCount, Options options)
    {
        if (Event.current.type != EventType.Repaint)
            return;
        if (brain == null || trace == null || !brain.DebugIsReady)
            return;

        EnemyAIDebugGizmoDrawer.EnsureStyles();
        EnsureStyles();

        var space = new EnemyAIDebugGizmoDrawer.GridSpace(brain.DebugGridManager);
        float cell = space.CellSize;
        IReadOnlyList<PathSearchNodeRecord> nodes = trace.ExpandedNodes;
        int count = Mathf.Clamp(visibleCount, 0, nodes.Count);
        float nodeSize = cell * 0.42f;

        // 展開済みノード（現在のステップまで）
        for (int i = 0; i < count; i++)
        {
            PathSearchNodeRecord node = nodes[i];
            float alpha = GetLayerAlpha(node.Position.y, options);
            if (alpha <= 0f)
                continue;

            // 色は全体に対する展開順で決め、再生中も同じノードの色が変わらないようにします。
            float t = nodes.Count <= 1 ? 1f : i / (float)(nodes.Count - 1);
            Color color = Color.HSVToRGB(Mathf.Lerp(0.62f, 0.07f, t), 0.75f, 1f);
            Vector3 center = space.Center(node.Position);

            if (options.ShowParentLinks && node.HasParent)
            {
                Handles.color = WithAlpha(color, 0.45f * alpha);
                Handles.DrawLine(space.Center(node.ParentPosition), center);
            }

            Handles.color = WithAlpha(color, 0.3f * alpha);
            Handles.CubeHandleCap(0, center, Quaternion.identity, nodeSize, EventType.Repaint);
            Handles.color = WithAlpha(color, alpha);
            Handles.DrawWireCube(center, Vector3.one * nodeSize);

            if (options.ShowOrderLabels || options.ShowCostLabels)
            {
                string text = options.ShowOrderLabels ? $"#{node.Order} y{node.Position.y}" : "";
                if (options.ShowCostLabels)
                    text += (text.Length > 0 ? "\n" : "") + FormatCost(trace.Algorithm, node, false);
                if (node.HasVirtualBlock)
                    text += "\n+VB";
                _nodeStyle.normal.textColor = WithAlpha(color, alpha);
                Handles.Label(center + Vector3.up * (nodeSize * 0.75f), text, _nodeStyle);
            }
        }

        DrawEndpoints(space, trace);

        // 最終経路：再生が最後まで進んだ時点（または常時表示ON）で、展開ノードと区別して描きます。
        bool reachedEnd = count >= nodes.Count;
        if (trace.IsCompleted && trace.Succeeded && (reachedEnd || options.ShowFinalPathAlways))
            DrawFinalPath(space, trace);

        // 現在のステップ（最後に展開したノード）
        if (count > 0)
        {
            PathSearchNodeRecord current = nodes[count - 1];
            Vector3 center = space.Center(current.Position);
            Handles.color = Color.white;
            Handles.DrawWireCube(center, Vector3.one * (cell * 0.95f));
            Handles.DrawWireCube(center, Vector3.one * (cell * 0.9f));
            Handles.Label(center + Vector3.up * (cell * 0.9f),
                $"STEP #{current.Order}  ({current.Position.x},{current.Position.y},{current.Position.z})\n" +
                FormatCost(trace.Algorithm, current, true),
                _detailStyle);
        }

        // 探索の概要（開始セルの上）
        string result = !trace.IsCompleted ? "running" :
            trace.Succeeded ? $"path {trace.FinalPathStepCount} steps" : "no path";
        Handles.Label(space.Center(trace.Start) + Vector3.up * (cell * 2.1f),
            $"[{GetAlgorithmName(trace.Algorithm)}] {trace.Label}\n" +
            $"expanded {count}/{trace.ExpandedCount}{(trace.IsTruncated ? " (truncated)" : "")}  {result}",
            _detailStyle);
    }

    public static string GetAlgorithmName(PathSearchAlgorithm algorithm)
    {
        switch (algorithm)
        {
            case PathSearchAlgorithm.Bfs: return "BFS";
            case PathSearchAlgorithm.AStar: return "A*";
            case PathSearchAlgorithm.Dijkstra: return "Dijkstra";
            default: return algorithm.ToString();
        }
    }

    /// <summary>アルゴリズムごとに意味の異なるコスト値を表示用文字列にします。</summary>
    public static string FormatCost(PathSearchAlgorithm algorithm, PathSearchNodeRecord node, bool verbose)
    {
        switch (algorithm)
        {
            case PathSearchAlgorithm.AStar:
                return verbose
                    ? $"g={node.GCost:0.00}  h={node.HCost:0.00}  f={node.FCost:0.00}"
                    : $"g{node.GCost:0.0} h{node.HCost:0.0} f{node.FCost:0.0}";
            case PathSearchAlgorithm.Dijkstra:
                return verbose ? $"cumulative time = {node.GCost:0.00}s" : $"{node.GCost:0.00}s";
            default:
                return verbose ? $"order {node.Order}, arrival = {node.GCost:0.00}s" : $"t{node.GCost:0.00}s";
        }
    }

    private static void DrawEndpoints(EnemyAIDebugGizmoDrawer.GridSpace space, PathSearchTrace trace)
    {
        float cell = space.CellSize;
        Handles.color = StartColor;
        Handles.DrawWireDisc(space.Bottom(trace.Start) + Vector3.up * 0.03f, Vector3.up, cell * 0.48f);
        _nodeStyle.normal.textColor = StartColor;
        Handles.Label(space.Bottom(trace.Start), "START", _nodeStyle);

        if (!trace.HasTarget)
            return;

        Handles.color = TargetColor;
        Handles.DrawWireDisc(space.Bottom(trace.Target) + Vector3.up * 0.03f, Vector3.up, cell * 0.48f);
        Handles.DrawWireDisc(space.Bottom(trace.Target) + Vector3.up * 0.03f, Vector3.up, cell * 0.36f);
        _nodeStyle.normal.textColor = TargetColor;
        Handles.Label(space.Bottom(trace.Target), $"TARGET y{trace.Target.y}", _nodeStyle);
    }

    private static void DrawFinalPath(EnemyAIDebugGizmoDrawer.GridSpace space, PathSearchTrace trace)
    {
        Vector3 offset = Vector3.up * (space.CellSize * 0.25f);
        IReadOnlyList<GridPathStep> steps = trace.FinalSteps;
        IReadOnlyList<Vector3Int> cells = trace.FinalPath;

        if (steps.Count > 0)
        {
            Vector3Int from = trace.Start;
            for (int i = 0; i < steps.Count; i++)
            {
                EnemyAIDebugGizmoDrawer.DrawStep(space, from, steps[i].Position, steps[i].Direction,
                    steps[i].Action, FinalPathColor, offset);
                from = steps[i].Position;
            }
        }
        else
        {
            // BFSはセル列のみなので、高さ差から行動種別を推定します（Phase 1と同じ規則）。
            for (int i = 1; i < cells.Count; i++)
            {
                Vector3Int difference = cells[i] - cells[i - 1];
                GridPathActionType action = difference.y > 0 ? GridPathActionType.JumpUp :
                    difference.y < 0 ? GridPathActionType.MoveAndFall : GridPathActionType.Move;
                EnemyAIDebugGizmoDrawer.DrawStep(space, cells[i - 1], cells[i],
                    new Vector3Int(difference.x, 0, difference.z), action, FinalPathColor, offset);
            }
        }

        if (cells.Count > 0)
            EnemyAIDebugGizmoDrawer.DrawGoalMarker(space, cells[cells.Count - 1], FinalPathColor, offset,
                $"GOAL ({trace.FinalPathStepCount} steps, {trace.FinalCost:0.00}s)");
    }

    private static float GetLayerAlpha(int y, Options options)
    {
        if (!options.FilterByLayer || y == options.Layer)
            return 1f;
        return options.DimOtherLayers ? 0.12f : 0f;
    }

    private static Color WithAlpha(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);

    private static void EnsureStyles()
    {
        if (_nodeStyle == null)
        {
            _nodeStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 9
            };
        }

        if (_detailStyle == null)
        {
            _detailStyle = new GUIStyle(EditorStyles.helpBox)
            {
                fontSize = 11,
                alignment = TextAnchor.UpperLeft
            };
            _detailStyle.normal.textColor = Color.white;
        }
    }
}
