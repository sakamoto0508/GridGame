using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ExplosionSystemが計算したセル情報を、方向性のある見た目用Effectとして表示します。
/// ダメージやBlock破壊などのゲーム判定は担当しません。
/// </summary>
public class ExplosionView : MonoBehaviour
{
    [SerializeField] private ExplosionVisualSettings _settings;

    /// <summary>指定された全セルへ、区分と方向に対応した爆風Effectを生成します。</summary>
    public bool Show(GridManager gridManager,IReadOnlyList<ExplosionCellData> explosionCells)
    {
        if (gridManager == null)
        {
            Debug.LogWarning("爆風を表示できません: GridManagerがnullです。", this);
            return false;
        }

        if (_settings == null ||
            _settings.CenterPrefab == null ||
            _settings.MiddlePrefab == null ||
            _settings.EndPrefab == null)
        {
            Debug.LogWarning(
                "爆風を表示できません: Center、Middle、End Prefabのいずれかが未設定です。",
                this);
            return false;
        }

        if (explosionCells == null || explosionCells.Count == 0)
            return false;

        for (int i = 0; i < explosionCells.Count; i++)
        {
            ExplosionCellData cell = explosionCells[i];
            ExplosionEffect prefab = GetPrefab(cell.Type);
            Vector3 worldPosition = gridManager.GetWorldPosition(cell.Position);
            Quaternion rotation = GetRotation(cell.Direction);

            // Bombの子にするとBomb破棄時に一緒に消えるため、親を設定せず生成します。
            ExplosionEffect effect = Instantiate(prefab, worldPosition, rotation);
            effect.Play(_settings.EffectDuration);
        }

        return true;
    }

    /// <summary>表示区分に対応するPrefabを返します。</summary>
    private ExplosionEffect GetPrefab(ExplosionCellType type)
    {
        switch (type)
        {
            case ExplosionCellType.Center:
                return _settings.CenterPrefab;
            case ExplosionCellType.Middle:
                return _settings.MiddlePrefab;
            case ExplosionCellType.BlockedEnd:
                // BlockedEndが未設定なら通常のEndを代用します。
                return _settings.BlockedEndPrefab != null
                    ? _settings.BlockedEndPrefab
                    : _settings.EndPrefab;
            default:
                return _settings.EndPrefab;
        }
    }

    /// <summary>
    /// ローカルZ+向きで作られたPrefabを、爆風が伸びるXYZ方向へ回転します。
    /// Centerには方向がないためPrefab本来の回転を使用します。
    /// </summary>
    private static Quaternion GetRotation(Vector3Int direction)
    {
        if (direction == Vector3Int.zero)
            return Quaternion.identity;

        return Quaternion.FromToRotation(Vector3.forward, (Vector3)direction);
    }
}
