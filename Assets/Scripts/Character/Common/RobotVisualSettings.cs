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
}
