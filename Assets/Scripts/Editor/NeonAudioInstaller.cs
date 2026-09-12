#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>合成済みSEを既存音声設定の空欄へ登録します。BGMや既存Clipは変更しません。</summary>
public static class NeonAudioInstaller
{
    [MenuItem("Tools/NEON DETONATOR/Audio/Assign Generated SE")]
    public static void Assign()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Playを停止してください。"); return; }
        GameAudioSettings settings = Selection.activeObject as GameAudioSettings;
        if (settings == null) settings = AssetDatabase.LoadAssetAtPath<GameAudioSettings>("Assets/Settings/Audio/GameAudioSettings.asset");
        if (settings == null) { Debug.LogError("GameAudioSettingsをProjectで選択して実行してください。"); return; }
        var clips = new Dictionary<SoundId, AudioClip>();
        foreach (SoundId id in Enum.GetValues(typeof(SoundId)))
        {
            string path = "Assets/Scripts/Audio/Clips/Neon/" + id + ".wav";
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) { Debug.LogError("音源がありません: " + path); return; }
            clips.Add(id, clip);
        }
        Undo.RecordObject(settings, "Assign Neon SE");
        var sounds = new List<SoundDefinition>(settings.Sounds ?? Array.Empty<SoundDefinition>());
        int changed = 0;
        foreach (var pair in clips)
        {
            SoundDefinition sound = sounds.Find(s => s != null && s.Id == pair.Key);
            if (sound == null) { sound = new SoundDefinition(pair.Key); sounds.Add(sound); }
            // 使用中の音源が1つでもあれば、その設定は上書きしません。
            if (sound.Clips != null && Array.Exists(sound.Clips, c => c != null)) continue;
            sound.Clips = new[] { pair.Value };
            sound.Volume = pair.Key == SoundId.Explosion ? 0.5f : pair.Key == SoundId.UiSelect ? 0.25f : 0.65f;
            if (pair.Key == SoundId.CameraRotate) sound.Volume = 0.35f;
            sound.SpatialBlend = 0;
            sound.MaxVoices = pair.Key == SoundId.Explosion ? 3 : 2;
            sound.Cooldown = pair.Key == SoundId.Explosion ? 0.07f : 0.04f;
            changed++;
        }
        settings.Sounds = sounds.ToArray();
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        Selection.activeObject = settings;
        Debug.Log($"SEを{changed}件登録しました。AudioManagerのSettingsがこのAssetを参照していることを確認してください。", settings);
    }
}
#endif
