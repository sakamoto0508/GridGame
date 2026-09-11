#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

/// <summary>既存Blockマテリアルの参照を維持したまま、不透明Flowへ切り替えます。</summary>
public static class BlockFlowMaterialInstaller
{
    private const string ShaderPath = "Assets/Scripts/Rendering/Shaders/BlockLitFlowURP.shader";
    private const string BackupFolder = "Assets/Scripts/Rendering/MaterialBackups";

    [MenuItem("Tools/NEON DETONATOR/Assets/Apply Block Lit Flow")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Playを停止してからBlockマテリアルを変更してください。");
            return;
        }
        AssetDatabase.ImportAsset(ShaderPath, ImportAssetOptions.ForceSynchronousImport);
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
        if (shader == null || ShaderUtil.ShaderHasError(shader))
        {
            Debug.LogError("Block Lit FlowのShaderコンパイルエラーを先に解消してください。");
            return;
        }
        string[] paths = { "Assets/Material/BreakBlockMaterial.mat", "Assets/Material/UnbreakBlockMaterial.mat" };
        Material[] materials = new Material[paths.Length];
        for (int i = 0; i < paths.Length; i++)
        {
            materials[i] = AssetDatabase.LoadAssetAtPath<Material>(paths[i]);
            if (materials[i] == null || !AssetDatabase.IsOpenForEdit(materials[i]))
            {
                Debug.LogError("対象マテリアルがないか書き込み不可です: " + paths[i]);
                return;
            }
        }
        if (!EditorUtility.DisplayDialog("Block Lit Flowを適用",
            "破壊可能・破壊不可の2マテリアルを、不透明本体＋流れる発光ラインへ変更します。初回は元マテリアルをバックアップします。\n緑=破壊可能、水色=破壊不可。変更済みのマテリアルは再設定しません。", "適用", "キャンセル")) return;

        if (!AssetDatabase.IsValidFolder(BackupFolder)) AssetDatabase.CreateFolder("Assets/Scripts/Rendering", "MaterialBackups");
        // 変更前に全対象のバックアップを確保。元Prefabの参照先は変更しません。
        for (int i = 0; i < paths.Length; i++)
        {
            if (materials[i].shader == shader) continue;
            string backup = BackupFolder + "/" + materials[i].name + "_BeforeBlockFlow.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(backup) == null && !AssetDatabase.CopyAsset(paths[i], backup))
            {
                Debug.LogError("バックアップを作成できなかったため適用を中止しました: " + backup);
                return;
            }
        }
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply Block Lit Flow");
        for (int i = 0; i < materials.Length; i++)
        {
            Material material = materials[i];
            if (material.shader == shader) continue;
            Undo.RecordObject(material, "Apply Block Lit Flow");
            material.shader = shader;
            // 元の半透明マテリアルに保存されていたQueue/無効Passも戻します。
            material.shaderKeywords = Array.Empty<string>();
            material.renderQueue = -1;
            material.SetOverrideTag("RenderType", "Opaque");
            foreach (string pass in new[] { "ShadowCaster", "SHADOWCASTER", "DepthOnly", "DepthNormals", "BlockForward" })
                material.SetShaderPassEnabled(pass, true);
            material.enableInstancing = true;
            material.SetColor("_BaseColor", i == 0 ? new Color(0.075f, 0.1f, 0.085f, 1) : new Color(0.05f, 0.075f, 0.1f, 1));
            material.SetColor("_EmissionColor", i == 0 ? new Color(0.2f, 1, 0.4f, 1) : new Color(0.1f, 0.75f, 1, 1));
            material.SetFloat("_EmissionColorIntensity", 2);
            material.SetFloat("_Metallic", 0.2f);
            material.SetFloat("_Smoothness", 0.35f);
            material.SetFloat("_UseProceduralPattern", 1);
            material.SetFloat("_LineWidth", 0.025f);
            material.SetFloat("_PulseSpeed", i == 0 ? 0.6f : 0.3f);
            material.SetFloat("_UseAlphaGradient", 0);
            material.SetFloat("_UseGradientColor", 0);
            material.SetVector("_TillingXY", new Vector4(1, 1, 0, 0));
            EditorUtility.SetDirty(material);
            Debug.Log("Block Lit Flow適用: " + paths[i], material);
        }
        Undo.CollapseUndoOperations(group);
        AssetDatabase.SaveAssets();
        Selection.objects = materials;
        Debug.Log("Block Lit Flowの適用が完了しました。既存Prefabやプール生成Blockも同じマテリアルを使用します。");
    }
}
#endif
