#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sceneビューのデバッグ描画（EnemyAIDebugGizmoDrawer）向けの読み取り専用アクセサです。
/// 判断ロジックには関与せず、Editor以外のビルドではコンパイルされません。
/// 戻り値のコレクションは参照のみを目的とし、変更してはいけません。
/// </summary>
public partial class EnemyBrain
{
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
}
#endif
