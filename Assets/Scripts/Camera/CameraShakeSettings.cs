using UnityEngine;

/// <summary>死亡時の揺れ。0にすると個別に無効化できます。</summary>
[CreateAssetMenu(menuName = "NEON DETONATOR/Camera Shake Settings")]
public class CameraShakeSettings : ScriptableObject
{
    public bool Enabled = true;
    [Min(0.01f)] public float Duration = 0.32f;
    [Min(0f), Tooltip("プレイヤー死亡時の移動幅（ワールド単位）。")]
    public float PlayerAmplitude = 0.18f;
    [Min(0f), Tooltip("敵死亡時の移動幅。撃破者に関係なく反応します。")]
    public float EnemyAmplitude = 0.08f;
    [Range(0f, 3f)] public float RotationDegrees = 0.6f;
    [Min(1f)] public float Frequency = 24f;
    [Min(0.1f), Tooltip("追従対象からこの距離以上離れた敵の死亡では揺れません。")]
    public float EnemyMaxDistance = 15f;
}
