using UnityEngine;

/// <summary>アイテム取得時のリングと光の粒の設定。能力値には影響しません。</summary>
[CreateAssetMenu(menuName = "NEON DETONATOR/Item Pickup Visual Settings")]
public class ItemPickupVisualSettings : ScriptableObject
{
    public bool Enabled = true;
    [Min(0.05f)] public float Duration = 0.65f;
    [ColorUsage(true, true)] public Color PowerColor = new Color(1.8f, 1.3f, 0.12f, 1f);
    [ColorUsage(true, true)] public Color CountColor = new Color(0.15f, 1.6f, 0.8f, 1f);
    [Header("広がるリング（ワールド単位）")]
    [Min(0.01f)] public float StartRadius = 0.25f;
    [Min(0.01f)] public float EndRadius = 0.85f;
    [Min(0.001f)] public float RingWidth = 0.045f;
    public float StartHeight = -0.3f;
    public float RiseHeight = 0.65f;
    [Header("上昇する光の粒")]
    [Range(1, 64)] public int ParticleCount = 18;
    [Min(0.01f)] public float ParticleSize = 0.055f;
    [Min(0f)] public float ParticleRiseSpeed = 1.3f;
}
