using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ゲーム全体のマスを管理する。
/// 責務:
///   ・マス生成
///   ・マス取得
///   ・座標変換
///   ・空きマス判定
///   ・オブジェクト配置
///
/// 登録・移動系のメソッドは、条件を満たさない場合は何も変更せずfalseを返します。
/// </summary>
public class GridManager : MonoBehaviour
{
    [SerializeField] private GridSettings _settings;

    private GridCell[][][] _cells;

    // 通常セルは0～Size-1のまま。外殻だけ別管理し、負座標を配列添字にしません。
    private readonly Dictionary<Vector3Int, Block> _boundaryBlocks = new();

    /// <summary>グリッド全体のセル数です。Settings未設定ならゼロです。</summary>
    public Vector3Int Size => _settings != null ? _settings.Size : Vector3Int.zero;

    /// <summary>1セルのワールド幅です。</summary>
    public float CellSize => _settings.CellSize;

    private void Awake()
    {
        if (_settings == null)
        {
            Debug.LogError("GridManagerのGrid Settingsが未設定です。", this);
            enabled = false;
            return;
        }

        InitializeGrid();
    }

    /// <summary>設定されたサイズの全セルを論理グリッドとして初期化します。</summary>
    private void InitializeGrid()
    {
        Vector3Int size = _settings.Size;
        _cells = new GridCell[size.x][][];

        for (int x = 0; x < size.x; x++)
        {
            _cells[x] = new GridCell[size.y][];

            for (int y = 0; y < size.y; y++)
            {
                _cells[x][y] = new GridCell[size.z];

                for (int z = 0; z < size.z; z++)
                {
                    // 配列の添字がそのままグリッド座標になります。
                    Vector3Int position = new Vector3Int(x, y, z);
                    _cells[x][y][z] = new GridCell(position);
                }
            }
        }
    }

    /// <summary>範囲外ならnullを返します。外殻はGridCellを持たないため含みません。</summary>
    private GridCell GetCell(Vector3Int position)
    {
        if (!Contains(position))
            return null;

        return _cells[position.x][position.y][position.z];
    }

    /// <summary>通常セル（0～Size-1）の範囲内ならtrueです。外殻は含みません。</summary>
    public bool Contains(Vector3Int position)
        => _settings != null && GridUtility.IsInside(position, _settings.Size);

    /// <summary>内部を厚さ1セルで囲う外殻座標か。15セルなら各軸-1～15です。</summary>
    public bool IsBoundaryPosition(Vector3Int position)
    {
        Vector3Int size = Size;

        return size.x > 0 && size.y > 0 && size.z > 0 &&
               position.x >= -1 && position.x <= size.x &&
               position.y >= -1 && position.y <= size.y &&
               position.z >= -1 && position.z <= size.z &&
               !Contains(position);
    }

    /// <summary>セル中心のワールド座標を返します。</summary>
    public Vector3 GetWorldPosition(Vector3Int position)
        => GridUtility.GridToWorld(position, _settings.CellSize, transform.position);

    public Vector3Int GetGridPosition(Vector3 worldPosition)
        => GridUtility.WorldToGrid(worldPosition, _settings.CellSize, transform.position);

    /// <summary>
    /// Characterが落下して入れるセルか判定します（GridCell.IsWalkable）。
    /// Bombは妨げにならないため、CanCharacterEnterとは結果が異なります。
    /// </summary>
    public bool CanEnter(Vector3Int position)
    {
        GridCell cell = GetCell(position);
        return cell != null && cell.IsWalkable;
    }

    public bool HasBlock(Vector3Int position)
    {
        return GetBlock(position) != null;
    }

    /// <summary>通常セルに加えて外殻のBlockも返します。それ以外の範囲外はnullです。</summary>
    public Block GetBlock(Vector3Int position)
    {
        GridCell cell = GetCell(position);
        if (cell != null)
            return cell.Block;

        return _boundaryBlocks.TryGetValue(position, out Block boundary) ? boundary : null;
    }

    public CharacterBase GetCharacter(Vector3Int position)
    {
        GridCell cell = GetCell(position);
        return cell?.Character;
    }

    public Bomb GetBomb(Vector3Int position)
    {
        GridCell cell = GetCell(position);
        return cell?.Bomb;
    }

    public Item GetItem(Vector3Int position) => GetCell(position)?.Item;

    public int CountItems()
    {
        int count = 0;

        for (int x = 0; x < Size.x; x++)
        {
            for (int y = 0; y < Size.y; y++)
            {
                for (int z = 0; z < Size.z; z++)
                {
                    if (GetItem(new Vector3Int(x, y, z)) != null)
                        count++;
                }
            }
        }

        return count;
    }

    /// <summary>
    /// 方向先にある1段高いBlockの上へジャンプできるかを判定します。
    /// 成功時はBlock上の着地セルを返します。
    /// </summary>
    public bool CanJumpUp(Vector3Int currentPosition, Vector3Int direction, out Vector3Int landingPosition)
    {
        landingPosition = currentPosition;

        if (!GridUtility.IsHorizontalUnit(direction))
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

    /// <summary>Block・Bomb・Characterのいずれもないセルならtrueです。</summary>
    public bool CanCharacterEnter(Vector3Int position)
    {
        GridCell cell = GetCell(position);

        return cell != null &&
               cell.Block == null &&
               cell.Bomb == null &&
               cell.Character == null;
    }

    public bool TryRegisterCharacter(Vector3Int position, CharacterBase character)
    {
        GridCell cell = GetCell(position);

        if (!CanCharacterEnter(position))
            return false;

        return cell.TrySetCharacter(character);
    }

    public bool TryUnregisterCharacter(Vector3Int position, CharacterBase character)
    {
        GridCell cell = GetCell(position);

        if (cell == null)
            return false;

        return cell.RemoveCharacter(character);
    }

    public bool TryMoveCharacter(Vector3Int from, Vector3Int to, CharacterBase character)
    {
        GridCell fromCell = GetCell(from);
        GridCell toCell = GetCell(to);

        if (fromCell == null || toCell == null || !CanCharacterEnter(to))
            return false;

        return Transfer(
            fromCell,
            toCell,
            cell => cell.TrySetCharacter(character),
            cell => cell.RemoveCharacter(character));
    }

    /// <summary>
    /// 移動先を先に確保してから移動元を解除します。解除に失敗したら移動先の確保を取り消します。
    /// Character・Block・Bombの移動で共通の手順です。
    /// </summary>
    private static bool Transfer(
        GridCell fromCell,
        GridCell toCell,
        System.Func<GridCell, bool> trySet,
        System.Func<GridCell, bool> remove)
    {
        if (!trySet(toCell))
            return false;

        if (remove(fromCell))
            return true;

        remove(toCell);
        return false;
    }

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
            failureReason = $"対象セル {position} にはBlock '{cell.Block.name}' が存在します。";
            return false;
        }

        if (cell.Bomb != null)
        {
            failureReason = $"対象セル {position} にはBomb '{cell.Bomb.name}' が存在します。";
            return false;
        }

        if (cell.Character != null)
        {
            failureReason = $"対象セル {position} にはCharacter '{cell.Character.name}' が存在します。";
            return false;
        }

        failureReason = string.Empty;
        return true;
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
               cell.Bomb == null;
    }

    /// <summary>
    /// 落下Blockとして指定セルへ登録します。Characterがいても登録でき、落下時に押し潰します。
    /// </summary>
    public bool TryRegisterFallingBlock(Vector3Int position, Block block)
        => CanFallingBlockEnter(position) && GetCell(position).TrySetBlock(block);

    public bool TryRegisterBlock(Vector3Int position, Block block)
    {
        if (!CanPlaceBlock(position))
            return false;

        GridCell cell = GetCell(position);
        return cell.TrySetBlock(block);
    }

    /// <summary>外殻には破壊不可Blockのみを登録します。通常の配置APIからは配置できません。</summary>
    public bool TryRegisterBoundaryBlock(Vector3Int position, Block block)
    {
        if (!IsBoundaryPosition(position) || block == null || block.Type != BlockType.Unbreakable)
            return false;

        if (_boundaryBlocks.TryGetValue(position, out Block existing) && existing != null)
            return false;

        _boundaryBlocks[position] = block;
        return true;
    }

    /// <summary>外殻Blockにも対応します。</summary>
    public bool TryUnregisterBlock(Vector3Int position, Block block)
    {
        if (_boundaryBlocks.TryGetValue(position, out Block boundary) && boundary == block)
            return _boundaryBlocks.Remove(position);

        GridCell cell = GetCell(position);
        if (cell == null || cell.Block != block)
            return false;

        return cell.RemoveBlock(block);
    }

    public bool TryMoveBlock(Vector3Int from, Vector3Int to, Block block)
    {
        GridCell fromCell = GetCell(from);
        GridCell toCell = GetCell(to);

        if (fromCell == null || toCell == null || fromCell.Block != block || !CanFallingBlockEnter(to))
            return false;

        return Transfer(
            fromCell,
            toCell,
            cell => cell.TrySetBlock(block),
            cell => cell.RemoveBlock(block));
    }

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

        failureReason = string.Empty;
        return true;
    }

    public bool TryRegisterBomb(Vector3Int position, Bomb bomb)
    {
        if (!CanPlaceBomb(position))
            return false;

        GridCell cell = GetCell(position);
        return cell.TrySetBomb(bomb);
    }

    public bool TryUnregisterBomb(Vector3Int position, Bomb bomb)
    {
        GridCell cell = GetCell(position);
        if (cell == null || cell.Bomb != bomb)
            return false;

        return cell.RemoveBomb(bomb);
    }

    public bool TryMoveBomb(Vector3Int from, Vector3Int to, Bomb bomb)
    {
        GridCell fromCell = GetCell(from);
        GridCell toCell = GetCell(to);

        if (fromCell == null || toCell == null || fromCell.Bomb != bomb || !CanPlaceBomb(to))
            return false;

        return Transfer(
            fromCell,
            toCell,
            cell => cell.TrySetBomb(bomb),
            cell => cell.RemoveBomb(bomb));
    }

    /// <summary>ItemはCharacter・Bombと同居できるため、ItemとBlockだけを確認します。</summary>
    public bool CanPlaceItem(Vector3Int position)
    {
        GridCell cell = GetCell(position);
        return cell != null && cell.Item == null && cell.Block == null;
    }

    public bool TryRegisterItem(Vector3Int position, Item item)
        => CanPlaceItem(position) && GetCell(position).TrySetItem(item);

    public bool TryUnregisterItem(Vector3Int position, Item item)
        => GetCell(position)?.RemoveItem(item) == true;

    /// <summary>移動元を照合し、移動先を確保してからItemの登録を移します。</summary>
    public bool TryMoveItem(Vector3Int from, Vector3Int to, Item item)
    {
        if (item == null || GetItem(from) != item || !TryRegisterItem(to, item))
            return false;

        if (TryUnregisterItem(from, item))
            return true;

        // 移動元の解除に失敗したら移動先の登録を取り消します。
        TryUnregisterItem(to, item);
        return false;
    }
}
