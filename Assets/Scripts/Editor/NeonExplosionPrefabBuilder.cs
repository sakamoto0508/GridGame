#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>元の炎Prefabを残し、セル内に収まる直線的なネオン爆風一式を生成します。</summary>
public static class NeonExplosionPrefabBuilder
{
    private const string Folder = "Assets/Scripts/Generated/NeonExplosion";
    [MenuItem("Tools/NEON DETONATOR/Assets/Create Neon Explosion Prefabs")]
    public static void Create()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Playを停止してください。"); return; }
        Shader shader = Resources.Load<Shader>("NeonExplosionParticle");
        if (shader == null || ShaderUtil.ShaderHasError(shader)) { Debug.LogError("NeonExplosionParticle Shaderのコンパイルを確認してください。"); return; }
        EnsureFolder(Folder);
        if (AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/NeonCenter.prefab") != null &&
            !EditorUtility.DisplayDialog("ネオン爆風を再生成", "生成済みのネオン爆風PrefabとMaterialをStyle設定から上書きします。元の炎Prefabは変更しません。", "再生成", "キャンセル")) return;
        NeonExplosionStyleSettings style = GetAsset<NeonExplosionStyleSettings>("NeonExplosionStyleSettings");
        Material beam = MaterialAsset("Beam", shader, style.BeamColor);
        Material core = MaterialAsset("Core", shader, style.CoreColor);
        Material spark = MaterialAsset("Spark", shader, style.SparkColor);
        // Unity組み込みMeshの参照を利用するため、外部モデルは不要です。
        GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Mesh cube = primitive.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(primitive);
        string[] names = { "NeonCenter", "NeonMiddle", "NeonEnd", "NeonBlockedEnd" };
        ExplosionEffect[] effects = new ExplosionEffect[4];
        for (int type = 0; type < 4; type++)
        {
            GameObject root = new GameObject(names[type], typeof(ExplosionEffect));
            try
            {
                int axes = type == 0 ? 3 : 1;
                for (int axis = 0; axis < axes; axis++)
                {
                    Quaternion rotation = type != 0 || axis == 2 ? Quaternion.identity :
                        axis == 0 ? Quaternion.Euler(0, 90, 0) : Quaternion.Euler(90, 0, 0);
                    float length = type == 3 ? 0.8f : 1;
                    Vector3 position = type == 3 ? new Vector3(0, 0, -0.1f) : Vector3.zero;
                    Particle(root, "Cyan Beam", cube, beam, position, rotation, new Vector3(style.BeamWidth, style.BeamWidth, length), style.Duration, 1, false);
                    Particle(root, "Bright Core", cube, core, position, rotation, new Vector3(style.CoreWidth, style.CoreWidth, length), style.Duration * 0.8f, 1, false);
                }
                if (type >= 2)
                {
                    // 先端は平らな発光板で止め、実際の爆風範囲の外へ延ばしません。
                    Particle(root, "End Cap", cube, type == 3 ? spark : beam, new Vector3(0, 0, type == 3 ? 0.32f : 0.45f),
                        Quaternion.identity, new Vector3(0.3f, 0.3f, 0.04f), style.Duration * 0.75f, 1, false);
                }
                if (style.SparkCount > 0)
                    Particle(root, "Digital Sparks", cube, spark, Vector3.zero, Quaternion.identity,
                        new Vector3(0.035f, 0.035f, 0.1f), style.Duration * 0.7f, style.SparkCount, true);
                effects[type] = PrefabUtility.SaveAsPrefabAsset(root, Folder + "/" + names[type] + ".prefab").GetComponent<ExplosionEffect>();
            }
            finally { Object.DestroyImmediate(root); }
        }
        ExplosionVisualSettings settings = GetAsset<ExplosionVisualSettings>("NeonExplosionVisualSettings");
        SerializedObject data = new SerializedObject(settings);
        string[] fields = { "_centerPrefab", "_middlePrefab", "_endPrefab", "_blockedEndPrefab" };
        for (int i = 0; i < fields.Length; i++) data.FindProperty(fields[i]).objectReferenceValue = effects[i];
        data.FindProperty("_effectDuration").floatValue = style.Duration;
        data.FindProperty("_scaleToCell").boolValue = true;
        data.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        Selection.activeObject = settings;
        Debug.Log("ネオン爆風を生成しました。BombPrefabのExplosionView > SettingsへNeonExplosionVisualSettingsを設定してください。");
    }

    private static void Particle(GameObject root, string name, Mesh mesh, Material material, Vector3 position,
        Quaternion rotation, Vector3 size, float lifetime, int count, bool sparks)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(root.transform, false);
        child.transform.localPosition = position;
        child.transform.localRotation = rotation;
        ParticleSystem ps = child.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = false;
        main.duration = lifetime;
        main.startLifetime = lifetime;
        main.startSpeed = sparks ? 0.15f : 0;
        main.startSize3D = true;
        main.startSizeX = size.x; main.startSizeY = size.y; main.startSizeZ = size.z;
        main.startColor = Color.white;
        main.maxParticles = count;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.playOnAwake = true;
        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0, (short)count) });
        var shape = ps.shape;
        shape.enabled = sparks;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.3f, 0.3f, 0.5f);
        var fade = ps.colorOverLifetime;
        fade.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
            new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0.8f, 0.3f), new GradientAlphaKey(0, 1) });
        fade.color = gradient;
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.mesh = mesh;
        renderer.alignment = ParticleSystemRenderSpace.Local;
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }
    private static Material MaterialAsset(string name, Shader shader, Color color)
    {
        string path = Folder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
        material.SetColor("_TintColor", color);
        EditorUtility.SetDirty(material);
        return material;
    }
    private static T GetAsset<T>(string name) where T : ScriptableObject
    {
        string path = Folder + "/" + name + ".asset";
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
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
