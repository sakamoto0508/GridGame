using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 屋上台座と四方の都市を少数の結合Meshで描画します。
/// ColliderやGrid登録を持たず、ゲームの乱数にも影響しません。
/// 長さの単位はすべてセルです（最後にセル幅で拡大します）。
/// </summary>
[DefaultExecutionOrder(10001)] // BoundaryVisibilityController(10000)と同様、Cinemachine更新後に表示判定します。
public class RooftopBackgroundController : MonoBehaviour
{
    private const string ShaderResourceName = "NeonCityBackground";

    private const int CitySideCount = 4;

    /// <summary>下方都市用の乱数シードのずらし量（素数）。遠景ビルの乱数列と独立させます。</summary>
    private const int LowerCitySeedOffset = 7919;

    /// <summary>下方都市の片側あたりの最大街区数。設定値が大きくても最大32×32街区に制限します。</summary>
    private const int MaxLowerCityBlocksPerHalfAxis = 16;

    /// <summary>カメラ方向との内積がこれを超える面の都市を「手前」とみなして隠します。</summary>
    private const float NearSideDotThreshold = 0.05f;

    // ---- 屋上台座 ----

    /// <summary>台座上面の高さ。外殻床の底(-1.5セル)より少し下に置き、床と重ねません。</summary>
    private const float PlatformTopY = -1.52f;

    /// <summary>グリッドの両側にある外殻の厚み（1セル×2）です。</summary>
    private const int BoundaryCellsPerAxis = 2;

    // 台座の縁を示す細いシアン帯
    private const float RimDropBelowTop = 0.25f;
    private const float RimThickness = 0.035f;
    private const float RimHeight = 0.06f;
    private const float RimColorScale = 0.65f;

    // ---- 遠景の高層ビル ----

    // 都市の地面の深さ（設定値の下限と、台座の底からの最低距離）
    private const float MinCityGroundDepth = 12f;
    private const float CityGroundBelowPlatform = 12f;

    // Settingsの値を収める範囲
    private const int MinBuildingsPerRow = 2;
    private const int MaxBuildingsPerRow = 16;
    private const int MinSkylineRows = 1;
    private const int MaxSkylineRows = 3;
    private const float MinBuildingWidth = 1f;
    private const float MaxBuildingWidth = 12f;
    private const float MinBuildingHeight = 2f;
    private const float MaxBuildingHeight = 60f;
    private const float MinCityGap = 3f;
    private const float MinSkylineRowSpacing = 10f;
    private const float MinSkylineHeightAboveGrid = 2f;

    // 配置（Jitterはランダムに足す最大量、PerRowは奥の列ほど増やす量）
    private const float SkylineSpanMultiplier = 2f;
    private const float CityGapJitter = 5f;
    private const float RowTangentSpread = 16f;
    private const float SkylineBottomBelowPlatform = 10f;
    private const float RoofHeightJitter = 6f;
    private const float RoofHeightPerRow = 10f;
    private const float WidthGrowthPerRow = 0.15f;

    /// <summary>高さに対する最小の幅の比率。細すぎる塔を防ぎます。</summary>
    private const float MinBuildingWidthToHeight = 0.14f;

    // 明るさのばらつき（0.75〜1.25倍）と、地面まで延ばす支柱のかすみ具合
    private const float BuildingShadeMin = 0.75f;
    private const float BuildingShadeRange = 0.5f;
    private const float PillarHaze = 0.7f;

    // 高層ビルの窓
    private const float WindowStartY = 1f;
    private const float WindowTopMargin = 0.5f;
    private const float WindowStepY = 1.25f;
    private const float WindowStartMarginU = 0.6f;
    private const float WindowEndMarginU = 0.3f;
    private const float WindowStepU = 0.9f;
    private const float WindowSurfaceOffset = 0.015f;
    private const double GreenWindowChance = 0.2;
    private const float WindowThickness = 0.02f;
    private const float WindowHeight = 0.18f;
    private const float WindowWidth = 0.35f;

    // ---- 台座下の建物と下方都市 ----

    // 建物胴体（台座よりわずかに細くします）
    private const float TowerFootprintScale = 0.94f;
    private const float TowerHaze = 0.25f;

    // 建物胴体の縦リブ
    private const float RibStartMargin = 1f;
    private const float RibSpacing = 2.5f;
    private const float RibSurfaceOffset = 0.025f;
    private const float RibThickness = 0.06f;
    private const float RibWidth = 0.12f;
    private const float RibColorScale = 1.3f;
    private const float RibHaze = 0.4f;

    // 建物胴体の窓
    private const float TowerWindowStartAboveGround = 2f;
    private const float TowerWindowTopMargin = 0.6f;
    private const float TowerWindowStepY = 2f;
    private const double TowerWindowChance = 0.35;
    private const float TowerWindowSurfaceOffset = 0.04f;
    private const float TowerWindowThickness = 0.035f;
    private const float TowerWindowHeight = 0.25f;
    private const float TowerWindowWidth = 0.6f;

    // 街区
    private const float MinStreetSpacing = 6f;
    private const float MaxStreetSpacing = 18f;
    private const float StreetMarginInSpacings = 2f;
    private const int MinLowerCityBlocksPerHalfAxis = 3;

    // 地面と道路灯
    private const float GroundSlabCenterBelow = 0.3f;
    private const float GroundSlabThickness = 0.5f;
    private const float GroundSlabSizeInRadius = 2.4f;
    private const float RoadLineWidth = 0.06f;
    private const float RoadLineHeight = 0.02f;
    private const float RoadLengthInRadius = 2f;

    // 低層ビル
    private const float LowerBuildingWidthMin = 0.4f;
    private const float LowerBuildingWidthRange = 0.25f;
    private const float CentralBuildingClearance = 1f;
    private const float MinLowerBuildingHeight = 1f;
    private const float LowerBuildingTopClearance = 4f;
    private const float LowerBuildingColorScale = 1.6f;

    // 低層ビル屋上の灯り
    private const double GreenLampChance = 0.15;
    private const float LampOffsetAboveRoof = 0.025f;
    private const float LampLengthRatio = 0.5f;
    private const float LampHeight = 0.03f;
    private const float LampDepth = 0.12f;

    /// <summary>距離0（手前）のものにも掛ける最低限のかすみ率です。</summary>
    private const float MinHazeAmount = 0.3f;

    private static readonly Vector3[] Sides = new Vector3[CitySideCount]
    {
        Vector3.right,
        Vector3.left,
        Vector3.forward,
        Vector3.back
    };

    [SerializeField] private GameObject _bakedRoot;
    [SerializeField] private MeshRenderer[] _bakedCitySides = new MeshRenderer[0];

    private readonly List<Mesh> _meshes = new List<Mesh>();
    private readonly List<MeshRenderer> _citySides = new List<MeshRenderer>();

    private GridManager _grid;
    private RooftopBackgroundSettings _settings;
    private RooftopBackgroundSettings _fallback;
    private GameObject _root;
    private Material _material;
    private Camera _camera;

    public GameObject BakedRoot => _bakedRoot;

    /// <summary>StageGeneratorから試合開始前に呼びます。SO未設定でも既定外観を表示します。</summary>
    public void Init(GridManager grid, RooftopBackgroundSettings settings)
    {
        Clear();
        _grid = grid;

        if (grid == null || grid.Size.x <= 0 || grid.Size.z <= 0)
            return;

        if (settings == null && _fallback == null)
            _fallback = ScriptableObject.CreateInstance<RooftopBackgroundSettings>();

        _settings = settings != null ? settings : _fallback;

        // シーンへ保存済みなら実行時の再生成は行わず、編集した配置を使用します。
        if (_bakedRoot != null)
        {
            _bakedRoot.SetActive(_settings.Enabled && isActiveAndEnabled);
            UpdateVisibility();
            return;
        }

        if (!_settings.Enabled)
            return;

        Shader shader = Resources.Load<Shader>(ShaderResourceName);
        if (shader == null)
        {
            Debug.LogError("NeonCityBackground Shaderが見つかりません。", this);
            return;
        }

        _material = new Material(shader) { name = "Rooftop Background (Runtime)" };

        _root = new GameObject("Rooftop Background (Visual Only)");
        _root.transform.SetParent(transform, false);

        // GridManagerと同じワールド座標系。親の位置や拡大率に背景サイズを左右されません。
        _root.transform.position =
            (grid.GetWorldPosition(Vector3Int.zero) + grid.GetWorldPosition(grid.Size - Vector3Int.one)) * 0.5f;
        _root.transform.rotation = Quaternion.identity;
        _root.transform.localScale = Vector3.one;

        float cell = grid.CellSize;

        // 水平方向はグリッド中央、高さはグリッドの床(y=0)に合わせます。
        _root.transform.position = new Vector3(
            _root.transform.position.x,
            grid.GetWorldPosition(Vector3Int.zero).y,
            _root.transform.position.z);

        float top = PlatformTopY;
        float depth = Mathf.Max(1, _settings.PlatformDepth);
        float width = grid.Size.x + BoundaryCellsPerAxis + Mathf.Max(0, _settings.PlatformMargin) * 2;
        float length = grid.Size.z + BoundaryCellsPerAxis + Mathf.Max(0, _settings.PlatformMargin) * 2;

        BuildPlatform(top, depth, width, length, cell);

        // 低い街は手前側のビル非表示処理から独立させ、回転しても空白に戻さないようにします。
        float cityGround = Mathf.Min(
            -Mathf.Max(MinCityGroundDepth, _settings.CityGroundDepth),
            top - depth - CityGroundBelowPlatform);

        if (_settings.ShowLowerCity)
            GenerateLowerCity(width, length, top - depth, cityGround, cell);

        GenerateSkyline(grid, top, depth, width, length, cityGround, cell);

        _root.SetActive(isActiveAndEnabled);
        UpdateVisibility();
    }

    /// <summary>屋上台座と、輪郭を示す細いシアン帯を生成します。</summary>
    private void BuildPlatform(float top, float depth, float width, float length, float cell)
    {
        Builder platform = new Builder();
        platform.Box(
            new Vector3(0, top - depth * 0.5f, 0),
            new Vector3(width, depth, length),
            _settings.PlatformColor);

        // 細いシアン帯だけで輪郭を示し、盤面より目立たせません。
        foreach (Vector3 side in Sides)
        {
            bool x = side.x != 0;
            Vector3 center = new Vector3(side.x * width * 0.5f, top - RimDropBelowTop, side.z * length * 0.5f);
            Vector3 size = x
                ? new Vector3(RimThickness, RimHeight, length)
                : new Vector3(width, RimHeight, RimThickness);

            platform.Box(center, size, _settings.Cyan * RimColorScale);
        }

        Build("Rooftop Platform", platform, cell);
    }

    /// <summary>四方に遠景の高層ビル群を生成します。面ごとに1つのMeshへまとめます。</summary>
    private void GenerateSkyline(
        GridManager grid, float top, float depth, float width, float length, float cityGround, float cell)
    {
        System.Random random = new System.Random(_settings.Seed);

        for (int sideIndex = 0; sideIndex < CitySideCount; sideIndex++)
        {
            Builder city = new Builder();
            Vector3 normal = Sides[sideIndex];
            Vector3 tangent = new Vector3(-normal.z, 0, normal.x);

            // Sidesの0,1番目は±X面（台座のZ方向の長さに沿って並ぶ）
            bool xSide = sideIndex < 2;
            float span = (xSide ? length : width) * SkylineSpanMultiplier;
            float distanceFromCenter = (xSide ? width : length) * 0.5f;

            int count = Mathf.Clamp(_settings.BuildingsPerSide, MinBuildingsPerRow, MaxBuildingsPerRow);
            int rows = Mathf.Clamp(_settings.SkylineRows, MinSkylineRows, MaxSkylineRows);

            for (int row = 0; row < rows; row++)
            {
                for (int i = 0; i < count; i++)
                {
                    float w = RandomUtility.Range(random, _settings.BuildingWidth, MinBuildingWidth, MaxBuildingWidth);
                    float h = RandomUtility.Range(random, _settings.BuildingHeight, MinBuildingHeight, MaxBuildingHeight);

                    // 面の法線方向へ（台座の端＋間隔＋ばらつき）だけ離し、接線方向へ等間隔に並べます。
                    float along = (i + 0.5f) / count;
                    float distance = distanceFromCenter
                        + Mathf.Max(MinCityGap, _settings.CityGap)
                        + (float)random.NextDouble() * CityGapJitter;
                    Vector3 p = normal * distance
                        + tangent * (along * span - span * 0.5f);

                    // 奥の列ほど遠く、横に広げます。
                    p += normal * (row * Mathf.Max(MinSkylineRowSpacing, _settings.SkylineRowSpacing));
                    p += tangent * ((along - 0.5f) * row * RowTangentSpread);

                    float bottom = top - depth - SkylineBottomBelowPlatform;

                    if (_settings.FitSkylineToGridHeight)
                    {
                        // 頂部をグリッド最高地点より上へ置き、奥の列はさらに高く・幅広くします。
                        // ワールド固定の建物寸法なので、Player上昇中に建物が伸縮しません。
                        float roof = grid.Size.y
                            + Mathf.Max(MinSkylineHeightAboveGrid, _settings.SkylineHeightAboveGrid)
                            + (float)random.NextDouble() * RoofHeightJitter
                            + row * RoofHeightPerRow;
                        h = roof - bottom;
                        w = Mathf.Max(w, h * MinBuildingWidthToHeight);
                    }

                    w *= 1 + row * WidthGrowthPerRow;
                    p.y = bottom + h * 0.5f;

                    float shade = BuildingShadeMin + (float)random.NextDouble() * BuildingShadeRange;
                    city.Box(p, new Vector3(w, h, w), _settings.BuildingColor * shade);

                    // 既存の高層ビルも地面まで支柱を延ばし、空中に浮いた底面をなくします。
                    if (_settings.ShowLowerCity && bottom > cityGround)
                    {
                        city.Box(
                            new Vector3(p.x, (bottom + cityGround) * 0.5f, p.z),
                            new Vector3(w, bottom - cityGround, w),
                            LowerCityColor(_settings.BuildingColor, PillarHaze));
                    }

                    AddSkylineWindows(city, random, p, w, h, bottom);
                }
            }

            _citySides.Add(Build("City Side " + sideIndex, city, cell));
        }
    }

    /// <summary>高層ビルの四面に小窓を追加。窓も同じMeshなので窓の数だけGameObjectを増やしません。</summary>
    private void AddSkylineWindows(Builder city, System.Random random, Vector3 p, float w, float h, float bottom)
    {
        foreach (Vector3 face in Sides)
        {
            Vector3 along = new Vector3(-face.z, 0, face.x);

            for (float y = WindowStartY; y < h - WindowTopMargin; y += WindowStepY)
            {
                for (float u = -w * 0.5f + WindowStartMarginU; u < w * 0.5f - WindowEndMarginU; u += WindowStepU)
                {
                    if (random.NextDouble() > _settings.LitWindowChance)
                        continue;

                    Vector3 window = p + face * (w * 0.5f + WindowSurfaceOffset) + along * u;
                    window.y = bottom + y;

                    Color color = random.NextDouble() < GreenWindowChance ? _settings.Green : _settings.Cyan;
                    Vector3 size = face.x != 0
                        ? new Vector3(WindowThickness, WindowHeight, WindowWidth)
                        : new Vector3(WindowWidth, WindowHeight, WindowThickness);

                    city.Box(window, size, color * _settings.WindowBrightness);
                }
            }
        }
    }

    /// <summary>台座の建物胴体・低層街区・道路灯を生成。すべて描画専用でColliderはありません。</summary>
    private void GenerateLowerCity(float width, float length, float platformBottom, float ground, float cell)
    {
        // 別の乱数列を使用するため、下方都市の設定を変えても既存の遠景ビルは変化しません。
        System.Random random = new System.Random(unchecked(_settings.Seed + LowerCitySeedOffset));

        BuildTowerBody(random, width, length, platformBottom, ground, cell);
        BuildLowerCityBlocks(random, width, length, platformBottom, ground, cell);
    }

    /// <summary>台座の下に伸びる建物胴体（縦リブと窓付き）を生成します。</summary>
    private void BuildTowerBody(
        System.Random random, float width, float length, float platformBottom, float ground, float cell)
    {
        Builder tower = new Builder();
        float towerWidth = width * TowerFootprintScale;
        float towerLength = length * TowerFootprintScale;
        float towerHeight = platformBottom - ground;
        float towerCenterY = (platformBottom + ground) * 0.5f;

        tower.Box(
            new Vector3(0, towerCenterY, 0),
            new Vector3(towerWidth, towerHeight, towerLength),
            LowerCityColor(_settings.PlatformColor, TowerHaze));

        foreach (Vector3 face in Sides)
        {
            bool xFace = face.x != 0;
            Vector3 along = new Vector3(-face.z, 0, face.x);
            float faceWidth = xFace ? towerLength : towerWidth;
            float faceDistance = (xFace ? towerWidth : towerLength) * 0.5f;

            // 少数の縦リブと間欠的な窓で建物の高さを伝えます。下ほどコントラストを抑えます。
            for (float u = -faceWidth * 0.5f + RibStartMargin; u < faceWidth * 0.5f; u += RibSpacing)
            {
                Vector3 rib = face * (faceDistance + RibSurfaceOffset) + along * u;
                rib.y = towerCenterY;

                Vector3 ribSize = xFace
                    ? new Vector3(RibThickness, towerHeight, RibWidth)
                    : new Vector3(RibWidth, towerHeight, RibThickness);
                tower.Box(rib, ribSize, LowerCityColor(_settings.PlatformColor * RibColorScale, RibHaze));

                for (float y = ground + TowerWindowStartAboveGround;
                     y < platformBottom - TowerWindowTopMargin;
                     y += TowerWindowStepY)
                {
                    if (random.NextDouble() > TowerWindowChance)
                        continue;

                    Vector3 window = rib + face * TowerWindowSurfaceOffset;
                    window.y = y;

                    float haze = 1 - Mathf.InverseLerp(ground, platformBottom, y);
                    Vector3 windowSize = xFace
                        ? new Vector3(TowerWindowThickness, TowerWindowHeight, TowerWindowWidth)
                        : new Vector3(TowerWindowWidth, TowerWindowHeight, TowerWindowThickness);

                    tower.Box(
                        window,
                        windowSize,
                        LowerCityColor(_settings.Cyan * _settings.LowerCityLightBrightness, haze));
                }
            }
        }

        Build("Rooftop Building Body", tower, cell);
    }

    /// <summary>台座の周囲に広がる地面・道路灯・低層ビルを生成します。</summary>
    private void BuildLowerCityBlocks(
        System.Random random, float width, float length, float platformBottom, float ground, float cell)
    {
        Builder streets = new Builder();
        Builder buildings = new Builder();

        float spacing = Mathf.Clamp(_settings.StreetSpacing, MinStreetSpacing, MaxStreetSpacing);
        float radius = Mathf.Max(
            _settings.LowerCityRadius,
            Mathf.Max(width, length) * 0.5f + spacing * StreetMarginInSpacings);

        // メッシュの過剰生成を防ぎ、設定値が大きくても最大32×32街区に制限します。
        int count = Mathf.Clamp(
            Mathf.CeilToInt(radius / spacing),
            MinLowerCityBlocksPerHalfAxis,
            MaxLowerCityBlocksPerHalfAxis);
        spacing = radius / count;

        // 地面
        float slabSize = radius * GroundSlabSizeInRadius;
        streets.Box(
            new Vector3(0, ground - GroundSlabCenterBelow, 0),
            new Vector3(slabSize, GroundSlabThickness, slabSize),
            _settings.LowerCityGroundColor);

        // 道路灯（縦横の格子）
        float roadLength = radius * RoadLengthInRadius;
        for (int i = -count; i <= count; i++)
        {
            float offset = i * spacing;
            Color roadColor = LowerCityColor(
                _settings.Cyan * _settings.LowerCityLightBrightness,
                Mathf.Abs(offset) / radius);

            // 面として光らせず細い道路灯だけを描き、対戦グリッドと区別します。
            streets.Box(new Vector3(offset, ground, 0), new Vector3(RoadLineWidth, RoadLineHeight, roadLength), roadColor);
            streets.Box(new Vector3(0, ground, offset), new Vector3(roadLength, RoadLineHeight, RoadLineWidth), roadColor);
        }

        // 低層ビル（各街区の中央に1棟）
        float maxLowerHeight = Mathf.Max(MinLowerBuildingHeight, platformBottom - ground - LowerBuildingTopClearance);

        for (int x = -count; x < count; x++)
        {
            for (int z = -count; z < count; z++)
            {
                Vector3 center = new Vector3((x + 0.5f) * spacing, 0, (z + 0.5f) * spacing);
                float bw = spacing * (LowerBuildingWidthMin + (float)random.NextDouble() * LowerBuildingWidthRange);

                // 中央建物と交差する街区は空けます。
                bool overlapsCenter =
                    Mathf.Abs(center.x) < width * 0.5f + bw * 0.5f + CentralBuildingClearance &&
                    Mathf.Abs(center.z) < length * 0.5f + bw * 0.5f + CentralBuildingClearance;
                if (overlapsCenter)
                    continue;

                float h = RandomUtility.Range(random, _settings.LowerBuildingHeight, MinLowerBuildingHeight, maxLowerHeight);
                center.y = ground + h * 0.5f;

                float haze = Mathf.Clamp01(new Vector2(center.x, center.z).magnitude / radius);
                buildings.Box(
                    center,
                    new Vector3(bw, h, bw),
                    LowerCityColor(_settings.BuildingColor * LowerBuildingColorScale, haze));

                // 屋上の小さな灯り。街灯用Lightを大量に作らず頂点カラーで表現します。
                Color lampBase = random.NextDouble() < GreenLampChance ? _settings.Green : _settings.Cyan;
                Color lamp = lampBase * _settings.LowerCityLightBrightness;

                buildings.Box(
                    new Vector3(center.x, ground + h + LampOffsetAboveRoof, center.z),
                    new Vector3(bw * LampLengthRatio, LampHeight, LampDepth),
                    LowerCityColor(lamp, haze));
            }
        }

        Build("Lower City Streets", streets, cell);
        Build("Lower City Buildings", buildings, cell);
    }

    /// <summary>背景専用の色調整。シーン全体のFogやライト設定は変更しません。</summary>
    private Color LowerCityColor(Color color, float distanceFactor)
    {
        float hazeAmount = Mathf.Clamp01(_settings.LowerCityHaze)
            * Mathf.Lerp(MinHazeAmount, 1, Mathf.Clamp01(distanceFactor));

        return Color.Lerp(color, _settings.LowerCityHazeColor, hazeAmount);
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
        if (_settings == null)
            return;

        if (_camera == null)
            _camera = Camera.main;

        if (_camera == null)
            return;

        Vector3 near = -_camera.transform.forward;
        near.y = 0;
        near.Normalize();

        for (int i = 0; i < _citySides.Count; i++)
        {
            if (_citySides[i] != null)
                _citySides[i].forceRenderingOff = IsHiddenSide(near, i);
        }

        for (int i = 0; i < _bakedCitySides.Length; i++)
        {
            if (_bakedCitySides[i] != null)
                _bakedCitySides[i].forceRenderingOff = IsHiddenSide(near, i);
        }
    }

    /// <summary>指定番号の都市面がカメラ手前側で、隠す設定ならtrueです。</summary>
    private bool IsHiddenSide(Vector3 near, int sideIndex)
    {
        return _settings.HideNearSideBuildings &&
               Vector3.Dot(near, Sides[sideIndex % CitySideCount]) > NearSideDotThreshold;
    }

    [ContextMenu("Rebuild Background (Play Mode)")]
    private void Rebuild()
    {
        if (Application.isPlaying)
            Init(_grid, _settings);
    }

    private void OnEnable()
    {
        if (_root != null)
            _root.SetActive(true);
    }

    private void OnDisable()
    {
        if (_root != null)
            _root.SetActive(false);

        // シーン保存された背景は破棄しません。カメラによる非表示だけ解除します。
        foreach (MeshRenderer side in _bakedCitySides)
        {
            if (side != null)
                side.forceRenderingOff = false;
        }
    }

    private void OnDestroy()
    {
        Clear();

        if (_fallback != null)
            DestroyGenerated(_fallback);
    }

    private void Clear()
    {
        if (_root != null)
        {
            _root.SetActive(false);
            DestroyGenerated(_root);
        }

        foreach (Mesh mesh in _meshes)
            DestroyGenerated(mesh);

        _meshes.Clear();
        _citySides.Clear();

        if (_material != null)
            DestroyGenerated(_material);
    }

    private static void DestroyGenerated(Object target)
    {
#if UNITY_EDITOR
        // ベイク済みMesh/Material Assetは一時生成物の片付けでは削除しません。
        if (UnityEditor.EditorUtility.IsPersistent(target))
            return;

        if (!Application.isPlaying)
        {
            DestroyImmediate(target);
            return;
        }
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

            foreach (MeshRenderer side in _citySides)
                side.forceRenderingOff = false;
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
        /// <summary>面ごとの明るさ（CubeCorner.Facesの順）。上面を最も明るく、底面を最も暗くして立体感を出します。</summary>
        private static readonly float[] FaceShades = { 0.8f, 0.8f, 0.65f, 0.9f, 1, 0.5f };

        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Color> _colors = new List<Color>();
        private readonly List<int> _triangles = new List<int>();

        public void Box(Vector3 center, Vector3 size, Color color)
        {
            Vector3 half = size * 0.5f;

            Vector3[] corners = new Vector3[CubeCorner.Count];
            for (int i = 0; i < CubeCorner.Count; i++)
                corners[i] = center + Vector3.Scale(half, CubeCorner.Sign(i));

            for (int face = 0; face < CubeCorner.FaceCount; face++)
            {
                int start = _vertices.Count;

                for (int j = 0; j < CubeCorner.VerticesPerFace; j++)
                {
                    _vertices.Add(corners[CubeCorner.Faces[face * CubeCorner.VerticesPerFace + j]]);
                    _colors.Add(color * FaceShades[face]);
                }

                // 四角形を2つの三角形に分割します。
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
