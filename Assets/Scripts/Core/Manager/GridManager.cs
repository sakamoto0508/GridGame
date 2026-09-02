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
    public Vector3Int Size => new Vector3Int(_sizeX, _sizeY, _sizeZ);
    [SerializeField] private int _sizeX = 7;
    [SerializeField] private int _sizeY = 7;
    [SerializeField] private int _sizeZ = 7;
    [SerializeField] private float _cellSize = 1.0f;

    private GridCell[][][] _cells;

    private void Awake()
    {
        InitializeGrid();
    }

    /// <summary>設定されたサイズの全セルを論理グリッドとして初期化します。</summary>
    private void InitializeGrid()
    {
        _cells = new GridCell[_sizeX][][];
        for (int x = 0; x < _sizeX; x++)
        {
            _cells[x] = new GridCell[_sizeY][];
            for (int y = 0; y < _sizeY; y++)
            {
                _cells[x][y] = new GridCell[_sizeZ];
                for (int z = 0; z < _sizeZ; z++)
                {
                    // グリッド座標を計算し、GridCellを生成して格納する
                    // グリッド座標はスタート位置を考慮して計算する
                    Vector3Int position = new Vector3Int(x, y, z);
                    _cells[x][y][z] = new GridCell(position);
                }
            }
        }
    }

    /// <summary>
    /// 指定されたグリッド座標に対応するGridCellを取得する。
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    private GridCell GetCell(Vector3Int position)
    {
        if (!Contains(position))
            return null;
        return _cells[position.x][position.y][position.z];
    }

    /// <summary>
    /// 指定されたグリッド座標がグリッド内に存在するかどうかを判定する。
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public bool Contains(Vector3Int position)
    => GridUtility.IsInside(position, new Vector3Int(_sizeX, _sizeY, _sizeZ));

    /// <summary>
    /// 指定されたグリッド座標をワールド座標に変換する。
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public Vector3 GetWorldPosition(Vector3Int position)
        => GridUtility.GridToWorld(position, _cellSize, transform.position);

    /// <summary>
    /// 指定されたワールド座標をグリッド座標に変換する。
    /// </summary>
    /// <param name="worldPosition"></param>
    /// <returns></returns>
    public Vector3Int GetGridPosition(Vector3 worldPosition)
        => GridUtility.WorldToGrid(worldPosition, _cellSize, transform.position);

    /// <summary>
    /// 指定されたグリッド座標にキャラクターが入れるかどうかを判定する。
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public bool CanEnter(Vector3Int position)
    {
        if (!Contains(position))
            return false;
        GridCell cell = GetCell(position);
        return cell != null && cell.IsWalkable;
    }

    /// <summary>指定セルにBlockが登録されているかを返します。</summary>
    public bool HasBlock(Vector3Int position)
    {
        GridCell cell = GetCell(position);
        return cell != null && cell.Block != null;
    }

    /// <summary>
    /// 方向先にある1段高いBlockの上へジャンプできるかを判定します。
    /// 成功時はBlock上の着地セルを返します。
    /// </summary>
    public bool CanJumpUp( Vector3Int currentPosition,Vector3Int direction,out Vector3Int landingPosition)
    {
        landingPosition = currentPosition;

        if (direction.y != 0 || Mathf.Abs(direction.x) + Mathf.Abs(direction.z) != 1)
            return false;
        
        Vector3Int blockPosition = currentPosition + direction;
        landingPosition = blockPosition + Vector3Int.up;

        GridCell blockCell = GetCell(blockPosition);
        GridCell landingCell = GetCell(landingPosition);

        return blockCell != null &&
               blockCell.Block != null &&
               landingCell != null &&
               landingCell.IsWalkable;
    }

    /// <summary>指定セルへBlockを配置できるかを判定します。</summary>
    public bool CanPlaceBlock(Vector3Int position)
    {
        return CanPlaceBlock(position, out _);
    }

    /// <summary>
    /// 指定セルへBlockを配置できるかを判定し、配置できない場合は理由を返します。
    /// </summary>
    public bool CanPlaceBlock(Vector3Int position, out string failureReason)
    {
        GridCell cell = GetCell(position);

        if (cell == null)
        {
            failureReason = $"対象セル {position} がグリッド範囲外です。";
            return false;
        }

        if (cell.Block != null)
        {
            failureReason =
                $"対象セル {position} にはBlock '{cell.Block.name}' が存在します。";
            return false;
        }

        if (cell.Bomb != null)
        {
            failureReason =
                $"対象セル {position} にはBomb '{cell.Bomb.name}' が存在します。";
            return false;
        }

        if (cell.Character != null)
        {
            failureReason =
                $"対象セル {position} にはCharacter '{cell.Character.name}' が存在します。";
            return false;
        }

        if (cell.IsReserved)
        {
            failureReason = $"対象セル {position} は予約されています。";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    /// <summary>
    /// 指定されたグリッド座標から別のグリッド座標にキャラクターを移動させる。
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="character"></param>
    /// <returns></returns>
    public bool TryMoveCharacter(Vector3Int from, Vector3Int to, CharacterBase character)
    {
        GridCell fromCell = GetCell(from);
        GridCell toCell = GetCell(to);
        if (fromCell == null || toCell == null)
            return false;
        if (!toCell.IsWalkable)
            return false;
        // 先に移動先を確保する
        if (!toCell.TrySetCharacter(character))
            return false;
        // 移動元の解除に失敗したら元へ戻す
        if (!fromCell.RemoveCharacter(character))
        {
            toCell.RemoveCharacter(character);
            return false;
        }

        return true;
    }

    /// <summary>
    /// 指定されたグリッド座標にキャラクターを登録する。
    /// </summary>
    /// <param name="position"></param>
    /// <param name="character"></param>
    /// <returns></returns>
    public bool TryRegisterCharacter(Vector3Int position,CharacterBase character)
    {
        GridCell cell = GetCell(position);

        if (cell == null || !cell.IsWalkable)
            return false;

        return cell.TrySetCharacter(character);
    }

    /// <summary>
    /// 指定されたグリッド座標からキャラクターを登録解除する。
    /// </summary>
    /// <param name="position"></param>
    /// <param name="character"></param>
    /// <returns></returns>
    public bool TryUnregisterCharacter(Vector3Int position, CharacterBase character)
    {
        GridCell cell = GetCell(position);

        if(cell == null)
            return false;

        return cell.RemoveCharacter(character);
    }

    /// <summary>
    /// 指定されたグリッド座標にブロックを配置する。
    /// </summary>
    /// <param name="position"></param>
    /// <param name="block"></param>
    /// <returns></returns>
    public bool TryRegisterBlock(Vector3Int position, Block block)
    {
        if (!CanPlaceBlock(position))
            return false;

        GridCell cell = GetCell(position);
        return cell.TrySetBlock(block);
    }

    /// <summary>
    /// 指定されたグリッド座標からブロックを削除する。
    /// </summary>
    /// <param name="position"></param>
    /// <param name="block"></param>
    /// <returns></returns>
    public bool TryUnregisterBlock(Vector3Int position, Block block)
    {
        GridCell cell = GetCell(position);
        if (cell == null || cell.Block != block)
            return false;
        return cell.RemoveBlock(block);
    }

    public bool TryMoveBlock(Vector3Int from, Vector3Int to, Block block)
    {
        GridCell fromCell = GetCell(from);
        GridCell toCell = GetCell(to);

        if (fromCell == null || toCell == null)
            return false;
        if (toCell.Block != null)
            return false;
        // 先に移動先を確保する
        if (!toCell.TrySetBlock(block))
            return false;
        // 移動元の解除に失敗したら元へ戻す
        if (!fromCell.RemoveBlock(block))
        {
            toCell.RemoveBlock(block);
            return false;
        }
        return true;
    }

    /// <summary>
    /// Gizmosを描画するためのメソッド。Unityエディタ上でグリッドの可視化に使用される。
    /// </summary>
    private void OnDrawGizmos()
    {

    }
}
