using UnityEngine;

/// <summary>ロボットの姿勢だけを制御する設定。移動時間やジャンプ高さは変更しません。</summary>
[CreateAssetMenu(menuName = "NEON DETONATOR/Robot Visual Settings")]
public class RobotVisualSettings : ScriptableObject
{
    [Min(0.01f)] public float TurnSpeed = 15;
    [Min(0.01f)] public float PoseSpeed = 18;
    [Min(0.1f)] public float StrideLength = 0.5f;
    [Range(0, 60)] public float WalkSwing = 30;
    [Range(0, 20)] public float WalkLean = 8;
    [Range(0, 0.05f)] public float IdleBob = 0.012f;
    [Min(0.1f)] public float IdleFrequency = 1.2f;
    [Min(0.05f)] public float PlacementDuration = 0.2f;
    [Min(0.05f)] public float LandingDuration = 0.16f;
    [Range(0, 0.3f)] public float LandingSquash = 0.16f;

    [Tooltip("着地で縦に潰れたとき、横方向へ広がる割合（縦の潰れ量に対する倍率）。")]
    [Range(0, 1)] public float LandingSpreadRatio = 0.4f;

    [Header("Pose Angles（度。X軸回転、負で前方へ振り上げ）")]
    [Tooltip("ジャンプ上昇中の腕の角度。")]
    public float JumpArmAngle = -55;
    [Tooltip("落下中の腕の角度。")]
    public float FallArmAngle = -100;
    [Tooltip("ジャンプ上昇中の脚の角度。")]
    public float JumpLegAngle = 35;
    [Tooltip("落下中の脚の角度。")]
    public float FallLegAngle = 12;
    [Tooltip("ジャンプ上昇中の上体の傾き。")]
    public float JumpLean = -8;
    [Tooltip("落下中の上体の傾き。")]
    public float FallLean = 5;
    [Tooltip("Bomb/Block設置時に腕を突き出す最大角度。")]
    public float PlacementArmAngle = -75;
}
