using UnityEngine;

/// <summary>Block Prefabごとの種類と落下時間を設定します。</summary>
[CreateAssetMenu(fileName = "BlockSettings", menuName = "3D Grid Bomber/Settings/Block")]
public class BlockSettings : ScriptableObject
{
    [SerializeField] private BlockType _type = BlockType.Breakable;
    [SerializeField, Min(0f)] private float _fallDuration = 0.5f;

    public BlockType Type => _type;
    public float FallDuration => _fallDuration;
}
