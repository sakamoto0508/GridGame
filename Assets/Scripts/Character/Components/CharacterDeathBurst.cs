using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>体の範囲から立方体Particleを放出する独立Effect。Character非表示後も再生します。</summary>
public class CharacterDeathBurst : MonoBehaviour
{
    private Mesh _mesh;
    private Material _material;

    public static void Spawn(CharacterBase character, Transform source, CharacterDeathVisualSettings settings, DeathCause cause)
    {
        CharacterDeathVisualSettings fallback = null;
        if (settings == null) settings = fallback = ScriptableObject.CreateInstance<CharacterDeathVisualSettings>();
        try
        {
            if (!settings.Enabled) return;
            Shader shader = Resources.Load<Shader>("CharacterDeathVoxel");
            if (shader == null) { Debug.LogWarning("CharacterDeathVoxel Shaderが見つかりません。", source); return; }
            Bounds body = new Bounds(source.position, new Vector3(0.6f, 0.9f, 0.6f));
            bool hasBounds = false;
            foreach (Renderer renderer in source.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled || !(renderer is MeshRenderer || renderer is SkinnedMeshRenderer)) continue;
                if (!hasBounds) { body = renderer.bounds; hasBounds = true; }
                else body.Encapsulate(renderer.bounds);
            }
            // 武器や装飾が大きい場合でも、出現範囲が画面全体へ広がらないよう制限します。
            Vector3 extent = Vector3.Min(body.extents, Vector3.one * 1.5f);
            GameObject root = new GameObject("Character Death Voxels");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, source.gameObject.scene);
            root.transform.position = body.center;
            CharacterDeathBurst effect = root.AddComponent<CharacterDeathBurst>();
            effect._mesh = CreateCube();
            effect._material = new Material(shader);
            ParticleSystem particles = root.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            float maximumLife = Mathf.Clamp(Mathf.Max(settings.Lifetime.x, settings.Lifetime.y), 0.1f, 5);
            var main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = maximumLife;
            main.startLifetime = maximumLife;
            main.startSpeed = 0;
            main.maxParticles = Mathf.Clamp(settings.FragmentCount, 10, 200);
            main.gravityModifier = Mathf.Clamp(settings.Gravity, 0, 4);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            main.stopAction = ParticleSystemStopAction.Destroy;
            main.startRotation3D = true;
            var emission = particles.emission;
            emission.enabled = false;
            var shape = particles.shape;
            shape.enabled = false;
            var size = particles.sizeOverLifetime;
            size.enabled = true;
            // 最後だけ小さくして消す。不透明な四角形を保ち、半透明の煙にはしません。
            size.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(new Keyframe(0, 1), new Keyframe(0.65f, 1), new Keyframe(1, 0)));
            var rendererSystem = particles.GetComponent<ParticleSystemRenderer>();
            rendererSystem.renderMode = ParticleSystemRenderMode.Mesh;
            rendererSystem.mesh = effect._mesh;
            rendererSystem.sharedMaterial = effect._material;
            rendererSystem.shadowCastingMode = ShadowCastingMode.Off;
            rendererSystem.receiveShadows = false;
            rendererSystem.SetActiveVertexStreams(new List<ParticleSystemVertexStream> { ParticleSystemVertexStream.Position, ParticleSystemVertexStream.Normal, ParticleSystemVertexStream.Color });
            // Stage/AI用のUnityEngine.Randomの状態を変更しません。
            System.Random random = new System.Random(unchecked(source.GetInstanceID() * 397 ^ System.Environment.TickCount));
            particles.useAutoRandomSeed = false;
            particles.randomSeed = (uint)random.Next(1, int.MaxValue);
            particles.Play();
            for (int i = 0; i < main.maxParticles; i++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2;
                float speed = Range(random, settings.HorizontalSpeed, 0, 10);
                float upward = Range(random, settings.UpwardSpeed, 0, 10);
                if (cause == DeathCause.FallingBlock) { speed *= 1.25f; upward *= 0.6f; }
                var fragment = new ParticleSystem.EmitParams
                {
                    position = body.center + Vector3.Scale(extent, new Vector3(Signed(random), Signed(random), Signed(random))),
                    velocity = new Vector3(Mathf.Cos(angle) * speed, upward, Mathf.Sin(angle) * speed),
                    startLifetime = Range(random, settings.Lifetime, 0.1f, maximumLife),
                    startSize = Range(random, settings.FragmentSize, 0.01f, 0.4f),
                    startColor = random.NextDouble() < settings.AccentRatio ?
                        (character is PlayerCharacter ? settings.PlayerAccent : settings.EnemyAccent) : settings.BodyColor,
                    rotation3D = new Vector3(Signed(random), Signed(random), Signed(random)) * 180,
                    angularVelocity3D = new Vector3(Signed(random), Signed(random), Signed(random)) * 8
                };
                particles.Emit(fragment, 1);
            }
        }
        finally { if (fallback != null) Destroy(fallback); }
    }

    private static float Signed(System.Random random) => (float)random.NextDouble() * 2 - 1;
    private static float Range(System.Random random, Vector2 values, float min, float max)
        => Mathf.Lerp(Mathf.Clamp(Mathf.Min(values.x, values.y), min, max), Mathf.Clamp(Mathf.Max(values.x, values.y), min, max), (float)random.NextDouble());

    private static Mesh CreateCube()
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        int[] faces = { 0,2,3,1, 4,5,7,6, 0,4,6,2, 1,3,7,5, 2,6,7,3, 0,1,5,4 };
        for (int face = 0; face < 6; face++)
        {
            int offset = vertices.Count;
            for (int j = 0; j < 4; j++)
            {
                int corner = faces[face * 4 + j];
                vertices.Add(new Vector3((corner & 1) == 0 ? -0.5f : 0.5f, (corner & 2) == 0 ? -0.5f : 0.5f, (corner & 4) == 0 ? -0.5f : 0.5f));
            }
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
        if (_mesh != null) Destroy(_mesh);
        if (_material != null) Destroy(_material);
    }
}
