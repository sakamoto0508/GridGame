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

    public bool IsReserved { get; private set; }

    public bool IsWalkable =>
        Block == null &&
        Character == null &&
        !IsReserved;

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
}