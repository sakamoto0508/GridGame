using UnityEngine;

/// <summary>ボムの見た目だけの設定。爆発時刻はBombの残りFuseを参照します。</summary>
[CreateAssetMenu(menuName = "NEON DETONATOR/Bomb Visual Settings")]
public class BombVisualSettings : ScriptableObject
{
    [ColorUsage(false, true)] public Color NormalColor = new Color(0.1f, 1.1f, 1.6f);
    [ColorUsage(false, true)] public Color WarningColor = new Color(1.8f, 1.2f, 0.1f);
    [Min(0)] public float WarningSeconds = 1;
    [Min(0)] public float NormalPulseSpeed = 1;
    [Min(0)] public float WarningPulseSpeed = 5;
    [Range(0, 0.1f)] public float PulseScale = 0.025f;
}
