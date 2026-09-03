#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 視認性確認用の爆風Particle Prefab一式をEditor上で自動生成します。
/// 生成物は出発点として使い、色・Texture・Particle数は後から調整できます。
/// </summary>
public static class ExplosionEffectPrefabGenerator
{
    private const string PrefabFolder = "Assets/Prefabs/Effects/Explosion";
    private const string MaterialFolder = "Assets/Materials/Effects/Explosion";

    private const string CenterPath = PrefabFolder + "/ExplosionCenter.prefab";
    private const string MiddlePath = PrefabFolder + "/ExplosionMiddle.prefab";
    private const string EndPath = PrefabFolder + "/ExplosionEnd.prefab";
    private const string BlockedEndPath = PrefabFolder + "/ExplosionBlockedEnd.prefab";

    /// <summary>Materialと4種類の爆風Prefabを生成します。</summary>
    [MenuItem("Tools/3D Grid Bomber/Create Explosion Effect Prefabs")]
    public static void CreateExplosionEffectPrefabs()
    {
        EnsureFolder(PrefabFolder);
        EnsureFolder(MaterialFolder);

        Material fireMaterial = CreateOrUpdateMaterial(
            MaterialFolder + "/ExplosionFire.mat",
            new Color(1f, 0.22f, 0.015f, 1f));
        Material coreMaterial = CreateOrUpdateMaterial(
            MaterialFolder + "/ExplosionCore.mat",
            new Color(1f, 0.82f, 0.08f, 1f));
        Material sparkMaterial = CreateOrUpdateMaterial(
            MaterialFolder + "/ExplosionSpark.mat",
            new Color(1f, 0.55f, 0.04f, 1f));

        CreateCenterPrefab(coreMaterial, fireMaterial);
        CreateMiddlePrefab(coreMaterial, fireMaterial);
        CreateEndPrefab(coreMaterial, fireMaterial);
        CreateBlockedEndPrefab(fireMaterial, sparkMaterial);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        TryAssignToSelectedBombPrefab();

        Debug.Log(
            $"Explosion Effect Prefabを生成しました: {PrefabFolder}\n" +
            "Bomb Prefabを選択して実行した場合はExplosionViewへ自動設定されています。");
    }

    /// <summary>中心用。小さな球状Flashと外側へ広がる炎を重ねます。</summary>
    private static void CreateCenterPrefab(Material coreMaterial, Material fireMaterial)
    {
        GameObject root = CreateEffectRoot("ExplosionCenter");

        CreateParticleSystem(
            root.transform,
            "CoreFlash",
            coreMaterial,
            ParticleSystemShapeType.Sphere,
            new Vector3(0.25f, 0.25f, 0.25f),
            18,
            0.22f,
            0.1f,
            0.55f);

        CreateParticleSystem(
            root.transform,
            "OuterFire",
            fireMaterial,
            ParticleSystemShapeType.Sphere,
            new Vector3(0.42f, 0.42f, 0.42f),
            24,
            0.3f,
            0.45f,
            0.32f);

        SavePrefab(root, CenterPath);
    }

    /// <summary>中間用。セル内に収まる細長い炎の帯をZ+方向へ作ります。</summary>
    private static void CreateMiddlePrefab(Material coreMaterial, Material fireMaterial)
    {
        GameObject root = CreateEffectRoot("ExplosionMiddle");

        CreateParticleSystem(
            root.transform,
            "EnergyCore",
            coreMaterial,
            ParticleSystemShapeType.Box,
            new Vector3(0.16f, 0.16f, 0.72f),
            20,
            0.28f,
            0f,
            0.2f);

        CreateParticleSystem(
            root.transform,
            "FlowingFire",
            fireMaterial,
            ParticleSystemShapeType.Box,
            new Vector3(0.28f, 0.28f, 0.82f),
            24,
            0.32f,
            0.12f,
            0.22f);

        SavePrefab(root, MiddlePath);
    }

    /// <summary>通常先端用。Z+方向へ少し広がる円錐状の炎を作ります。</summary>
    private static void CreateEndPrefab(Material coreMaterial, Material fireMaterial)
    {
        GameObject root = CreateEffectRoot("ExplosionEnd");

        ParticleSystem core = CreateParticleSystem(
            root.transform,
            "EndCore",
            coreMaterial,
            ParticleSystemShapeType.Cone,
            new Vector3(0.18f, 0.18f, 0.45f),
            12,
            0.24f,
            0.35f,
            0.24f);
        SetCone(core, 14f, 0.08f);

        ParticleSystem fire = CreateParticleSystem(
            root.transform,
            "EndFire",
            fireMaterial,
            ParticleSystemShapeType.Cone,
            new Vector3(0.3f, 0.3f, 0.55f),
            18,
            0.32f,
            0.6f,
            0.3f);
        SetCone(fire, 24f, 0.12f);

        SavePrefab(root, EndPath);
    }

    /// <summary>衝突先端用。短い炎と前方へ散る火花を組み合わせます。</summary>
    private static void CreateBlockedEndPrefab(Material fireMaterial, Material sparkMaterial)
    {
        GameObject root = CreateEffectRoot("ExplosionBlockedEnd");

        ParticleSystem impact = CreateParticleSystem(
            root.transform,
            "ImpactFire",
            fireMaterial,
            ParticleSystemShapeType.Hemisphere,
            new Vector3(0.34f, 0.34f, 0.25f),
            16,
            0.22f,
            0.2f,
            0.28f);

        ParticleSystem sparks = CreateParticleSystem(
            root.transform,
            "Sparks",
            sparkMaterial,
            ParticleSystemShapeType.Cone,
            new Vector3(0.25f, 0.25f, 0.25f),
            14,
            0.26f,
            1.4f,
            0.08f);
        SetCone(sparks, 38f, 0.08f);

        SavePrefab(root, BlockedEndPath);
    }

    /// <summary>ExplosionEffectだけを持つ、ColliderなしのPrefabルートを作ります。</summary>
    private static GameObject CreateEffectRoot(string name)
    {
        GameObject root = new GameObject(name);
        root.AddComponent<ExplosionEffect>();
        return root;
    }

    /// <summary>1回だけBurst再生する見た目専用Particle Systemを作ります。</summary>
    private static ParticleSystem CreateParticleSystem(
        Transform parent,
        string name,
        Material material,
        ParticleSystemShapeType shapeType,
        Vector3 shapeScale,
        short particleCount,
        float lifetime,
        float speed,
        float size)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);

        ParticleSystem particleSystem = child.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particleSystem.main;
        main.duration = 0.35f;
        main.loop = false;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = lifetime;
        main.startSpeed = speed;
        main.startSize = size;
        main.startColor = Color.white;
        main.maxParticles = Mathf.Max(32, particleCount * 2);

        ParticleSystem.EmissionModule emission = particleSystem.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, particleCount)
        });

        ParticleSystem.ShapeModule shape = particleSystem.shape;
        shape.enabled = true;
        shape.shapeType = shapeType;
        shape.scale = shapeScale;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f,
            AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient fadeGradient = new Gradient();
        fadeGradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(1f, 0.45f, 0.08f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = fadeGradient;

        ParticleSystemRenderer renderer = child.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = material;

        return particleSystem;
    }

    /// <summary>Cone Particleの角度と半径を設定します。</summary>
    private static void SetCone(ParticleSystem particleSystem, float angle, float radius)
    {
        ParticleSystem.ShapeModule shape = particleSystem.shape;
        shape.angle = angle;
        shape.radius = radius;
    }

    /// <summary>一時GameObjectをPrefabとして保存し、Editor Sceneから破棄します。</summary>
    private static void SavePrefab(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    /// <summary>Particle用Materialを作成し、既存なら色だけ更新して再利用します。</summary>
    private static Material CreateOrUpdateMaterial(string path, Color color)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            Shader shader = FindParticleShader();

            if (shader == null)
                throw new System.InvalidOperationException("使用可能なParticle Shaderが見つかりません。");

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);

        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        return material;
    }

    /// <summary>URPとBuilt-in Render PipelineのParticle Shaderを順番に探します。</summary>
    private static Shader FindParticleShader()
    {
        return Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
               Shader.Find("Particles/Standard Unlit") ??
               Shader.Find("Sprites/Default") ??
               Shader.Find("Unlit/Color");
    }

    /// <summary>
    /// ProjectウィンドウでBomb Prefabが選択されていれば、生成PrefabをExplosionViewへ設定します。
    /// </summary>
    private static void TryAssignToSelectedBombPrefab()
    {
        GameObject selectedObject = Selection.activeObject as GameObject;
        string selectedPath = AssetDatabase.GetAssetPath(selectedObject);

        if (selectedObject == null || string.IsNullOrEmpty(selectedPath) ||
            !selectedPath.EndsWith(".prefab"))
        {
            return;
        }

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(selectedPath);

        try
        {
            Bomb bomb = prefabRoot.GetComponentInChildren<Bomb>(true);

            if (bomb == null)
                return;

            ExplosionView view = bomb.GetComponent<ExplosionView>();

            if (view == null)
                view = bomb.gameObject.AddComponent<ExplosionView>();

            SerializedObject serializedView = new SerializedObject(view);
            serializedView.FindProperty("_centerPrefab").objectReferenceValue =
                LoadEffectPrefab(CenterPath);
            serializedView.FindProperty("_middlePrefab").objectReferenceValue =
                LoadEffectPrefab(MiddlePath);
            serializedView.FindProperty("_endPrefab").objectReferenceValue =
                LoadEffectPrefab(EndPath);
            serializedView.FindProperty("_blockedEndPrefab").objectReferenceValue =
                LoadEffectPrefab(BlockedEndPath);
            serializedView.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, selectedPath);
            Debug.Log($"ExplosionViewをBomb Prefabへ設定しました: {selectedPath}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    /// <summary>Prefab AssetのルートからExplosionEffect Componentを取得します。</summary>
    private static ExplosionEffect LoadEffectPrefab(string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        return prefab != null ? prefab.GetComponent<ExplosionEffect>() : null;
    }

    /// <summary>Assets以下のフォルダを階層順に作成します。</summary>
    private static void EnsureFolder(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string currentPath = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string nextPath = currentPath + "/" + parts[i];

            if (!AssetDatabase.IsValidFolder(nextPath))
                AssetDatabase.CreateFolder(currentPath, parts[i]);

            currentPath = nextPath;
        }
    }
}
#endif
