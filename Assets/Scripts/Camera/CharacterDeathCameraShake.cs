using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Cinemachineの最終出力にだけ揺れを加えます。
/// Cameraや追従対象のTransformを直接変更しないため、追従・辺の切替と競合しません。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CinemachineCamera))]
public class CharacterDeathCameraShake : CinemachineExtension
{
    /// <summary>Settings未指定時にResourcesから読み込むAsset名です。</summary>
    private const string DefaultSettingsResourceName = "CameraShakeSettings";

    private const float MinEnemyMaxDistance = 0.1f;
    private const float MinDuration = 0.01f;
    private const float MinFrequency = 1f;

    /// <summary>回転量の比率計算で0除算を防ぐための最小振幅です。</summary>
    private const float MinReferenceAmplitude = 0.001f;

    [SerializeField] private CameraShakeSettings _settings;

    [Tooltip("縦揺れの位相の進み方（横揺れは2）。横とずらして直線的な揺れを避けます。")]
    [SerializeField] private float _verticalPhaseMultiplier = 2.74f;

    private float _startTime;
    private float _amplitude;
    private float _rotation;
    private float _duration;

    protected override void OnEnable()
    {
        base.OnEnable();
        _amplitude = 0f;
        LifeComponent.AnyCharacterDied += HandleDeath;
    }

    private void OnDisable()
    {
        LifeComponent.AnyCharacterDied -= HandleDeath;
        _amplitude = 0f;
    }

    private void HandleDeath(CharacterBase character, DeathCause cause)
    {
        if (!Application.isPlaying || character == null || character.gameObject.scene != gameObject.scene)
            return;

        if (_settings == null)
            _settings = Resources.Load<CameraShakeSettings>(DefaultSettingsResourceName);

        if (_settings == null || !_settings.Enabled)
            return;

        bool playerDied = character is PlayerCharacter;
        float amplitude = playerDied ? _settings.PlayerAmplitude : _settings.EnemyAmplitude;

        if (!playerDied)
        {
            Transform target = ComponentOwner != null ? ComponentOwner.Follow : null;
            if (target == null)
                return;

            // 遠くの敵の死亡は弱める。カメラ自体ではなくプレイヤーとの距離を使います。
            float distance = Vector3.Distance(target.position, character.transform.position);
            float maxDistance = Mathf.Max(MinEnemyMaxDistance, _settings.EnemyMaxDistance);
            amplitude *= Mathf.Clamp01(1f - distance / maxDistance);
        }

        if (amplitude <= 0f)
            return;

        float remaining = Envelope();

        // 複数死亡を足し算しないため、連鎖爆発でも過剰な揺れになりません。
        _amplitude = Mathf.Max(amplitude, _amplitude * remaining);

        // 回転量は、最大振幅に対する今回の振幅の比率で決めます。
        float referenceAmplitude = Mathf.Max(
            MinReferenceAmplitude,
            _settings.PlayerAmplitude,
            _settings.EnemyAmplitude);
        float rotationRatio = Mathf.Clamp01(amplitude / referenceAmplitude);
        _rotation = Mathf.Max(_settings.RotationDegrees * rotationRatio, _rotation * remaining);

        _duration = Mathf.Max(MinDuration, _settings.Duration);
        _startTime = Time.unscaledTime;
    }

    /// <summary>揺れの減衰率（1→0）。残り割合の2乗で、終わり際ほどゆっくり収まります。</summary>
    private float Envelope()
    {
        if (_duration <= 0f)
            return 0f;

        float remaining = 1f - Mathf.Clamp01((Time.unscaledTime - _startTime) / _duration);
        return remaining * remaining;
    }

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Finalize || !Application.isPlaying)
            return;

        // deltaTimeが負のときはCinemachineのリセット要求なので揺れを止めます。
        if (deltaTime < 0f)
        {
            _amplitude = 0f;
            return;
        }

        if (_settings == null || !_settings.Enabled || _amplitude <= 0f)
            return;

        float envelope = Envelope();
        if (envelope <= 0f)
        {
            _amplitude = 0f;
            return;
        }

        // 時刻で評価し、同じフレームに複数回評価されても進みすぎないようにします。
        float phase = (Time.unscaledTime - _startTime) * Mathf.Max(MinFrequency, _settings.Frequency);
        float x = Mathf.Sin(phase * Mathf.PI * 2f);

        // 縦揺れの周期を横とずらして、直線的な揺れを避けます。
        float y = Mathf.Sin(phase * Mathf.PI * _verticalPhaseMultiplier);

        state.PositionCorrection += state.RawOrientation * new Vector3(x, y, 0f) * (_amplitude * envelope);
        state.OrientationCorrection *= Quaternion.Euler(0f, 0f, x * _rotation * envelope);
    }
}
