using UnityEditor;

/// <summary>
/// Enemy AIデバッグ表示のON/OFFをEditorPrefsへ保存し、メニューから切り替えます。
/// メニュー: Tools/NEON DETONATOR/AI Debug/
/// </summary>
internal static class EnemyAIDebugSettings
{
    private const string PrefsPrefix = "NeonDetonator.AIDebug.";
    private const string MenuRoot = "Tools/NEON DETONATOR/AI Debug/";

    private const string EnabledMenu = MenuRoot + "Enable AI Debug Gizmos";
    private const string DangerCellsMenu = MenuRoot + "Layers/Danger Cells";
    private const string DangerTimersMenu = MenuRoot + "Layers/Danger Timers (sec)";
    private const string EscapePathMenu = MenuRoot + "Layers/Escape Path (BFS)";
    private const string AttackPathMenu = MenuRoot + "Layers/Attack Path (A*)";
    private const string ItemPathMenu = MenuRoot + "Layers/Item Path (Dijkstra)";
    private const string StateLabelMenu = MenuRoot + "Layers/State and Goal";
    private const string HeightGuidesMenu = MenuRoot + "Layers/Height Guides";
    private const string NearHeightOnlyMenu = MenuRoot + "Filters/Danger Near Enemy Height Only";
    private const string SelectedOnlyMenu = MenuRoot + "Filters/Selected Enemy Only";

    public static bool Enabled => Get(nameof(Enabled), true);
    public static bool DangerCells => Get(nameof(DangerCells), true);
    public static bool DangerTimers => Get(nameof(DangerTimers), true);
    public static bool EscapePath => Get(nameof(EscapePath), true);
    public static bool AttackPath => Get(nameof(AttackPath), true);
    public static bool ItemPath => Get(nameof(ItemPath), true);
    public static bool StateLabel => Get(nameof(StateLabel), true);
    public static bool HeightGuides => Get(nameof(HeightGuides), true);
    public static bool NearHeightOnly => Get(nameof(NearHeightOnly), false);
    public static bool SelectedOnly => Get(nameof(SelectedOnly), false);

    private static bool Get(string key, bool defaultValue)
        => EditorPrefs.GetBool(PrefsPrefix + key, defaultValue);

    private static void Toggle(string key, bool defaultValue)
    {
        EditorPrefs.SetBool(PrefsPrefix + key, !Get(key, defaultValue));
        SceneView.RepaintAll();
    }

    [MenuItem(EnabledMenu, false, 0)]
    private static void ToggleEnabled() => Toggle(nameof(Enabled), true);
    [MenuItem(EnabledMenu, true)]
    private static bool ValidateEnabled() { Menu.SetChecked(EnabledMenu, Enabled); return true; }

    [MenuItem(DangerCellsMenu, false, 20)]
    private static void ToggleDangerCells() => Toggle(nameof(DangerCells), true);
    [MenuItem(DangerCellsMenu, true)]
    private static bool ValidateDangerCells() { Menu.SetChecked(DangerCellsMenu, DangerCells); return true; }

    [MenuItem(DangerTimersMenu, false, 21)]
    private static void ToggleDangerTimers() => Toggle(nameof(DangerTimers), true);
    [MenuItem(DangerTimersMenu, true)]
    private static bool ValidateDangerTimers() { Menu.SetChecked(DangerTimersMenu, DangerTimers); return true; }

    [MenuItem(EscapePathMenu, false, 22)]
    private static void ToggleEscapePath() => Toggle(nameof(EscapePath), true);
    [MenuItem(EscapePathMenu, true)]
    private static bool ValidateEscapePath() { Menu.SetChecked(EscapePathMenu, EscapePath); return true; }

    [MenuItem(AttackPathMenu, false, 23)]
    private static void ToggleAttackPath() => Toggle(nameof(AttackPath), true);
    [MenuItem(AttackPathMenu, true)]
    private static bool ValidateAttackPath() { Menu.SetChecked(AttackPathMenu, AttackPath); return true; }

    [MenuItem(ItemPathMenu, false, 24)]
    private static void ToggleItemPath() => Toggle(nameof(ItemPath), true);
    [MenuItem(ItemPathMenu, true)]
    private static bool ValidateItemPath() { Menu.SetChecked(ItemPathMenu, ItemPath); return true; }

    [MenuItem(StateLabelMenu, false, 25)]
    private static void ToggleStateLabel() => Toggle(nameof(StateLabel), true);
    [MenuItem(StateLabelMenu, true)]
    private static bool ValidateStateLabel() { Menu.SetChecked(StateLabelMenu, StateLabel); return true; }

    [MenuItem(HeightGuidesMenu, false, 26)]
    private static void ToggleHeightGuides() => Toggle(nameof(HeightGuides), true);
    [MenuItem(HeightGuidesMenu, true)]
    private static bool ValidateHeightGuides() { Menu.SetChecked(HeightGuidesMenu, HeightGuides); return true; }

    [MenuItem(NearHeightOnlyMenu, false, 40)]
    private static void ToggleNearHeightOnly() => Toggle(nameof(NearHeightOnly), false);
    [MenuItem(NearHeightOnlyMenu, true)]
    private static bool ValidateNearHeightOnly() { Menu.SetChecked(NearHeightOnlyMenu, NearHeightOnly); return true; }

    [MenuItem(SelectedOnlyMenu, false, 41)]
    private static void ToggleSelectedOnly() => Toggle(nameof(SelectedOnly), false);
    [MenuItem(SelectedOnlyMenu, true)]
    private static bool ValidateSelectedOnly() { Menu.SetChecked(SelectedOnlyMenu, SelectedOnly); return true; }
}
