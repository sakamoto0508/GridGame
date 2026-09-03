using UnityEngine;

/// <summary>
/// 1つの爆風セルに表示される、見た目専用のEffectです。
/// 当たり判定はExplosionSystemが処理するため、このComponentはColliderを必要としません。
/// </summary>
public class ExplosionEffect : MonoBehaviour
{
    private bool _isPlaying;

    /// <summary>指定時間だけ表示した後、このEffect GameObjectを破棄します。</summary>
    public void Play(float duration)
    {
        if (_isPlaying)
            return;

        _isPlaying = true;
        _ = DestroyAfterDelayAsync(Mathf.Max(0f, duration));
    }

    /// <summary>Time.deltaTimeで表示時間を計測します。</summary>
    private async Awaitable DestroyAfterDelayAsync(float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            await Awaitable.NextFrameAsync();
        }

        Destroy(gameObject);
    }
}
