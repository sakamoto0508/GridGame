using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 画像素材なしで角落としパネルを描くUGUI Graphic。CanvasGroup/Maskに対応します。
/// 再描画はRectや色の変更時のみで、UpdateによるUI更新は行いません。
/// </summary>
[AddComponentMenu("UI/3D Grid Bomber/Cyberpunk Panel")]
[RequireComponent(typeof(CanvasRenderer))]
public class CyberpunkPanelGraphic : MaskableGraphic
{
    /// <summary>角を落とした八角形の頂点数です。</summary>
    private const int CornerVertexCount = 8;

    /// <summary>枠線の上限を短辺に対するこの割合に抑え、内側の面に幅と高さを残します。</summary>
    private const float MaxBorderFraction = 0.49f;

    /// <summary>角のカット量は短辺の半分までに制限します。</summary>
    private const float MaxCornerCutFraction = 0.5f;

    // 頂点バッファ内の各頂点グループの先頭インデックスです。
    private const int OuterBorderStart = 0;
    private const int InnerBorderStart = CornerVertexCount;
    private const int FillCenterIndex = CornerVertexCount * 2;
    private const int FillRingStart = FillCenterIndex + 1;

    [SerializeField] private Color _border = Color.cyan;
    [SerializeField, Min(0)] private float _borderWidth = 2f;
    [SerializeField, Min(0)] private float _cornerCut = 14f;

    public void SetStyle(Color fill, Color border, float width, float cut)
    {
        color = fill;
        _border = border;
        _borderWidth = width;
        _cornerCut = cut;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();
        if (rect.width <= 0 || rect.height <= 0)
            return;

        float shortSide = Mathf.Min(rect.width, rect.height);
        float cut = Mathf.Clamp(_cornerCut, 0, shortSide * MaxCornerCutFraction);
        float width = Mathf.Clamp(_borderWidth, 0, shortSide * MaxBorderFraction);

        Vector2[] outer = Corners(rect, cut);
        Rect inset = new Rect(
            rect.x + width,
            rect.y + width,
            rect.width - width * 2,
            rect.height - width * 2);

        // 45度の斜辺もほぼ同じ幅になるように内側のカット量を調整します。
        // (2 - √2) は、斜辺の法線方向の幅を軸方向の幅に揃えるための係数です。
        float innerCut = Mathf.Clamp(
            cut - width * (2 - Mathf.Sqrt(2)),
            0,
            Mathf.Min(inset.width, inset.height) * MaxCornerCutFraction);
        Vector2[] inner = Corners(inset, innerCut);

        // 枠線: 外周と内周の頂点を結んで帯状に描きます。
        for (int i = 0; i < CornerVertexCount; i++)
            vh.AddVert(outer[i], _border, Vector2.zero);

        for (int i = 0; i < CornerVertexCount; i++)
            vh.AddVert(inner[i], _border, Vector2.zero);

        for (int i = 0; i < CornerVertexCount; i++)
        {
            int next = (i + 1) % CornerVertexCount;
            vh.AddTriangle(OuterBorderStart + i, OuterBorderStart + next, InnerBorderStart + next);
            vh.AddTriangle(OuterBorderStart + i, InnerBorderStart + next, InnerBorderStart + i);
        }

        // 塗りの頂点は枠と分離し、色補間で枠がにじむのを防ぎます。
        vh.AddVert(inset.center, color, Vector2.zero);

        for (int i = 0; i < CornerVertexCount; i++)
            vh.AddVert(inner[i], color, Vector2.zero);

        for (int i = 0; i < CornerVertexCount; i++)
        {
            vh.AddTriangle(
                FillCenterIndex,
                FillRingStart + i,
                FillRingStart + (i + 1) % CornerVertexCount);
        }
    }

    /// <summary>矩形の4隅をcだけ切り落とした八角形の頂点を、反時計回りで返します。</summary>
    private static Vector2[] Corners(Rect r, float c) => new[]
    {
        new Vector2(r.xMin + c, r.yMin),
        new Vector2(r.xMax - c, r.yMin),
        new Vector2(r.xMax, r.yMin + c),
        new Vector2(r.xMax, r.yMax - c),
        new Vector2(r.xMax - c, r.yMax),
        new Vector2(r.xMin + c, r.yMax),
        new Vector2(r.xMin, r.yMax - c),
        new Vector2(r.xMin, r.yMin + c)
    };
}
