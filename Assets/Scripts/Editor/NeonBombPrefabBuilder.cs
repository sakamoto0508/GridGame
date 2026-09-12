#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>既存Bomb Prefabのゲーム設定と参照を保ち、描画子だけを追加します。</summary>
public static class NeonBombPrefabBuilder
{
    private const string Folder = "Assets/Scripts/Generated/NeonBomb";
    private const string PrefabPath = "Assets/Prefabs/Block/BombPrefab.prefab";

    [MenuItem("Tools/NEON DETONATOR/Assets/Apply Neon Bomb Visual")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Playを停止してください。"); return; }
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null || prefab.GetComponent<Bomb>() == null) { Debug.LogError("対象Bomb Prefabがありません: " + PrefabPath); return; }
        if (prefab.transform.Find("Neon Bomb Visual") != null) { Debug.Log("ネオンBombは適用済みです。PrefabやBombVisualSettingsから調整してください。"); return; }
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (lit == null || unlit == null || ShaderUtil.ShaderHasError(lit) || ShaderUtil.ShaderHasError(unlit))
        { Debug.LogError("URPのLit/Unlitシェーダーを使用できません。"); return; }
        if (!EditorUtility.DisplayDialog("ボムの見た目を変更", "既存BombPrefabをバックアップし、暗い球体と水色の発光帯を追加します。爆発・Collider設定は維持します。", "適用", "キャンセル")) return;
        EnsureFolder(Folder);
        string backup = Folder + "/BombPrefab_BeforeNeon.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(backup) == null && !AssetDatabase.CopyAsset(PrefabPath, backup))
        { Debug.LogError("バックアップに失敗したため中止しました。"); return; }
        string settingsPath = Folder + "/BombVisualSettings.asset";
        BombVisualSettings settings = AssetDatabase.LoadAssetAtPath<BombVisualSettings>(settingsPath);
        if (settings == null) { settings = ScriptableObject.CreateInstance<BombVisualSettings>(); AssetDatabase.CreateAsset(settings, settingsPath); }
        Material shell = MaterialAsset("BombShell", lit, new Color(0.07f, 0.09f, 0.12f));
        shell.SetFloat("_Metallic", 0.55f);
        shell.SetFloat("_Smoothness", 0.5f);
        EditorUtility.SetDirty(shell);
        Material glow = MaterialAsset("BombGlow", unlit, settings.NormalColor);
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            // 元のMeshは削除せずRendererだけOFF。Bomb/ExplosionView/Colliderはそのまま。
            MeshRenderer original = root.GetComponent<MeshRenderer>();
            if (original != null) original.enabled = false;
            Transform visual = new GameObject("Neon Bomb Visual").transform;
            visual.SetParent(root.transform, false);
            Part(visual, "Armored Core", PrimitiveType.Sphere, new Vector3(0, -0.14f, 0), Vector3.one * 0.68f, shell);
            List<Renderer> lights = new List<Renderer>();
            for (int i = 0; i < 24; i++)
            {
                float angle = i * Mathf.PI * 2 / 24;
                Renderer segment = Part(visual, "Energy Belt", PrimitiveType.Cube,
                    new Vector3(Mathf.Cos(angle) * 0.337f, -0.14f, Mathf.Sin(angle) * 0.337f), new Vector3(0.018f, 0.055f, 0.078f), glow);
                segment.transform.localRotation = Quaternion.Euler(0, -angle * Mathf.Rad2Deg, 0);
                lights.Add(segment);
            }
            Part(visual, "Fuse Socket", PrimitiveType.Cylinder, new Vector3(0, 0.21f, 0), new Vector3(0.16f, 0.045f, 0.16f), shell);
            lights.Add(Part(visual, "Fuse Light", PrimitiveType.Sphere, new Vector3(0, 0.3f, 0), Vector3.one * 0.11f, glow));
            // 上面からも見つけやすい発光パネル。
            lights.Add(Part(visual, "Top Indicator", PrimitiveType.Cube, new Vector3(0, 0.12f, -0.2f), new Vector3(0.17f, 0.055f, 0.08f), glow));
            NeonBombView view = root.AddComponent<NeonBombView>();
            SerializedObject data = new SerializedObject(view);
            data.FindProperty("_bomb").objectReferenceValue = root.GetComponent<Bomb>();
            data.FindProperty("_visual").objectReferenceValue = visual;
            data.FindProperty("_settings").objectReferenceValue = settings;
            SerializedProperty array = data.FindProperty("_lights");
            array.arraySize = lights.Count;
            for (int i = 0; i < lights.Count; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = lights[i];
            data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Debug.Log("BombPrefabの見た目を更新しました。通常は水色、爆発直前は黄色で速く点滅します。");
    }

    private static Renderer Part(Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 scale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        Object.DestroyImmediate(part.GetComponent<Collider>());
        part.transform.SetParent(parent, false);
        part.transform.localPosition = pos;
        part.transform.localScale = scale;
        Renderer renderer = part.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = material.shader.name.EndsWith("/Unlit") ? ShadowCastingMode.Off : ShadowCastingMode.On;
        return renderer;
    }
    private static Material MaterialAsset(string name, Shader shader, Color color)
    {
        string path = Folder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        material = new Material(shader) { name = name };
        material.SetColor("_BaseColor", color);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }
    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        string parent = path.Substring(0, slash);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
    }
}
#endif
