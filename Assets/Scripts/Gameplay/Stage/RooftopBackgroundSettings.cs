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
    [Range(0, 1)] public float LitWindowChance = 0.3f;
    public bool HideNearSideBuildings = true;
    public Color PlatformColor = new Color(0.035f, 0.065f, 0.085f);
    public Color BuildingColor = new Color(0.022f, 0.04f, 0.06f);
    [ColorUsage(false, true)] public Color Cyan = new Color(0.15f, 1.1f, 1.5f);
    [ColorUsage(false, true)] public Color Green = new Color(0.2f, 1.2f, 0.5f);
    [Range(0, 1)] public float WindowBrightness = 0.5f;
}
