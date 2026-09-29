using UnityEngine;
using UnityEngine.Rendering;

/// <summary>取得者に追従する短いネオン演出。消費されるItemから独立して再生します。</summary>
public class ItemPickupBurst : MonoBehaviour
{
    private const int RingSegments = 48;
    private Transform _target;
    private ItemPickupVisualSettings _settings;
    private LineRenderer _ring;
    private Material _material;
    private Color _color;
    private float _elapsed;

    public static void Spawn(CharacterBase character, ItemType type)
    {
        ItemPickupVisualSettings settings = Resources.Load<ItemPickupVisualSettings>("ItemPickupVisualSettings");
        if (character == null || settings == null || !settings.Enabled) return;
        Shader shader = Resources.Load<Shader>("NeonExplosionParticle");
        if (shader == null) return;

        GameObject root = new GameObject("Item Pickup Glow");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, character.gameObject.scene);
        root.transform.position = character.transform.position;
        ItemPickupBurst effect = root.AddComponent<ItemPickupBurst>();
        // 初期化中に失敗しても、生成途中のObjectとMaterialを残しません。
        try { effect.Initialize(character.transform, settings, shader, type); }
        catch { Destroy(root); throw; }
    }

    private void Initialize(Transform target, ItemPickupVisualSettings settings, Shader shader, ItemType type)
    {
        _target = target;
        _settings = settings;
        _color = type == ItemType.BombPower ? settings.PowerColor : settings.CountColor;
        _material = new Material(shader) { name = "Item Pickup Glow (Runtime)" };
        // HDR色はMaterial側に渡し、ParticleのColor32変換で明るさが失われるのを防ぎます。
        _material.SetColor("_TintColor", _color);

        // ParticleSystemRendererとLineRendererを同じObjectへ追加しないよう分離します。
        GameObject ringObject = new GameObject("Pickup Ring");
        ringObject.transform.SetParent(transform, false);
        _ring = ringObject.AddComponent<LineRenderer>();
        _ring.sharedMaterial = _material;
        _ring.useWorldSpace = false;
        _ring.loop = true;
        _ring.positionCount = RingSegments;
        _ring.shadowCastingMode = ShadowCastingMode.Off;
        _ring.receiveShadows = false;
        UpdateRing(0f);

        ParticleSystem particles = gameObject.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        float duration = Mathf.Max(0.05f, settings.Duration);
        var main = particles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = duration;
        main.startLifetime = duration;
        main.startSpeed = 0f;
        main.startColor = Color.white;
        main.startSize = Mathf.Max(0.01f, settings.ParticleSize);
        main.maxParticles = Mathf.Clamp(settings.ParticleCount, 1, 64);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.useUnscaledTime = true;
        var emission = particles.emission;
        emission.enabled = false;
        var shape = particles.shape;
        shape.enabled = false;
        var size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
        var fade = particles.colorOverLifetime;
        fade.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        fade.color = gradient;
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = _material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        particles.Play();
        // 等間隔＋黄金角の高さずらし。ゲーム側の乱数シードを消費しません。
        for (int i = 0; i < main.maxParticles; i++)
        {
            float angle = i * 2.399963f;
            Vector3 outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            particles.Emit(new ParticleSystem.EmitParams
            {
                position = outward * Mathf.Max(0.01f, settings.StartRadius) + Vector3.up * (settings.StartHeight + (i % 5) * 0.1f),
                velocity = outward * 0.25f + Vector3.up * Mathf.Max(0f, settings.ParticleRiseSpeed)
            }, 1);
        }
    }

    private void LateUpdate()
    {
        // 死亡直後に「強化中」の光が残らないよう、取得者が消えたら終了します。
        if (_target == null || !_target.gameObject.activeInHierarchy || _settings == null)
        {
            Destroy(gameObject);
            return;
        }
        transform.position = _target.position;
        _elapsed += Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01(_elapsed / Mathf.Max(0.05f, _settings.Duration));
        UpdateRing(progress);
        if (progress >= 1f) Destroy(gameObject);
    }

    private void UpdateRing(float progress)
    {
        float radius = Mathf.Lerp(_settings.StartRadius, _settings.EndRadius, 1f - (1f - progress) * (1f - progress));
        float height = _settings.StartHeight + _settings.RiseHeight * progress;
        _ring.widthMultiplier = Mathf.Max(0.001f, _settings.RingWidth) * (1f - progress * 0.7f);
        Color color = Color.white;
        color.a = 1f - progress;
        _ring.startColor = _ring.endColor = color;
        for (int i = 0; i < RingSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / RingSegments;
            _ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius));
        }
    }

    private void OnDestroy()
    {
        if (_material != null) Destroy(_material);
    }
}
