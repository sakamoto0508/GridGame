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
    /// このセルが予約されているかどうかを示すフラグ。予約されている場合、キャラクターはこのセルに移動できません。
    /// </summary>
    public bool IsReserved { get; private set; }

    public bool IsWalkable => Block == null && Character == null && !IsReserved;

    /// <summary>
    /// 指定されたキャラクターをこのセルに設定しようとします。
    /// </summary>
    /// <param name="character"></param>
    /// <returns></returns>
    public bool TrySetCharacter(CharacterBase character)
    {
        if (character == null || Character != null || IsReserved)
            return false;

        Character = character;
        return true;
    }

    /// <summary>
    /// 指定されたブロックをこのセルに設定しようとします。
    /// </summary>
    /// <param name="block"></param>
    /// <returns></returns>
    public bool TrySetBlock(Block block)
    {
        // 落下BlockはCharacterと同居して押し潰せるが、Bomb・予約セルとは重複できない。
        if (block == null || Block != null || Bomb != null || IsReserved)
            return false;
        Block = block;
        return true;
    }

    /// <summary>
    /// 指定されたキャラクターをこのセルから削除しようとします。
    /// </summary>
    /// <param name="character"></param>
    /// <returns></returns>
    public bool RemoveCharacter(CharacterBase character)
    {
        if (Character != character)
            return false;

        Character = null;
        return true;
    }

    /// <summary>
    /// 指定されたブロックをこのセルから削除しようとします。
    /// </summary>
    /// <param name="block"></param>
    /// <returns></returns>
    public bool RemoveBlock(Block block)
    {
        if (Block != block)
            return false;
        Block = null;
        return true;
    }

    /// <summary>
    /// 指定された爆弾をこのセルに設定しようとします。
    /// </summary>
    /// <param name="bomb"></param>
    /// <returns></returns>
    public bool TrySetBomb(Bomb bomb)
    {
        // Characterとは同居可能だが、Block・既存Bomb・予約セルとは重複させない。
        if (bomb == null || Block != null || Bomb != null || IsReserved)
            return false;
        Bomb = bomb;
        return true;
    }

    /// <summary>
    /// 指定された爆弾をこのセルから削除しようとします。
    /// </summary>
    /// <param name="bomb"></param>
    /// <returns></returns>
    public bool RemoveBomb(Bomb bomb)
    {
        if (Bomb != bomb)
            return false;
        Bomb = null;
        return true;
    }

    /// <summary>
    /// このセルを予約します。予約済みのセルは他のオブジェクトが使用できなくなる。
    /// </summary>
    public void Reserve()
    {
        IsReserved = true;
    }
}
