using UnityEngine;

/// <summary>グリッド全体のサイズと1セルのワールド幅を設定します。</summary>
[CreateAssetMenu(fileName = "GridSettings", menuName = "3D Grid Bomber/Settings/Grid")]
public class GridSettings : ScriptableObject
{
    [SerializeField] private Vector3Int _size = new Vector3Int(7, 7, 7);
    [SerializeField, Min(0.01f)] private float _cellSize = 1f;

    public Vector3Int Size => _size;
    public float CellSize => _cellSize;
}
