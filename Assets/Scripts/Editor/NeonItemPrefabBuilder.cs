#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>元のテスト用Prefabを上書きせず、2種類のネオンItemを作成します。</summary>
public static class NeonItemPrefabBuilder
{
    private const string Folder = "Assets/Scripts/Generated/NeonItems";
    [MenuItem("Tools/NEON DETONATOR/Assets/Create Neon Item Prefabs")]
    public static void Create()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Playを停止してください。"); return; }
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null || ShaderUtil.ShaderHasError(shader)) { Debug.LogError("URP/Unlit Shaderを使用できません。"); return; }
        EnsureFolder(Folder);
        ItemVisualSettings visualSettings = Asset<ItemVisualSettings>("ItemVisualSettings");
        Item[] prefabs = new Item[2];
        for (int type = 0; type < 2; type++)
        {
            string label = type == 0 ? "NeonBombPowerItem" : "NeonBombCountItem";
            string path = Folder + "/" + label + ".prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) { prefabs[type] = existing.GetComponent<Item>(); continue; }
            ItemSettings settings = Asset<ItemSettings>(label + "Settings");
            SerializedObject settingData = new SerializedObject(settings);
            settingData.FindProperty("_type").enumValueIndex = type;
            settingData.ApplyModifiedPropertiesWithoutUndo();
            Material accent = MaterialAsset(label + "Glow", shader, type == 0 ? new Color(1.5f, 1.15f, 0.2f) : new Color(0.2f, 1.4f, 0.5f));
            Material body = MaterialAsset("ItemDarkBody", shader, new Color(0.06f, 0.085f, 0.12f));
            GameObject root = new GameObject(label);
            try
            {
                Item item = root.AddComponent<Item>();
                SerializedObject itemData = new SerializedObject(item);
                itemData.FindProperty("_settings").objectReferenceValue = settings;
                itemData.ApplyModifiedPropertiesWithoutUndo();
                Transform visual = new GameObject("Visual").transform;
                visual.SetParent(root.transform, false);
                Transform ring = new GameObject("Rotating Ring").transform;
                ring.SetParent(visual, false);
                // 間欠的な小片を円周上に並べ、軽い立体リングにします。
                for (int i = 0; i < 32; i++)
                {
                    float angle = i * Mathf.PI * 2 / 32;
                    Transform segment = Part(ring, "Ring Segment", PrimitiveType.Cube,
                        new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * 0.34f,
                        new Vector3(0.025f, 0.055f, 0.04f), accent);
                    segment.localRotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
                }
                if (type == 0)
                {
                    Part(visual, "Energy Core", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.14f, accent);
                    // 4方向の矢印。色だけでなく形で爆風アップと判別できます。
                    for (int i = 0; i < 4; i++)
                    {
                        Transform arrow = new GameObject("Outward Arrow").transform;
                        arrow.SetParent(visual, false);
                        arrow.localRotation = Quaternion.Euler(0, 0, i * 90);
                        Part(arrow, "Shaft", PrimitiveType.Cube, new Vector3(0, 0.155f, 0), new Vector3(0.04f, 0.14f, 0.05f), accent);
                        for (int side = -1; side <= 1; side += 2)
                        {
                            Transform tip = Part(arrow, "Arrowhead", PrimitiveType.Cube, new Vector3(side * 0.032f, 0.205f, 0), new Vector3(0.035f, 0.09f, 0.05f), accent);
                            tip.localRotation = Quaternion.Euler(0, 0, side * 45);
                        }
                    }
                }
                else
                {
                    Part(visual, "Bomb", PrimitiveType.Sphere, new Vector3(-0.075f, -0.015f, 0), Vector3.one * 0.29f, body);
                    Transform fuse = Part(visual, "Fuse", PrimitiveType.Cube, new Vector3(-0.04f, 0.16f, 0), new Vector3(0.035f, 0.09f, 0.035f), accent);
                    fuse.localRotation = Quaternion.Euler(0, 0, -25);
                    Part(visual, "Plus Horizontal", PrimitiveType.Cube, new Vector3(0.15f, 0, -0.08f), new Vector3(0.17f, 0.045f, 0.045f), accent);
                    Part(visual, "Plus Vertical", PrimitiveType.Cube, new Vector3(0.15f, 0, -0.08f), new Vector3(0.045f, 0.17f, 0.045f), accent);
                }
                NeonItemView view = root.AddComponent<NeonItemView>();
                SerializedObject viewData = new SerializedObject(view);
                viewData.FindProperty("_visual").objectReferenceValue = visual;
                viewData.FindProperty("_ring").objectReferenceValue = ring;
                viewData.FindProperty("_settings").objectReferenceValue = visualSettings;
                viewData.ApplyModifiedPropertiesWithoutUndo();
                prefabs[type] = PrefabUtility.SaveAsPrefabAsset(root, path).GetComponent<Item>();
            }
            finally { Object.DestroyImmediate(root); }
        }
        // 自動で既存のドロップ設定を差し替えない。ユーザーの確率/間隔を保持します。
        AssetDatabase.SaveAssets();
        Selection.objects = prefabs;
        Debug.Log("ネオンItem Prefabを作成しました。既存ItemDropSettingsのPrefabsへこの2種類を設定してください。場所: " + Folder);
    }

    private static Transform Part(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        Object.DestroyImmediate(part.GetComponent<Collider>());
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        Renderer renderer = part.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return part.transform;
    }

    private static T Asset<T>(string name) where T : ScriptableObject
    {
        string path = Folder + "/" + name + ".asset";
        T value = AssetDatabase.LoadAssetAtPath<T>(path);
        if (value != null) return value;
        value = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(value, path);
        return value;
    }
    private static Material MaterialAsset(string name, Shader shader, Color color)
    {
        string path = Folder + "/" + name + ".mat";
        Material value = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (value != null) return value;
        value = new Material(shader) { name = name, enableInstancing = true };
        value.SetColor("_BaseColor", color);
        AssetDatabase.CreateAsset(value, path);
        return value;
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
