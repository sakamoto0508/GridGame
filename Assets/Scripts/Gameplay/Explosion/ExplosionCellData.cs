using UnityEngine;

/// <summary>爆風セルを表示するときの形状区分です。</summary>
public enum ExplosionCellType
{
    Center,
    Middle,
    End,
    BlockedEnd
}

/// <summary>爆風1セル分の座標、伸びる方向、表示区分を保持します。</summary>
public readonly struct ExplosionCellData
{
    public Vector3Int Position { get; }
    public Vector3Int Direction { get; }
    public ExplosionCellType Type { get; }

    public ExplosionCellData( Vector3Int position,Vector3Int direction,ExplosionCellType type)
    {
        Position = position;
        Direction = direction;
        Type = type;
    }
}
