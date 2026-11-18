using System.Collections.Generic;
using UnityEngine;

/// <summary>天井の内側にSpot Lightを配置します。Block生成や表示切替には依存しません。</summary>
public class StageLightingController : MonoBehaviour
{
    /// <summary>セル中心からセル端までの距離（セル単位）です。</summary>
    private const float HalfCell = 0.5f;

    /// <summary>真下を向くライトの回転です。</summary>
    private static readonly Quaternion DownwardRotation = Quaternion.Euler(90f, 0f, 0f);

    private readonly List<Light> _lights = new();

    private GridManager _grid;
    private StageLightingSettings _settings;
    private StageLightingSettings _runtimeSettings;
    private Transform _root;

    public void Init(GridManager grid, StageLightingSettings settings)
    {
        _grid = grid;

        // 設定がなければ既定値のSettingsを実行時に作って使います。
        if (settings == null)
        {
            if (_runtimeSettings == null)
                _runtimeSettings = ScriptableObject.CreateInstance<StageLightingSettings>();

            settings = _runtimeSettings;
        }

        _settings = settings;
        RefreshLighting();
    }

    /// <summary>再生成時にも既存の照明を使い、必要な数だけ追加します。</summary>
    [ContextMenu("Refresh Lighting")]
    public void RefreshLighting()
    {
        if (_grid == null || _settings == null)
            return;

        if (_root == null)
        {
            GameObject root = new GameObject("LightingRoot");
            root.transform.SetParent(transform, false);
            _root = root.transform;
        }

        _root.gameObject.SetActive(isActiveAndEnabled && _settings.Enabled);

        int count = _settings.Enabled ? _settings.CountX * _settings.CountZ : 0;
        while (_lights.Count < count)
        {
            GameObject lamp = new GameObject("Ceiling Spot " + (_lights.Count + 1));
            lamp.transform.SetParent(_root, false);
            _lights.Add(lamp.AddComponent<Light>());
        }

        Vector3 origin = _grid.GetWorldPosition(Vector3Int.zero);
        float cell = _grid.CellSize;
        Vector3 extent = (Vector3)_grid.Size * cell;

        // 外殻天井の下面は(Size.y-0.5)セル。そこから少し内部へ下げます。
        float height = Mathf.Max(0f, _grid.Size.y - HalfCell - _settings.CeilingInsetInCells) * cell;

        for (int i = 0; i < _lights.Count; i++)
        {
            Light light = _lights[i];
            light.gameObject.SetActive(i < count);
            if (i >= count)
                continue;

            // フィールドをCountX×CountZに等分し、各区画の中央に配置します。
            int x = i % _settings.CountX;
            int z = i / _settings.CountX;
            Vector3 localPosition = new Vector3(
                -cell * HalfCell + extent.x * (x + HalfCell) / _settings.CountX,
                height,
                -cell * HalfCell + extent.z * (z + HalfCell) / _settings.CountZ);

            light.transform.SetPositionAndRotation(origin + localPosition, DownwardRotation);
            light.type = LightType.Spot;
            light.color = _settings.Color;
            light.intensity = _settings.Intensity;
            light.spotAngle = _settings.SpotAngle;
            light.innerSpotAngle = _settings.InnerSpotAngle;
            light.range = Mathf.Max(cell, extent.magnitude * _settings.RangeMultiplier);
            light.shadows = _settings.Shadows;
        }
    }

    private void OnEnable()
    {
        if (_root != null && _settings != null)
            _root.gameObject.SetActive(_settings.Enabled);
    }

    private void OnDisable()
    {
        if (_root != null)
            _root.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_root != null)
            Destroy(_root.gameObject);

        if (_runtimeSettings != null)
            Destroy(_runtimeSettings);
    }
}
