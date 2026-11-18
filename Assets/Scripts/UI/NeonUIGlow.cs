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
    private static readonly int FlowRectId = Shader.PropertyToID("_FlowRect");
    private static readonly int FlowSpeedId = Shader.PropertyToID("_FlowSpeed");
    private static readonly int FlowStrengthId = Shader.PropertyToID("_FlowStrength");
    private static readonly int FlowWidthId = Shader.PropertyToID("_FlowWidth");

    private const string FlowShaderName = "NeonUIFlow";

    /// <summary>0除算を防ぐための最小値です。</summary>
    private const float MinDivisor = 0.001f;

    /// <summary>Spriteの差を左右（上下）に半分ずつ配分するための係数です。</summary>
    private const float HalfRatio = 0.5f;

    private static readonly Vector2 CenterPivot = new Vector2(0.5f, 0.5f);

    [SerializeField] private Image _glowImage;

    private CyberpunkUITheme _theme;
    private Color _accent;
    private bool _emphasized;
    private bool _hovered;
    private bool _focused;

    private Material _flowMaterial;
    private Material _originalMaterial;
    private Image _materialImage;

    /// <summary>Editorで用意したImageを接続します。実行時にはUIを生成しません。</summary>
    public void SetImage(Image glowImage)
    {
        _glowImage = glowImage;
        Refresh();
    }

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
        if (TryGetComponent(out CyberpunkUIStyle style))
            style.Apply();
        Refresh();
    }

    private void OnDisable()
    {
        _hovered = false;
        _focused = false;

        if (_glowImage != null)
            _glowImage.enabled = false;

        ReleaseFlowMaterial();
    }

    private void OnCanvasGroupChanged() => Refresh();

    public void OnPointerEnter(PointerEventData data)
    {
        _hovered = true;
        Refresh();
    }

    public void OnPointerExit(PointerEventData data)
    {
        _hovered = false;
        Refresh();
    }

    public void OnSelect(BaseEventData data)
    {
        _focused = true;
        Refresh();
    }

    public void OnDeselect(BaseEventData data)
    {
        _focused = false;
        Refresh();
    }

    public void Refresh()
    {
        if (_glowImage == null || _theme == null)
            return;

        Graphic surface = GetComponent<Graphic>();

        _glowImage.enabled =
            isActiveAndEnabled &&
            _theme.EnableGlow &&
            _theme.GlowSprite != null &&
            surface != null &&
            surface.enabled;
        _glowImage.raycastTarget = false;
        _glowImage.sprite = _theme.GlowSprite;
        _glowImage.type = Image.Type.Sliced;

        // 発光の濃さ: 通常/強調 → ホバー・フォーカス時は強める → 操作不可なら弱める
        float alpha = _emphasized ? _theme.EmphasizedGlowOpacity : _theme.GlowOpacity;
        if (_hovered || _focused)
            alpha = Mathf.Max(alpha, _theme.HoverGlowOpacity);

        Selectable selectable = GetComponent<Selectable>();
        if (selectable != null && !selectable.IsInteractable())
            alpha = _theme.DisabledGlowOpacity;

        Color color = _accent;
        color.a = Mathf.Clamp01(alpha);
        _glowImage.color = color;

        RectTransform rect = _glowImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;

        // Undo.SetTransformParentはワールドScaleを保つため、子のScaleを必ず正規化します。
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.pivot = CenterPivot;

        Vector3 localPosition = rect.localPosition;
        localPosition.z = 0;
        rect.localPosition = localPosition;

        Vector2 padding = CalculateGlowPadding(surface as Image);
        rect.offsetMin = -padding;
        rect.offsetMax = padding;

        RefreshFlowMaterial(rect, selectable == null || selectable.IsInteractable());
    }

    /// <summary>
    /// 発光Spriteが元のSpriteより大きい分だけ、外側へはみ出させる量を求めます。
    /// </summary>
    private Vector2 CalculateGlowPadding(Image source)
    {
        Vector2 padding = Vector2.zero;
        if (source == null || source.sprite == null || _theme.GlowSprite == null)
            return padding;

        float ppu = Mathf.Max(MinDivisor, source.pixelsPerUnit * source.pixelsPerUnitMultiplier);
        _glowImage.pixelsPerUnitMultiplier = ppu / Mathf.Max(MinDivisor, _glowImage.pixelsPerUnit);

        // 同梱画像では(288-256)/2 = 16px。CanvasのPPUと9スライス縮小も考慮します。
        padding = (_theme.GlowSprite.rect.size - source.sprite.rect.size) * (HalfRatio / ppu);

        // 9スライスの枠より小さく表示されている場合は、同じ比率でpaddingも縮めます。
        Vector4 borders = source.sprite.border / ppu;
        Rect sourceRect = source.rectTransform.rect;
        padding.x *= Mathf.Min(1, sourceRect.width / Mathf.Max(MinDivisor, borders.x + borders.z));
        padding.y *= Mathf.Min(1, sourceRect.height / Mathf.Max(MinDivisor, borders.y + borders.w));

        return padding;
    }

    /// <summary>設定/選択/サイズが変わった時だけ値を転送。光の移動はGPUの時間で行います。</summary>
    private void RefreshFlowMaterial(RectTransform rect, bool interactable)
    {
        if (!_theme.EnableFlowGlow || !_glowImage.enabled)
        {
            ReleaseFlowMaterial();
            return;
        }

        if (_materialImage != null && _materialImage != _glowImage)
            ReleaseFlowMaterial();

        if (_flowMaterial == null)
        {
            Shader shader = Resources.Load<Shader>(FlowShaderName);

            // 未インポート時は元の静的Glowを維持。
            if (shader == null)
                return;

            _flowMaterial = new Material(shader)
            {
                name = "Neon UI Flow (Instance)",
                hideFlags = HideFlags.HideAndDontSave
            };
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
            if (Application.isPlaying)
                Destroy(_flowMaterial);
            else
                DestroyImmediate(_flowMaterial);
        }

        _flowMaterial = null;
        _materialImage = null;
        _originalMaterial = null;
    }

    private void OnDestroy() => ReleaseFlowMaterial();

    // 画面サイズ/CanvasScaler変更時にも再計算。Updateでの監視はしません。
    private void OnRectTransformDimensionsChange() => Refresh();
}
