using UnityEngine;

/// <summary>天井照明の配置と見た目。距離はセル単位なのでフィールド寸法に追従します。</summary>
[CreateAssetMenu(menuName = "3D Grid Bomber/Settings/Stage Lighting", fileName = "StageLightingSettings")]
public class StageLightingSettings : ScriptableObject
{
    [SerializeField] private bool _enabled = true;
    /// <summary>
    /// 天井照明のX方向の個数。1～4で制限します。1個なら中央に配置されます。
    /// </summary>
    [SerializeField, Range(1, 4)] private int _countX = 2;
    /// <summary>
    /// 天井照明のZ方向の個数。1～4で制限します。1個なら中央に配置されます。
    /// </summary>
    [SerializeField, Range(1, 4)] private int _countZ = 2;
    /// <summary>
    /// 天井照明の色。白色に近い暖色系を推奨します。
    /// </summary>
    [SerializeField] private Color _color = new Color(1f, 0.96f, 0.88f);
    /// <summary>
    /// 天井照明の光源強度。0以上で制限します。
    /// </summary>
    [SerializeField, Min(0f)] private float _intensity = 30f;
    /// <summary>
    /// 天井照明のスポットライト角度。1～160度で制限します。
    /// </summary>
    [SerializeField, Range(1f, 160f)] private float _spotAngle = 100f;
    /// <summary>
    /// 天井照明のスポットライト内側角度。0～SpotAngleで制限します。
    /// </summary>
    [SerializeField, Range(0f, 160f)] private float _innerSpotAngle = 70f;
    /// <summary>
    /// 天井照明の天井からの距離をセル単位で指定します。0以上で制限します。
    /// </summary>
    [SerializeField, Min(0f)] private float _ceilingInsetInCells = 0.25f;
    [Tooltip("フィールド対角長に対する照射距離。1以上で床まで届く余裕を確保します。")]
    [SerializeField, Min(1f)] private float _rangeMultiplier = 1.1f;
    [Tooltip("影は負荷が増えるため、既定ではOFF。必要ならSoftに変更してください。")]
    [SerializeField] private LightShadows _shadows = LightShadows.None;
    public bool Enabled => _enabled;
    public int CountX => Mathf.Clamp(_countX, 1, 4);
    public int CountZ => Mathf.Clamp(_countZ, 1, 4);
    public Color Color => _color;
    public float Intensity => Mathf.Max(0f, _intensity);
    public float SpotAngle => Mathf.Clamp(_spotAngle, 1f, 160f);
    public float InnerSpotAngle => Mathf.Clamp(_innerSpotAngle, 0f, SpotAngle);
    public float CeilingInsetInCells => Mathf.Max(0f, _ceilingInsetInCells);
    public float RangeMultiplier => Mathf.Max(1f, _rangeMultiplier);
    public LightShadows Shadows => _shadows;
}
