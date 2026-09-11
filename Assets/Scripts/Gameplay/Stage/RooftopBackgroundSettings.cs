using UnityEngine;

/// <summary>対戦判定を持たない屋上・都市背景の外観設定。長さはセル単位です。</summary>
[CreateAssetMenu(menuName = "NEON DETONATOR/Environment/Rooftop Background Settings")]
public class RooftopBackgroundSettings : ScriptableObject
{
    public bool Enabled = true;
    public int Seed = 4721;
    [Range(2, 16)] public int BuildingsPerSide = 6;
    [Min(3)] public float CityGap = 9;
    [Min(1)] public float PlatformDepth = 6;
    [Min(0)] public float PlatformMargin = 1;
    public Vector2 BuildingHeight = new Vector2(8, 22);
    public Vector2 BuildingWidth = new Vector2(2, 5);
    [Header("Skyline")]
    public bool FitSkylineToGridHeight = true;
    [Min(2)] public float SkylineHeightAboveGrid = 8;
    [Range(1, 3)] public int SkylineRows = 2;
    [Min(10)] public float SkylineRowSpacing = 18;
    [Range(0, 1)] public float LitWindowChance = 0.3f;
    public bool HideNearSideBuildings = true;
    public Color PlatformColor = new Color(0.035f, 0.065f, 0.085f);
    public Color BuildingColor = new Color(0.022f, 0.04f, 0.06f);
    [ColorUsage(false, true)] public Color Cyan = new Color(0.15f, 1.1f, 1.5f);
    [ColorUsage(false, true)] public Color Green = new Color(0.2f, 1.2f, 0.5f);
    [Range(0, 1)] public float WindowBrightness = 0.5f;

    [Header("Lower City / Rooftop Building")]
    public bool ShowLowerCity = true;
    [Tooltip("プレイヤーの高さ0から街の地面までの距離。台座より必ず下へ補正します。")]
    [Min(12)] public float CityGroundDepth = 32;
    [Tooltip("下方の街の半径。小さい場合は台座サイズに合わせて補正します。")]
    [Min(20)] public float LowerCityRadius = 65;
    [Range(6, 18)] public float StreetSpacing = 10;
    public Vector2 LowerBuildingHeight = new Vector2(2, 7);
    [Range(0, 1)] public float LowerCityLightBrightness = 0.15f;
    [Range(0, 1)] public float LowerCityHaze = 0.55f;
    public Color LowerCityGroundColor = new Color(0.012f, 0.023f, 0.036f);
    public Color LowerCityHazeColor = new Color(0.022f, 0.035f, 0.052f);
}
