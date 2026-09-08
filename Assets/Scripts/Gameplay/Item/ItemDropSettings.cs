using UnityEngine;

/// <summary>試合中の定期落下設定です。登録したPrefabから均等に抽選します。</summary>
[CreateAssetMenu(fileName = "ItemDropSettings", menuName = "3D Grid Bomber/Settings/Item Drop")]
public class ItemDropSettings : ScriptableObject
{
    [SerializeField] private Item[] _prefabs;
    [SerializeField, Min(0.1f)] private float _dropInterval = 6f;
    [SerializeField, Min(1)] private int _maxItemCount = 5;
    public Item[] Prefabs => _prefabs;
    public float DropInterval => Mathf.Max(0.1f, _dropInterval);
    public int MaxItemCount => Mathf.Max(1, _maxItemCount);
}
