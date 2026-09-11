#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>同梱PNGを9スライスSpriteとしてImportし、テーマへ接続します。画像自体は上書きしません。</summary>
public static class NeonUISpriteAssets
{
    public const string Folder = "Assets/Scripts/UI/Art/Neon/";
    public static void Assign(CyberpunkUITheme theme)
    {
        Undo.RecordObject(theme, "Assign UI sprites");
        // 手動で差し替えたSpriteは維持します。
        if (theme.PanelCyanSprite == null) theme.PanelCyanSprite = Load("PanelCyan");
        if (theme.PanelGreenSprite == null) theme.PanelGreenSprite = Load("PanelGreen");
        if (theme.ButtonNeutralSprite == null) theme.ButtonNeutralSprite = Load("ButtonNeutral");
        if (theme.ButtonSelectedSprite == null) theme.ButtonSelectedSprite = Load("ButtonSelected");
        if (theme.ButtonPrimarySprite == null) theme.ButtonPrimarySprite = Load("ButtonPrimary");
        if (theme.FrameSprite == null) theme.FrameSprite = Load("FrameCyan");
        if (theme.KeycapSprite == null) theme.KeycapSprite = Load("KeycapCyan");
        if (theme.WarningSprite == null) theme.WarningSprite = Load("PanelWarning");
        if (theme.GlowSprite == null) theme.GlowSprite = Load("GlowRing", 40);
        EditorUtility.SetDirty(theme);
    }

    private static Sprite Load(string name, float border = 24)
    {
        string path = Folder + name + ".png";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new System.InvalidOperationException("UI Spriteがありません: " + path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100;
        importer.spriteBorder = Vector4.one * border;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
#endif
