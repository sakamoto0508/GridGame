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
        if (rect.width <= 0 || rect.height <= 0) return;
        float cut = Mathf.Clamp(_cornerCut, 0, Mathf.Min(rect.width, rect.height) * 0.5f);
        float width = Mathf.Clamp(_borderWidth, 0, Mathf.Min(rect.width, rect.height) * 0.49f);
        Vector2[] outer = Corners(rect, cut);
        Rect inset = new Rect(rect.x + width, rect.y + width, rect.width - width * 2, rect.height - width * 2);
        // 45度の斜辺もほぼ同じ幅になるように内側のカット量を調整します。
        Vector2[] inner = Corners(inset, Mathf.Clamp(cut - width * (2 - Mathf.Sqrt(2)), 0,
            Mathf.Min(inset.width, inset.height) * 0.5f));
        for (int i = 0; i < 8; i++) vh.AddVert(outer[i], _border, Vector2.zero);
        for (int i = 0; i < 8; i++) vh.AddVert(inner[i], _border, Vector2.zero);
        for (int i = 0; i < 8; i++)
        {
            int next = (i + 1) % 8;
            vh.AddTriangle(i, next, 8 + next);
            vh.AddTriangle(i, 8 + next, 8 + i);
        }
        // 塗りの頂点は枠と分離し、色補間で枠がにじむのを防ぎます。
        vh.AddVert(inset.center, color, Vector2.zero);
        for (int i = 0; i < 8; i++) vh.AddVert(inner[i], color, Vector2.zero);
        for (int i = 0; i < 8; i++) vh.AddTriangle(16, 17 + i, 17 + (i + 1) % 8);
    }

    private static Vector2[] Corners(Rect r, float c) => new[]
    {
        new Vector2(r.xMin + c, r.yMin), new Vector2(r.xMax - c, r.yMin),
        new Vector2(r.xMax, r.yMin + c), new Vector2(r.xMax, r.yMax - c),
        new Vector2(r.xMax - c, r.yMax), new Vector2(r.xMin + c, r.yMax),
        new Vector2(r.xMin, r.yMax - c), new Vector2(r.xMin, r.yMin + c)
    };
}
