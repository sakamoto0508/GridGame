using UnityEngine;

/// <summary>
/// Enemy AIデバッグ表示（危険予測・BFS/A*/Dijkstra経路・状態）のON/OFFをInspectorで切り替えます。
///
/// シーン上に1つ配置すると、Play中のSceneビューへ描画されます（描画処理はEditor専用の
/// EnemyAIDebugGizmoDrawerが担当します）。このコンポーネントは設定値を保持するだけで、
/// Update等の処理を持たないため、ビルド時のゲームプレイや性能には影響しません。
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("NEON DETONATOR/Debug/Enemy AI Debug Visualizer")]
public sealed class EnemyAIDebugVisualizer : MonoBehaviour
{
    [Header("Master")]
    [Tooltip("すべてのAIデバッグ表示をON/OFFします。")]
    [SerializeField] private bool _showGizmos = true;

    [Header("Danger Map")]
    [Tooltip("爆発予測で危険になるセルを表示します。")]
    [SerializeField] private bool _showDangerCells = true;
    [Tooltip("各危険セルが危険になるまでの残り秒数と高さ(y)を表示します。")]
    [SerializeField] private bool _showDangerTimers = true;

    [Header("Paths")]
    [Tooltip("BFSで選択された逃走経路を表示します。")]
    [SerializeField] private bool _showEscapePath = true;
    [Tooltip("A*で選択された攻撃位置までの経路を表示します。")]
    [SerializeField] private bool _showAttackPath = true;
    [Tooltip("Dijkstraで選択されたItem取得経路を表示します。")]
    [SerializeField] private bool _showItemPath = true;

    [Header("Labels / Height")]
    [Tooltip("現在のEnemyAIStateと探索目標を表示します。")]
    [SerializeField] private bool _showStateLabel = true;
    [Tooltip("空中のセルからy=0の床まで点線を下ろし、高さを示します。")]
    [SerializeField] private bool _showHeightGuides = true;

    [Header("Filters")]
    [Tooltip("危険セルをEnemyの高さ±1の範囲だけに絞ります。")]
    [SerializeField] private bool _dangerNearEnemyHeightOnly;
    [Tooltip("Hierarchyで選択中のEnemyだけを表示します。")]
    [SerializeField] private bool _selectedEnemyOnly;

    [Header("Search History (Phase 2)")]
    [Tooltip("BFS・A*・Dijkstraの展開ノードを記録します。OFFまたはShow Gizmos OFFの間は一切収集しません。")]
    [SerializeField] private bool _recordSearchHistory = true;
    [Tooltip("Enemy 1体あたりに保持する探索履歴の最大数です。超えた分は古い順に破棄します。")]
    [SerializeField, Range(1, 64)] private int _maxHistoryPerEnemy = 16;
    [Tooltip("1回の探索で記録する展開ノードの最大数です。展開数の統計は上限を超えても正確に数えます。")]
    [SerializeField, Range(64, 8192)] private int _maxNodesPerSearch = 2048;

    public bool ShowGizmos => _showGizmos;
    public bool ShowDangerCells => _showDangerCells;
    public bool ShowDangerTimers => _showDangerTimers;
    public bool ShowEscapePath => _showEscapePath;
    public bool ShowAttackPath => _showAttackPath;
    public bool ShowItemPath => _showItemPath;
    public bool ShowStateLabel => _showStateLabel;
    public bool ShowHeightGuides => _showHeightGuides;
    public bool DangerNearEnemyHeightOnly => _dangerNearEnemyHeightOnly;
    public bool SelectedEnemyOnly => _selectedEnemyOnly;

    public bool RecordSearchHistory => _recordSearchHistory;
    public int MaxHistoryPerEnemy => _maxHistoryPerEnemy;
    public int MaxNodesPerSearch => _maxNodesPerSearch;

#if UNITY_EDITOR
    // 探索履歴の収集設定はEditor上でのみ共有設定へ反映します。ビルドでは収集は常に無効です。
    private void OnEnable() => ApplySearchHistorySettings();

    private void OnDisable() => PathSearchDebug.IsCollecting = false;

    // Inspectorでチェックを切り替えた直後にSceneビューへ反映します。
    private void OnValidate()
    {
        ApplySearchHistorySettings();
        UnityEditor.SceneView.RepaintAll();
    }

    private void ApplySearchHistorySettings()
    {
        PathSearchDebug.IsCollecting = isActiveAndEnabled && _showGizmos && _recordSearchHistory;
        PathSearchDebug.MaxHistoryPerEnemy = _maxHistoryPerEnemy;
        PathSearchDebug.MaxNodesPerTrace = _maxNodesPerSearch;
    }
#endif
}
