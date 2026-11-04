using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Enemyの探索履歴（PathSearchTrace）を一覧し、展開ノードを1ステップずつ再生するEditorWindowです。
/// メニュー: Tools/NEON DETONATOR/AI Debug/Search History Player
///
/// 再生はこのウィンドウが保持する「表示ノード数」を進めるだけで、探索そのものは
/// 記録時点ですでに同期的に完了しています（探索を遅延実行していません）。
/// </summary>
internal sealed class PathSearchHistoryWindow : EditorWindow
{
    private const string VisualizerMenuPath = "Tools/NEON DETONATOR/AI Debug/Create or Select Visualizer";
    private const int RecentNodeRows = 8;

    private EnemyBrain _brain;
    private bool _useSelection = true;
    private bool _followLatest = true;
    private bool _showBfs = true;
    private bool _showAStar = true;
    private bool _showDijkstra = true;

    private PathSearchTrace _trace;
    private int _visibleCount;
    private bool _isPlaying;
    private float _stepsPerSecond = 15f;
    private double _lastTickTime;
    private double _nextBrainSearchTime;
    private float _stepAccumulator;

    private readonly PathSearchTraceDrawer.Options _options = new PathSearchTraceDrawer.Options();
    private Vector2 _historyScroll;
    private Vector2 _windowScroll;

    [MenuItem("Tools/NEON DETONATOR/AI Debug/Search History Player")]
    private static void Open()
    {
        GetWindow<PathSearchHistoryWindow>("AI Search History");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        EditorApplication.update += Tick;
        Selection.selectionChanged += Repaint;
        _lastTickTime = EditorApplication.timeSinceStartup;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        EditorApplication.update -= Tick;
        Selection.selectionChanged -= Repaint;
        SceneView.RepaintAll();
    }

    // ------------------------------------------------------------------
    // 再生制御
    // ------------------------------------------------------------------

    private void Tick()
    {
        double now = EditorApplication.timeSinceStartup;
        float deltaTime = (float)(now - _lastTickTime);
        _lastTickTime = now;

        if (!EditorApplication.isPlaying)
            return;

        ResolveBrain();

        // 最新追従中は、新しい探索が記録されたら全ノード表示で切り替えます。
        if (_followLatest)
        {
            PathSearchTrace latest = FindLatestTrace();
            if (latest != _trace)
            {
                SelectTrace(latest, true);
                Repaint();
            }
        }

        if (!_isPlaying || _trace == null)
            return;

        _stepAccumulator += deltaTime * _stepsPerSecond;
        int advance = (int)_stepAccumulator;
        if (advance <= 0)
            return;

        _stepAccumulator -= advance;
        SetVisibleCount(_visibleCount + advance);
        if (_visibleCount >= _trace.ExpandedNodes.Count)
            _isPlaying = false;
        Repaint();
    }

    private void ResolveBrain()
    {
        if (_useSelection && Selection.activeGameObject != null &&
            Selection.activeGameObject.TryGetComponent(out EnemyBrain selected) && selected != _brain)
        {
            _brain = selected;
            SelectTrace(null, false);
        }

        // 未選択時の自動検索は負荷を抑えるため0.5秒ごとに限定します。
        if (_brain == null && EditorApplication.timeSinceStartup >= _nextBrainSearchTime)
        {
            _nextBrainSearchTime = EditorApplication.timeSinceStartup + 0.5;
            _brain = FindFirstObjectByType<EnemyBrain>();
        }
    }

    private PathSearchTrace FindLatestTrace()
    {
        if (_brain == null)
            return null;
        IReadOnlyList<PathSearchTrace> history = _brain.DebugSearchHistory;
        for (int i = history.Count - 1; i >= 0; i--)
            if (PassesFilter(history[i]))
                return history[i];
        return null;
    }

    private bool PassesFilter(PathSearchTrace trace)
    {
        switch (trace.Algorithm)
        {
            case PathSearchAlgorithm.Bfs: return _showBfs;
            case PathSearchAlgorithm.AStar: return _showAStar;
            case PathSearchAlgorithm.Dijkstra: return _showDijkstra;
            default: return true;
        }
    }

    private void SelectTrace(PathSearchTrace trace, bool showAll)
    {
        _trace = trace;
        _isPlaying = false;
        _stepAccumulator = 0f;
        _visibleCount = trace != null && showAll ? trace.ExpandedNodes.Count : 0;

        if (trace != null && trace.ExpandedNodes.Count > 0)
            _options.Layer = trace.Start.y;
        SceneView.RepaintAll();
    }

    private void SetVisibleCount(int count)
    {
        int max = _trace != null ? _trace.ExpandedNodes.Count : 0;
        _visibleCount = Mathf.Clamp(count, 0, max);
        SceneView.RepaintAll();
    }

    /// <summary>手動操作をしたら最新追従を止め、選択中の履歴を固定します。</summary>
    private void TakeManualControl() => _followLatest = false;

    // ------------------------------------------------------------------
    // GUI
    // ------------------------------------------------------------------

    private void OnGUI()
    {
        _windowScroll = EditorGUILayout.BeginScrollView(_windowScroll);

        if (!EditorApplication.isPlaying)
            EditorGUILayout.HelpBox("Play中に探索履歴が記録されます。", MessageType.Info);

        if (!PathSearchDebug.IsCollecting)
        {
            EditorGUILayout.HelpBox(
                "探索履歴を収集していません。シーンのEnemyAIDebugVisualizerで\n" +
                "Show Gizmos と Record Search History をONにしてください。",
                MessageType.Warning);
            if (GUILayout.Button("Create / Select Visualizer"))
                EditorApplication.ExecuteMenuItem(VisualizerMenuPath);
        }

        DrawStatusSection();
        DrawTargetSection();
        DrawHistorySection();
        DrawTraceSection();

        EditorGUILayout.EndScrollView();
    }

    /// <summary>「表示されない」ときに、どの前提条件が欠けているかを一覧で示します。</summary>
    private void DrawStatusSection()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);

        SceneView sceneView = SceneView.lastActiveSceneView;
        bool brainGizmoEnabled = !GizmoUtility.TryGetGizmoInfo(typeof(EnemyBrain), out GizmoInfo info) ||
                                 info.gizmoEnabled;

        StatusLine("Play Mode", EditorApplication.isPlaying);
        StatusLine("Visualizer in scene & Show Gizmos ON", EnemyAIDebugSettings.Enabled);
        StatusLine("Recording search history", PathSearchDebug.IsCollecting);
        StatusLine("Scene view open", sceneView != null);
        StatusLine("Scene view Gizmos button ON", sceneView != null && sceneView.drawGizmos);
        StatusLine("Gizmos menu: EnemyBrain checked", brainGizmoEnabled);
        StatusLine("Enemy found & initialized", _brain != null && _brain.DebugIsReady);
        if (_brain != null && EditorApplication.isPlaying)
        {
            EditorGUILayout.LabelField("Last bomb decision",
                _brain.DebugLastBombDecision == EnemyBrain.BombAttemptResult.None
                    ? "-"
                    : $"{_brain.DebugLastBombDecision} ({Time.time - _brain.DebugLastBombDecisionTime:0.0}s ago)");
        }
    }

    private static void StatusLine(string label, bool ok)
    {
        EditorGUILayout.LabelField(label, ok ? "✔ OK" : "✘ NG", ok ? EditorStyles.label : EditorStyles.boldLabel);
    }

    private void DrawTargetSection()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Target", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        EnemyBrain brain = (EnemyBrain)EditorGUILayout.ObjectField("Enemy", _brain, typeof(EnemyBrain), true);
        if (EditorGUI.EndChangeCheck())
        {
            _brain = brain;
            SelectTrace(null, false);
        }
        _useSelection = EditorGUILayout.Toggle("Use Hierarchy Selection", _useSelection);

        EditorGUILayout.BeginHorizontal();
        _showBfs = GUILayout.Toggle(_showBfs, "BFS", EditorStyles.miniButtonLeft);
        _showAStar = GUILayout.Toggle(_showAStar, "A*", EditorStyles.miniButtonMid);
        _showDijkstra = GUILayout.Toggle(_showDijkstra, "Dijkstra", EditorStyles.miniButtonRight);
        EditorGUILayout.EndHorizontal();

        _followLatest = EditorGUILayout.ToggleLeft("Follow Latest Search", _followLatest);
    }

    private void DrawHistorySection()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("History (newest first)", EditorStyles.boldLabel);

        if (_brain == null || !EditorApplication.isPlaying)
        {
            EditorGUILayout.LabelField("—");
            return;
        }

        IReadOnlyList<PathSearchTrace> history = _brain.DebugSearchHistory;
        _historyScroll = EditorGUILayout.BeginScrollView(_historyScroll, GUILayout.Height(150f));
        for (int i = history.Count - 1; i >= 0; i--)
        {
            PathSearchTrace trace = history[i];
            if (!PassesFilter(trace))
                continue;

            bool isSelectedItem = trace.Algorithm == PathSearchAlgorithm.Dijkstra && trace.HasTarget &&
                                  _brain.DebugHasItemTarget && trace.Target == _brain.DebugItemTargetPosition;
            string result = trace.Succeeded ? $"path {trace.FinalPathStepCount}" : "no path";
            string text =
                $"{trace.CreatedAt,7:0.00}s  [{PathSearchTraceDrawer.GetAlgorithmName(trace.Algorithm)}] " +
                $"{trace.Label}{(isSelectedItem ? " ★" : "")}  " +
                $"exp {trace.ExpandedCount}{(trace.IsTruncated ? "+" : "")}  {result}";

            bool isCurrent = trace == _trace;
            if (GUILayout.Toggle(isCurrent, text, EditorStyles.toolbarButton) && !isCurrent)
            {
                TakeManualControl();
                SelectTrace(trace, true);
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawTraceSection()
    {
        if (_trace == null)
            return;

        IReadOnlyList<PathSearchNodeRecord> nodes = _trace.ExpandedNodes;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Selected Search", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Algorithm",
            $"{PathSearchTraceDrawer.GetAlgorithmName(_trace.Algorithm)}  ({_trace.Label})");
        EditorGUILayout.LabelField("Start", Format(_trace.Start));
        EditorGUILayout.LabelField("Target", _trace.HasTarget ? Format(_trace.Target) : "nearest safe cell");
        EditorGUILayout.LabelField("Expanded Nodes",
            _trace.IsTruncated
                ? $"{_trace.ExpandedCount} (recorded {nodes.Count}, limit reached)"
                : _trace.ExpandedCount.ToString());
        EditorGUILayout.LabelField("Final Path",
            _trace.Succeeded
                ? $"{_trace.FinalPathStepCount} steps, cost {_trace.FinalCost:0.00}s → {Format(_trace.FinalPath[_trace.FinalPath.Count - 1])}"
                : "not found");

        DrawPlaybackControls(nodes.Count);
        DrawCurrentNode(nodes);
        DrawDisplayOptions(nodes);
    }

    private void DrawPlaybackControls(int nodeCount)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Playback", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("|◀ Reset"))
        {
            TakeManualControl();
            _isPlaying = false;
            SetVisibleCount(0);
        }
        if (GUILayout.Button("◀ Step"))
        {
            TakeManualControl();
            _isPlaying = false;
            SetVisibleCount(_visibleCount - 1);
        }
        if (GUILayout.Button(_isPlaying ? "❚❚ Pause" : "▶ Play"))
        {
            TakeManualControl();
            if (!_isPlaying && _visibleCount >= nodeCount)
                SetVisibleCount(0);
            _isPlaying = !_isPlaying;
            _stepAccumulator = 0f;
        }
        if (GUILayout.Button("Step ▶"))
        {
            TakeManualControl();
            _isPlaying = false;
            SetVisibleCount(_visibleCount + 1);
        }
        if (GUILayout.Button("End ▶|"))
        {
            TakeManualControl();
            _isPlaying = false;
            SetVisibleCount(nodeCount);
        }
        EditorGUILayout.EndHorizontal();

        _stepsPerSecond = EditorGUILayout.Slider("Steps / sec", _stepsPerSecond, 1f, 120f);

        EditorGUI.BeginChangeCheck();
        int step = EditorGUILayout.IntSlider("Step", _visibleCount, 0, nodeCount);
        if (EditorGUI.EndChangeCheck())
        {
            TakeManualControl();
            _isPlaying = false;
            SetVisibleCount(step);
        }
    }

    private void DrawCurrentNode(IReadOnlyList<PathSearchNodeRecord> nodes)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Recently Expanded", EditorStyles.boldLabel);

        if (_visibleCount <= 0)
        {
            EditorGUILayout.LabelField("—");
            return;
        }

        int first = Mathf.Max(0, _visibleCount - RecentNodeRows);
        for (int i = _visibleCount - 1; i >= first; i--)
        {
            PathSearchNodeRecord node = nodes[i];
            string action = node.HasAction ? node.Action.ToString() : "start";
            string parent = node.HasParent ? Format(node.ParentPosition) : "-";
            string virtualBlock = node.HasVirtualBlock ? $"  VB{Format(node.VirtualBlock)}" : "";
            EditorGUILayout.LabelField(
                $"#{node.Order}  {Format(node.Position)}  y{node.Position.y}",
                $"{PathSearchTraceDrawer.FormatCost(_trace.Algorithm, node, true)}  ← {parent} [{action}]{virtualBlock}",
                i == _visibleCount - 1 ? EditorStyles.boldLabel : EditorStyles.label);
        }
    }

    private void DrawDisplayOptions(IReadOnlyList<PathSearchNodeRecord> nodes)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Display", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        _options.ShowOrderLabels = EditorGUILayout.Toggle("Order Labels (#n, y)", _options.ShowOrderLabels);
        _options.ShowCostLabels = EditorGUILayout.Toggle(
            _trace.Algorithm == PathSearchAlgorithm.AStar ? "Cost Labels (g/h/f)" : "Cost Labels (time)",
            _options.ShowCostLabels);
        _options.ShowParentLinks = EditorGUILayout.Toggle("Parent Links", _options.ShowParentLinks);
        _options.ShowFinalPathAlways = EditorGUILayout.Toggle("Final Path Always", _options.ShowFinalPathAlways);

        GetLayerRange(nodes, out int minY, out int maxY);
        _options.FilterByLayer = EditorGUILayout.Toggle("Filter Y Layer", _options.FilterByLayer);
        using (new EditorGUI.DisabledScope(!_options.FilterByLayer))
        {
            _options.Layer = EditorGUILayout.IntSlider("Y Layer", _options.Layer, minY, Mathf.Max(minY, maxY));
            _options.DimOtherLayers = EditorGUILayout.Toggle("Dim Other Layers", _options.DimOtherLayers);
        }
        if (EditorGUI.EndChangeCheck())
            SceneView.RepaintAll();
    }

    private void GetLayerRange(IReadOnlyList<PathSearchNodeRecord> nodes, out int minY, out int maxY)
    {
        minY = _trace.Start.y;
        maxY = _trace.Start.y;
        for (int i = 0; i < nodes.Count; i++)
        {
            minY = Mathf.Min(minY, nodes[i].Position.y);
            maxY = Mathf.Max(maxY, nodes[i].Position.y);
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!EditorApplication.isPlaying || _brain == null || _trace == null)
            return;
        PathSearchTraceDrawer.Draw(_brain, _trace, _visibleCount, _options);
    }

    private static string Format(Vector3Int p) => $"({p.x},{p.y},{p.z})";
}
