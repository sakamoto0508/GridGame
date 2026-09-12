using UnityEngine;

/// <summary>取得判定から独立したItemの表示設定。</summary>
[CreateAssetMenu(menuName = "NEON DETONATOR/Item Visual Settings")]
public class ItemVisualSettings : ScriptableObject
{
    public bool FaceCamera = true;
    [Range(-90, 90)] public float RingRotationSpeed = 20;
    [Range(0.2f, 1.2f)] public float VisualScale = 1;
}
