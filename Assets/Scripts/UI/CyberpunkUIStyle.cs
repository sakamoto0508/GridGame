using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>事前配置UIの見た目だけを設定します。Object生成・ゲーム状態の判定はしません。</summary>
[ExecuteAlways, DisallowMultipleComponent]
public class CyberpunkUIStyle : MonoBehaviour
{
    [SerializeField] private CyberpunkUITheme _theme;
    [SerializeField] private CyberpunkUIColor _role;
    [SerializeField] private bool _filled;
    [SerializeField] private bool _heading;
    [SerializeField] private bool _outlineOnly;
    [SerializeField] private Sprite _spriteOverride;
    public CyberpunkUITheme Theme => _theme;
    public void SetSpriteOverride(Sprite sprite) { _spriteOverride = sprite; Apply(); }
    private void OnEnable() => Apply();
    private void OnValidate() => Apply();

    public void Configure(CyberpunkUITheme theme, CyberpunkUIColor role, bool filled = false, bool heading = false, bool outlineOnly = false)
    {
        _theme = theme;
        _role = role;
        _filled = filled;
        _heading = heading;
        _outlineOnly = outlineOnly;
        Apply();
    }

    [ContextMenu("Apply Theme")]
    public void Apply()
    {
        if (_theme == null) return;
        Color accent = _theme.GetColor(_role);
        TMP_Text text = GetComponent<TMP_Text>();
        if (text != null)
        {
            text.color = accent;
            TMP_FontAsset font = _heading && _theme.HeadingFont != null ? _theme.HeadingFont : _theme.BodyFont;
            if (font != null) text.font = font;
        }
        CyberpunkPanelGraphic panel = GetComponent<CyberpunkPanelGraphic>();
        if (panel != null) panel.SetStyle(_outlineOnly ? Color.clear : _filled ? accent : _theme.Panel, accent, _theme.BorderWidth, _theme.CornerCut);
        Selectable selectable = GetComponent<Selectable>();
        // 9スライスImageは作成済みPNGを使用。旧Sceneの独自Graphicは移行まで維持します。
        Image decoration = GetComponent<Image>();
        if (decoration != null && decoration.type == Image.Type.Sliced)
        {
            decoration.sprite = _spriteOverride != null ? _spriteOverride :
                _outlineOnly ? _theme.FrameSprite :
                selectable is Button ? (_filled ? _theme.ButtonPrimarySprite :
                    _role == CyberpunkUIColor.Success ? _theme.ButtonSelectedSprite : _theme.ButtonNeutralSprite) :
                _role == CyberpunkUIColor.Success ? _theme.PanelGreenSprite :
                _role == CyberpunkUIColor.Warning ? _theme.WarningSprite : _theme.PanelCyanSprite;
            decoration.color = Color.white;
        }
        else if (decoration != null && selectable == null) decoration.color = accent;
        Graphic target = decoration != null ? decoration : panel;
        GetComponent<NeonUIGlow>()?.SetStyle(_theme, accent,
            selectable is Button && (_filled || _role == CyberpunkUIColor.Success));
        if (selectable == null || target == null) return;
        selectable.targetGraphic = target;
        selectable.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = selectable.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.Lerp(Color.white, accent, _theme.HighlightTint);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1);
        colors.disabledColor = new Color(1, 1, 1, _theme.DisabledAlpha);
        colors.colorMultiplier = 1;
        colors.fadeDuration = _theme.ButtonFadeDuration;
        selectable.colors = colors;
        // ボタン文字を親の色でまとめます。Dropdown内部の選択リストには触れません。
        if (selectable is Button)
            foreach (TMP_Text label in GetComponentsInChildren<TMP_Text>(true))
            {
                label.color = _filled ? new Color(_theme.Panel.r, _theme.Panel.g, _theme.Panel.b, 1) : accent;
                if (_theme.BodyFont != null) label.font = _theme.BodyFont;
            }
    }
}
