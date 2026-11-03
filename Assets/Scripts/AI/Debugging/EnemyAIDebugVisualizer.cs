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

#if UNITY_EDITOR
    // Inspectorでチェックを切り替えた直後にSceneビューへ反映します。
    private void OnValidate() => UnityEditor.SceneView.RepaintAll();
#endif
}
