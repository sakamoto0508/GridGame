using NUnit.Framework;
using UnityEngine;

/// <summary>
///    ゲーム全体のマスを管理する。
///    責務
///    マス生成
///    マス取得
///    座標変換
///     空きマス判定
///    オブジェクト配置
/// </summary>
public class GridManager : MonoBehaviour
{
    [SerializeField] private int _sizeX = 7;
    [SerializeField] private int _sizeY = 7;
    [SerializeField] private int _sizeZ = 7;

    GridCell[][][] _cells;

    private void InitGrid()
    {
        for(int i=0;i<_sizeX; i++)
        {
            for (int j = 0; j < _sizeY; j++)
            {
                for (int k = 0; k < _sizeZ; k++)
                {
                    _cells[i][j][k] = new GridCell();
                }
            }
        }
    }

    public void GetCell()
    {

    }

    public void GetWorldLocation()
    {

    }

    public void GetGridIndex()
    {

    }

    public void CanMove()
    {

    }

    public void PlaceActor()
    {

    }

    public void RemoveActor()
    {

    }

    public void GetNeighborCells()
    {

    }
}
