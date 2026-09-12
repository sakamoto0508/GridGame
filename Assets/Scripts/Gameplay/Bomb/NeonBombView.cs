using UnityEngine;

/// <summary>残り時間に応じて発光色と点滅を変更。ルート位置・Collider・爆発処理は変更しません。</summary>
public class NeonBombView : MonoBehaviour
{
    [SerializeField] private Bomb _bomb;
    [SerializeField] private Transform _visual;
    [SerializeField] private Renderer[] _lights;
    [SerializeField] private BombVisualSettings _settings;
    private MaterialPropertyBlock _properties;
    private float _phase;
    private Vector3 _baseScale;
    private static readonly int ColorId = Shader.PropertyToID("_BaseColor");

    private void Awake()
    {
        _properties = new MaterialPropertyBlock();
        if (_visual != null) _baseScale = _visual.localScale;
    }
    private void LateUpdate()
    {
        if (_bomb == null || _settings == null || _visual == null) return;
        float remaining = _bomb.RemainingFuseTime;
        bool warning = remaining > 0 && remaining <= _settings.WarningSeconds;
        float speed = warning ? _settings.WarningPulseSpeed : _settings.NormalPulseSpeed;
        _phase = Mathf.Repeat(_phase + Time.deltaTime * speed, 1);
        float pulse = Mathf.Sin(_phase * Mathf.PI * 2) * 0.5f + 0.5f;
        Color color = (warning ? _settings.WarningColor : _settings.NormalColor) * Mathf.Lerp(0.45f, 1, pulse);
        color.a = 1;
        _properties.SetColor(ColorId, color);
        foreach (Renderer lightPart in _lights)
            if (lightPart != null) lightPart.SetPropertyBlock(_properties);
        _visual.localScale = _baseScale * (1 + pulse * _settings.PulseScale);
    }
}
