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

    // 展開ノードの色: 展開順に色相を青(0.62)→橙(0.07)へ変化させます。
    private const float FirstNodeHue = 0.62f;
    private const float LastNodeHue = 0.07f;
    private const float NodeSaturation = 0.75f;
    private const float NodeValue = 1f;

    // 展開ノードの描画（サイズはセル幅に対する割合、αは不透明度の倍率）
    private const float NodeSizeInCells = 0.42f;
    private const float ParentLinkAlpha = 0.45f;
    private const float NodeFillAlpha = 0.3f;
    private const float NodeLabelHeightInNodes = 0.75f;

    // 現在のステップを示す二重枠とラベル（セル幅に対する割合）
    private const float CurrentOuterFrameInCells = 0.95f;
    private const float CurrentInnerFrameInCells = 0.9f;
    private const float CurrentLabelHeightInCells = 0.9f;

    /// <summary>探索の概要ラベルを開始セルからどれだけ上に出すか（セル単位）。</summary>
    private const float SummaryLabelHeightInCells = 2.1f;

    // 開始・目標マーカー（床から少し浮かせ、セル幅に対する割合の半径で描きます）
    private const float MarkerFloorOffset = 0.03f;
    private const float MarkerOuterRadiusInCells = 0.48f;
    private const float MarkerInnerRadiusInCells = 0.36f;

    /// <summary>最終経路をノードと重ならないよう持ち上げる高さ（セル単位）。</summary>
    private const float FinalPathHeightInCells = 0.25f;

    /// <summary>Y階層フィルタで「他の階層を薄く表示」するときの不透明度。</summary>
    private const float DimmedLayerAlpha = 0.12f;

    private const int NodeLabelFontSize = 9;
    private const int DetailLabelFontSize = 11;

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
        float nodeSize = cell * NodeSizeInCells;

        // 展開済みノード（現在のステップまで）
        for (int i = 0; i < count; i++)
        {
            PathSearchNodeRecord node = nodes[i];
            float alpha = GetLayerAlpha(node.Position.y, options);
            if (alpha <= 0f)
                continue;

            // 色は全体に対する展開順で決め、再生中も同じノードの色が変わらないようにします。
            float t = nodes.Count <= 1 ? 1f : i / (float)(nodes.Count - 1);
            Color color = Color.HSVToRGB(Mathf.Lerp(FirstNodeHue, LastNodeHue, t), NodeSaturation, NodeValue);
            Vector3 center = space.Center(node.Position);

            if (options.ShowParentLinks && node.HasParent)
            {
                Handles.color = WithAlpha(color, ParentLinkAlpha * alpha);
                Handles.DrawLine(space.Center(node.ParentPosition), center);
            }

            Handles.color = WithAlpha(color, NodeFillAlpha * alpha);
            Handles.CubeHandleCap(0, center, Quaternion.identity, nodeSize, EventType.Repaint);

            Handles.color = WithAlpha(color, alpha);
            Handles.DrawWireCube(center, Vector3.one * nodeSize);

            if (options.ShowOrderLabels || options.ShowCostLabels)
            {
                string text = options.ShowOrderLabels ? $"#{node.Order} y{node.Position.y}" : "";

                if (options.ShowCostLabels)
                    text += (text.Length > 0 ? "\n" : "") + FormatCost(trace.Algorithm, node, false);

                // VB = 経路中に仮想設置したBlock（Virtual Block）があるノード
                if (node.HasVirtualBlock)
                    text += "\n+VB";

                _nodeStyle.normal.textColor = WithAlpha(color, alpha);
                Handles.Label(center + Vector3.up * (nodeSize * NodeLabelHeightInNodes), text, _nodeStyle);
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
            Handles.DrawWireCube(center, Vector3.one * (cell * CurrentOuterFrameInCells));
            Handles.DrawWireCube(center, Vector3.one * (cell * CurrentInnerFrameInCells));

            Handles.Label(
                center + Vector3.up * (cell * CurrentLabelHeightInCells),
                $"ステップ #{current.Order}  ({current.Position.x},{current.Position.y},{current.Position.z})\n" +
                FormatCost(trace.Algorithm, current, true),
                _detailStyle);
        }

        // 探索の概要（開始セルの上）
        string result = !trace.IsCompleted ? "探索中"
            : trace.Succeeded ? $"経路 {trace.FinalPathStepCount}手"
            : "経路なし";
        string truncated = trace.IsTruncated ? "（記録上限で省略）" : "";

        Handles.Label(
            space.Center(trace.Start) + Vector3.up * (cell * SummaryLabelHeightInCells),
            $"[{GetAlgorithmName(trace.Algorithm)}] {trace.Label}\n" +
            $"展開 {count}/{trace.ExpandedCount}{truncated}  {result}",
            _detailStyle);
    }

    public static string GetAlgorithmName(PathSearchAlgorithm algorithm)
    {
        switch (algorithm)
        {
            case PathSearchAlgorithm.Bfs:
                return "BFS";

            case PathSearchAlgorithm.AStar:
                return "A*";

            case PathSearchAlgorithm.Dijkstra:
                return "Dijkstra";

            default:
                return algorithm.ToString();
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
                return verbose
                    ? $"累積時間 = {node.GCost:0.00}秒"
                    : $"{node.GCost:0.00}s";

            default:
                return verbose
                    ? $"展開順 {node.Order}、到着 = {node.GCost:0.00}秒"
                    : $"t{node.GCost:0.00}s";
        }
    }

    private static void DrawEndpoints(EnemyAIDebugGizmoDrawer.GridSpace space, PathSearchTrace trace)
    {
        float cell = space.CellSize;
        Vector3 markerOffset = Vector3.up * MarkerFloorOffset;

        Handles.color = StartColor;
        Handles.DrawWireDisc(space.Bottom(trace.Start) + markerOffset, Vector3.up, cell * MarkerOuterRadiusInCells);
        _nodeStyle.normal.textColor = StartColor;
        Handles.Label(space.Bottom(trace.Start), "開始", _nodeStyle);

        if (!trace.HasTarget)
            return;

        Handles.color = TargetColor;
        Handles.DrawWireDisc(space.Bottom(trace.Target) + markerOffset, Vector3.up, cell * MarkerOuterRadiusInCells);
        Handles.DrawWireDisc(space.Bottom(trace.Target) + markerOffset, Vector3.up, cell * MarkerInnerRadiusInCells);
        _nodeStyle.normal.textColor = TargetColor;
        Handles.Label(space.Bottom(trace.Target), $"目標 y{trace.Target.y}", _nodeStyle);
    }

    private static void DrawFinalPath(EnemyAIDebugGizmoDrawer.GridSpace space, PathSearchTrace trace)
    {
        Vector3 offset = Vector3.up * (space.CellSize * FinalPathHeightInCells);
        IReadOnlyList<GridPathStep> steps = trace.FinalSteps;
        IReadOnlyList<Vector3Int> cells = trace.FinalPath;

        if (steps.Count > 0)
        {
            Vector3Int from = trace.Start;
            for (int i = 0; i < steps.Count; i++)
            {
                EnemyAIDebugGizmoDrawer.DrawStep(
                    space,
                    from,
                    steps[i].Position,
                    steps[i].Direction,
                    steps[i].Action,
                    FinalPathColor,
                    offset);
                from = steps[i].Position;
            }
        }
        else
        {
            // BFSはセル列のみなので、高さ差から行動種別を推定します（Phase 1と同じ規則）。
            for (int i = 1; i < cells.Count; i++)
            {
                Vector3Int difference = cells[i] - cells[i - 1];
                GridPathActionType action = EnemyAIDebugGizmoDrawer.InferAction(difference);

                EnemyAIDebugGizmoDrawer.DrawStep(
                    space,
                    cells[i - 1],
                    cells[i],
                    new Vector3Int(difference.x, 0, difference.z),
                    action,
                    FinalPathColor,
                    offset);
            }
        }

        if (cells.Count > 0)
        {
            EnemyAIDebugGizmoDrawer.DrawGoalMarker(
                space,
                cells[cells.Count - 1],
                FinalPathColor,
                offset,
                $"ゴール（{trace.FinalPathStepCount}手、{trace.FinalCost:0.00}秒）");
        }
    }

    private static float GetLayerAlpha(int y, Options options)
    {
        if (!options.FilterByLayer || y == options.Layer)
            return 1f;

        return options.DimOtherLayers ? DimmedLayerAlpha : 0f;
    }

    private static Color WithAlpha(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);

    private static void EnsureStyles()
    {
        if (_nodeStyle == null)
        {
            _nodeStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = NodeLabelFontSize
            };
        }

        if (_detailStyle == null)
        {
            _detailStyle = new GUIStyle(EditorStyles.helpBox)
            {
                fontSize = DetailLabelFontSize,
                alignment = TextAnchor.UpperLeft
            };
            _detailStyle.normal.textColor = Color.white;
        }
    }
}
