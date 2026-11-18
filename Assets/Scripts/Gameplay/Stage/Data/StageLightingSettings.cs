using UnityEngine;

/// <summary>天井照明の配置と見た目。距離はセル単位なのでフィールド寸法に追従します。</summary>
[CreateAssetMenu(menuName = "3D Grid Bomber/Settings/Stage Lighting", fileName = "StageLightingSettings")]
public class StageLightingSettings : ScriptableObject
{
    [SerializeField] private bool _enabled = true;

    [Tooltip("X方向に並べる照明の数。フィールドを等分した各区画の中央に置きます。")]
    [SerializeField, Range(1, 4)] private int _countX = 2;

    [Tooltip("Z方向に並べる照明の数。")]
    [SerializeField, Range(1, 4)] private int _countZ = 2;

    [Tooltip("白色に近い暖色系を推奨します。")]
    [SerializeField] private Color _color = new Color(1f, 0.96f, 0.88f);

    [SerializeField, Min(0f)] private float _intensity = 30f;
    [SerializeField, Range(1f, 160f)] private float _spotAngle = 100f;

    [Tooltip("Spot Angleを超える値はSpot Angleに補正されます。")]
    [SerializeField, Range(0f, 160f)] private float _innerSpotAngle = 70f;

    [Tooltip("天井の下面から照明を下げる距離（セル単位）。")]
    [SerializeField, Min(0f)] private float _ceilingInsetInCells = 0.25f;

    [Tooltip("フィールド対角長に対する照射距離。1以上で床まで届く余裕を確保します。")]
    [SerializeField, Min(1f)] private float _rangeMultiplier = 1.1f;
    [Tooltip("影は負荷が増えるため、既定ではOFF。必要ならSoftに変更してください。")]
    [SerializeField] private LightShadows _shadows = LightShadows.None;

    // Inspectorの範囲制限と同じ値です。古いAssetの範囲外の値もここで補正します。
    private const int MinLightCount = 1;
    private const int MaxLightCount = 4;
    private const float MinSpotAngle = 1f;
    private const float MaxSpotAngle = 160f;

    public bool Enabled => _enabled;
    public int CountX => Mathf.Clamp(_countX, MinLightCount, MaxLightCount);
    public int CountZ => Mathf.Clamp(_countZ, MinLightCount, MaxLightCount);
    public Color Color => _color;
    public float Intensity => Mathf.Max(0f, _intensity);
    public float SpotAngle => Mathf.Clamp(_spotAngle, MinSpotAngle, MaxSpotAngle);
    public float InnerSpotAngle => Mathf.Clamp(_innerSpotAngle, 0f, SpotAngle);
    public float CeilingInsetInCells => Mathf.Max(0f, _ceilingInsetInCells);
    public float RangeMultiplier => Mathf.Max(1f, _rangeMultiplier);
    public LightShadows Shadows => _shadows;
}
