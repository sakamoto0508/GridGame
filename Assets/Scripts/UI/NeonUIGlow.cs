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
    private Material _flowMaterial;
    private Material _originalMaterial;
    private Image _materialImage;
    private static readonly int FlowRectId = Shader.PropertyToID("_FlowRect");
    private static readonly int FlowSpeedId = Shader.PropertyToID("_FlowSpeed");
    private static readonly int FlowStrengthId = Shader.PropertyToID("_FlowStrength");
    private static readonly int FlowWidthId = Shader.PropertyToID("_FlowWidth");

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
        ReleaseFlowMaterial();
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
        RefreshFlowMaterial(rect, selectable == null || selectable.IsInteractable());
    }

    /// <summary>設定/選択/サイズが変わった時だけ値を転送。光の移動はGPUの時間で行います。</summary>
    private void RefreshFlowMaterial(RectTransform rect, bool interactable)
    {
        if (!_theme.EnableFlowGlow || !_glowImage.enabled)
        {
            ReleaseFlowMaterial();
            return;
        }
        if (_materialImage != null && _materialImage != _glowImage) ReleaseFlowMaterial();
        if (_flowMaterial == null)
        {
            Shader shader = Resources.Load<Shader>("NeonUIFlow");
            if (shader == null) return; // 未インポート時は元の静的Glowを維持。
            _flowMaterial = new Material(shader) { name = "Neon UI Flow (Instance)", hideFlags = HideFlags.HideAndDontSave };
            _materialImage = _glowImage;
            _originalMaterial = _glowImage.material;
            _glowImage.material = _flowMaterial;
        }
        Rect bounds = rect.rect;
        Vector4 flowRect = new Vector4(bounds.xMin, bounds.yMin, bounds.width, bounds.height);
        float strength = interactable ? _theme.GlowFlowStrength : 0;
        SetFlowProperties(_flowMaterial, flowRect, strength);
        // Maskは複製Materialを使うため、実際の描画用Materialにも変更を反映します。
        Material renderingMaterial = _glowImage.materialForRendering;
        if (renderingMaterial != null && renderingMaterial != _flowMaterial)
            SetFlowProperties(renderingMaterial, flowRect, strength);
    }

    private void SetFlowProperties(Material material, Vector4 rect, float strength)
    {
        material.SetVector(FlowRectId, rect);
        material.SetFloat(FlowSpeedId, _theme.GlowFlowSpeed);
        material.SetFloat(FlowStrengthId, strength);
        material.SetFloat(FlowWidthId, _theme.GlowFlowWidth);
    }

    private void ReleaseFlowMaterial()
    {
        if (_materialImage != null && _materialImage.material == _flowMaterial)
            _materialImage.material = _originalMaterial;
        if (_flowMaterial != null)
        {
            if (Application.isPlaying) Destroy(_flowMaterial);
            else DestroyImmediate(_flowMaterial);
        }
        _flowMaterial = null;
        _materialImage = null;
        _originalMaterial = null;
    }
    private void OnDestroy() => ReleaseFlowMaterial();

    // 画面サイズ/CanvasScaler変更時にも再計算。Updateでの監視はしません。
    private void OnRectTransformDimensionsChange() => Refresh();
}
