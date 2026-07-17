using NUnit.Framework;
using System.Collections.Generic;
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

    private void Awake()
    {
        InitGrid();
    }

    private void InitGrid()
    {
        _cells = new GridCell[_sizeX][][];
        for (int i = 0; i < _sizeX; i++)
        {
            _cells[i] = new GridCell[_sizeY][];
            for (int j = 0; j < _sizeY; j++)
            {
                _cells[i][j] = new GridCell[_sizeZ];
                for (int k = 0; k < _sizeZ; k++)
                {
                    _cells[i][j][k] = new GridCell(new Vector3Int(i, j, k), true);
                }
            }
        }
    }

    /// <summary>
    /// 指定された座標のマスを取得する
    /// </summary>
    /// <param name="gridPos"></param>
    /// <returns></returns>
    public GridCell GetCell(Vector3Int gridPos)
    {
        if (!IsInsideGrid(gridPos))
            return null;

        return _cells[gridPos.x][gridPos.y][gridPos.z];
    }

    /// <summary>
    /// (X,Y,Z) を Cells の配列インデックスへ変換する。
    /// </summary>
    public int GetGridIndex(Vector3Int gridPos)
    {
        return gridPos.x + gridPos.y * _sizeX + gridPos.z * _sizeX * _sizeY;
    }

    /// <summary>
    /// 指定された座標がグリッド内にあるかどうかを判定する。
    /// </summary>
    /// <param name="gridPos"></param>
    /// <returns></returns>
    public bool IsInsideGrid(Vector3Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < _sizeX &&
               gridPos.y >= 0 && gridPos.y < _sizeY &&
               gridPos.z >= 0 && gridPos.z < _sizeZ;
    }

    /// <summary>
    /// グリッド座標をワールド座標に変換する。
    /// </summary>
    /// <param name="gridPos"></param>
    /// <returns></returns>
    public Vector3 GetWorldLocation(Vector3Int gridPos)
    {
        // グリッド座標をワールド座標に変換する処理を実装
        return Vector3.zero;
    }


    /// <summary>
    /// 指定された座標のマスに移動可能かどうかを判定する。
    /// </summary>
    public bool IsWalkble(Vector3Int gridPos)
    {
        GridCell cell = GetCell(gridPos);

        if (cell == null)
            return false;
        if(!cell.IsWalkable)
            return false;
        if(cell.OccupyingObject != null)
            return false;

        return true;
    }

    //引数にplayerやボムを必要とするので後で書く。
    /// <summary>
    /// キャラクターを移動させる。
    /// </summary>
    //public bool MoveCharacter()

    /// <summary>
    /// ボムを設置する。設置できたらtrue
    /// </summary>
    //public bool PlaceBomb()

    /// <summary>
    /// ボムを削除する。
    /// </summary>
    //public bool RemoveBomb()

    /// <summary>
    /// ブロックを設置する。設置できたらtrue
    /// </summary>
    //public bool PlaceBlock()

    /// <summary>
    /// ブロックを削除する。
    /// </summary>  
    //public bool RemoveBlock()

    /// <summary>
    /// アイテムを設置する。設置できたらtrue
    /// </summary>
    //public bool PlaceItem()

    /// <summary>
    /// アイテムを削除する。
    /// </summary>
    //public bool RemoveItem()

    /// <summary>
    /// 上下左右前後の隣接するマスを取得する。
    /// </summary>
    public List<GridCell> GetNeighborCells(Vector3Int gridPos)
    {

    }

    /// <summary>
    /// ランダムな空きマスを返す。
    /// アイテムや敵のスポーンに使う。
    /// </summary>
    /// <returns></returns>
    public GridCell GetRandomEmptyCell()
    {
    }

    /// <summary>
    /// セル情報をリセットする。
    /// </summary>
    public void ClearCell(Vector3Int pos)
    {
        if (!IsInsideGrid(pos))
            return;

        _cells[pos.x][pos.y][pos.z].ResetCell();
    }
}
