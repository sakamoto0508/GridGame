using UnityEngine;

/// <summary>記号はカメラへ向け、リングだけ回転。Item本体の位置と落下処理には触れません。</summary>
public class NeonItemView : MonoBehaviour
{
    [SerializeField] private Transform _visual;
    [SerializeField] private Transform _ring;
    [SerializeField] private ItemVisualSettings _settings;
    private Camera _camera;
    private float _angle;

    private void OnEnable()
    {
        _angle = 0;
        if (_ring != null) _ring.localRotation = Quaternion.identity;
    }

    private void LateUpdate()
    {
        if (_visual == null || _settings == null) return;
        if (_camera == null) _camera = Camera.main;
        if (_settings.FaceCamera && _camera != null) _visual.rotation = _camera.transform.rotation;
        _visual.localScale = Vector3.one * _settings.VisualScale;
        _angle = Mathf.Repeat(_angle + _settings.RingRotationSpeed * Time.deltaTime, 360);
        if (_ring != null) _ring.localRotation = Quaternion.Euler(0, 0, _angle);
    }
}
