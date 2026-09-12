#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>既存Player/Enemy Prefabのロジックを残し、モデルとパーツ式アニメーションを追加。</summary>
public static class RobotCharacterPrefabBuilder
{
    private const string Folder = "Assets/Scripts/Generated/RobotCharacters";
    [MenuItem("Tools/NEON DETONATOR/Assets/Apply Robot Characters")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Playを停止してください。"); return; }
        string[] paths = { "Assets/Prefabs/Player/Player.prefab", "Assets/Prefabs/Enemy/TestEnemyPrefab.prefab" };
        foreach (string path in paths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null || prefab.GetComponent<CharacterBase>() == null || !AssetDatabase.IsOpenForEdit(prefab))
            { Debug.LogError("対象Character Prefabがないか変更できません: " + path); return; }
        }
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (lit == null || unlit == null) { Debug.LogError("URP Shaderが見つかりません。"); return; }
        if (!EditorUtility.DisplayDialog("ロボットモデルを適用", "PlayerとEnemyの元Prefabをバックアップし、描画のみ変更します。適用済みのモデルは上書きしません。", "適用", "キャンセル")) return;
        EnsureFolder(Folder);
        string settingsPath = Folder + "/RobotVisualSettings.asset";
        RobotVisualSettings settings = AssetDatabase.LoadAssetAtPath<RobotVisualSettings>(settingsPath);
        if (settings == null) { settings = ScriptableObject.CreateInstance<RobotVisualSettings>(); AssetDatabase.CreateAsset(settings, settingsPath); }
        Material shell = MaterialAsset("RobotShell", lit, new Color(0.7f, 0.77f, 0.82f));
        Material dark = MaterialAsset("RobotJoints", lit, new Color(0.035f, 0.05f, 0.075f));
        for (int index = 0; index < paths.Length; index++)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(paths[index]);
            if (asset.GetComponent<RobotCharacterView>() != null || asset.transform.Find("Robot Visual") != null) continue;
            string backup = Folder + "/" + asset.name + "_BeforeRobot.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(backup) == null && !AssetDatabase.CopyAsset(paths[index], backup))
            { Debug.LogError("バックアップに失敗しました: " + paths[index]); return; }
            Material glow = MaterialAsset(index == 0 ? "PlayerCyan" : "EnemyYellow", unlit,
                index == 0 ? new Color(0.1f, 1.1f, 1.5f) : new Color(1.5f, 1.05f, 0.15f));
            GameObject root = PrefabUtility.LoadPrefabContents(paths[index]);
            try
            {
                // 保存済みの本体Meshは残し、RendererだけOFF。Collider/入力/AIは触りません。
                Renderer original = root.GetComponent<Renderer>();
                if (original != null) original.enabled = false;
                Transform facing = Joint(root.transform, "Robot Visual", new Vector3(0, -0.48f, 0));
                Transform pose = Joint(facing, "Pose", Vector3.zero);
                Part(pose, "Torso", PrimitiveType.Capsule, new Vector3(0, 0.42f, 0), new Vector3(0.34f, 0.22f, 0.27f), shell);
                Part(pose, "Head", PrimitiveType.Sphere, new Vector3(0, 0.73f, 0), new Vector3(0.48f, 0.37f, 0.4f), shell);
                Part(pose, "Visor Housing", PrimitiveType.Cube, new Vector3(0, 0.73f, 0.17f), new Vector3(0.38f, 0.14f, 0.07f), dark);
                Part(pose, "Visor", PrimitiveType.Cube, new Vector3(0, 0.73f, 0.212f), new Vector3(0.32f, 0.07f, 0.025f), glow);
                Part(pose, "Chest Indicator", PrimitiveType.Cube, new Vector3(0, 0.46f, 0.14f), new Vector3(0.1f, 0.055f, 0.025f), glow);
                Transform[] arms = new Transform[2];
                Transform[] legs = new Transform[2];
                for (int side = 0; side < 2; side++)
                {
                    float sign = side == 0 ? -1 : 1;
                    arms[side] = Joint(pose, side == 0 ? "Left Shoulder" : "Right Shoulder", new Vector3(sign * 0.235f, 0.53f, 0));
                    Part(arms[side], "Shoulder Joint", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.115f, dark);
                    Part(arms[side], "Arm", PrimitiveType.Capsule, new Vector3(0, -0.1f, 0), new Vector3(0.105f, 0.1f, 0.105f), shell);
                    Part(arms[side], "Hand", PrimitiveType.Sphere, new Vector3(0, -0.21f, 0), Vector3.one * 0.12f, dark);
                    legs[side] = Joint(pose, side == 0 ? "Left Hip" : "Right Hip", new Vector3(sign * 0.095f, 0.29f, 0));
                    Part(legs[side], "Leg", PrimitiveType.Capsule, new Vector3(0, -0.1f, 0), new Vector3(0.115f, 0.095f, 0.115f), dark);
                    Part(legs[side], "Boot", PrimitiveType.Cube, new Vector3(0, -0.235f, 0.035f), new Vector3(0.15f, 0.105f, 0.23f), shell);
                }
                if (index == 1)
                {
                    Part(pose, "Antenna", PrimitiveType.Cylinder, new Vector3(0.13f, 0.96f, 0), new Vector3(0.025f, 0.065f, 0.025f), dark);
                    Part(pose, "Antenna Light", PrimitiveType.Sphere, new Vector3(0.13f, 1.025f, 0), Vector3.one * 0.06f, glow);
                }
                RobotCharacterView view = root.AddComponent<RobotCharacterView>();
                SerializedObject data = new SerializedObject(view);
                data.FindProperty("_settings").objectReferenceValue = settings;
                data.FindProperty("_facing").objectReferenceValue = facing;
                data.FindProperty("_pose").objectReferenceValue = pose;
                data.FindProperty("_leftArm").objectReferenceValue = arms[0];
                data.FindProperty("_rightArm").objectReferenceValue = arms[1];
                data.FindProperty("_leftLeg").objectReferenceValue = legs[0];
                data.FindProperty("_rightLeg").objectReferenceValue = legs[1];
                data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, paths[index]);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
        Selection.activeObject = settings;
        Debug.Log("ロボットとパーツ式アニメーションを適用しました。RobotVisualSettingsで動きを調整できます。");
    }
    private static Transform Joint(Transform parent, string name, Vector3 position)
    {
        Transform joint = new GameObject(name).transform;
        joint.SetParent(parent, false); joint.localPosition = position;
        return joint;
    }
    private static void Part(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        Object.DestroyImmediate(part.GetComponent<Collider>());
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().sharedMaterial = material;
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
        EnsureFolder(parent); AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
    }
}
#endif
