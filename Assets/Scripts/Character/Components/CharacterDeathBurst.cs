using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>体の範囲から立方体Particleを放出する独立Effect。Character非表示後も再生します。</summary>
public class CharacterDeathBurst : MonoBehaviour
{
    private const string ShaderResourceName = "CharacterDeathVoxel";

    /// <summary>Rendererが見つからないときに使う体の大きさ（ワールド単位）です。</summary>
    private static readonly Vector3 FallbackBodySize = new Vector3(0.6f, 0.9f, 0.6f);

    /// <summary>破片の出現範囲の半径の上限です。武器や装飾が大きくても画面全体へ広がらないようにします。</summary>
    private const float MaxSpawnExtent = 1.5f;

    // Settingsの値を安全な範囲に収めるための上下限です。
    private const float MinLifetime = 0.1f;
    private const float MaxLifetime = 5f;
    private const int MinFragmentCount = 10;
    private const int MaxFragmentCount = 200;
    private const float MinGravity = 0f;
    private const float MaxGravity = 4f;
    private const float MinSpeed = 0f;
    private const float MaxSpeed = 10f;
    private const float MinFragmentSize = 0.01f;
    private const float MaxFragmentSize = 0.4f;

    /// <summary>寿命のこの割合までは大きさを保ち、残りの時間で縮めます。</summary>
    private const float ShrinkStartRatio = 0.65f;

    /// <summary>破片の初期回転の最大角度（度）です。</summary>
    private const float MaxInitialRotationDegrees = 180f;

    /// <summary>破片の回転速度の最大値です。</summary>
    private const float MaxAngularVelocity = 8f;

    /// <summary>乱数シード生成でInstanceIDを散らすための係数（素数）です。</summary>
    private const int InstanceIdSeedMultiplier = 397;

    /// <summary>破片Meshは1辺1の立方体です（大きさはParticleのstartSizeで決まります）。</summary>
    private const float CubeHalfSize = 0.5f;

    private Mesh _mesh;
    private Material _material;

    public static void Spawn(
        CharacterBase character,
        Transform source,
        CharacterDeathVisualSettings settings,
        DeathCause cause)
    {
        // Settingsがなければ既定値のインスタンスを一時的に作り、最後に破棄します。
        CharacterDeathVisualSettings fallback = null;
        if (settings == null)
            settings = fallback = ScriptableObject.CreateInstance<CharacterDeathVisualSettings>();

        try
        {
            if (!settings.Enabled)
                return;

            Shader shader = Resources.Load<Shader>(ShaderResourceName);
            if (shader == null)
            {
                Debug.LogWarning("CharacterDeathVoxel Shaderが見つかりません。", source);
                return;
            }

            Bounds body = CalculateBodyBounds(source);

            // 武器や装飾が大きい場合でも、出現範囲が画面全体へ広がらないよう制限します。
            Vector3 extent = Vector3.Min(body.extents, Vector3.one * MaxSpawnExtent);

            GameObject root = new GameObject("Character Death Voxels");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, source.gameObject.scene);
            root.transform.position = body.center;

            CharacterDeathBurst effect = root.AddComponent<CharacterDeathBurst>();
            effect._mesh = CreateCube();
            effect._material = new Material(shader);

            ParticleSystem particles = root.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            float maximumLife = Mathf.Clamp(
                Mathf.Max(settings.Lifetime.x, settings.Lifetime.y),
                MinLifetime,
                MaxLifetime);

            var main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = maximumLife;
            main.startLifetime = maximumLife;
            main.startSpeed = 0;
            main.maxParticles = Mathf.Clamp(settings.FragmentCount, MinFragmentCount, MaxFragmentCount);
            main.gravityModifier = Mathf.Clamp(settings.Gravity, MinGravity, MaxGravity);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            main.stopAction = ParticleSystemStopAction.Destroy;
            main.startRotation3D = true;

            var emission = particles.emission;
            emission.enabled = false;

            var shape = particles.shape;
            shape.enabled = false;

            // 最後だけ小さくして消す。不透明な四角形を保ち、半透明の煙にはしません。
            // 寿命の65%までは大きさを保ち、残りの時間で縮めます。
            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(
                new Keyframe(0, 1),
                new Keyframe(ShrinkStartRatio, 1),
                new Keyframe(1, 0)));

            var rendererSystem = particles.GetComponent<ParticleSystemRenderer>();
            rendererSystem.renderMode = ParticleSystemRenderMode.Mesh;
            rendererSystem.mesh = effect._mesh;
            rendererSystem.sharedMaterial = effect._material;
            rendererSystem.shadowCastingMode = ShadowCastingMode.Off;
            rendererSystem.receiveShadows = false;
            rendererSystem.SetActiveVertexStreams(new List<ParticleSystemVertexStream>
            {
                ParticleSystemVertexStream.Position,
                ParticleSystemVertexStream.Normal,
                ParticleSystemVertexStream.Color
            });

            // Stage/AI用のUnityEngine.Randomの状態を変更しません。
            // 発生元と時刻を混ぜ、演出専用の乱数シードを作ります。
            System.Random random = new System.Random(
                unchecked(source.GetInstanceID() * InstanceIdSeedMultiplier ^ System.Environment.TickCount));
            // ParticleSystemのシードは0を避け、1以上の値を設定します。
            particles.useAutoRandomSeed = false;
            particles.randomSeed = (uint)random.Next(1, int.MaxValue);
            particles.Play();

            Color accentColor = character is PlayerCharacter ? settings.PlayerAccent : settings.EnemyAccent;

            for (int i = 0; i < main.maxParticles; i++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2;
                float speed = RandomUtility.Range(random, settings.HorizontalSpeed, MinSpeed, MaxSpeed);
                float upward = RandomUtility.Range(random, settings.UpwardSpeed, MinSpeed, MaxSpeed);

                // 落下Blockに潰されたときは、横へ広く・低く飛び散らせます。
                if (cause == DeathCause.FallingBlock)
                {
                    speed *= settings.CrushHorizontalSpeedMultiplier;
                    upward *= settings.CrushUpwardSpeedMultiplier;
                }

                var fragment = new ParticleSystem.EmitParams
                {
                    position = body.center + Vector3.Scale(extent, RandomSignedVector(random)),
                    velocity = new Vector3(Mathf.Cos(angle) * speed, upward, Mathf.Sin(angle) * speed),
                    startLifetime = RandomUtility.Range(random, settings.Lifetime, MinLifetime, maximumLife),
                    startSize = RandomUtility.Range(random, settings.FragmentSize, MinFragmentSize, MaxFragmentSize),
                    startColor = random.NextDouble() < settings.AccentRatio ? accentColor : settings.BodyColor,
                    rotation3D = RandomSignedVector(random) * MaxInitialRotationDegrees,
                    angularVelocity3D = RandomSignedVector(random) * MaxAngularVelocity
                };

                particles.Emit(fragment, 1);
            }
        }
        finally
        {
            if (fallback != null)
                Destroy(fallback);
        }
    }

    /// <summary>表示中のMeshRenderer/SkinnedMeshRendererを囲む範囲を返します。なければ既定サイズです。</summary>
    private static Bounds CalculateBodyBounds(Transform source)
    {
        Bounds body = new Bounds(source.position, FallbackBodySize);
        bool hasBounds = false;

        foreach (Renderer renderer in source.GetComponentsInChildren<Renderer>())
        {
            if (!renderer.enabled || !(renderer is MeshRenderer || renderer is SkinnedMeshRenderer))
                continue;

            if (!hasBounds)
            {
                body = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                body.Encapsulate(renderer.bounds);
            }
        }

        return body;
    }

    /// <summary>-1〜1の乱数を返します。</summary>
    private static float Signed(System.Random random) => (float)random.NextDouble() * 2 - 1;

    /// <summary>各成分が-1〜1の乱数ベクトルを返します（X, Y, Zの順に乱数を消費）。</summary>
    private static Vector3 RandomSignedVector(System.Random random) =>
        new Vector3(Signed(random), Signed(random), Signed(random));

    private static Mesh CreateCube()
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();

        // 面ごとに頂点を分け、法線が面ごとに平らになるようにします。
        for (int face = 0; face < CubeCorner.FaceCount; face++)
        {
            int offset = vertices.Count;

            for (int j = 0; j < CubeCorner.VerticesPerFace; j++)
            {
                int corner = CubeCorner.Faces[face * CubeCorner.VerticesPerFace + j];
                vertices.Add(CubeCorner.Sign(corner) * CubeHalfSize);
            }

            // 四角形を2つの三角形に分割します。
            triangles.AddRange(new[] { offset, offset + 1, offset + 2, offset, offset + 2, offset + 3 });
        }

        Mesh mesh = new Mesh { name = "Death Voxel Cube" };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void OnDestroy()
    {
        if (_mesh != null)
            Destroy(_mesh);

        if (_material != null)
            Destroy(_material);
    }
}
