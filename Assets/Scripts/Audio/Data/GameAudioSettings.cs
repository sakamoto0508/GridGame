using System;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>文字列の打ち間違いを防ぐSE識別子。追加時はSettingsのSoundsにも項目を追加します。</summary>
public enum SoundId { BombPlace, Explosion, BlockPlace, ItemCollect, CharacterDeath, UiConfirm, Win, Lose, UiSelect }

[Serializable]
public class SoundDefinition
{
    public SoundId Id;
    [Tooltip("複数登録すると、再生のたびにランダム選択します。")]
    public AudioClip[] Clips;
    [Range(0f, 1f)] public float Volume = 1f;
    [Range(0.1f, 3f)] public float Pitch = 1f;
    [Min(1)] public int MaxVoices = 3;
    [Tooltip("同じ音を受け付ける最短間隔（秒）。連鎖爆発の重なりを抑えます。")]
    [Min(0f)] public float Cooldown = 0.05f;
    [Range(0f, 1f)] public float SpatialBlend = 0f;
    [Min(0.01f)] public float MinDistance = 3f;
    [Min(0.01f)] public float MaxDistance = 30f;
    public SoundDefinition(SoundId id) { Id = id; }
}

/// <summary>UnityEngine.AudioSettingsと区別するためGameAudioSettingsと命名しています。</summary>
[CreateAssetMenu(menuName = "3D Grid Bomber/Settings/Audio")]
public class GameAudioSettings : ScriptableObject
{
    /// <summary>
    /// マスターボリューム（全体の音量）。
    /// </summary>
    [Range(0f, 1f)] public float MasterVolume = 1f;
    /// <summary>
    /// 効果音の音量。
    /// </summary>
    [Range(0f, 1f)] public float SfxVolume = 1f;
    /// <summary>
    /// 音楽の音量。
    /// </summary>
    [Range(0f, 1f)] public float MusicVolume = 0.4f;
    /// <summary>
    /// 同時に再生可能な音声の最大数。
    /// </summary>
    [Min(1)] public int VoiceCount = 16;
    [Tooltip("任意。未設定なら通常のAudioListenerへ直接出力します。")]
    /// <summary>
    /// 効果音の出力先。
    /// </summary>
    public AudioMixerGroup SfxOutput;
    /// <summary>
    /// 音楽の出力先。
    /// </summary>
    public AudioMixerGroup MusicOutput;
    /// <summary>
    /// ゲーム開始時に再生する音楽。未設定なら再生しません。
    /// </summary>
    public AudioClip DefaultMusic;
    /// <summary>
    /// ゲーム開始時に音楽を再生するかどうか。DefaultMusicが設定されていない場合は無視されます。
    /// </summary>
    public bool PlayMusicOnStart;
    /// <summary>
    /// ゲーム内で使用する効果音の定義。SoundIdとClipsを対応付けます。
    /// </summary>
    public SoundDefinition[] Sounds =
    {
        new(SoundId.BombPlace), new(SoundId.Explosion), new(SoundId.BlockPlace),
        new(SoundId.ItemCollect), new(SoundId.CharacterDeath), new(SoundId.UiConfirm),
        new(SoundId.Win), new(SoundId.Lose), new(SoundId.UiSelect)
    };
}
