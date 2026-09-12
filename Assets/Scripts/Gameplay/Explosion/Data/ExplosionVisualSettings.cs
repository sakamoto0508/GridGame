using UnityEngine;

/// <summary>爆風の種類別Prefabと表示時間を設定します。</summary>
[CreateAssetMenu(fileName = "ExplosionVisualSettings", menuName = "3D Grid Bomber/Settings/Explosion Visual")]
public class ExplosionVisualSettings : ScriptableObject
{
    /// <summary>
    /// 爆風の中心に表示するPrefabです。
    /// </summary>
    [SerializeField] private ExplosionEffect _centerPrefab;
    /// <summary>
    /// 爆風の中間に表示するPrefabです。
    /// </summary>
    [SerializeField] private ExplosionEffect _middlePrefab;
    /// <summary>
    /// 爆風の端に表示するPrefabです。
    /// </summary>
    [SerializeField] private ExplosionEffect _endPrefab;
    /// <summary>
    /// 爆風がブロックに当たったときに表示するPrefabです。
    /// </summary>
    [SerializeField] private ExplosionEffect _blockedEndPrefab;
    /// <summary>
    /// 爆風の表示時間です。
    /// </summary>
    [SerializeField, Min(0f)] private float _effectDuration = 0.35f;
    [Tooltip("1セル=1単位で作られたPrefabを実際のセルサイズへ拡大縮小します。")]
    [SerializeField] private bool _scaleToCell;
    public bool ScaleToCell => _scaleToCell;

    public ExplosionEffect CenterPrefab => _centerPrefab;
    public ExplosionEffect MiddlePrefab => _middlePrefab;
    public ExplosionEffect EndPrefab => _endPrefab;
    public ExplosionEffect BlockedEndPrefab => _blockedEndPrefab;
    public float EffectDuration => _effectDuration;
}
