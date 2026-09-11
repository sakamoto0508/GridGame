using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 屋上台座と四方の都市を少数の結合Meshで描画します。
/// ColliderやGrid登録を持たず、ゲームの乱数にも影響しません。
/// </summary>
[DefaultExecutionOrder(10001)]
public class RooftopBackgroundController : MonoBehaviour
{
    private GridManager _grid;
    private RooftopBackgroundSettings _settings;
    private RooftopBackgroundSettings _fallback;
    private GameObject _root;
    private Material _material;
    private Camera _camera;
    private readonly List<Mesh> _meshes = new List<Mesh>();
    private readonly List<MeshRenderer> _citySides = new List<MeshRenderer>();
    private static readonly Vector3[] Sides = { Vector3.right, Vector3.left, Vector3.forward, Vector3.back };

    /// <summary>StageGeneratorから試合開始前に呼びます。SO未設定でも既定外観を表示します。</summary>
    public void Init(GridManager grid, RooftopBackgroundSettings settings)
    {
        Clear();
        _grid = grid;
        if (grid == null || grid.Size.x <= 0 || grid.Size.z <= 0) return;
        if (settings == null && _fallback == null) _fallback = ScriptableObject.CreateInstance<RooftopBackgroundSettings>();
        _settings = settings != null ? settings : _fallback;
        if (!_settings.Enabled) return;
        Shader shader = Resources.Load<Shader>("NeonCityBackground");
        if (shader == null) { Debug.LogError("NeonCityBackground shaderが見つかりません。", this); return; }
        _material = new Material(shader) { name = "Rooftop Background (Runtime)" };
        _root = new GameObject("Rooftop Background (Visual Only)");
        _root.transform.SetParent(transform, false);
        // GridManagerと同じワールド座標系。親の位置や拡大率に背景サイズを左右されません。
        _root.transform.position = (grid.GetWorldPosition(Vector3Int.zero) + grid.GetWorldPosition(grid.Size - Vector3Int.one)) * 0.5f;
        _root.transform.rotation = Quaternion.identity;
        _root.transform.localScale = Vector3.one;
        float cell = Vector3.Distance(grid.GetWorldPosition(Vector3Int.zero), grid.GetWorldPosition(Vector3Int.right));
        _root.transform.position = new Vector3(_root.transform.position.x, grid.GetWorldPosition(Vector3Int.zero).y, _root.transform.position.z);
        // 台座上面は外殻床の底(-1.5セル)より下に置き、床と重ねません。
        float top = -1.52f;
        float depth = Mathf.Max(1, _settings.PlatformDepth);
        float width = grid.Size.x + 2 + Mathf.Max(0, _settings.PlatformMargin) * 2;
        float length = grid.Size.z + 2 + Mathf.Max(0, _settings.PlatformMargin) * 2;
        Builder platform = new Builder();
        platform.Box(new Vector3(0, top - depth * 0.5f, 0), new Vector3(width, depth, length), _settings.PlatformColor);
        // 細いシアン帯だけで輪郭を示し、盤面より目立たせません。
        foreach (Vector3 side in Sides)
        {
            bool x = side.x != 0;
            platform.Box(new Vector3(side.x * width * 0.5f, top - 0.25f, side.z * length * 0.5f),
                x ? new Vector3(0.035f, 0.06f, length) : new Vector3(width, 0.06f, 0.035f), _settings.Cyan * 0.65f);
        }
        Build("Rooftop Platform", platform, cell);
        System.Random random = new System.Random(_settings.Seed);
        for (int sideIndex = 0; sideIndex < 4; sideIndex++)
        {
            Builder city = new Builder();
            Vector3 normal = Sides[sideIndex];
            Vector3 tangent = new Vector3(-normal.z, 0, normal.x);
            float span = (sideIndex < 2 ? length : width) * 2;
            int count = Mathf.Clamp(_settings.BuildingsPerSide, 2, 16);
            for (int i = 0; i < count; i++)
            {
                float w = Range(random, _settings.BuildingWidth, 1, 12);
                float h = Range(random, _settings.BuildingHeight, 2, 60);
                Vector3 p = normal * ((sideIndex < 2 ? width : length) * 0.5f + Mathf.Max(3, _settings.CityGap) + (float)random.NextDouble() * 5)
                    + tangent * ((i + 0.5f) / count * span - span * 0.5f);
                float bottom = top - depth - 10;
                p.y = bottom + h * 0.5f;
                city.Box(p, new Vector3(w, h, w), _settings.BuildingColor * (0.75f + (float)random.NextDouble() * 0.5f));
                // 四面に小窓。窓も同じMeshなので窓の数だけGameObjectを増やしません。
                foreach (Vector3 face in Sides)
                {
                    Vector3 along = new Vector3(-face.z, 0, face.x);
                    for (float y = 1; y < h - 0.5f; y += 1.25f)
                    for (float u = -w * 0.5f + 0.6f; u < w * 0.5f - 0.3f; u += 0.9f)
                    {
                        if (random.NextDouble() > _settings.LitWindowChance) continue;
                        Vector3 window = p + face * (w * 0.5f + 0.015f) + along * u;
                        window.y = bottom + y;
                        Color color = random.NextDouble() < 0.2 ? _settings.Green : _settings.Cyan;
                        city.Box(window, face.x != 0 ? new Vector3(0.02f, 0.18f, 0.35f) : new Vector3(0.35f, 0.18f, 0.02f), color * _settings.WindowBrightness);
                    }
                }
            }
            _citySides.Add(Build("City Side " + sideIndex, city, cell));
        }
        _root.SetActive(isActiveAndEnabled);
        UpdateVisibility();
    }

    private static float Range(System.Random random, Vector2 range, float min, float max)
    {
        float a = Mathf.Clamp(Mathf.Min(range.x, range.y), min, max);
        float b = Mathf.Clamp(Mathf.Max(range.x, range.y), min, max);
        return Mathf.Lerp(a, b, (float)random.NextDouble());
    }

    private MeshRenderer Build(string label, Builder builder, float cell)
    {
        GameObject part = new GameObject(label, typeof(MeshFilter), typeof(MeshRenderer));
        part.transform.SetParent(_root.transform, false);
        // ワールド座標で生成したMeshを使い、親階層のスケールを相殺します。
        part.transform.SetParent(null, true);
        part.transform.localScale = Vector3.one * cell;
        part.transform.SetParent(_root.transform, true);
        Mesh mesh = builder.Create(label);
        _meshes.Add(mesh);
        part.GetComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = part.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = _material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return renderer;
    }

    private void LateUpdate() => UpdateVisibility();

    /// <summary>Cinemachine更新後の実カメラを見て、手前側の都市だけ隠します。</summary>
    private void UpdateVisibility()
    {
        if (_settings == null) return;
        if (_camera == null) _camera = Camera.main;
        if (_camera == null) return;
        Vector3 near = -_camera.transform.forward;
        near.y = 0;
        near.Normalize();
        for (int i = 0; i < _citySides.Count; i++)
            _citySides[i].forceRenderingOff = _settings.HideNearSideBuildings && Vector3.Dot(near, Sides[i]) > 0.05f;
    }

    [ContextMenu("Rebuild Background (Play Mode)")]
    private void Rebuild() { if (Application.isPlaying) Init(_grid, _settings); }
    private void OnEnable() { if (_root != null) _root.SetActive(true); }
    private void OnDisable() { if (_root != null) _root.SetActive(false); }
    private void OnDestroy() { Clear(); if (_fallback != null) Destroy(_fallback); }
    private void Clear()
    {
        if (_root != null) { _root.SetActive(false); Destroy(_root); }
        foreach (Mesh mesh in _meshes) Destroy(mesh);
        _meshes.Clear();
        _citySides.Clear();
        if (_material != null) Destroy(_material);
    }

    /// <summary>面ごとの明暗を頂点色で付ける簡易ボックス結合器。ライト不要です。</summary>
    private sealed class Builder
    {
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Color> _colors = new List<Color>();
        private readonly List<int> _triangles = new List<int>();
        public void Box(Vector3 center, Vector3 size, Color color)
        {
            Vector3 half = size * 0.5f;
            Vector3[] corners = new Vector3[8];
            for (int i = 0; i < 8; i++) corners[i] = center + Vector3.Scale(half, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            int[] faces = { 0,2,3,1, 4,5,7,6, 0,4,6,2, 1,3,7,5, 2,6,7,3, 0,1,5,4 };
            float[] shades = { 0.8f, 0.8f, 0.65f, 0.9f, 1, 0.5f };
            for (int face = 0; face < 6; face++)
            {
                int start = _vertices.Count;
                for (int j = 0; j < 4; j++) { _vertices.Add(corners[faces[face * 4 + j]]); _colors.Add(color * shades[face]); }
                _triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            }
        }
        public Mesh Create(string label)
        {
            Mesh mesh = new Mesh { name = label, indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(_vertices);
            mesh.SetColors(_colors);
            mesh.SetTriangles(_triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
