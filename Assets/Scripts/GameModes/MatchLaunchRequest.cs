using UnityEngine;

/// <summary>Scene再読込にだけ使う一度きりの引継ぎ。SOや永続セーブは変更しません。</summary>
public static class MatchLaunchRequest
{
    private static string _scenePath;
    private static EnemyDifficulty _difficulty;
    private static bool _autoStart;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Clear() => _scenePath = null;

    public static void Set(string scenePath, EnemyDifficulty difficulty, bool autoStart)
    {
        _scenePath = scenePath;
        _difficulty = difficulty;
        _autoStart = autoStart;
    }

    public static bool Consume(string scenePath, out EnemyDifficulty difficulty, out bool autoStart)
    {
        bool matches = _scenePath == scenePath;
        difficulty = _difficulty;
        autoStart = matches && _autoStart;
        Clear();
        return matches;
    }
}
