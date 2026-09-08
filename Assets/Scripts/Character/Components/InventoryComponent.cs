using UnityEngine;

public class InventoryComponent : MonoBehaviour
{
    // SOは共有設定、ボーナスはこのCharacterだけの試合中の状態です。
    public int BombPowerBonus { get; private set; }
    public int BombCountBonus { get; private set; }
    public event System.Action Changed;

    /// <summary>取得したItemの効果を適用します。上限でもItemは消費します。</summary>
    public void Apply(ItemSettings settings)
    {
        if (settings == null) return;
        if (settings.Type == ItemType.BombPower)
            BombPowerBonus = (int)System.Math.Min((long)BombPowerBonus + settings.IncreaseAmount, settings.MaxBonus);
        else if (settings.Type == ItemType.BombCount)
            BombCountBonus = (int)System.Math.Min((long)BombCountBonus + settings.IncreaseAmount, settings.MaxBonus);
        Changed?.Invoke();
    }
}
