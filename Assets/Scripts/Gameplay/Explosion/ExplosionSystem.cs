using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Bombを起点として爆風が届くセルを計算し、爆風の効果を盤面へ適用します。
/// セル探索と効果適用を分け、探索結果をテストしやすい形にしています。
/// </summary>
public static class ExplosionSystem
{
    /// <summary>UnityではY軸が高さなので、XYZ各軸の正負6方向を探索します。</summary>
    private static readonly Vector3Int[] Directions =
    {
        Vector3Int.right,
        Vector3Int.left,
        Vector3Int.up,
        Vector3Int.down,
        Vector3Int.forward,
        Vector3Int.back
    };

    /// <summary>爆風の座標・方向・表示区分を計算します。起点はCenterとして含まれます。</summary>
    public static IReadOnlyList<ExplosionCellData> CalculateExplosionCells(GridManager gridManager,Vector3Int origin
                                                                            ,int explosionPower)
    {
        List<ExplosionCellData> explosionCells = new List<ExplosionCellData>();

        if (gridManager == null)
        {
            Debug.LogError("爆風を計算できません: GridManagerがnullです。");
            return explosionCells;
        }

        if (!gridManager.Contains(origin))
        {
            Debug.LogWarning($"爆風を計算できません: 起点セル {origin} がグリッド範囲外です。");
            return explosionCells;
        }

        explosionCells.Add(new ExplosionCellData(
            origin,
            Vector3Int.zero,
            ExplosionCellType.Center));

        int range = Mathf.Max(0, explosionPower);

        foreach (Vector3Int direction in Directions)
            AddCellsInDirection(gridManager, origin, direction, range, explosionCells);

        return explosionCells;
    }

    /// <summary>座標だけが必要な既存処理やテスト向けの互換APIです。</summary>
    public static IReadOnlyList<Vector3Int> CalculateAffectedCells(GridManager gridManager,Vector3Int origin,
                                                                    int explosionPower)
    {
        IReadOnlyList<ExplosionCellData> explosionCells =
            CalculateExplosionCells(gridManager, origin, explosionPower);
        List<Vector3Int> positions = new List<Vector3Int>(explosionCells.Count);

        for (int i = 0; i < explosionCells.Count; i++)
            positions.Add(explosionCells[i].Position);

        return positions;
    }

    /// <summary>爆風を計算して盤面へ効果を適用し、表示用データを返します。</summary>
    public static IReadOnlyList<ExplosionCellData> GenerateExplosion( GridManager gridManager,Vector3Int origin
                                                                        ,int explosionPower)
    {
        IReadOnlyList<ExplosionCellData> explosionCells =
            CalculateExplosionCells(gridManager, origin, explosionPower);

        ApplyExplosionEffects(gridManager, explosionCells);
        LogExplosion(origin, explosionPower, explosionCells);
        return explosionCells;
    }

    /// <summary>
    /// 計算済みの爆風セルへゲーム上の効果を適用します。
    /// 盤面走査中の変更を避けるため、連鎖対象Bombは先に収集して最後に爆発させます。
    /// </summary>
    private static void ApplyExplosionEffects(GridManager gridManager,IReadOnlyList<ExplosionCellData> explosionCells)
    {
        if (gridManager == null)
            return;

        HashSet<Bomb> chainedBombs = new HashSet<Bomb>();

        for (int i = 0; i < explosionCells.Count; i++)
        {
            Vector3Int position = explosionCells[i].Position;
            Block block = gridManager.GetBlock(position);
            CharacterBase character = gridManager.GetCharacter(position);
            Bomb bomb = gridManager.GetBomb(position);

            if (bomb != null && bomb.State != BombState.Exploded)
                chainedBombs.Add(bomb);

            if (character != null)
                character.Kill(DeathCause.Explosion);

            if (block != null && block.Type == BlockType.Breakable)
                block.BlockBreak();
        }

        foreach (Bomb chainedBomb in chainedBombs)
        {
            if (chainedBomb != null)
                chainedBomb.Explode();
        }
    }

    /// <summary>1方向を射程まで走査し、Blockまたはグリッド端で停止します。</summary>
    private static void AddCellsInDirection(GridManager gridManager,Vector3Int origin,Vector3Int direction
                                                ,int range,List<ExplosionCellData> explosionCells)
    {
        int firstCellIndex = explosionCells.Count;

        for (int distance = 1; distance <= range; distance++)
        {
            Vector3Int position = origin + direction * distance;

            if (!gridManager.Contains(position))
            {
                MarkLastDirectionCellBlocked(explosionCells, firstCellIndex);
                break;
            }

            Block block = gridManager.GetBlock(position);

            if (block != null && block.Type == BlockType.Unbreakable)
            {
                MarkLastDirectionCellBlocked(explosionCells, firstCellIndex);
                break;
            }

            bool isBreakableBlock = block != null && block.Type == BlockType.Breakable;
            bool isRangeEnd = distance == range;
            bool isGridEnd = !gridManager.Contains(position + direction);

            ExplosionCellType type = isBreakableBlock
                ? ExplosionCellType.BlockedEnd
                : isRangeEnd || isGridEnd
                    ? ExplosionCellType.End
                    : ExplosionCellType.Middle;

            explosionCells.Add(new ExplosionCellData(position, direction, type));

            if (isBreakableBlock || isGridEnd)
                break;
        }
    }

    /// <summary>
    /// 破壊不能Blockまたはグリッド端の直前まで届いた爆風をBlockedEndへ変更します。
    /// 障害物が起点に隣接する場合は方向セルがないため何も変更しません。
    /// </summary>
    private static void MarkLastDirectionCellBlocked( List<ExplosionCellData> explosionCells,int firstCellIndex)
    {
        if (explosionCells.Count <= firstCellIndex)
            return;

        int lastIndex = explosionCells.Count - 1;
        ExplosionCellData lastCell = explosionCells[lastIndex];
        explosionCells[lastIndex] = new ExplosionCellData(
            lastCell.Position,
            lastCell.Direction,
            ExplosionCellType.BlockedEnd);
    }

    /// <summary>セル座標と表示区分をConsoleへ出力します。</summary>
    private static void LogExplosion(Vector3Int origin,int explosionPower,IReadOnlyList<ExplosionCellData> explosionCells)
    {
        StringBuilder message = new StringBuilder();
        message.Append($"Explosion cells: Origin={origin}, Power={explosionPower}, Cells=");

        for (int i = 0; i < explosionCells.Count; i++)
        {
            if (i > 0)
                message.Append(", ");

            ExplosionCellData cell = explosionCells[i];
            message.Append($"{cell.Position}[{cell.Type}]");
        }

        Debug.Log(message.ToString());
    }
}
