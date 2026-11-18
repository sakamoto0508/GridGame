using UnityEngine;

/// <summary>残り時間に応じて発光色と点滅を変更。ルート位置・Collider・爆発処理は変更しません。</summary>
public class NeonBombView : MonoBehaviour
{
    private static readonly int ColorId = Shader.PropertyToID("_BaseColor");

    [SerializeField] private Bomb _bomb;
    [SerializeField] private Transform _visual;
    [SerializeField] private Renderer[] _lights;
    [SerializeField] private BombVisualSettings _settings;

    [Tooltip("点滅が最も暗いときの明るさの倍率（0〜1）。")]
    [SerializeField, Range(0f, 1f)] private float _minPulseBrightness = 0.45f;

    private MaterialPropertyBlock _properties;
    private float _phase;
    private Vector3 _baseScale;

    private void Awake()
    {
        _properties = new MaterialPropertyBlock();

        if (_visual != null)
            _baseScale = _visual.localScale;
    }

    private void LateUpdate()
    {
        if (_bomb == null || _settings == null || _visual == null)
            return;

        // 残り時間が警告秒数以下なら警告色・高速点滅に切り替えます。
        float remaining = _bomb.RemainingFuseTime;
        bool warning = remaining > 0 && remaining <= _settings.WarningSeconds;
        float speed = warning ? _settings.WarningPulseSpeed : _settings.NormalPulseSpeed;

        // 位相を0〜1で回し、サイン波を0〜1に変換して点滅の強さにします。
        _phase = Mathf.Repeat(_phase + Time.deltaTime * speed, 1);
        float pulse = Mathf.Sin(_phase * Mathf.PI * 2) * 0.5f + 0.5f;

        Color baseColor = warning ? _settings.WarningColor : _settings.NormalColor;
        Color color = baseColor * Mathf.Lerp(_minPulseBrightness, 1, pulse);
        color.a = 1;

        _properties.SetColor(ColorId, color);
        foreach (Renderer lightPart in _lights)
        {
            if (lightPart != null)
                lightPart.SetPropertyBlock(_properties);
        }

        _visual.localScale = _baseScale * (1 + pulse * _settings.PulseScale);
    }
}
