#if UNITY_EDITOR
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 編集時に既存UIをテーマ化します。Play中にUIを生成せず、結果をSceneへ保存できます。
/// 名前検索ではなくMatchSetupUI/GameHudの参照を使い、Buttonイベントは変更しません。
/// </summary>
public static class CyberpunkUIThemeInstaller
{
    private const string ThemePath = "Assets/Settings/UI/CyberpunkUITheme.asset";

    [MenuItem("Tools/NEON DETONATOR/UI/Apply Layout and Sprites")]
    public static void InstallNeon() => Install();

    private static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("UIテーマはPlayを停止してから適用してください。");
            return;
        }
        Scene scene = SceneManager.GetActiveScene();
        var setups = FindInScene<MatchSetupUI>(scene);
        var huds = FindInScene<GameHud>(scene);
        if (setups.Length != 1 || huds.Length != 1)
        {
            Debug.LogError("対象SceneにはMatchSetupUIとGameHudを1個ずつ配置してください。");
            return;
        }
        // 参照不備では何も変更せず終了します。
        SerializedObject setup = new SerializedObject(setups[0]);
        SerializedObject hud = new SerializedObject(huds[0]);
        GameObject setupPanel = Ref<GameObject>(setup, "_setupPanel");
        GameObject playingPanel = Ref<GameObject>(setup, "_playingPanel");
        GameObject resultPanel = Ref<GameObject>(hud, "_resultPanel");
        TMP_Dropdown dropdown = Ref<TMP_Dropdown>(setup, "_difficultyDropdown");
        Button start = Ref<Button>(setup, "_startButton");
        Button retry = Ref<Button>(hud, "_restartButton");
        Button back = Ref<Button>(hud, "_returnToSetupButton");
        TMP_Text controls = Ref<TMP_Text>(setup, "_controlsText");
        TMP_Text error = Ref<TMP_Text>(setup, "_errorText");
        TMP_Text alive = Ref<TMP_Text>(hud, "_aliveCountText");
        TMP_Text status = Ref<TMP_Text>(hud, "_playerStatusText");
        TMP_Text result = Ref<TMP_Text>(hud, "_resultText");
        UnityEngine.Object[] required = { setupPanel, playingPanel, resultPanel, start, retry, back,
            controls, error, alive, status, result };
        if (required.Any(value => value == null))
        {
            Debug.LogError("MatchSetupUIとGameHudのPanel/Text/Button参照を全て設定してください。");
            return;
        }
        Canvas canvas = setupPanel.GetComponentInParent<Canvas>(true);
        if (canvas == null || playingPanel.GetComponentInParent<Canvas>(true) != canvas ||
            resultPanel.GetComponentInParent<Canvas>(true) != canvas)
        {
            Debug.LogError("Setup/Playing/Result Panelは同じCanvasの下に配置してください。");
            return;
        }
        if (!EditorUtility.DisplayDialog("NEON DETONATOR Layout",
            "既存UIの配色・サイズ・配置を変更し、見出しとHUD枠を追加します。\n" +
            "ゲーム処理・Buttonイベント・音源・3D素材は変更しません。適用後はUndoで戻せます。",
            "適用", "キャンセル")) return;

        EnsureFolder("Assets/Settings/UI");
        CyberpunkUITheme theme = AssetDatabase.LoadAssetAtPath<CyberpunkUITheme>(ThemePath);
        if (theme == null)
        {
            theme = ScriptableObject.CreateInstance<CyberpunkUITheme>();
            AssetDatabase.CreateAsset(theme, ThemePath);
        }
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply Cyberpunk UI Theme");
        try
        {
            // 旧版の独自Graphicを先に修復。RequireComponent追加だけでは既存Sceneは直りません。
            RepairRenderers(canvas.gameObject);
            Undo.RecordObject(theme, "Use NEON layout sizes");
            theme.UseNeonLayout();
            NeonUISpriteAssets.Assign(theme);
            EditorUtility.SetDirty(theme);
            CanvasScaler scaler = GetOrAdd<CanvasScaler>(canvas.gameObject);
            Undo.RecordObject(scaler, "Scale UI");
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = theme.ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // 各PanelはCanvas直下に揃え、非表示状態が互いに伝播しないようにします。
            Parent(setupPanel.transform, canvas.transform);
            Parent(playingPanel.transform, canvas.transform);
            Parent(resultPanel.transform, canvas.transform);
            Layout(setupPanel.transform, new Vector2(0.5f, 0.5f), Vector2.zero, theme.SetupSize);
            Panel(setupPanel, theme, CyberpunkUIColor.Primary);
            GameObject backdrop = EnsureRect(setupPanel.transform, "NeonBackdrop");
            GetOrAdd<CanvasRenderer>(backdrop);
            Image backdropImage = GetOrAdd<Image>(backdrop);
            Undo.RecordObject(backdropImage, "Setup backdrop");
            backdropImage.sprite = theme.SetupBackground;
            backdropImage.color = theme.SetupBackgroundTint;
            backdropImage.raycastTarget = false;
            backdropImage.enabled = theme.SetupBackground != null;
            Stretch((RectTransform)backdrop.transform, 24);
            Undo.RecordObject(backdrop.transform, "Place backdrop behind text");
            backdrop.transform.SetAsFirstSibling();
            Stretch((RectTransform)playingPanel.transform);
            Image playingBackground = playingPanel.GetComponent<Image>();
            if (playingBackground != null) { Undo.RecordObject(playingBackground, "Hide HUD background"); playingBackground.enabled = false; }
            GameObject screenFrame = EnsureRect(playingPanel.transform, "NeonScreenFrame");
            Stretch((RectTransform)screenFrame.transform, 12);
            Panel(screenFrame, theme, CyberpunkUIColor.Primary);
            Style(screenFrame, theme, CyberpunkUIColor.Primary, outlineOnly: true);
            Undo.RecordObject(screenFrame.transform, "Place border behind HUD");
            screenFrame.transform.SetAsFirstSibling();
            Layout(resultPanel.transform, new Vector2(0.5f, 0.5f), Vector2.zero, theme.ResultSize);
            Panel(resultPanel, theme, CyberpunkUIColor.Success);

            TMP_Text title = EnsureText(setupPanel.transform, "CyberpunkTitle", controls.font);
            title.text = theme.Title;
            TextStyle(title, theme, CyberpunkUIColor.Text, theme.TitleSize, true);
            PlaceInPanel(title.transform, theme.SetupSize, new Vector2(0.345f, 0.80f), new Vector2(0.53f, 0.22f));
            title.alignment = TextAlignmentOptions.MidlineLeft;
            TMP_Text subtitle = EnsureText(setupPanel.transform, "NeonJapaneseTitle", controls.font);
            subtitle.text = theme.JapaneseTitle;
            TextStyle(subtitle, theme, CyberpunkUIColor.Text, theme.SmallSize);
            PlaceInPanel(subtitle.transform, theme.SetupSize, new Vector2(0.345f, 0.65f), new Vector2(0.53f, 0.045f));
            subtitle.alignment = TextAlignmentOptions.MidlineLeft;
            TMP_Text category = EnsureText(setupPanel.transform, "NeonCategory", controls.font);
            category.text = "3D GRID ACTION";
            TextStyle(category, theme, CyberpunkUIColor.Warning, theme.SmallSize);
            PlaceInPanel(category.transform, theme.SetupSize, new Vector2(0.28f, 0.925f), new Vector2(0.44f, 0.05f));
            category.alignment = TextAlignmentOptions.MidlineLeft;
            SetActive(category.gameObject, false);
            GameObject separator = EnsureRect(setupPanel.transform, "NeonColumnDivider");
            Image separatorImage = GetOrAdd<Image>(separator);
            Undo.RecordObject(separatorImage, "Style divider");
            separatorImage.raycastTarget = false;
            Layout(separator.transform, new Vector2(0.54f, 0.5f), Vector2.zero, new Vector2(theme.BorderWidth, theme.SetupSize.y * 0.78f));
            Style(separator, theme, CyberpunkUIColor.Muted);
            SetActive(separator, false);
            GameObject titleLine = EnsureRect(setupPanel.transform, "NeonTitleUnderline");
            GetOrAdd<CanvasRenderer>(titleLine);
            Image titleLineImage = GetOrAdd<Image>(titleLine);
            Undo.RecordObject(titleLineImage, "Title underline");
            titleLineImage.raycastTarget = false;
            PlaceInPanel(titleLine.transform, theme.SetupSize, new Vector2(0.225f, 0.615f), new Vector2(0.29f, 0.004f));
            Style(titleLine, theme, CyberpunkUIColor.Success);
            TMP_Text difficultyLabel = EnsureText(setupPanel.transform, "CyberpunkDifficultyLabel", controls.font);
            difficultyLabel.text = "難易度を選択";
            TextStyle(difficultyLabel, theme, CyberpunkUIColor.Text, theme.BodySize);
            PlaceInPanel(difficultyLabel.transform, theme.SetupSize, new Vector2(0.5f, 0.535f), new Vector2(0.84f, 0.06f));
            difficultyLabel.alignment = TextAlignmentOptions.MidlineLeft;
            // 旧DropdownはUndoで戻せるよう削除せず非表示にし、新しい3ボタンを事前配置します。
            if (dropdown != null) SetActive(dropdown.gameObject, false);
            Button[] difficultyButtons = new Button[3];
            string[] difficultyNames = { "EASY", "NORMAL", "HARD" };
            string[] difficultyFields = { "_easyButton", "_normalButton", "_hardButton" };
            for (int i = 0; i < 3; i++)
            {
                Button option = EnsureButton(setupPanel.transform, "NeonDifficulty" + difficultyNames[i], controls.font);
                difficultyButtons[i] = option;
                PlaceInPanel(option.transform, theme.SetupSize, new Vector2(0.215f + i * 0.285f, 0.415f), new Vector2(0.27f, 0.12f));
                bool selected = Ref<GridBomberGameMode>(setup, "_gameMode")?.SelectedDifficulty == (EnemyDifficulty)i;
                ButtonStyle(option, theme, selected ? CyberpunkUIColor.Success : CyberpunkUIColor.Muted, false, difficultyNames[i]);
                setup.FindProperty(difficultyFields[i]).objectReferenceValue = option;
            }
            // 標準UI入力の左右でフォーカス、Submitで確定。上下でSTARTへ移動できます。
            for (int i = 0; i < 3; i++)
            {
                Undo.RecordObject(difficultyButtons[i], "Difficulty navigation");
                Navigation navigation = difficultyButtons[i].navigation;
                navigation.mode = Navigation.Mode.Explicit;
                navigation.selectOnLeft = difficultyButtons[Mathf.Max(0, i - 1)];
                navigation.selectOnRight = difficultyButtons[Mathf.Min(2, i + 1)];
                navigation.selectOnDown = start;
                difficultyButtons[i].navigation = navigation;
            }
            setup.FindProperty("_theme").objectReferenceValue = theme;
            setup.ApplyModifiedProperties();
            Undo.RecordObject(start, "Start navigation");
            Navigation startNavigation = start.navigation;
            startNavigation.mode = Navigation.Mode.Explicit;
            startNavigation.selectOnUp = difficultyButtons[1];
            startNavigation.selectOnLeft = null;
            startNavigation.selectOnRight = null;
            startNavigation.selectOnDown = null;
            start.navigation = startNavigation;
            Parent(start.transform, setupPanel.transform);
            PlaceInPanel(start.transform, theme.SetupSize, new Vector2(0.5f, 0.17f), new Vector2(0.52f, 0.14f));
            ButtonStyle(start, theme, CyberpunkUIColor.Success, true, "START   >");
            TMP_Text menuHint = EnsureText(setupPanel.transform, "NeonMenuHint", controls.font);
            menuHint.text = "A / D  DIFFICULTY     W / S  FOCUS     ENTER  START";
            TextStyle(menuHint, theme, CyberpunkUIColor.Muted, theme.SmallSize);
            PlaceInPanel(menuHint.transform, theme.SetupSize, new Vector2(0.5f, 0.055f), new Vector2(0.84f, 0.045f));
            GameObject controlsCard = EnsureRect(setupPanel.transform, "NeonControlsCard");
            Vector2 controlsSize = Vector2.Scale(theme.SetupSize, new Vector2(0.28f, 0.31f));
            PlaceInPanel(controlsCard.transform, theme.SetupSize, new Vector2(0.79f, 0.78f), new Vector2(0.28f, 0.31f));
            Panel(controlsCard, theme, CyberpunkUIColor.Primary);
            // 見本の右上余白を操作説明に使い、大きな説明枠は表示しません。
            Image controlsBackground = controlsCard.GetComponent<Image>();
            Undo.RecordObject(controlsBackground, "Hide controls box");
            controlsBackground.enabled = false;
            controlsCard.GetComponent<NeonUIGlow>()?.Refresh();
            TMP_Text controlsHeader = EnsureText(controlsCard.transform, "Heading", controls.font);
            controlsHeader.text = "HOW TO PLAY";
            TextStyle(controlsHeader, theme, CyberpunkUIColor.Primary, theme.SmallSize);
            PlaceInPanel(controlsHeader.transform, controlsSize, new Vector2(0.5f, 0.86f), new Vector2(0.86f, 0.16f));
            Parent(controls.transform, controlsCard.transform);
            PlaceInPanel(controls.transform, controlsSize, new Vector2(0.5f, 0.42f), new Vector2(0.86f, 0.64f));
            SetActive(controls.gameObject, true);
            MatchSetupSettings setupSettings = Ref<MatchSetupSettings>(setup, "_settings");
            Undo.RecordObject(controls, "Preview controls");
            controls.text = setupSettings != null ? setupSettings.ControlsText : "Move: WASD / Arrow Keys\nJump: Space\nPlace Block: F\nPlace Bomb: R\nCamera: Q / E";
            TextStyle(controls, theme, CyberpunkUIColor.Muted, theme.SmallSize);
            controls.alignment = TextAlignmentOptions.MidlineLeft;
            Parent(error.transform, setupPanel.transform);
            PlaceInPanel(error.transform, theme.SetupSize, new Vector2(0.5f, 0.29f), new Vector2(0.84f, 0.07f));
            TextStyle(error, theme, CyberpunkUIColor.Warning, theme.SmallSize);

            GameObject aliveCard = EnsureRect(playingPanel.transform, "CyberpunkAliveCard");
            Layout(aliveCard.transform, new Vector2(0, 1),
                new Vector2(theme.ScreenMargin + theme.AliveSize.x / 2, -theme.ScreenMargin - theme.AliveSize.y / 2), theme.AliveSize);
            Panel(aliveCard, theme, CyberpunkUIColor.Primary);
            Parent(alive.transform, aliveCard.transform);
            Stretch((RectTransform)alive.transform, 18);
            TextStyle(alive, theme, CyberpunkUIColor.Primary, theme.BodySize);
            SetActive(alive.gameObject, false);
            TMP_Text aliveLabel = EnsureText(aliveCard.transform, "Label", controls.font);
            aliveLabel.text = "ALIVE";
            TextStyle(aliveLabel, theme, CyberpunkUIColor.Text, theme.SmallSize);
            PlaceInPanel(aliveLabel.transform, theme.AliveSize, new Vector2(0.5f, 0.79f), new Vector2(0.8f, 0.24f));
            TMP_Text aliveValue = EnsureText(aliveCard.transform, "Value", controls.font);
            aliveValue.text = "02";
            TextStyle(aliveValue, theme, CyberpunkUIColor.Primary, theme.AliveNumberSize, true);
            PlaceInPanel(aliveValue.transform, theme.AliveSize, new Vector2(0.5f, 0.38f), new Vector2(0.84f, 0.54f));
            hud.FindProperty("_aliveValueText").objectReferenceValue = aliveValue;
            TMP_Text hudTitle = EnsureText(playingPanel.transform, "NeonHudTitle", controls.font);
            hudTitle.text = theme.CompactTitle;
            TextStyle(hudTitle, theme, CyberpunkUIColor.Muted, theme.SmallSize, true);
            Layout(hudTitle.transform, new Vector2(0.5f, 1), new Vector2(0, -theme.ScreenMargin - 20), new Vector2(460, 36));
            GameObject statusCard = EnsureRect(playingPanel.transform, "CyberpunkStatusCard");
            Layout(statusCard.transform, new Vector2(1, 1),
                new Vector2(-theme.ScreenMargin - theme.StatusSize.x / 2, -theme.ScreenMargin - theme.StatusSize.y / 2), theme.StatusSize);
            Panel(statusCard, theme, CyberpunkUIColor.Primary);
            Parent(status.transform, statusCard.transform);
            Stretch((RectTransform)status.transform, 22);
            TextStyle(status, theme, CyberpunkUIColor.Primary, theme.BodySize);
            status.alignment = TextAlignmentOptions.MidlineLeft;
            SetActive(status.gameObject, false);
            string[] rowLabels = { "RANGE", "LIMIT", "PLACED", "GRID" };
            string[] rowFields = { "_powerValueText", "_limitValueText", "_placedValueText", "_positionValueText" };
            for (int i = 0; i < rowLabels.Length; i++)
            {
                float y = 0.81f - i * 0.205f;
                TMP_Text label = EnsureText(statusCard.transform, "NeonLabel" + i, controls.font);
                label.text = rowLabels[i];
                TextStyle(label, theme, CyberpunkUIColor.Text, theme.SmallSize);
                PlaceInPanel(label.transform, theme.StatusSize, new Vector2(0.235f, y), new Vector2(0.35f, 0.16f));
                label.alignment = TextAlignmentOptions.MidlineLeft;
                TMP_Text value = EnsureText(statusCard.transform, "NeonValue" + i, controls.font);
                value.text = i == 3 ? "-- / -- / --" : "--";
                TextStyle(value, theme, CyberpunkUIColor.Primary, i == 3 ? theme.SmallSize : theme.HudNumberSize);
                PlaceInPanel(value.transform, theme.StatusSize, new Vector2(0.705f, y), new Vector2(0.47f, 0.16f));
                value.alignment = TextAlignmentOptions.MidlineRight;
                hud.FindProperty(rowFields[i]).objectReferenceValue = value;
            }
            hud.ApplyModifiedProperties();
            TMP_Text hints = EnsureText(playingPanel.transform, "CyberpunkInputHints", controls.font);
            hints.text = "WASD  MOVE     SPACE  JUMP     F  BLOCK     R  BOMB     Q / E  CAMERA";
            TextStyle(hints, theme, CyberpunkUIColor.Muted, theme.SmallSize);
            Layout(hints.transform, new Vector2(0.5f, 0), new Vector2(0, 34), new Vector2(1200, 38));
            SetActive(hints.gameObject, false);
            GameObject keys = EnsureRect(playingPanel.transform, "NeonKeyBar");
            Layout(keys.transform, new Vector2(0.5f, 0), new Vector2(0, 42), new Vector2(1180, 54));
            string[] keyNames = { "WASD", "SPACE", "F", "R", "Q / E" };
            string[] keyActions = { "MOVE", "JUMP", "BLOCK", "BOMB", "CAMERA" };
            for (int i = 0; i < keyNames.Length; i++)
            {
                GameObject cap = EnsureRect(keys.transform, "Key" + i);
                Layout(cap.transform, new Vector2(0, 0.5f), new Vector2(52 + i * 236, 0), new Vector2(98, 42));
                Panel(cap, theme, CyberpunkUIColor.Primary);
                cap.GetComponent<CyberpunkUIStyle>().SetSpriteOverride(theme.KeycapSprite);
                TMP_Text key = EnsureText(cap.transform, "Label", controls.font);
                key.text = keyNames[i];
                Stretch((RectTransform)key.transform, 5);
                TextStyle(key, theme, CyberpunkUIColor.Text, theme.SmallSize);
                TMP_Text action = EnsureText(keys.transform, "Action" + i, controls.font);
                action.text = keyActions[i];
                Layout(action.transform, new Vector2(0, 0.5f), new Vector2(168 + i * 236, 0), new Vector2(122, 42));
                TextStyle(action, theme, CyberpunkUIColor.Muted, theme.SmallSize);
            }

            Parent(result.transform, resultPanel.transform);
            TMP_Text resultBrand = EnsureText(resultPanel.transform, "NeonResultBrand", controls.font);
            resultBrand.text = theme.CompactTitle;
            TextStyle(resultBrand, theme, CyberpunkUIColor.Muted, theme.SmallSize, true);
            PlaceInPanel(resultBrand.transform, theme.ResultSize, new Vector2(0.5f, 0.86f), new Vector2(0.8f, 0.10f));
            PlaceInPanel(result.transform, theme.ResultSize, new Vector2(0.5f, 0.59f), new Vector2(0.86f, 0.30f));
            TextStyle(result, theme, CyberpunkUIColor.Text, theme.HeadingSize, true);
            Parent(retry.transform, resultPanel.transform);
            Parent(back.transform, resultPanel.transform);
            float buttonWidth = (theme.ResultSize.x - 110) / 2;
            Layout(retry.transform, new Vector2(0.5f, 0), new Vector2(-buttonWidth / 2 - 15, 85), new Vector2(buttonWidth, 70));
            Layout(back.transform, new Vector2(0.5f, 0), new Vector2(buttonWidth / 2 + 15, 85), new Vector2(buttonWidth, 70));
            ButtonStyle(retry, theme, CyberpunkUIColor.Success, false, "RETRY");
            ButtonStyle(back, theme, CyberpunkUIColor.Primary, false, null); // 日本語など既存ラベルは維持。
            Undo.RecordObject(resultPanel.transform, "Place result above HUD");
            resultPanel.transform.SetAsLastSibling();
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeObject = theme;
            Debug.Log("Cyberpunk UIを適用しました。Sceneを保存してください。SO編集後はRefresh Cyberpunk UI Stylesで色/Fontを反映できます。");
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            Debug.LogException(exception);
            return;
        }
        Undo.CollapseUndoOperations(undoGroup);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/NEON DETONATOR/UI/Refresh Styles")]
    public static void Refresh()
    {
        // 配置を変更せず、SOの色・Fontだけ再反映する専用メニューです。
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetActiveScene();
        foreach (CyberpunkUIStyle style in FindInScene<CyberpunkUIStyle>(scene))
        {
            Undo.RegisterFullObjectHierarchyUndo(style.gameObject, "Refresh UI Style");
            style.Apply();
        }
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static void RepairRenderers(GameObject root)
    {
        foreach (CyberpunkPanelGraphic panel in root.GetComponentsInChildren<CyberpunkPanelGraphic>(true))
            EnsurePanelRenderer(panel.gameObject);
    }

    /// <summary>
    /// RendererをGraphicより先にUndo登録します。旧Graphicに破棄済みRendererのキャッシュが
    /// 残っている場合は、privateフィールドに触れずGraphic自体を設定保持で作り直します。
    /// </summary>
    private static void EnsurePanelRenderer(GameObject go)
    {
        CanvasRenderer renderer = GetOrAdd<CanvasRenderer>(go);
        CyberpunkPanelGraphic old = go.GetComponent<CyberpunkPanelGraphic>();
        if (old == null || old.canvasRenderer == renderer) return;
        string serialized = EditorJsonUtility.ToJson(old);
        Selectable selectable = go.GetComponent<Selectable>();
        bool rebind = selectable != null && selectable.targetGraphic == old;
        if (rebind) Undo.RecordObject(selectable, "Repair graphic reference");
        Undo.DestroyObjectImmediate(old);
        CyberpunkPanelGraphic replacement = Undo.AddComponent<CyberpunkPanelGraphic>(go);
        EditorJsonUtility.FromJsonOverwrite(serialized, replacement);
        if (rebind) selectable.targetGraphic = replacement;
        replacement.SetAllDirty();
    }

    private static T[] FindInScene<T>(Scene scene) where T : Component =>
        scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
    private static T Ref<T>(SerializedObject so, string name) where T : UnityEngine.Object =>
        so.FindProperty(name)?.objectReferenceValue as T;
    private static T GetOrAdd<T>(GameObject go) where T : Component => go.GetComponent<T>() ?? Undo.AddComponent<T>(go);
    private static void Parent(Transform child, Transform parent)
    {
        if (child.parent != parent) Undo.SetTransformParent(child, parent, "Arrange UI");
    }
    private static void Layout(Transform transform, Vector2 anchor, Vector2 position, Vector2 size)
    {
        RectTransform rect = (RectTransform)transform;
        Undo.RecordObject(rect, "Layout UI");
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    /// <summary>Panel内の配置を比率で指定し、SOでPanelサイズを変えても各領域の間隔を保ちます。</summary>
    private static void PlaceInPanel(Transform child, Vector2 panelSize, Vector2 center, Vector2 sizeRatio)
        => Layout(child, center, Vector2.zero, Vector2.Scale(panelSize, sizeRatio));
    private static void Stretch(RectTransform rect, float padding = 0)
    {
        Undo.RecordObject(rect, "Stretch UI");
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.one * padding;
        rect.offsetMax = Vector2.one * -padding;
        rect.localScale = Vector3.one;
    }
    private static GameObject EnsureRect(Transform parent, string name)
    {
        Transform found = parent.Find(name);
        if (found != null) return found.gameObject;
        // Graphic側の自動追加に任せず、Rendererも生成時のUndo対象へ含めます。
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        Undo.RegisterCreatedObjectUndo(go, "Create UI decoration");
        Undo.SetTransformParent(go.transform, parent, "Parent decoration");
        go.layer = parent.gameObject.layer;
        return go;
    }
    private static TMP_Text EnsureText(Transform parent, string name, TMP_FontAsset fallback)
    {
        GameObject go = EnsureRect(parent, name);
        GetOrAdd<CanvasRenderer>(go);
        TMP_Text text = GetOrAdd<TextMeshProUGUI>(go);
        Undo.RecordObject(text, "Create heading");
        if (text.font == null) text.font = fallback;
        return text;
    }

    private static void SetActive(GameObject go, bool active)
    {
        Undo.RecordObject(go, "Set UI visibility");
        go.SetActive(active);
    }

    private static Button EnsureButton(Transform parent, string name, TMP_FontAsset font)
    {
        GameObject go = EnsureRect(parent, name);
        Button button = GetOrAdd<Button>(go);
        TMP_Text label = EnsureText(go.transform, "Label", font);
        Stretch((RectTransform)label.transform, 8);
        SetActive(go, true);
        return button;
    }
    private static void TextStyle(TMP_Text text, CyberpunkUITheme theme, CyberpunkUIColor role, float size, bool heading = false)
    {
        Undo.RecordObject(text, "Theme text");
        text.raycastTarget = false;
        text.enableAutoSizing = true;
        text.fontSizeMin = size * 0.7f;
        text.fontSizeMax = size;
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.Center;
        Style(text.gameObject, theme, role, false, heading);
    }
    private static void Panel(GameObject go, CyberpunkUITheme theme, CyberpunkUIColor role, bool interactive = false)
    {
        // 独自Graphicから標準Imageへ移行。CanvasRendererは削除せずそのまま使用します。
        EnsurePanelRenderer(go);
        CyberpunkPanelGraphic old = go.GetComponent<CyberpunkPanelGraphic>();
        if (old != null) Undo.DestroyObjectImmediate(old);
        Image panel = GetOrAdd<Image>(go);
        Undo.RecordObject(panel, "Theme panel");
        panel.enabled = true;
        panel.type = Image.Type.Sliced;
        panel.pixelsPerUnitMultiplier = 1;
        panel.raycastTarget = interactive;
        GameObject glowObject = EnsureRect(go.transform, "NeonGlow");
        GetOrAdd<CanvasRenderer>(glowObject);
        Image glowImage = GetOrAdd<Image>(glowObject);
        Undo.RecordObject(glowImage, "Configure glow image");
        glowImage.raycastTarget = false;
        glowImage.type = Image.Type.Sliced;
        Undo.RecordObject(glowObject.transform, "Place glow behind labels");
        glowObject.transform.SetAsFirstSibling();
        NeonUIGlow glow = GetOrAdd<NeonUIGlow>(go);
        Undo.RecordObject(glow, "Assign glow layer");
        glow.SetImage(glowImage);
        Style(go, theme, role);
    }
    private static void ButtonStyle(Button button, CyberpunkUITheme theme, CyberpunkUIColor role, bool filled, string label)
    {
        Undo.RecordObject(button, "Theme button");
        Panel(button.gameObject, theme, role, true);
        foreach (TMP_Text text in button.GetComponentsInChildren<TMP_Text>(true))
        {
            Undo.RecordObject(text, "Theme button label");
            if (label != null) text.text = label;
            text.raycastTarget = false;
            text.enableAutoSizing = true;
            text.fontSizeMin = theme.SmallSize;
            text.fontSizeMax = theme.BodySize;
            text.alignment = TextAlignmentOptions.Center;
        }
        Style(button.gameObject, theme, role, filled);
    }
    private static void Style(GameObject go, CyberpunkUITheme theme, CyberpunkUIColor role, bool filled = false, bool heading = false, bool outlineOnly = false)
    {
        Undo.RegisterFullObjectHierarchyUndo(go, "Style UI");
        CyberpunkUIStyle style = GetOrAdd<CyberpunkUIStyle>(go);
        Undo.RecordObject(style, "Assign UI theme");
        style.Configure(theme, role, filled, heading, outlineOnly);
        EditorUtility.SetDirty(style);
    }
    private static void EnsureFolder(string path)
    {
        string current = "Assets";
        foreach (string part in path.Split('/').Skip(1))
        {
            string next = current + "/" + part;
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, part);
            current = next;
        }
    }
}
#endif
