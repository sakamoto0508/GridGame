using UnityEngine;

public enum ItemType { BombPower, BombCount }

/// <summary>Itemの効果と落下速度を設定します。取得でこのAssetを書き換えません。</summary>
[CreateAssetMenu(fileName = "ItemSettings", menuName = "3D Grid Bomber/Settings/Item")]
public class ItemSettings : ScriptableObject
{
    [SerializeField] private ItemType _type;
    [SerializeField, Min(1)] private int _increaseAmount = 1;
    [SerializeField, Min(1)] private int _maxBonus = 8;
    [SerializeField, Min(0.01f)] private float _fallDurationPerCell = 0.12f;

    // Inspectorの下限値と同じ値です。古いAssetの不正値もここで補正します。
    private const int MinAmount = 1;
    private const float MinFallDurationPerCell = 0.01f;

    public ItemType Type => _type;
    public int IncreaseAmount => Mathf.Max(MinAmount, _increaseAmount);
    public int MaxBonus => Mathf.Max(MinAmount, _maxBonus);
    public float FallDurationPerCell => Mathf.Max(MinFallDurationPerCell, _fallDurationPerCell);
}
