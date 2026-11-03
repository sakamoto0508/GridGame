using UnityEditor;
using UnityEngine;

/// <summary>
/// シーン上のEnemyAIDebugVisualizer（Inspectorのbool設定）を描画側へ渡します。
/// Visualizerがシーンにない、または無効な場合は何も描画しません。
/// </summary>
internal static class EnemyAIDebugSettings
{
    private static EnemyAIDebugVisualizer _cached;

    private static EnemyAIDebugVisualizer Current
    {
        get
        {
            if (_cached == null)
                _cached = Object.FindFirstObjectByType<EnemyAIDebugVisualizer>();
            return _cached;
        }
    }

    public static bool Enabled
    {
        get
        {
            EnemyAIDebugVisualizer visualizer = Current;
            return visualizer != null && visualizer.isActiveAndEnabled && visualizer.ShowGizmos;
        }
    }

    public static bool DangerCells => Current.ShowDangerCells;
    public static bool DangerTimers => Current.ShowDangerTimers;
    public static bool EscapePath => Current.ShowEscapePath;
    public static bool AttackPath => Current.ShowAttackPath;
    public static bool ItemPath => Current.ShowItemPath;
    public static bool StateLabel => Current.ShowStateLabel;
    public static bool HeightGuides => Current.ShowHeightGuides;
    public static bool NearHeightOnly => Current.DangerNearEnemyHeightOnly;
    public static bool SelectedOnly => Current.SelectedEnemyOnly;

    /// <summary>シーンにVisualizerを1つ作成して選択します（既にあればそれを選択）。</summary>
    [MenuItem("Tools/NEON DETONATOR/AI Debug/Create or Select Visualizer")]
    private static void CreateOrSelectVisualizer()
    {
        EnemyAIDebugVisualizer visualizer =
            Object.FindFirstObjectByType<EnemyAIDebugVisualizer>(FindObjectsInactive.Include);

        if (visualizer == null)
        {
            var go = new GameObject("EnemyAIDebugVisualizer");
            Undo.RegisterCreatedObjectUndo(go, "Create Enemy AI Debug Visualizer");
            visualizer = go.AddComponent<EnemyAIDebugVisualizer>();
        }

        _cached = visualizer;
        Selection.activeGameObject = visualizer.gameObject;
        EditorGUIUtility.PingObject(visualizer.gameObject);
    }
}
