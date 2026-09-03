using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ExplosionSystemが計算したセル情報を、方向性のある見た目用Effectとして表示します。
/// ダメージやBlock破壊などのゲーム判定は担当しません。
/// </summary>
public class ExplosionView : MonoBehaviour
{
    [Header("Explosion Effect Prefabs")]
    [SerializeField] private ExplosionEffect _centerPrefab;
    [SerializeField] private ExplosionEffect _middlePrefab;
    [SerializeField] private ExplosionEffect _endPrefab;
    [SerializeField] private ExplosionEffect _blockedEndPrefab;

    [Tooltip("Middle/End PrefabはローカルZ+方向へ伸びる向きで作成してください。")]
    [SerializeField, Min(0f)] private float _effectDuration = 0.35f;

    /// <summary>指定された全セルへ、区分と方向に対応した爆風Effectを生成します。</summary>
    public bool Show(
        GridManager gridManager,
        IReadOnlyList<ExplosionCellData> explosionCells)
    {
        if (gridManager == null)
        {
            Debug.LogWarning("爆風を表示できません: GridManagerがnullです。", this);
            return false;
        }

        if (_centerPrefab == null || _middlePrefab == null || _endPrefab == null)
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
            effect.Play(_effectDuration);
        }

        return true;
    }

    /// <summary>表示区分に対応するPrefabを返します。</summary>
    private ExplosionEffect GetPrefab(ExplosionCellType type)
    {
        switch (type)
        {
            case ExplosionCellType.Center:
                return _centerPrefab;
            case ExplosionCellType.Middle:
                return _middlePrefab;
            case ExplosionCellType.BlockedEnd:
                // BlockedEndが未設定なら通常のEndを代用します。
                return _blockedEndPrefab != null ? _blockedEndPrefab : _endPrefab;
            default:
                return _endPrefab;
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
