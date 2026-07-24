using UnityEngine;

/// <summary>
/// 破壊可能かどうかを示すブロックの種類
/// </summary>
public enum BlockType
{
    Breakable,
    Unbreakable
}

/// <summary>
/// グリッド上のブロックを表すクラス
/// </summary>
public class Block : MonoBehaviour
{
    [SerializeField] private BlockType _blockType;

    public BlockType Type => _blockType;
    public Vector3Int GridPosition { get; private set; }

    public void Initialize(Vector3Int gridPosition)
    {
        GridPosition = gridPosition;
    }
}