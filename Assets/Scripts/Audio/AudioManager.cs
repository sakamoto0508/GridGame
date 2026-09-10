using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sceneに1つ配置する音の窓口。再読込時に破棄するため、前の試合の音は残りません。
/// SEは固定数のSourceを再利用し、発音元の破棄やPool返却から独立させます。
/// </summary>
[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public class AudioManager : MonoBehaviour
{
    [SerializeField] private GameAudioSettings _settings;
    public static AudioManager Instance { get; private set; }
    private sealed class Voice
    {
        public AudioSource Source;
        public SoundId Id;
        public float Volume;
    }
    private Voice[] _voices;
    private AudioSource _music;
    private readonly Dictionary<SoundId, SoundDefinition> _sounds = new();
    private readonly Dictionary<SoundId, double> _lastPlayed = new();
    // 音の選択でUnityの乱数列を進めず、StageやAIの抽選に影響させません。
    private readonly System.Random _clipRandom = new();
    private float _masterVolume, _sfxVolume, _musicVolume;

    // Domain Reload無効でも、前回Play時のstatic参照を持ち越しません。
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic() => Instance = null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("AudioManagerはSceneに1つだけ配置してください。重複Componentを無効化します。", this);
            enabled = false;
            return;
        }
        if (_settings == null)
        {
            Debug.LogWarning("AudioManager: GameAudioSettingsを指定してください。音は再生されません。", this);
            enabled = false;
            return;
        }
        Instance = this;
        _masterVolume = _settings.MasterVolume;
        _sfxVolume = _settings.SfxVolume;
        _musicVolume = _settings.MusicVolume;
        if (_settings.Sounds != null)
            foreach (SoundDefinition sound in _settings.Sounds)
            {
                if (sound == null) continue;
                if (!_sounds.TryAdd(sound.Id, sound))
                    Debug.LogWarning($"AudioManager: {sound.Id}が重複しています。最初の設定を使用します。", this);
            }
        _voices = new Voice[Mathf.Max(1, _settings.VoiceCount)];
        for (int i = 0; i < _voices.Length; i++)
            _voices[i] = new Voice { Source = CreateSource($"SE {i}") };
        _music = CreateSource("BGM");
        _music.outputAudioMixerGroup = _settings.MusicOutput;
        _music.loop = true;
    }

    private void Start()
    {
        if (_settings.PlayMusicOnStart) PlayMusic(_settings.DefaultMusic);
    }

    private AudioSource CreateSource(string objectName)
    {
        GameObject child = new GameObject(objectName);
        child.transform.SetParent(transform, false);
        AudioSource source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.dopplerLevel = 0f;
        return source;
    }

    /// <summary>UIなど位置を持たない音を2D再生。未配置・未設定・上限到達時はfalseで安全に終了。</summary>
    public static bool Play(SoundId id) => Instance != null && Instance.TryPlay(id, null);

    /// <summary>発生位置を固定して再生。2D/3Dの割合や距離減衰は音ごとのSettingsで指定。</summary>
    public static bool PlayAt(SoundId id, Vector3 position) => Instance != null && Instance.TryPlay(id, position);

    private bool TryPlay(SoundId id, Vector3? position)
    {
        if (!isActiveAndEnabled || !_sounds.TryGetValue(id, out SoundDefinition sound)) return false;
        double now = Time.unscaledTimeAsDouble;
        if (_lastPlayed.TryGetValue(id, out double last) && now - last < sound.Cooldown) return false;
        Voice available = null;
        int count = 0;
        foreach (Voice voice in _voices)
        {
            if (!voice.Source.isPlaying) { available ??= voice; }
            else if (voice.Id == id) count++;
        }
        // 再生中の音は途中で切らず、新しい要求を捨てます。
        if (available == null || count >= Mathf.Max(1, sound.MaxVoices)) return false;
        if (sound.Clips == null) return false;
        // nullスロットを除外し、割当済みClipだけを均等に選びます。
        AudioClip clip = null;
        int validCount = 0;
        foreach (AudioClip candidate in sound.Clips)
            if (candidate != null && _clipRandom.Next(++validCount) == 0) clip = candidate;
        if (clip == null) return false;
        available.Id = id;
        available.Volume = Mathf.Clamp01(sound.Volume);
        AudioSource source = available.Source;
        source.transform.position = position ?? transform.position;
        source.clip = clip;
        source.volume = available.Volume * _masterVolume * _sfxVolume;
        source.pitch = Mathf.Clamp(sound.Pitch, 0.1f, 3f);
        source.spatialBlend = position.HasValue ? sound.SpatialBlend : 0f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = Mathf.Max(0.01f, sound.MinDistance);
        source.maxDistance = Mathf.Max(source.minDistance, sound.MaxDistance);
        source.outputAudioMixerGroup = _settings.SfxOutput;
        source.Play();
        _lastPlayed[id] = now;
        return true;
    }

    /// <summary>BGMを差し替えてループ再生。nullなら停止します。</summary>
    public void PlayMusic(AudioClip clip)
    {
        if (!isActiveAndEnabled || _music == null) return;
        _music.Stop();
        _music.clip = clip;
        _music.volume = _masterVolume * _musicVolume;
        if (clip != null) _music.Play();
    }
    public void StopMusic() { if (_music != null) _music.Stop(); }

    /// <summary>SliderのOnValueChanged(float)にも接続可能。SOを書き換えず再生中の音にも反映。</summary>
    public void SetMasterVolume(float value) { _masterVolume = Mathf.Clamp01(value); RefreshVolumes(); }
    public void SetSfxVolume(float value) { _sfxVolume = Mathf.Clamp01(value); RefreshVolumes(); }
    public void SetMusicVolume(float value) { _musicVolume = Mathf.Clamp01(value); RefreshVolumes(); }
    private void RefreshVolumes()
    {
        if (_voices != null)
            foreach (Voice voice in _voices) voice.Source.volume = voice.Volume * _masterVolume * _sfxVolume;
        if (_music != null) _music.volume = _masterVolume * _musicVolume;
    }
    public void StopAllSfx()
    {
        if (_voices != null)
            foreach (Voice voice in _voices) voice.Source.Stop();
        _lastPlayed.Clear();
    }
    private void OnDisable() { StopAllSfx(); StopMusic(); }
    private void OnDestroy() { if (Instance == this) Instance = null; }
}
