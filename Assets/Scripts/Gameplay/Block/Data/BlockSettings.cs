using UnityEngine;

/// <summary>Block Prefabごとの種類と落下時間を設定します。</summary>
[CreateAssetMenu(fileName = "BlockSettings", menuName = "3D Grid Bomber/Settings/Block")]
public class BlockSettings : ScriptableObject
{
    [SerializeField] private BlockType _type = BlockType.Breakable;
    [SerializeField, Min(0f)] private float _fallDuration = 0.5f;
    [Header("Breakable Block Outline")]
    [SerializeField] private bool _showOutline = true;
    [SerializeField] private Color _outlineColor = new Color(0.15f, 0.2f, 0.25f, 1f);
    [SerializeField, Min(0.001f)] private float _outlineWidthInCells = 0.025f;

    public BlockType Type => _type;
    public float FallDuration => _fallDuration;
    public bool ShowOutline => _showOutline;
    public Color OutlineColor => _outlineColor;
    public float OutlineWidthInCells => Mathf.Max(0.001f, _outlineWidthInCells);
}
