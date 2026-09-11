#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>背景MeshをAssetへ、対戦BlockをPrefabインスタンスとしてSceneへ保存します。</summary>
public static class SceneMapBaker
{
    [MenuItem("Tools/NEON DETONATOR/Stage/Bake Background Into Scene")]
    public static void BakeBackground()
    {
        StageGenerator stage = FindStage();
        if (stage == null) return;
        RooftopBackgroundController owner = stage.GetComponent<RooftopBackgroundController>();
        if (owner != null && owner.BakedRoot != null && !EditorUtility.DisplayDialog("背景を再生成",
            "保存済み背景を置き換えます。背景への手動編集は失われます。旧Mesh Assetは残します。", "再生成", "キャンセル")) return;
        GameObject temporary = new GameObject("Temporary Background Baker");
        SceneManager.MoveGameObjectToScene(temporary, stage.gameObject.scene);
        RooftopBackgroundController generator = temporary.AddComponent<RooftopBackgroundController>();
        GameObject result = null;
        try
        {
            generator.Init(stage.Grid, stage.BackgroundSettings);
            result = generator.DetachForSceneBake();
            if (result == null) throw new InvalidOperationException("背景が生成されませんでした。Gridと背景のEnabled設定を確認してください。");
            EnsureFolder("Assets/Scripts/Generated/CityBackgrounds");
            // Sceneが再読込されてもMeshが消えないよう、必ずAssetとして永続化します。
            Material material = result.GetComponentInChildren<MeshRenderer>().sharedMaterial;
            string path = AssetDatabase.GenerateUniqueAssetPath("Assets/Scripts/Generated/CityBackgrounds/RooftopBackground.asset");
            material.name = "Rooftop Background Material";
            AssetDatabase.CreateAsset(material, path);
            foreach (MeshFilter filter in result.GetComponentsInChildren<MeshFilter>())
                AssetDatabase.AddObjectToAsset(filter.sharedMesh, material);
            AssetDatabase.SaveAssets();
            MeshRenderer[] sides = new MeshRenderer[4];
            for (int i = 0; i < 4; i++) sides[i] = result.transform.Find("City Side " + i).GetComponent<MeshRenderer>();
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Bake Background Into Scene");
            if (owner == null) owner = Undo.AddComponent<RooftopBackgroundController>(stage.gameObject);
            if (owner.BakedRoot != null) Undo.DestroyObjectImmediate(owner.BakedRoot);
            result.name = "Scene Rooftop Background";
            Undo.RegisterCreatedObjectUndo(result, "Create Scene Background");
            Undo.RecordObject(owner, "Assign Scene Background");
            owner.SetSceneBackground(result, sides);
            EditorUtility.SetDirty(owner);
            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(stage.gameObject.scene);
            Selection.activeGameObject = result;
            Debug.Log("背景をSceneへ配置しました。Sceneを保存してください。再生時に二重生成しません。", result);
        }
        catch (Exception error)
        {
            if (result != null) UnityEngine.Object.DestroyImmediate(result);
            Debug.LogException(error);
        }
        finally { UnityEngine.Object.DestroyImmediate(temporary); }
    }

    [MenuItem("Tools/NEON DETONATOR/Stage/Bake Blocks Into Scene")]
    public static void BakeBlocks()
    {
        StageGenerator stage = FindStage();
        if (stage == null) return;
        Transform previous = stage.SceneBlocksRoot;
        if (previous != null && !EditorUtility.DisplayDialog("Blockを再生成",
            "Scene Stage Blocks内の手動編集を破棄し、StageSettingsから作り直します。Undoで戻せます。", "再生成", "キャンセル")) return;
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Bake Blocks Into Scene");
        try
        {
            Transform root = stage.BakeBlocksForScene();
            Undo.RegisterCreatedObjectUndo(root.gameObject, "Create Scene Blocks");
            if (previous != null) Undo.DestroyObjectImmediate(previous.gameObject);
            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(stage.gameObject.scene);
            Selection.activeGameObject = root.gameObject;
            Debug.Log("外殻と対戦BlockをSceneへ配置しました。子のBlockを移動・複製・削除して編集し、Sceneを保存してください。", root);
        }
        catch (Exception error) { Debug.LogException(error); }
    }

    private static StageGenerator FindStage()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Playを停止してからベイクしてください。");
            return null;
        }
        StageGenerator selected = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponentInParent<StageGenerator>() : null;
        if (selected != null) return selected;
        StageGenerator[] stages = UnityEngine.Object.FindObjectsByType<StageGenerator>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (stages.Length == 1) return stages[0];
        Debug.LogError("対象のStageGeneratorをHierarchyで選択してください。");
        return null;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        string parent = path.Substring(0, slash);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
    }
}
#endif
