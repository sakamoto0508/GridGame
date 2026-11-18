using UnityEngine;

/// <summary>爆風の種類別Prefabと表示時間を設定します。</summary>
[CreateAssetMenu(fileName = "ExplosionVisualSettings", menuName = "3D Grid Bomber/Settings/Explosion Visual")]
public class ExplosionVisualSettings : ScriptableObject
{
    [SerializeField] private ExplosionEffect _centerPrefab;
    [SerializeField] private ExplosionEffect _middlePrefab;
    [SerializeField] private ExplosionEffect _endPrefab;

    [Tooltip("爆風が破壊可能Blockや障害物の手前で止まったときの端。未設定ならEnd Prefabを使います。")]
    [SerializeField] private ExplosionEffect _blockedEndPrefab;

    [Tooltip("爆風を表示する秒数。")]
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
