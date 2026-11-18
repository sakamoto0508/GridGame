using System;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>文字列の打ち間違いを防ぐSE識別子。追加時はSettingsのSoundsにも項目を追加します。</summary>
public enum SoundId
{
    BombPlace,
    Explosion,
    BlockPlace,
    ItemCollect,
    CharacterDeath,
    UiConfirm,
    Win,
    Lose,
    UiSelect,
    CameraRotate
}

/// <summary>1種類の効果音の再生設定です。</summary>
[Serializable]
public class SoundDefinition
{
    public SoundId Id;

    [Tooltip("複数登録すると、再生のたびにランダム選択します。")]
    public AudioClip[] Clips;

    [Range(0f, 1f)] public float Volume = 1f;
    [Range(0.1f, 3f)] public float Pitch = 1f;

    [Tooltip("この音を同時に鳴らせる最大数です。")]
    [Min(1)] public int MaxVoices = 3;

    [Tooltip("同じ音を受け付ける最短間隔（秒）。連鎖爆発の重なりを抑えます。")]
    [Min(0f)] public float Cooldown = 0.05f;

    [Tooltip("0で2D、1で完全な3D音響になります。")]
    [Range(0f, 1f)] public float SpatialBlend = 0f;

    [Min(0.01f)] public float MinDistance = 3f;
    [Min(0.01f)] public float MaxDistance = 30f;

    public SoundDefinition(SoundId id)
    {
        Id = id;
    }
}

/// <summary>UnityEngine.AudioSettingsと区別するためGameAudioSettingsと命名しています。</summary>
[CreateAssetMenu(menuName = "3D Grid Bomber/Settings/Audio")]
public class GameAudioSettings : ScriptableObject
{
    [Range(0f, 1f)] public float MasterVolume = 1f;
    [Range(0f, 1f)] public float SfxVolume = 1f;
    [Range(0f, 1f)] public float MusicVolume = 0.4f;

    [Tooltip("効果音用に用意するAudioSourceの数（全SE合計の同時再生数）。")]
    [Min(1)] public int VoiceCount = 16;

    [Tooltip("任意。未設定なら通常のAudioListenerへ直接出力します。")]
    public AudioMixerGroup SfxOutput;

    [Tooltip("任意。未設定なら通常のAudioListenerへ直接出力します。")]
    public AudioMixerGroup MusicOutput;

    [Tooltip("Play Music On Start がONのとき、開始時に再生するBGM。")]
    public AudioClip DefaultMusic;

    public bool PlayMusicOnStart;

    [Tooltip("SoundIdごとの効果音。SoundIdを追加したらここにも項目を追加してください。")]
    public SoundDefinition[] Sounds =
    {
        new(SoundId.BombPlace),
        new(SoundId.Explosion),
        new(SoundId.BlockPlace),
        new(SoundId.ItemCollect),
        new(SoundId.CharacterDeath),
        new(SoundId.UiConfirm),
        new(SoundId.Win),
        new(SoundId.Lose),
        new(SoundId.UiSelect),
        new(SoundId.CameraRotate)
    };
}
