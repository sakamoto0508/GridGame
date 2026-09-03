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
    /// 指定セルに存在するBlockを返します。
    /// セルが範囲外、またはBlockが存在しない場合はnullを返します。
    /// </summary>
    public Block GetBlock(Vector3Int position)
    {
        GridCell cell = GetCell(position);
        return cell?.Block;
    }

    /// <summary>
    /// 指定セルに存在するCharacterを返します。
    /// セルが範囲外、またはCharacterが存在しない場合はnullを返します。
    /// </summary>
    public CharacterBase GetCharacter(Vector3Int position)
    {
        GridCell cell = GetCell(position);
        return cell?.Character;
    }

    /// <summary>
    /// 指定セルに存在するBombを返します。
    /// セルが範囲外、またはBombが存在しない場合はnullを返します。
    /// </summary>
    public Bomb GetBomb(Vector3Int position)
    {
        GridCell cell = GetCell(position);
        return cell?.Bomb;
    }

    /// <summary>
    /// 落下中のBlockが指定セルを通過できるか判定します。
    /// Characterは押し潰す対象なので、Blockの落下を妨げません。
    /// </summary>
    public bool CanFallingBlockEnter(Vector3Int position)
    {
        GridCell cell = GetCell(position);

        return cell != null &&
               cell.Block == null &&
               cell.Bomb == null &&
               !cell.IsReserved;
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

    /// <summary>指定セルへBombを配置できるかを判定します。</summary>
    public bool CanPlaceBomb(Vector3Int position)
    {
        return CanPlaceBomb(position, out _);
    }

    /// <summary>
    /// 指定セルへBombを配置できるかを判定し、失敗時は理由を返します。
    /// CharacterとBombは同じセルに存在できるため、Characterは妨げになりません。
    /// </summary>
    public bool CanPlaceBomb(Vector3Int position, out string failureReason)
    {
        GridCell cell = GetCell(position);

        if (cell == null)
        {
            failureReason = $"対象セル {position} がグリッド範囲外です。";
            return false;
        }

        if (cell.Block != null)
        {
            failureReason = $"対象セル {position} にはBlock '{cell.Block.name}' が存在します。";
            return false;
        }

        if (cell.Bomb != null)
        {
            failureReason = $"対象セル {position} にはBomb '{cell.Bomb.name}' が存在します。";
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

    /// <summary>
    /// 指定されたグリッド座標から別のグリッド座標にブロックを移動させる。
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="block"></param>
    /// <returns></returns>
    public bool TryMoveBlock(Vector3Int from, Vector3Int to, Block block)
    {
        GridCell fromCell = GetCell(from);
        GridCell toCell = GetCell(to);

        if (fromCell == null || toCell == null)
            return false;
        if (fromCell.Block != block || !CanFallingBlockEnter(to))
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
    /// 指定されたグリッド座標から別のグリッド座標に爆弾を移動させる。
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="bomb"></param>
    /// <returns></returns>
    public bool TryMoveBomb(Vector3Int from, Vector3Int to, Bomb bomb)
    {
        GridCell fromCell = GetCell(from);
        GridCell toCell = GetCell(to);
        if (fromCell == null || toCell == null)
            return false;
        if (fromCell.Bomb != bomb || !CanPlaceBomb(to))
            return false;
        // 先に移動先を確保する
        if (!toCell.TrySetBomb(bomb))
            return false;
        // 移動元の解除に失敗したら元へ戻す
        if (!fromCell.RemoveBomb(bomb))
        {
            toCell.RemoveBomb(bomb);
            return false;
        }
        return true;
    }

    /// <summary>
    /// 指定されたグリッド座標に爆弾を登録する。
    /// </summary>
    /// <param name="position"></param>
    /// <param name="bomb"></param>
    /// <returns></returns>
    public bool TryRegisterBomb(Vector3Int position, Bomb bomb)
    {
        if (!CanPlaceBomb(position))
            return false;

        GridCell cell = GetCell(position);
        return cell.TrySetBomb(bomb);
    }

    /// <summary>
    /// 指定されたグリッド座標から爆弾を登録解除する。
    /// </summary>
    /// <param name="position"></param>
    /// <param name="bomb"></param>
    /// <returns></returns>
    public bool TryUnregisterBomb(Vector3Int position, Bomb bomb)
    {
        GridCell cell = GetCell(position);
        if (cell == null || cell.Bomb != bomb)
            return false;
        return cell.RemoveBomb(bomb);
    }

    /// <summary>
    /// 指定されたグリッド座標のセルを予約する。予約済みのセルは他のオブジェクトが使用できなくなる。
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public bool TryReserveCell(Vector3Int position)
    {
        GridCell cell = GetCell(position);
        if (cell == null || cell.IsReserved)
            return false;
        cell.Reserve();
        return true;
    }


    /// <summary>
    /// Gizmosを描画するためのメソッド。Unityエディタ上でグリッドの可視化に使用される。
    /// </summary>
    private void OnDrawGizmos()
    {

    }
}
