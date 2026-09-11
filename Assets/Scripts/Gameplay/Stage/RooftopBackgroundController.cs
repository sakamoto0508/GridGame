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
    [SerializeField] private GameObject _bakedRoot;
    [SerializeField] private MeshRenderer[] _bakedCitySides = new MeshRenderer[0];
    public GameObject BakedRoot => _bakedRoot;
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
        // シーンへ保存済みなら実行時の再生成は行わず、編集した配置を使用します。
        if (_bakedRoot != null)
        {
            _bakedRoot.SetActive(_settings.Enabled && isActiveAndEnabled);
            UpdateVisibility();
            return;
        }
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
        // 低い街は手前側のビル非表示処理から独立させ、回転しても空白に戻さないようにします。
        float cityGround = Mathf.Min(-Mathf.Max(12, _settings.CityGroundDepth), top - depth - 12);
        if (_settings.ShowLowerCity) GenerateLowerCity(width, length, top - depth, cityGround, cell);
        System.Random random = new System.Random(_settings.Seed);
        for (int sideIndex = 0; sideIndex < 4; sideIndex++)
        {
            Builder city = new Builder();
            Vector3 normal = Sides[sideIndex];
            Vector3 tangent = new Vector3(-normal.z, 0, normal.x);
            float span = (sideIndex < 2 ? length : width) * 2;
            int count = Mathf.Clamp(_settings.BuildingsPerSide, 2, 16);
            for (int row = 0; row < Mathf.Clamp(_settings.SkylineRows, 1, 3); row++)
            for (int i = 0; i < count; i++)
            {
                float w = Range(random, _settings.BuildingWidth, 1, 12);
                float h = Range(random, _settings.BuildingHeight, 2, 60);
                Vector3 p = normal * ((sideIndex < 2 ? width : length) * 0.5f + Mathf.Max(3, _settings.CityGap) + (float)random.NextDouble() * 5)
                    + tangent * ((i + 0.5f) / count * span - span * 0.5f);
                p += normal * (row * Mathf.Max(10, _settings.SkylineRowSpacing));
                p += tangent * (((i + 0.5f) / count - 0.5f) * row * 16);
                float bottom = top - depth - 10;
                if (_settings.FitSkylineToGridHeight)
                {
                    // 頂部をグリッド最高地点より上へ置き、奥の列はさらに高く・幅広くします。
                    // ワールド固定の建物寸法なので、Player上昇中に建物が伸縮しません。
                    float roof = grid.Size.y + Mathf.Max(2, _settings.SkylineHeightAboveGrid)
                        + (float)random.NextDouble() * 6 + row * 10;
                    h = roof - bottom;
                    w = Mathf.Max(w, h * 0.14f);
                }
                w *= 1 + row * 0.15f;
                p.y = bottom + h * 0.5f;
                city.Box(p, new Vector3(w, h, w), _settings.BuildingColor * (0.75f + (float)random.NextDouble() * 0.5f));
                // 既存の高層ビルも地面まで支柱を延ばし、空中に浮いた底面をなくします。
                if (_settings.ShowLowerCity && bottom > cityGround)
                    city.Box(new Vector3(p.x, (bottom + cityGround) * 0.5f, p.z),
                        new Vector3(w, bottom - cityGround, w), LowerCityColor(_settings.BuildingColor, 0.7f));
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

    /// <summary>台座の建物胴体・低層街区・道路灯を生成。すべて描画専用でColliderはありません。</summary>
    private void GenerateLowerCity(float width, float length, float platformBottom, float ground, float cell)
    {
        // 別の乱数列を使用するため、下方都市の設定を変えても既存の遠景ビルは変化しません。
        System.Random random = new System.Random(unchecked(_settings.Seed + 7919));
        Builder tower = new Builder();
        float towerWidth = width * 0.94f;
        float towerLength = length * 0.94f;
        float towerHeight = platformBottom - ground;
        tower.Box(new Vector3(0, (platformBottom + ground) * 0.5f, 0),
            new Vector3(towerWidth, towerHeight, towerLength), LowerCityColor(_settings.PlatformColor, 0.25f));
        foreach (Vector3 face in Sides)
        {
            bool xFace = face.x != 0;
            Vector3 along = new Vector3(-face.z, 0, face.x);
            float faceWidth = xFace ? towerLength : towerWidth;
            float faceDistance = (xFace ? towerWidth : towerLength) * 0.5f;
            // 少数の縦リブと間欠的な窓で建物の高さを伝えます。下ほどコントラストを抑えます。
            for (float u = -faceWidth * 0.5f + 1; u < faceWidth * 0.5f; u += 2.5f)
            {
                Vector3 rib = face * (faceDistance + 0.025f) + along * u;
                rib.y = (platformBottom + ground) * 0.5f;
                tower.Box(rib, xFace ? new Vector3(0.06f, towerHeight, 0.12f) : new Vector3(0.12f, towerHeight, 0.06f),
                    LowerCityColor(_settings.PlatformColor * 1.3f, 0.4f));
                for (float y = ground + 2; y < platformBottom - 0.6f; y += 2)
                {
                    if (random.NextDouble() > 0.35) continue;
                    Vector3 window = rib + face * 0.04f;
                    window.y = y;
                    float haze = 1 - Mathf.InverseLerp(ground, platformBottom, y);
                    tower.Box(window, xFace ? new Vector3(0.035f, 0.25f, 0.6f) : new Vector3(0.6f, 0.25f, 0.035f),
                        LowerCityColor(_settings.Cyan * _settings.LowerCityLightBrightness, haze));
                }
            }
        }
        Build("Rooftop Building Body", tower, cell);

        Builder streets = new Builder();
        Builder buildings = new Builder();
        float spacing = Mathf.Clamp(_settings.StreetSpacing, 6, 18);
        float radius = Mathf.Max(_settings.LowerCityRadius, Mathf.Max(width, length) * 0.5f + spacing * 2);
        // メッシュの過剰生成を防ぎ、設定値が大きくても最大32×32街区に制限します。
        int count = Mathf.Clamp(Mathf.CeilToInt(radius / spacing), 3, 16);
        spacing = radius / count;
        streets.Box(new Vector3(0, ground - 0.3f, 0), new Vector3(radius * 2.4f, 0.5f, radius * 2.4f), _settings.LowerCityGroundColor);
        for (int i = -count; i <= count; i++)
        {
            float offset = i * spacing;
            Color roadColor = LowerCityColor(_settings.Cyan * _settings.LowerCityLightBrightness, Mathf.Abs(offset) / radius);
            // 面として光らせず細い道路灯だけを描き、対戦グリッドと区別します。
            streets.Box(new Vector3(offset, ground, 0), new Vector3(0.06f, 0.02f, radius * 2), roadColor);
            streets.Box(new Vector3(0, ground, offset), new Vector3(radius * 2, 0.02f, 0.06f), roadColor);
        }
        for (int x = -count; x < count; x++)
        for (int z = -count; z < count; z++)
        {
            Vector3 center = new Vector3((x + 0.5f) * spacing, 0, (z + 0.5f) * spacing);
            float bw = spacing * (0.4f + (float)random.NextDouble() * 0.25f);
            // 中央建物と交差する街区は空けます。
            if (Mathf.Abs(center.x) < width * 0.5f + bw * 0.5f + 1 && Mathf.Abs(center.z) < length * 0.5f + bw * 0.5f + 1) continue;
            float h = Range(random, _settings.LowerBuildingHeight, 1, Mathf.Max(1, platformBottom - ground - 4));
            center.y = ground + h * 0.5f;
            float haze = Mathf.Clamp01(new Vector2(center.x, center.z).magnitude / radius);
            buildings.Box(center, new Vector3(bw, h, bw), LowerCityColor(_settings.BuildingColor * 1.6f, haze));
            // 屋上の小さな灯り。街灯用Lightを大量に作らず頂点カラーで表現します。
            Color lamp = (random.NextDouble() < 0.15 ? _settings.Green : _settings.Cyan) * _settings.LowerCityLightBrightness;
            buildings.Box(new Vector3(center.x, ground + h + 0.025f, center.z), new Vector3(bw * 0.5f, 0.03f, 0.12f), LowerCityColor(lamp, haze));
        }
        Build("Lower City Streets", streets, cell);
        Build("Lower City Buildings", buildings, cell);
    }

    /// <summary>背景専用の色調整。シーン全体のFogやライト設定は変更しません。</summary>
    private Color LowerCityColor(Color color, float distanceFactor)
        => Color.Lerp(color, _settings.LowerCityHazeColor,
            Mathf.Clamp01(_settings.LowerCityHaze) * Mathf.Lerp(0.3f, 1, Mathf.Clamp01(distanceFactor)));

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
            if (_citySides[i] != null) _citySides[i].forceRenderingOff = _settings.HideNearSideBuildings && Vector3.Dot(near, Sides[i % 4]) > 0.05f;
        for (int i = 0; i < _bakedCitySides.Length; i++)
            if (_bakedCitySides[i] != null) _bakedCitySides[i].forceRenderingOff = _settings.HideNearSideBuildings && Vector3.Dot(near, Sides[i % 4]) > 0.05f;
    }

    [ContextMenu("Rebuild Background (Play Mode)")]
    private void Rebuild() { if (Application.isPlaying) Init(_grid, _settings); }
    private void OnEnable() { if (_root != null) _root.SetActive(true); }
    private void OnDisable()
    {
        if (_root != null) _root.SetActive(false);
        // シーン保存された背景は破棄しません。カメラによる非表示だけ解除します。
        foreach (MeshRenderer side in _bakedCitySides) if (side != null) side.forceRenderingOff = false;
    }
    private void OnDestroy() { Clear(); if (_fallback != null) DestroyGenerated(_fallback); }
    private void Clear()
    {
        if (_root != null) { _root.SetActive(false); DestroyGenerated(_root); }
        foreach (Mesh mesh in _meshes) DestroyGenerated(mesh);
        _meshes.Clear();
        _citySides.Clear();
        if (_material != null) DestroyGenerated(_material);
    }

    private static void DestroyGenerated(Object target)
    {
#if UNITY_EDITOR
        // ベイク済みMesh/Material Assetは一時生成物の片付けでは削除しません。
        if (UnityEditor.EditorUtility.IsPersistent(target)) return;
        if (!Application.isPlaying) { DestroyImmediate(target); return; }
#endif
        Destroy(target);
    }

#if UNITY_EDITOR
    /// <summary>Editorベイク用。一時生成物の所有権をシーンへ渡します。</summary>
    public GameObject DetachForSceneBake()
    {
        GameObject result = _root;
        if (result != null)
        {
            result.transform.SetParent(null, true);
            foreach (MeshRenderer side in _citySides) side.forceRenderingOff = false;
        }
        _root = null;
        _material = null;
        _meshes.Clear();
        _citySides.Clear();
        return result;
    }

    public void SetSceneBackground(GameObject root, MeshRenderer[] sides)
    {
        _bakedRoot = root;
        _bakedCitySides = sides;
    }
#endif

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
