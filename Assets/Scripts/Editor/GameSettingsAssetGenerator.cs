#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>ジャンル別の既定Settings Assetを一括生成します。</summary>
public static class GameSettingsAssetGenerator
{
    [MenuItem("Tools/3D Grid Bomber/Create Default Settings Assets")]
    public static void CreateDefaultSettingsAssets()
    {
        GridSettings grid =
            CreateAssetIfMissing<GridSettings>("Assets/Settings/Grid/GridSettings.asset");
        CharacterMovementSettings characterMovement = CreateAssetIfMissing<CharacterMovementSettings>(
            "Assets/Settings/Character/CharacterMovementSettings.asset");
        CharacterPrefabSettings characterPrefabs = CreateAssetIfMissing<CharacterPrefabSettings>(
            "Assets/Settings/Character/CharacterPrefabSettings.asset");
        BombSettings bomb =
            CreateAssetIfMissing<BombSettings>("Assets/Settings/Bomb/BombSettings.asset");
        BlockSettings breakableBlockSettings = CreateAssetIfMissing<BlockSettings>(
            "Assets/Settings/Block/BreakableBlockSettings.asset");
        BlockSettings unbreakableBlockSettings = CreateAssetIfMissing<BlockSettings>(
            "Assets/Settings/Block/UnbreakableBlockSettings.asset");
        SetBlockType(breakableBlockSettings, BlockType.Breakable);
        SetBlockType(unbreakableBlockSettings, BlockType.Unbreakable);
        StageSettings stage =
            CreateAssetIfMissing<StageSettings>("Assets/Settings/Stage/StageSettings.asset");
        EnemyAISettings enemyAI =
            CreateAssetIfMissing<EnemyAISettings>("Assets/Settings/Enemy/EnemyAISettings.asset");
        ExplosionVisualSettings explosionVisual = CreateAssetIfMissing<ExplosionVisualSettings>(
            "Assets/Settings/Effects/ExplosionVisualSettings.asset");
        GameHudSettings gameHud =
            CreateAssetIfMissing<GameHudSettings>("Assets/Settings/UI/GameHudSettings.asset");
        GameConfig catalog =
            CreateAssetIfMissing<GameConfig>("Assets/Settings/GameConfig.asset");

        PopulateCatalog(
            catalog,
            grid,
            characterMovement,
            characterPrefabs,
            bomb,
            stage,
            enemyAI,
            explosionVisual,
            gameHud);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("ジャンル別のSettings AssetをAssets/Settingsへ生成しました。");
    }

    /// <summary>生成したジャンル別AssetをGameConfigカタログへ登録します。</summary>
    private static void PopulateCatalog(
        GameConfig catalog,
        GridSettings grid,
        CharacterMovementSettings characterMovement,
        CharacterPrefabSettings characterPrefabs,
        BombSettings bomb,
        StageSettings stage,
        EnemyAISettings enemyAI,
        ExplosionVisualSettings explosionVisual,
        GameHudSettings gameHud)
    {
        SerializedObject serializedCatalog = new SerializedObject(catalog);
        serializedCatalog.FindProperty("_grid").objectReferenceValue = grid;
        serializedCatalog.FindProperty("_characterMovement").objectReferenceValue = characterMovement;
        serializedCatalog.FindProperty("_characterPrefabs").objectReferenceValue = characterPrefabs;
        serializedCatalog.FindProperty("_bomb").objectReferenceValue = bomb;
        serializedCatalog.FindProperty("_stage").objectReferenceValue = stage;
        serializedCatalog.FindProperty("_enemyAI").objectReferenceValue = enemyAI;
        serializedCatalog.FindProperty("_explosionVisual").objectReferenceValue = explosionVisual;
        serializedCatalog.FindProperty("_gameHud").objectReferenceValue = gameHud;
        serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
    }

    /// <summary>生成したBlock SettingsへPrefab用途に対応する種類を設定します。</summary>
    private static void SetBlockType(BlockSettings settings, BlockType type)
    {
        SerializedObject serializedSettings = new SerializedObject(settings);
        serializedSettings.FindProperty("_type").enumValueIndex = (int)type;
        serializedSettings.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(settings);
    }

    /// <summary>対象Assetがまだ存在しない場合だけ、既定値で新規作成します。</summary>
    private static T CreateAssetIfMissing<T>(string path) where T : ScriptableObject
    {
        T existing = AssetDatabase.LoadAssetAtPath<T>(path);

        if (existing != null)
            return existing;

        EnsureFolder(System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/'));
        T asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    /// <summary>Assets以下のフォルダを階層順に作成します。</summary>
    private static void EnsureFolder(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath))
            return;

        string[] parts = folderPath.Split('/');
        string currentPath = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string nextPath = currentPath + "/" + parts[i];

            if (!AssetDatabase.IsValidFolder(nextPath))
                AssetDatabase.CreateFolder(currentPath, parts[i]);

            currentPath = nextPath;
        }
    }
}
#endif
