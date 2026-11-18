using UnityEngine;

/// <summary>
/// 死亡時の四角い破片の外観。ゲームの死亡判定には影響しません。
/// Vector2の範囲設定は、破片ごとにX〜Yの間からランダムに選びます（大小が逆でも可）。
/// </summary>
[CreateAssetMenu(menuName = "NEON DETONATOR/Character Death Visual Settings")]
public class CharacterDeathVisualSettings : ScriptableObject
{
    public bool Enabled = true;

    [Tooltip("生成する破片の数。多すぎるとフレームレートが低下する可能性があります。")]
    [Range(10, 200)] public int FragmentCount = 90;

    [Tooltip("破片の大きさの範囲（X〜Y）。")]
    public Vector2 FragmentSize = new Vector2(0.06f, 0.13f);

    [Tooltip("破片の寿命（秒）の範囲（X〜Y）。")]
    public Vector2 Lifetime = new Vector2(0.7f, 1.3f);

    [Tooltip("水平方向の初速の範囲（X〜Y）。")]
    public Vector2 HorizontalSpeed = new Vector2(1.3f, 3.4f);

    [Tooltip("上方向の初速の範囲（X〜Y）。")]
    public Vector2 UpwardSpeed = new Vector2(1.8f, 4.5f);

    [Tooltip("重力の倍率。0で無重力、1で通常の重力です。")]
    [Range(0, 4)] public float Gravity = 1.5f;

    [Tooltip("破片の基本色。Player・Enemy共通です。")]
    public Color BodyColor = new Color(0.7f, 0.78f, 0.85f);

    [Tooltip("Player死亡時のアクセント色。")]
    public Color PlayerAccent = new Color(0.1f, 0.85f, 1);

    [Tooltip("Enemy死亡時のアクセント色。")]
    public Color EnemyAccent = new Color(1, 0.8f, 0.12f);

    [Tooltip("アクセント色になる破片の割合。0で全て基本色（Player/Enemyの区別なし）、1で全てアクセント色。")]
    [Range(0, 1)] public float AccentRatio = 0.4f;

    [Header("落下Blockで潰されたとき")]
    [Tooltip("水平速度の倍率。横へ飛び散る印象を強めます。")]
    [Min(0)] public float CrushHorizontalSpeedMultiplier = 1.25f;

    [Tooltip("上方向速度の倍率。上から押さえつけられた印象にします。")]
    [Min(0)] public float CrushUpwardSpeedMultiplier = 0.6f;
}
