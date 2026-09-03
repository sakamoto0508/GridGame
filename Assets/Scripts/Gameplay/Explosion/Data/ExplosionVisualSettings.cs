using UnityEngine;

/// <summary>爆風の種類別Prefabと表示時間を設定します。</summary>
[CreateAssetMenu(fileName = "ExplosionVisualSettings", menuName = "3D Grid Bomber/Settings/Explosion Visual")]
public class ExplosionVisualSettings : ScriptableObject
{
    [SerializeField] private ExplosionEffect _centerPrefab;
    [SerializeField] private ExplosionEffect _middlePrefab;
    [SerializeField] private ExplosionEffect _endPrefab;
    [SerializeField] private ExplosionEffect _blockedEndPrefab;
    [SerializeField, Min(0f)] private float _effectDuration = 0.35f;

    public ExplosionEffect CenterPrefab => _centerPrefab;
    public ExplosionEffect MiddlePrefab => _middlePrefab;
    public ExplosionEffect EndPrefab => _endPrefab;
    public ExplosionEffect BlockedEndPrefab => _blockedEndPrefab;
    public float EffectDuration => _effectDuration;
}
