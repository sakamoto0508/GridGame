using TMPro;
using UnityEngine;

public enum CyberpunkUIColor { Primary, Success, Warning, Text, Muted }

/// <summary>UI専用の配色・フォント・初期配置設定。ゲームルールや3D素材は変更しません。</summary>
[CreateAssetMenu(menuName = "3D Grid Bomber/Settings/Cyberpunk UI Theme")]
public class CyberpunkUITheme : ScriptableObject
{
    public Color Primary = new Color32(112, 223, 255, 255);
    public Color Success = new Color32(110, 245, 154, 255);
    public Color Warning = new Color32(255, 228, 92, 255);
    public Color Text = new Color32(232, 244, 249, 255);
    public Color Muted = new Color32(144, 170, 184, 255);
    public Color Panel = new Color32(11, 17, 24, 242);
    [Tooltip("未設定なら現在のTMP Fontを維持します。日本語を含むUIには日本語対応Fontを指定。")]
    public TMP_FontAsset BodyFont;
    [Tooltip("英字タイトル用。未設定ならBodyFont、それも未設定なら既存Fontを維持。")]
    public TMP_FontAsset HeadingFont;
    [Min(0f)] public float CornerCut = 14f;
    [Min(0f)] public float BorderWidth = 2f;
    [Range(0f, 1f)] public float HighlightTint = 0.2f;
    [Range(0f, 1f)] public float DisabledAlpha = 0.35f;
    [Min(0f)] public float ButtonFadeDuration = 0.1f;
    [Header("Editorでの初期配置（再適用時に使用）")]
    public Vector2 ReferenceResolution = new Vector2(1920, 1080);
    public Vector2 SetupSize = new Vector2(640, 700);
    public Vector2 ResultSize = new Vector2(660, 380);
    public Vector2 StatusSize = new Vector2(380, 230);
    public Vector2 AliveSize = new Vector2(190, 100);
    public float ScreenMargin = 32f;
    public float HeadingSize = 54f;
    public float BodySize = 26f;
    public float SmallSize = 21f;

    [Header("NEON DETONATOR")]
    public string Title = "NEON\nDETONATOR";
    public string JapaneseTitle = "ネオン・デトネーター";
    public string CompactTitle = "NEON DETONATOR";
    public float TitleSize = 76f;
    public float AliveNumberSize = 76f;
    public float HudNumberSize = 32f;
    [Tooltip("任意の盤面背景Sprite。未指定なら暗い背景を使用します。UI入りの見本画像は指定しないでください。")]
    public Sprite SetupBackground;
    public Color SetupBackgroundTint = new Color(0.35f, 0.42f, 0.48f, 1f);
    [Header("9-sliced UI Sprites")]
    public Sprite PanelCyanSprite;
    public Sprite PanelGreenSprite;
    public Sprite ButtonNeutralSprite;
    public Sprite ButtonSelectedSprite;
    public Sprite ButtonPrimarySprite;
    public Sprite FrameSprite;
    public Sprite KeycapSprite;
    public Sprite WarningSprite;
    [Header("UI Glow（Overlay対応）")]
    public bool EnableGlow = true;
    public Sprite GlowSprite;
    [Range(0, 1)] public float GlowOpacity = 0.3f;
    [Range(0, 1)] public float EmphasizedGlowOpacity = 0.85f;
    [Range(0, 1)] public float HoverGlowOpacity = 0.65f;
    [Range(0, 1)] public float DisabledGlowOpacity = 0.08f;
    [Header("UI Flow Glow")]
    public bool EnableFlowGlow = true;
    [Range(-2, 2)] public float GlowFlowSpeed = 0.25f;
    [Range(0, 2)] public float GlowFlowStrength = 0.8f;
    [Range(0.02f, 0.5f)] public float GlowFlowWidth = 0.18f;

    /// <summary>既存SOにも新レイアウトを適用。ユーザー指定の配色/Fontは変更しません。</summary>
    public void UseNeonLayout()
    {
        ReferenceResolution = new Vector2(1920, 1080);
        SetupSize = new Vector2(1040, 860);
        ResultSize = new Vector2(720, 420);
        StatusSize = new Vector2(380, 210);
        AliveSize = new Vector2(190, 160);
        ScreenMargin = 36;
        HeadingSize = 60;
        TitleSize = 76;
        BodySize = 28;
        SmallSize = 22;
    }

    public Color GetColor(CyberpunkUIColor role) => role switch
    {
        CyberpunkUIColor.Success => Success,
        CyberpunkUIColor.Warning => Warning,
        CyberpunkUIColor.Text => Text,
        CyberpunkUIColor.Muted => Muted,
        _ => Primary
    };
}
