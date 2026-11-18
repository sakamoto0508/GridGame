using UnityEngine;

/// <summary>Scene再読込にだけ使う一度きりの引継ぎ。SOや永続セーブは変更しません。</summary>
public static class MatchLaunchRequest
{
    private static string _scenePath;
    private static EnemyDifficulty _difficulty;
    private static bool _autoStart;

    /// <summary>
    /// 要求を破棄します。Consumeは_scenePathが一致したときだけ他の値を使うため、
    /// _scenePathを消すだけで要求全体が無効になります。
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Clear() => _scenePath = null;

    /// <summary>次に読み込むSceneへ渡す難易度と、自動で試合を始めるかを保存します。</summary>
    public static void Set(string scenePath, EnemyDifficulty difficulty, bool autoStart)
    {
        _scenePath = scenePath;
        _difficulty = difficulty;
        _autoStart = autoStart;
    }

    /// <summary>
    /// 保存された要求を取り出して破棄します。対象Sceneが一致した場合だけtrueを返します。
    /// </summary>
    public static bool Consume(string scenePath, out EnemyDifficulty difficulty, out bool autoStart)
    {
        bool matches = _scenePath == scenePath;
        difficulty = _difficulty;
        autoStart = matches && _autoStart;
        Clear();
        return matches;
    }
}
