using System.Collections.Generic;
using UnityEngine;

/// <summary>探索履歴を記録したアルゴリズムの種類です。</summary>
public enum PathSearchAlgorithm
{
    Bfs,
    AStar,
    Dijkstra
}

/// <summary>
/// 探索中に「展開した」ノード1件の記録です。BFS・A*・Dijkstraで共通の形式を使います。
/// 候補としてopenへ追加しただけのノードは記録しません（実際に取り出して隣接を調べたノードだけ）。
/// </summary>
public readonly struct PathSearchNodeRecord
{
    /// <summary>0始まりの展開順序です。</summary>
    public int Order { get; }
    public Vector3Int Position { get; }
    public bool HasParent { get; }
    public Vector3Int ParentPosition { get; }

    /// <summary>
    /// 開始からの累積コスト（秒）。BFS=到着予想時刻、Dijkstra=累積移動時間、A*=gCost。
    /// </summary>
    public float GCost { get; }

    /// <summary>A*のヒューリスティック値です。BFS・Dijkstraでは0です。</summary>
    public float HCost { get; }

    public float FCost => GCost + HCost;

    /// <summary>親からこのノードへ到達した行動。BFSと開始ノードでは持ちません。</summary>
    public bool HasAction { get; }
    public GridPathActionType Action { get; }

    /// <summary>A*で経路中に仮想設置したBlockの位置です。</summary>
    public bool HasVirtualBlock { get; }
    public Vector3Int VirtualBlock { get; }

    public PathSearchNodeRecord(int order, Vector3Int position, Vector3Int? parent,
        float gCost, float hCost, GridPathActionType? action, Vector3Int? virtualBlock)
    {
        Order = order;
        Position = position;
        HasParent = parent.HasValue;
        ParentPosition = parent.GetValueOrDefault();
        GCost = gCost;
        HCost = hCost;
        HasAction = action.HasValue;
        Action = action.GetValueOrDefault();
        HasVirtualBlock = virtualBlock.HasValue;
        VirtualBlock = virtualBlock.GetValueOrDefault();
    }
}

/// <summary>
/// 1回の経路探索の履歴（展開ノード・最終経路・統計）です。
///
/// 探索関数は引数でこのインスタンスを受け取った場合だけ記録し、nullなら一切記録しません。
/// 記録は探索結果や探索順序に影響しない「追記のみ」の操作で、描画処理は持ちません。
/// メモリ使用量を抑えるため、記録するノード数には上限があります（ExpandedCountは上限を超えても正確に数えます）。
/// </summary>
public sealed class PathSearchTrace
{
    private readonly List<PathSearchNodeRecord> _expandedNodes = new();
    private readonly List<Vector3Int> _finalPath = new();
    private readonly List<GridPathStep> _finalSteps = new();
    private readonly int _maxRecordedNodes;

    public PathSearchTrace(PathSearchAlgorithm algorithm, string label, int maxRecordedNodes)
    {
        Algorithm = algorithm;
        Label = label;
        _maxRecordedNodes = Mathf.Max(1, maxRecordedNodes);
        CreatedAt = Time.time;
        CreatedFrame = Time.frameCount;
    }

    public PathSearchAlgorithm Algorithm { get; }

    /// <summary>呼び出し目的（Escape、Attackなど）です。</summary>
    public string Label { get; }

    public float CreatedAt { get; }
    public int CreatedFrame { get; }

    public Vector3Int Start { get; private set; }

    /// <summary>探索で狙った目標（Item位置、Player位置）。BFSの逃走探索では持ちません。</summary>
    public bool HasTarget { get; private set; }
    public Vector3Int Target { get; private set; }

    public bool IsCompleted { get; private set; }
    public bool Succeeded { get; private set; }

    /// <summary>実際に展開したノードの総数です（記録上限を超えた分も含みます）。</summary>
    public int ExpandedCount { get; private set; }

    /// <summary>記録上限に達し、展開ノードの一部を保存していない場合true。</summary>
    public bool IsTruncated => ExpandedCount > _expandedNodes.Count;

    /// <summary>展開順に並んだ展開ノードです。</summary>
    public IReadOnlyList<PathSearchNodeRecord> ExpandedNodes => _expandedNodes;

    /// <summary>最終経路のセル列です。開始セルを含みます。</summary>
    public IReadOnlyList<Vector3Int> FinalPath => _finalPath;

    /// <summary>最終経路の行動列です（A*・Dijkstraのみ。BFSは空）。</summary>
    public IReadOnlyList<GridPathStep> FinalSteps => _finalSteps;

    /// <summary>最終経路の手数です。</summary>
    public int FinalPathStepCount => Mathf.Max(0, _finalPath.Count - 1);

    /// <summary>最終経路のゴールまでの累積コスト（秒）です。</summary>
    public float FinalCost { get; private set; }

    public void BeginSearch(Vector3Int start, Vector3Int? target)
    {
        Start = start;
        HasTarget = target.HasValue;
        Target = target.GetValueOrDefault();
    }

    public void RecordExpanded(Vector3Int position, Vector3Int? parent, float gCost, float hCost,
        GridPathActionType? action = null, Vector3Int? virtualBlock = null)
    {
        int order = ExpandedCount++;
        if (_expandedNodes.Count < _maxRecordedNodes)
        {
            _expandedNodes.Add(new PathSearchNodeRecord(
                order, position, parent, gCost, hCost, action, virtualBlock));
        }
    }

    /// <summary>セル列（開始セルを含む）で最終経路を記録します。BFS用です。</summary>
    public void CompleteWithCells(IReadOnlyList<Vector3Int> pathIncludingStart, float cost)
    {
        _finalPath.Clear();
        _finalSteps.Clear();
        for (int i = 0; i < pathIncludingStart.Count; i++)
            _finalPath.Add(pathIncludingStart[i]);
        Finish(true, cost);
    }

    /// <summary>行動列（開始セルを含まない）で最終経路を記録します。A*・Dijkstra用です。</summary>
    public void CompleteWithSteps(IReadOnlyList<GridPathStep> steps, float cost)
    {
        _finalPath.Clear();
        _finalSteps.Clear();
        _finalPath.Add(Start);
        for (int i = 0; i < steps.Count; i++)
        {
            _finalSteps.Add(steps[i]);
            _finalPath.Add(steps[i].Position);
        }
        Finish(true, cost);
    }

    /// <summary>経路が見つからなかった場合に呼びます。</summary>
    public void CompleteWithoutPath()
    {
        _finalPath.Clear();
        _finalSteps.Clear();
        Finish(false, 0f);
    }

    private void Finish(bool succeeded, float cost)
    {
        IsCompleted = true;
        Succeeded = succeeded;
        FinalCost = cost;
    }
}

/// <summary>
/// 探索履歴を収集するかどうかの共有設定です。
/// EnemyAIDebugVisualizer（Editor上）が書き換え、通常ビルドでは常に収集しません。
/// </summary>
public static class PathSearchDebug
{
    /// <summary>falseの間、EnemyBrainはTraceを作らず、探索関数にはnullが渡されます。</summary>
    public static bool IsCollecting { get; set; }

    /// <summary>Enemy 1体あたりに保持するTraceの最大数です（古い順に破棄）。</summary>
    public static int MaxHistoryPerEnemy { get; set; } = 16;

    /// <summary>1 Traceあたりに記録する展開ノードの最大数です。</summary>
    public static int MaxNodesPerTrace { get; set; } = 2048;
}
