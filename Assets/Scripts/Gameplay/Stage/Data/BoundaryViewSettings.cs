using UnityEngine;

/// <summary>外殻の描画専用設定。衝突・グリッドのルールには影響しません。</summary>
[CreateAssetMenu(menuName = "3D Grid Bomber/Settings/Boundary View", fileName = "BoundaryViewSettings")]
public class BoundaryViewSettings : ScriptableObject
{
    [SerializeField] private bool _showOutline = true;
    [SerializeField] private bool _showFloorGrid = true;
    [SerializeField] private bool _showWallGrid = true;
    [SerializeField] private Color _lineColor = new Color(0.4f, 0.75f, 1f, 1f);
    [SerializeField, Min(0.001f)] private float _lineWidthInCells = 0.025f;
    [Tooltip("軸方向の微小な誤差で隣の壁が点滅することを防ぎます。")]
    [SerializeField, Range(0f, 0.1f)] private float _directionThreshold = 0.01f;
    public bool ShowOutline => _showOutline;
    public bool ShowFloorGrid => _showFloorGrid;
    public bool ShowWallGrid => _showWallGrid;
    public Color LineColor => _lineColor;
    public float LineWidthInCells => Mathf.Max(0.001f, _lineWidthInCells);
    public float DirectionThreshold => _directionThreshold;
}
