using UnityEngine;

/// <summary>
/// グリッド上の1つのセルを表すクラス
/// </summary>
public class GridCell
{
    public GridCell(Vector3Int position)
    {
        Position = position;
    }

    public Vector3Int Position { get; }

    public Block Block { get; private set; }
    public Bomb Bomb { get; private set; }
    public Item Item { get; private set; }
    public CharacterBase Character { get; private set; }

    /// <summary>
    /// Characterが入れるセルか（落下先・ジャンプ着地点の判定用）。
    /// BombはCharacterと同居できるため、ここではBombを妨げとして扱いません。
    /// 歩いて入る場合はGridManager.CanCharacterEnter（Bombも不可）を使います。
    /// </summary>
    public bool IsWalkable => Block == null && Character == null;

    /// <summary>ItemはCharacter・Bombと同居可能ですが、Blockのあるセルには置けません。</summary>
    public bool TrySetItem(Item item)
    {
        if (item == null || Item != null || Block != null)
            return false;

        Item = item;
        return true;
    }

    public bool RemoveItem(Item item)
    {
        if (Item != item)
            return false;

        Item = null;
        return true;
    }

    public bool TrySetCharacter(CharacterBase character)
    {
        if (character == null || Character != null)
            return false;

        Character = character;
        return true;
    }

    public bool TrySetBlock(Block block)
    {
        // 落下BlockはCharacterと同居して押し潰せるが、Bombとは重複できない。
        if (block == null || Block != null || Bomb != null)
            return false;

        Block = block;
        return true;
    }

    /// <summary>このセルにいるCharacterと一致した場合だけ削除します。</summary>
    public bool RemoveCharacter(CharacterBase character)
    {
        if (Character != character)
            return false;

        Character = null;
        return true;
    }

    /// <summary>このセルのBlockと一致した場合だけ削除します。</summary>
    public bool RemoveBlock(Block block)
    {
        if (Block != block)
            return false;

        Block = null;
        return true;
    }

    public bool TrySetBomb(Bomb bomb)
    {
        // Characterとは同居可能だが、Block・既存Bombとは重複させない。
        if (bomb == null || Block != null || Bomb != null)
            return false;

        Bomb = bomb;
        return true;
    }

    /// <summary>このセルのBombと一致した場合だけ削除します。</summary>
    public bool RemoveBomb(Bomb bomb)
    {
        if (Bomb != bomb)
            return false;

        Bomb = null;
        return true;
    }
}
