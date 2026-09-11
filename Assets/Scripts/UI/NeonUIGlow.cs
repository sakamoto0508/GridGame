using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 事前配置したImageに発光用Spriteを表示。Bloom/追加Camera/毎フレーム更新は不要です。
/// ポインターとキーボードフォーカスを別に保持し、片方が外れてももう片方の強調を維持します。
/// </summary>
[ExecuteAlways, DisallowMultipleComponent]
public class NeonUIGlow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private Image _glowImage;
    private CyberpunkUITheme _theme;
    private Color _accent;
    private bool _emphasized;
    private bool _hovered;
    private bool _focused;

    /// <summary>Editorで用意したImageを接続します。実行時にはUIを生成しません。</summary>
    public void SetImage(Image glowImage) { _glowImage = glowImage; Refresh(); }
    public void SetStyle(CyberpunkUITheme theme, Color accent, bool emphasized)
    {
        _theme = theme;
        _accent = accent;
        _emphasized = emphasized;
        Refresh();
    }
    private void OnEnable()
    {
        _hovered = false;
        _focused = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
        // StyleとのOnEnable順に依存しないように再取得します。
        GetComponent<CyberpunkUIStyle>()?.Apply();
        Refresh();
    }
    private void OnDisable()
    {
        _hovered = _focused = false;
        if (_glowImage != null) _glowImage.enabled = false;
    }
    private void OnCanvasGroupChanged() => Refresh();
    public void OnPointerEnter(PointerEventData data) { _hovered = true; Refresh(); }
    public void OnPointerExit(PointerEventData data) { _hovered = false; Refresh(); }
    public void OnSelect(BaseEventData data) { _focused = true; Refresh(); }
    public void OnDeselect(BaseEventData data) { _focused = false; Refresh(); }

    public void Refresh()
    {
        if (_glowImage == null || _theme == null) return;
        Graphic surface = GetComponent<Graphic>();
        _glowImage.enabled = isActiveAndEnabled && _theme.EnableGlow && _theme.GlowSprite != null &&
            surface != null && surface.enabled;
        _glowImage.raycastTarget = false;
        _glowImage.sprite = _theme.GlowSprite;
        _glowImage.type = Image.Type.Sliced;
        float alpha = _emphasized ? _theme.EmphasizedGlowOpacity : _theme.GlowOpacity;
        if (_hovered || _focused) alpha = Mathf.Max(alpha, _theme.HoverGlowOpacity);
        Selectable selectable = GetComponent<Selectable>();
        if (selectable != null && !selectable.IsInteractable()) alpha = _theme.DisabledGlowOpacity;
        Color color = _accent;
        color.a = Mathf.Clamp01(alpha);
        _glowImage.color = color;
        RectTransform rect = _glowImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        // Undo.SetTransformParentはワールドScaleを保つため、子のScaleを必ず正規化します。
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.pivot = new Vector2(0.5f, 0.5f);
        Vector3 localPosition = rect.localPosition;
        localPosition.z = 0;
        rect.localPosition = localPosition;
        Image source = surface as Image;
        Vector2 padding = Vector2.zero;
        if (source != null && source.sprite != null && _theme.GlowSprite != null)
        {
            float ppu = Mathf.Max(0.001f, source.pixelsPerUnit * source.pixelsPerUnitMultiplier);
            _glowImage.pixelsPerUnitMultiplier = ppu / Mathf.Max(0.001f, _glowImage.pixelsPerUnit);
            // 同梱画像では(288-256)/2 = 16px。CanvasのPPUと9スライス縮小も考慮します。
            padding = (_theme.GlowSprite.rect.size - source.sprite.rect.size) * (0.5f / ppu);
            Vector4 borders = source.sprite.border / ppu;
            Rect sourceRect = source.rectTransform.rect;
            padding.x *= Mathf.Min(1, sourceRect.width / Mathf.Max(0.001f, borders.x + borders.z));
            padding.y *= Mathf.Min(1, sourceRect.height / Mathf.Max(0.001f, borders.y + borders.w));
        }
        rect.offsetMin = -padding;
        rect.offsetMax = padding;
    }

    // 画面サイズ/CanvasScaler変更時にも再計算。Updateでの監視はしません。
    private void OnRectTransformDimensionsChange() => Refresh();
}
