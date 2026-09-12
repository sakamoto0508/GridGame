using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEngine;

/// <summary>
/// Player追従をCinemachineに任せたまま、フィールドを見る側を4方向に切り替えます。
/// Main Cameraを直接動かさず、Followのオフセットと仮想カメラの向きを変更します。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CinemachineCamera))]
public class GridCameraSideController : MonoBehaviour
{
    [SerializeField] private CameraSideSettings _settings;
    private CameraSideSettings _runtimeSettings;
    private CinemachineCamera _camera;
    private CinemachineFollow _follow;
    private Vector3 _initialOffset;
    private Quaternion _initialRotation;
    private float _currentAngle;
    private float _startAngle;
    private float _targetAngle;
    private float _elapsed;
    private bool _transitioning;
    private bool _initialized;
    private int _previousInputDirection;

    public bool Init()
    {
        if (_initialized) return true;
        _camera = GetComponent<CinemachineCamera>();
        _follow = GetComponent<CinemachineFollow>();
        if (_follow == null || !_follow.enabled)
        {
            Debug.LogWarning("辺の視点切替にはCinemachineのPosition ControlをFollowにしてください。", this);
            return false;
        }

        // Playerの向きでオフセットが回らないよう、ワールド基準で保持します。
        _follow.TrackerSettings.BindingMode = BindingMode.WorldSpace;
        _initialOffset = _follow.FollowOffset;
        _initialRotation = transform.rotation;
        if (_settings == null)
        {
            // 設定AssetがなくてもSOの既定値で動作します。
            _runtimeSettings = ScriptableObject.CreateInstance<CameraSideSettings>();
            _settings = _runtimeSettings;
        }
        _initialized = true;
        return true;
    }

    /// <summary>
    /// Xが正なら右辺、負なら左辺へ切り替えます。Yは今回使用しません。
    /// 同じ方向の入力が続いても1回だけ実行し、ゼロ入力で押下状態を解除します。
    /// </summary>
    public void HandleInput(Vector2 input)
    {
        int direction = input.x > 0f ? 1 : input.x < 0f ? -1 : 0;
        if (direction == _previousInputDirection) return;
        _previousInputDirection = direction;
        if (direction == 0 || Time.timeScale <= 0f) return;
        ChangeSide(direction);
    }

    private void ChangeSide(int step)
    {
        if (!_initialized || _camera == null || _follow == null ||
            _camera.Target.TrackingTarget == null) return;

        // 途中入力でも現在の表示角度から開始。到達予定の辺をさらに1つ進めます。
        _startAngle = _currentAngle;
        _targetAngle -= 90f * step;
        _elapsed = 0f;
        _transitioning = true;
        // 回転要求が受理された時だけ2D再生。追従移動やキーの押しっぱなしでは連続再生しません。
        AudioManager.Play(SoundId.CameraRotate);
        if (_settings.TransitionDuration <= 0f)
            UpdateTransition(0f);
    }

    private void Update()
    {
        if (_initialized && _transitioning && Time.deltaTime > 0f)
            UpdateTransition(Time.deltaTime);
    }

    private void UpdateTransition(float deltaTime)
    {
        _elapsed += deltaTime;
        float duration = _settings.TransitionDuration;
        float progress = duration <= 0f ? 1f : Mathf.Clamp01(_elapsed / duration);
        // 直線で位置を結ぶとPlayerに近づくため、角度を補間して円弧上を移動します。
        _currentAngle = Mathf.Lerp(_startAngle, _targetAngle, Mathf.SmoothStep(0f, 1f, progress));
        Quaternion turn = Quaternion.AngleAxis(_currentAngle, Vector3.up);
        _follow.FollowOffset = turn * _initialOffset;
        transform.rotation = turn * _initialRotation;

        // Cinemachineの追従履歴は無効化しません。Player追従と回転を並行して滑らかに処理。
        if (progress >= 1f)
        {
            _transitioning = false;
            _currentAngle = _targetAngle = Mathf.Repeat(_targetAngle, 360f);
        }
    }

    private void OnDestroy()
    {
        if (_runtimeSettings != null) Destroy(_runtimeSettings);
    }
}
