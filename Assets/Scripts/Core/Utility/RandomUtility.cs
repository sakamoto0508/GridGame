using UnityEngine;

/// <summary>
/// 演出用のSystem.Randomを使う補助関数です。
/// UnityEngine.Randomの乱数列を進めないため、ステージ生成やAIの抽選に影響しません。
/// </summary>
public static class RandomUtility
{
    /// <summary>
    /// rangeの小さい方〜大きい方（それぞれmin〜maxに制限）の間で乱数を返します。
    /// Inspectorで大小を逆に入力しても動くようにしています。
    /// </summary>
    public static float Range(System.Random random, Vector2 range, float min, float max)
    {
        float a = Mathf.Clamp(Mathf.Min(range.x, range.y), min, max);
        float b = Mathf.Clamp(Mathf.Max(range.x, range.y), min, max);
        return Mathf.Lerp(a, b, (float)random.NextDouble());
    }
}
