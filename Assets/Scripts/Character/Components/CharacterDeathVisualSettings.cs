using UnityEngine;

/// <summary>死亡時の四角い破片の外観。ゲームの死亡判定には影響しません。</summary>
[CreateAssetMenu(menuName = "NEON DETONATOR/Character Death Visual Settings")]
public class CharacterDeathVisualSettings : ScriptableObject
{
    public bool Enabled = true;
    /// <summary>
    /// 死亡時に生成する破片の数です。多すぎるとフレームレートが低下する可能性があります。
    /// </summary>
    [Range(10, 200)] public int FragmentCount = 90;
    /// <summary>
    /// 破片のサイズの範囲です。Xが幅、Yが高さです。
    /// </summary>
    public Vector2 FragmentSize = new Vector2(0.06f, 0.13f);
    /// <summary>
    /// 破片の寿命の範囲です。Xが最短、Yが最長です。
    /// </summary>
    public Vector2 Lifetime = new Vector2(0.7f, 1.3f);
    /// <summary>
    /// 破片の初速の範囲です。Xが最小、Yが最大です。
    /// </summary>
    public Vector2 HorizontalSpeed = new Vector2(1.3f, 3.4f);
    /// <summary>
    /// 破片の上方向の初速の範囲です。Xが最小、Yが最大です。
    /// </summary>
    public Vector2 UpwardSpeed = new Vector2(1.8f, 4.5f);
    /// <summary>
    /// 破片にかかる重力の強さです。0で無重力、1で通常の重力、2以上で強い重力になります。
    /// </summary>
    [Range(0, 4)] public float Gravity = 1.5f;
    /// <summary>
    /// 破片の色です。プレイヤーと敵で色を変えたい場合はAccentRatioを0にしてください。
    /// </summary>
    public Color BodyColor = new Color(0.7f, 0.78f, 0.85f);
    /// <summary>
    /// 破片のアクセント色です。プレイヤーと敵で色を変えたい場合はAccentRatioを0にしてください。
    /// </summary>
    public Color PlayerAccent = new Color(0.1f, 0.85f, 1);
    /// <summary>
    /// 破片のアクセント色です。プレイヤーと敵で色を変えたい場合はAccentRatioを0にしてください。
    /// </summary>
    public Color EnemyAccent = new Color(1, 0.8f, 0.12f);
    /// <summary>
    /// 破片のアクセント色の割合です。0でアクセント色なし、1でアクセント色のみになります。
    /// </summary>
    [Range(0, 1)] public float AccentRatio = 0.4f;
}
