using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sceneビューのデバッグ描画（EnemyAIDebugGizmoDrawer / PathSearchHistoryWindow）向けの機能です。
/// 判断ロジックには関与しません。アクセサはEditor以外のビルドではコンパイルされず、
/// DebugBeginSearchTraceはビルドでは常にnullを返します。
/// 戻り値のコレクションは参照のみを目的とし、変更してはいけません。
/// </summary>
public partial class EnemyBrain
{
    /// <summary>
    /// 探索履歴の収集が有効な場合だけTraceを作成して履歴へ追加し、探索関数へ渡します。
    /// 無効時はnullを返すため、探索関数は記録処理を一切行いません。
    /// </summary>
    private PathSearchTrace DebugBeginSearchTrace(PathSearchAlgorithm algorithm, string label)
    {
#if UNITY_EDITOR
        if (!PathSearchDebug.IsCollecting)
            return null;

        var trace = new PathSearchTrace(algorithm, label, PathSearchDebug.MaxNodesPerTrace);

        // 保持数の上限を超えた分は古い順に破棄し、メモリ使用量を一定に保ちます。
        int maxHistory = Mathf.Max(1, PathSearchDebug.MaxHistoryPerEnemy);
        while (_debugSearchHistory.Count >= maxHistory)
            _debugSearchHistory.RemoveAt(0);

        _debugSearchHistory.Add(trace);
        return trace;
#else
        return null;
#endif
    }

    /// <summary>Bomb設置判断の結果（デバッグ表示用）。</summary>
    public enum DebugBombDecision
    {
        None,
        Placed,
        MaxBombCount,
        ChanceFailed,
        NoEscapeRoute,
        PlaceFailed
    }

    /// <summary>
    /// Bombを置いた／置かなかった理由を記録します。Editor以外では呼び出しごと削除されます。
    /// デバッグ収集が有効な場合は、理由または位置が変わったときだけConsoleへ出力します。
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void DebugRecordBombDecision(DebugBombDecision decision, PathSearchTrace escapeTrace)
    {
#if UNITY_EDITOR
        Vector3Int position = _movement.CurrentGridPosition;
        bool changed = decision != _debugBombDecision || position != _debugBombDecisionPosition;
        _debugBombDecision = decision;
        _debugBombDecisionPosition = position;
        _debugBombDecisionTime = Time.time;

        if (!changed || !PathSearchDebug.IsCollecting)
            return;

        string detail = "";
        if (escapeTrace != null)
        {
            detail = $" (escape BFS expanded {escapeTrace.ExpandedCount}";
            if (decision == DebugBombDecision.NoEscapeRoute)
            {
                var cells = new System.Text.StringBuilder();
                int shown = Mathf.Min(escapeTrace.ExpandedNodes.Count, 12);
                for (int i = 0; i < shown; i++)
                    cells.Append(escapeTrace.ExpandedNodes[i].Position).Append(' ');
                detail += $": {cells}";
            }
            detail += ")";
        }

        Vector3Int player = _playerMovement != null ? _playerMovement.CurrentGridPosition : default;
        Debug.Log($"[AI Debug] Bomb decision at {position} (player {player}): {decision}{detail}", this);
#endif
    }

#if UNITY_EDITOR
    private DebugBombDecision _debugBombDecision;
    private Vector3Int _debugBombDecisionPosition;
    private float _debugBombDecisionTime;

    /// <summary>直近のBomb設置判断の結果です。</summary>
    public DebugBombDecision DebugLastBombDecision => _debugBombDecision;
    public float DebugLastBombDecisionTime => _debugBombDecisionTime;

    private readonly List<PathSearchTrace> _debugSearchHistory = new();

    /// <summary>探索履歴（古い順）。保持数はPathSearchDebug.MaxHistoryPerEnemyまでです。</summary>
    public IReadOnlyList<PathSearchTrace> DebugSearchHistory => _debugSearchHistory;

    /// <summary>Init済みで、描画に必要な参照がそろっているか。</summary>
    public bool DebugIsReady => _isInitialized && _gridManager != null && _movement != null;

    public GridManager DebugGridManager => _gridManager;

    /// <summary>MovementComponentが保持する現在の論理グリッド座標です。</summary>
    public Vector3Int DebugCurrentPosition => _movement.CurrentGridPosition;

    /// <summary>最後の思考時にRebuildした危険予測（Rebuild時点からの相対秒）。</summary>
    public IReadOnlyDictionary<Vector3Int, float> DebugDangerTimes => _dangerMap.DangerTimes;

    /// <summary>DebugDangerTimesの基準となるRebuild時刻（Time.time）。</summary>
    public float DebugDangerMapBuiltAt => _dangerMap.DebugBuiltAt;

    /// <summary>BFSで選んだ逃走経路。開始セルは含まず、これから通るセルだけを持ちます。</summary>
    public IReadOnlyList<Vector3Int> DebugEscapePath => _escapePath;

    /// <summary>A*で選んだ攻撃位置までの残り行動列です。</summary>
    public IReadOnlyList<GridPathStep> DebugAttackPath => _attackPath;

    public bool DebugHasAttackPlan => _hasAttackPlan;

    /// <summary>攻撃経路を計算した時点のPlayer位置です。</summary>
    public Vector3Int DebugPlannedPlayerPosition => _plannedPlayerPosition;

    /// <summary>Block設置済みで、次の思考でジャンプする待機中か。</summary>
    public bool DebugIsWaitingForPlacedBlockJump => _isWaitingForPlacedBlockJump;

    /// <summary>Dijkstraで選んだItem取得までの残り行動列です。</summary>
    public IReadOnlyList<GridPathStep> DebugItemPath => _itemPath;

    public bool DebugHasItemTarget => _itemTarget != null;

    public Vector3Int DebugItemTargetPosition => _itemTargetPosition;
#endif
}
