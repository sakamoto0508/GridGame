using UnityEngine;

/// <summary>BlockとItem共通の再利用契約。ゲーム上の登録・状態は派生側で解除します。</summary>
public abstract class PooledGridObject : MonoBehaviour
{
    private GridObjectPool _pool;

    internal void PrepareForRent(GridObjectPool pool)
    {
        _pool = pool;
        ResetForRent();
    }

    /// <summary>前回の破壊・取得フラグなどを、非アクティブな間にリセットします。</summary>
    protected abstract void ResetForRent();

    /// <summary>グリッド参照と実行中の処理を解除します。複数回呼ばれても安全にします。</summary>
    internal abstract void ClearForPool();

    protected void ReturnToPool()
    {
        if (_pool != null)
            _pool.Return(this);
        else
        {
            // Sceneに手動配置されたものなど、プール外の生成物も従来どおり扱います。
            ClearForPool();
            Destroy(gameObject);
        }
    }

    // Scene終了や外部からの無効化でも、盤面に占有情報を残しません。
    protected virtual void OnDisable() => ClearForPool();
}
