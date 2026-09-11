#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>旧Flowシェーダーを使うマテリアルだけをURP版へ切り替えます。</summary>
public static class FlowShaderURPConverter
{
    private const string OldShaderName = "Xuqi/FlowEffect/Flow_OnlyEmission_Transparent";
    private const string NewShaderPath = "Assets/Scripts/Rendering/Shaders/FlowOnlyEmissionTransparentURP.shader";

    [MenuItem("Tools/NEON DETONATOR/Assets/Convert Flow Materials to URP")]
    public static void Convert()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Playを停止してからFlowシェーダーを変換してください。");
            return;
        }
        AssetDatabase.ImportAsset(NewShaderPath, ImportAssetOptions.ForceSynchronousImport);
        Shader replacement = AssetDatabase.LoadAssetAtPath<Shader>(NewShaderPath);
        if (replacement == null || ShaderUtil.ShaderHasError(replacement))
        {
            Debug.LogError("URP版Flowシェーダーが見つからないか、コンパイルエラーがあります。ShaderのInspectorを確認してください。");
            return;
        }

        List<Material> targets = new List<Material>();
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            // FBX内蔵マテリアルなどは対象外。独立した.matのみを変更します。
            if (!path.EndsWith(".mat", StringComparison.OrdinalIgnoreCase)) continue;
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null || material.shader == null || material.shader.name != OldShaderName) continue;
            if (!AssetDatabase.IsOpenForEdit(material))
            {
                Debug.LogWarning("書き込み不可のためスキップ: " + path, material);
                continue;
            }
            targets.Add(material);
        }
        if (targets.Count == 0)
        {
            Debug.Log("旧Flowシェーダーの対象マテリアルはありません（変換済みの場合も含みます）。");
            return;
        }
        if (!EditorUtility.DisplayDialog("FlowシェーダーをURPへ変換",
            targets.Count + "個のマテリアルを更新します。テクスチャ・色・速度を維持し、元シェーダーは残します。装飾の影は投影しなくなります。\n直後ならUndoで戻せます。", "変換", "キャンセル")) return;

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Convert Flow Materials to URP");
        foreach (Material material in targets)
        {
            // 同名プロパティをコピーして、シェーダー変更時の設定消失を防ぎます。
            Material snapshot = new Material(material);
            try
            {
                Undo.RecordObject(material, "Convert Flow Material to URP");
                material.shader = replacement;
                material.CopyPropertiesFromMaterial(snapshot);
                // 新版は数値でToggleを切り替えるので旧ASEキーワードは不要です。
                material.shaderKeywords = Array.Empty<string>();
                EditorUtility.SetDirty(material);
                Debug.Log("URP Flowへ変換: " + AssetDatabase.GetAssetPath(material), material);
            }
            finally { UnityEngine.Object.DestroyImmediate(snapshot); }
        }
        Undo.CollapseUndoOperations(group);
        AssetDatabase.SaveAssets();
        Debug.Log("Flowマテリアル " + targets.Count + "個のURP変換が完了しました。");
    }
}
#endif
