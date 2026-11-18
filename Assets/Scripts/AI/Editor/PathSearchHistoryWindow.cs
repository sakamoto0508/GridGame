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

    /// <summary>「最近展開したノード」欄に表示する行数です。</summary>
    private const int RecentNodeRows = 8;

    /// <summary>Enemy未選択時に自動検索する間隔（秒）。毎フレーム検索しないよう負荷を抑えます。</summary>
    private const double BrainSearchIntervalSeconds = 0.5;

    /// <summary>履歴一覧のスクロール領域の高さ（ピクセル）です。</summary>
    private const float HistoryListHeight = 150f;

    // 再生速度（1秒あたりの展開ステップ数）
    private const float DefaultStepsPerSecond = 15f;
    private const float MinStepsPerSecond = 1f;
    private const float MaxStepsPerSecond = 120f;

    private readonly PathSearchTraceDrawer.Options _options = new PathSearchTraceDrawer.Options();

    private EnemyBrain _brain;
    private bool _useSelection = true;
    private bool _followLatest = true;
    private bool _showBfs = true;
    private bool _showAStar = true;
    private bool _showDijkstra = true;

    private PathSearchTrace _trace;
    private int _visibleCount;
    private bool _isPlaying;
    private float _stepsPerSecond = DefaultStepsPerSecond;
    private double _lastTickTime;
    private double _nextBrainSearchTime;
    private float _stepAccumulator;

    private Vector2 _historyScroll;
    private Vector2 _windowScroll;

    [MenuItem("Tools/NEON DETONATOR/AI Debug/Search History Player")]
    private static void Open()
    {
        GetWindow<PathSearchHistoryWindow>("AI探索履歴");
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

        // 経過時間×速度を貯め、整数分だけステップを進めます。
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
            _nextBrainSearchTime = EditorApplication.timeSinceStartup + BrainSearchIntervalSeconds;
            _brain = FindFirstObjectByType<EnemyBrain>();
        }
    }

    private PathSearchTrace FindLatestTrace()
    {
        if (_brain == null)
            return null;

        IReadOnlyList<PathSearchTrace> history = _brain.DebugSearchHistory;
        for (int i = history.Count - 1; i >= 0; i--)
        {
            if (PassesFilter(history[i]))
                return history[i];
        }

        return null;
    }

    private bool PassesFilter(PathSearchTrace trace)
    {
        switch (trace.Algorithm)
        {
            case PathSearchAlgorithm.Bfs:
                return _showBfs;

            case PathSearchAlgorithm.AStar:
                return _showAStar;

            case PathSearchAlgorithm.Dijkstra:
                return _showDijkstra;

            default:
                return true;
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
    // 探索履歴ウィンドウの描画
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

            if (GUILayout.Button("Visualizerを作成 / 選択"))
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
        EditorGUILayout.LabelField("状態チェック", EditorStyles.boldLabel);

        SceneView sceneView = SceneView.lastActiveSceneView;
        bool brainGizmoEnabled =
            !GizmoUtility.TryGetGizmoInfo(typeof(EnemyBrain), out GizmoInfo info) ||
            info.gizmoEnabled;

        StatusLine("Play中", EditorApplication.isPlaying);
        StatusLine("シーンにVisualizerがあり Show Gizmos がON", EnemyAIDebugSettings.Enabled);
        StatusLine("探索履歴を記録中", PathSearchDebug.IsCollecting);
        StatusLine("Sceneビューが開いている", sceneView != null);
        StatusLine("SceneビューのGizmosボタンがON", sceneView != null && sceneView.drawGizmos);
        StatusLine("GizmosメニューでEnemyBrainにチェック", brainGizmoEnabled);
        StatusLine("Enemyが見つかり初期化済み", _brain != null && _brain.DebugIsReady);

        if (_brain != null && EditorApplication.isPlaying)
        {
            string lastDecision = _brain.DebugLastBombDecision == EnemyBrain.BombAttemptResult.None
                ? "-"
                : $"{_brain.DebugLastBombDecision}（{Time.time - _brain.DebugLastBombDecisionTime:0.0}秒前）";

            EditorGUILayout.LabelField("直近のBomb設置判定", lastDecision);
        }
    }

    private static void StatusLine(string label, bool ok)
    {
        EditorGUILayout.LabelField(
            label,
            ok ? "✔ OK" : "✘ NG",
            ok ? EditorStyles.label : EditorStyles.boldLabel);
    }

    private void DrawTargetSection()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("対象", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        EnemyBrain brain = (EnemyBrain)EditorGUILayout.ObjectField("Enemy", _brain, typeof(EnemyBrain), true);
        if (EditorGUI.EndChangeCheck())
        {
            _brain = brain;
            SelectTrace(null, false);
        }

        _useSelection = EditorGUILayout.Toggle("Hierarchyの選択を使う", _useSelection);

        EditorGUILayout.BeginHorizontal();
        _showBfs = GUILayout.Toggle(_showBfs, "BFS", EditorStyles.miniButtonLeft);
        _showAStar = GUILayout.Toggle(_showAStar, "A*", EditorStyles.miniButtonMid);
        _showDijkstra = GUILayout.Toggle(_showDijkstra, "Dijkstra", EditorStyles.miniButtonRight);
        EditorGUILayout.EndHorizontal();

        _followLatest = EditorGUILayout.ToggleLeft("最新の探索に追従", _followLatest);
    }

    private void DrawHistorySection()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("履歴（新しい順）", EditorStyles.boldLabel);

        if (_brain == null || !EditorApplication.isPlaying)
        {
            EditorGUILayout.LabelField("—");
            return;
        }

        IReadOnlyList<PathSearchTrace> history = _brain.DebugSearchHistory;
        _historyScroll = EditorGUILayout.BeginScrollView(_historyScroll, GUILayout.Height(HistoryListHeight));

        for (int i = history.Count - 1; i >= 0; i--)
        {
            PathSearchTrace trace = history[i];
            if (!PassesFilter(trace))
                continue;

            // 現在追っているItemへのDijkstra探索には★を付けます。
            bool isSelectedItem =
                trace.Algorithm == PathSearchAlgorithm.Dijkstra &&
                trace.HasTarget &&
                _brain.DebugHasItemTarget &&
                trace.Target == _brain.DebugItemTargetPosition;

            string result = trace.Succeeded ? $"経路 {trace.FinalPathStepCount}手" : "経路なし";
            string text =
                $"{trace.CreatedAt,7:0.00}s  [{PathSearchTraceDrawer.GetAlgorithmName(trace.Algorithm)}] " +
                $"{trace.Label}{(isSelectedItem ? " ★" : "")}  " +
                $"展開 {trace.ExpandedCount}{(trace.IsTruncated ? "+" : "")}  {result}";

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
        EditorGUILayout.LabelField("選択中の探索", EditorStyles.boldLabel);

        EditorGUILayout.LabelField(
            "アルゴリズム",
            $"{PathSearchTraceDrawer.GetAlgorithmName(_trace.Algorithm)}  ({_trace.Label})");

        EditorGUILayout.LabelField("開始", EnemyAIDebugGizmoDrawer.Format(_trace.Start));

        EditorGUILayout.LabelField("目標", _trace.HasTarget ? EnemyAIDebugGizmoDrawer.Format(_trace.Target) : "最寄りの安全セル");

        EditorGUILayout.LabelField(
            "展開ノード数",
            _trace.IsTruncated
                ? $"{_trace.ExpandedCount}（記録 {nodes.Count}、上限に到達）"
                : _trace.ExpandedCount.ToString());

        string finalPath = _trace.Succeeded
            ? $"{_trace.FinalPathStepCount}手、コスト {_trace.FinalCost:0.00}秒 → {EnemyAIDebugGizmoDrawer.Format(_trace.FinalPath[_trace.FinalPath.Count - 1])}"
            : "見つかりませんでした";
        EditorGUILayout.LabelField("最終経路", finalPath);

        DrawPlaybackControls(nodes.Count);
        DrawCurrentNode(nodes);
        DrawDisplayOptions(nodes);
    }

    private void DrawPlaybackControls(int nodeCount)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("再生", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("|◀ 最初へ"))
        {
            TakeManualControl();
            _isPlaying = false;
            SetVisibleCount(0);
        }

        if (GUILayout.Button("◀ 1つ戻る"))
        {
            TakeManualControl();
            _isPlaying = false;
            SetVisibleCount(_visibleCount - 1);
        }

        if (GUILayout.Button(_isPlaying ? "❚❚ 一時停止" : "▶ 再生"))
        {
            TakeManualControl();

            // 最後まで表示済みなら、最初から再生し直します。
            if (!_isPlaying && _visibleCount >= nodeCount)
                SetVisibleCount(0);

            _isPlaying = !_isPlaying;
            _stepAccumulator = 0f;
        }

        if (GUILayout.Button("1つ進む ▶"))
        {
            TakeManualControl();
            _isPlaying = false;
            SetVisibleCount(_visibleCount + 1);
        }

        if (GUILayout.Button("最後へ ▶|"))
        {
            TakeManualControl();
            _isPlaying = false;
            SetVisibleCount(nodeCount);
        }

        EditorGUILayout.EndHorizontal();

        _stepsPerSecond = EditorGUILayout.Slider(
            "再生速度（ステップ/秒）",
            _stepsPerSecond,
            MinStepsPerSecond,
            MaxStepsPerSecond);

        EditorGUI.BeginChangeCheck();
        int step = EditorGUILayout.IntSlider("ステップ", _visibleCount, 0, nodeCount);
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
        EditorGUILayout.LabelField("最近展開したノード", EditorStyles.boldLabel);

        if (_visibleCount <= 0)
        {
            EditorGUILayout.LabelField("—");
            return;
        }

        int first = Mathf.Max(0, _visibleCount - RecentNodeRows);

        for (int i = _visibleCount - 1; i >= first; i--)
        {
            PathSearchNodeRecord node = nodes[i];
            string action = node.HasAction ? node.Action.ToString() : "開始";
            string parent = node.HasParent ? EnemyAIDebugGizmoDrawer.Format(node.ParentPosition) : "-";

            // VB = 経路中に仮想設置したBlock（Virtual Block）
            string virtualBlock = node.HasVirtualBlock ? $"  VB{EnemyAIDebugGizmoDrawer.Format(node.VirtualBlock)}" : "";

            EditorGUILayout.LabelField(
                $"#{node.Order}  {EnemyAIDebugGizmoDrawer.Format(node.Position)}  y{node.Position.y}",
                $"{PathSearchTraceDrawer.FormatCost(_trace.Algorithm, node, true)}  ← {parent} [{action}]{virtualBlock}",
                i == _visibleCount - 1 ? EditorStyles.boldLabel : EditorStyles.label);
        }
    }

    private void DrawDisplayOptions(IReadOnlyList<PathSearchNodeRecord> nodes)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("表示", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        _options.ShowOrderLabels = EditorGUILayout.Toggle("展開順ラベル (#n, y)", _options.ShowOrderLabels);
        _options.ShowCostLabels = EditorGUILayout.Toggle(
            _trace.Algorithm == PathSearchAlgorithm.AStar ? "コストラベル (g/h/f)" : "コストラベル (時間)",
            _options.ShowCostLabels);
        _options.ShowParentLinks = EditorGUILayout.Toggle("親ノードへの線", _options.ShowParentLinks);
        _options.ShowFinalPathAlways = EditorGUILayout.Toggle("最終経路を常に表示", _options.ShowFinalPathAlways);

        GetLayerRange(nodes, out int minY, out int maxY);
        _options.FilterByLayer = EditorGUILayout.Toggle("Y階層で絞り込む", _options.FilterByLayer);

        using (new EditorGUI.DisabledScope(!_options.FilterByLayer))
        {
            _options.Layer = EditorGUILayout.IntSlider("Y階層", _options.Layer, minY, Mathf.Max(minY, maxY));
            _options.DimOtherLayers = EditorGUILayout.Toggle("他の階層を薄く表示", _options.DimOtherLayers);
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
}
