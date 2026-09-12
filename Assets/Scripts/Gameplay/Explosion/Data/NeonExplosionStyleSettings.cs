using UnityEngine;

/// <summary>Editorでネオン爆風Prefabを生成するときの外観設定。変更後は再生成します。</summary>
[CreateAssetMenu(menuName = "NEON DETONATOR/Neon Explosion Style")]
public class NeonExplosionStyleSettings : ScriptableObject
{
    [ColorUsage(false, true)] public Color BeamColor = new Color(0.08f, 0.9f, 1.6f, 0.65f);
    [ColorUsage(false, true)] public Color CoreColor = new Color(1.8f, 1.6f, 0.55f, 1);
    [ColorUsage(false, true)] public Color SparkColor = new Color(1.4f, 1.1f, 0.15f, 1);
    [Range(0.05f, 0.4f)] public float BeamWidth = 0.22f;
    [Range(0.02f, 0.15f)] public float CoreWidth = 0.065f;
    [Range(0.1f, 0.8f)] public float Duration = 0.35f;
    [Range(0, 20)] public int SparkCount = 6;
}
