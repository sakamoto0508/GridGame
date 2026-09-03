using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Bombの現在状態を表します。</summary>
public enum BombState
{
    Armed,
    Falling,
    Exploded
}

/// <summary>
/// グリッド上のBombを表します。Blockとは継承関係を持たず、
/// Fuse、重力、爆発要求というBomb固有の責務を管理します。
/// </summary>
[RequireComponent(typeof(ExplosionView))]
public class Bomb : MonoBehaviour
{
    /// <summary>Bombが爆発して盤面から除去されたときに通知されます。</summary>
    public event Action<Bomb> Exploded;

    /// <summary>現在Bombが登録されている論理グリッド座標です。</summary>
    public Vector3Int GridPosition { get; private set; }

    /// <summary>爆風が各方向へ届く最大セル数です。</summary>
    public int ExplosionPower { get; private set; }

    /// <summary>このBombを設置したCharacterです。</summary>
    public CharacterBase Owner { get; private set; }

    /// <summary>現在のBomb状態です。</summary>
    public BombState State { get; private set; }

    /// <summary>爆発までの残り秒数です。</summary>
    public float RemainingFuseTime => Mathf.Max(0f, _remainingFuseTime);

    [SerializeField, Min(0f)] private float _fallDurationPerCell = 0.12f;

    private GridManager _gridManager;
    private ExplosionView _explosionView;
    private float _remainingFuseTime;
    private bool _isInitialized;

    private void Awake()
    {
        _explosionView = GetComponent<ExplosionView>();
    }

    private void Update()
    {
        if (!_isInitialized || State == BombState.Exploded)
            return;

        // 落下中もFuseは進行します。
        _remainingFuseTime -= Time.deltaTime;

        if (_remainingFuseTime <= 0f)
        {
            Explode();
            return;
        }

        // 足場が後から失われた場合にも再び落下します。
        if (State == BombState.Armed)
            TryStartFall();
    }

    /// <summary>
    /// 生成直後のBombへ必要な情報を設定し、Fuseと重力を開始します。
    /// GridCellへの初期登録は呼び出し側が先に済ませます。
    /// </summary>
    public void Init(GridManager gridManager, Vector3Int gridPosition, CharacterBase owner,
        float fuseTime, int explosionPower)
    {
        _gridManager = gridManager;
        GridPosition = gridPosition;
        Owner = owner;
        _remainingFuseTime = Mathf.Max(0f, fuseTime);
        ExplosionPower = Mathf.Max(1, explosionPower);
        State = BombState.Armed;
        _isInitialized = true;

        transform.position = _gridManager.GetWorldPosition(gridPosition);
        TryStartFall();
    }

    /// <summary>
    /// 二重爆発を防ぎながら爆風セルを計算し、Bombを盤面から取り除きます。
    /// ExplosionSystemはBlock破壊、Character死亡、ほかのBombの連鎖爆発を処理します。
    /// </summary>
    public bool Explode()
    {
        if (!_isInitialized || State == BombState.Exploded)
            return false;

        State = BombState.Exploded;

        // 先に盤面から外し、爆風探索で爆発元自身を連鎖対象にしないようにします。
        _gridManager.TryUnregisterBomb(GridPosition, this);

        // 登録解除後も保持している論理座標を起点として爆風を生成できます。
        IReadOnlyList<ExplosionCellData> affectedCells =
            ExplosionSystem.GenerateExplosion(_gridManager, GridPosition, ExplosionPower);

        // ゲーム判定済みのセル一覧を使い、Colliderを持たない見た目だけを表示します。
        if (_explosionView != null)
        {
            _explosionView.Show(_gridManager, affectedCells);
        }
        else
        {
            Debug.LogWarning("BombにExplosionViewがないため、爆風を表示できません。", this);
        }

        Debug.Log(
            $"Bomb exploded: Position={GridPosition}, Power={ExplosionPower}",
            this);

        Exploded?.Invoke(this);
        Destroy(gameObject);
        return true;
    }

    /// <summary>直下に空きがあれば着地可能セルまで落下を開始します。</summary>
    private bool TryStartFall()
    {
        if (State != BombState.Armed)
            return false;

        if (!GridGravitySystem.TryGetBombFallDestination(
                _gridManager,
                GridPosition,
                out Vector3Int destination))
        {
            return false;
        }

        Vector3Int startPosition = GridPosition;

        if (!_gridManager.TryMoveBomb(startPosition, destination, this))
            return false;

        GridPosition = destination;
        int fallDistance = startPosition.y - destination.y;
        _ = FallAwaitable(_gridManager.GetWorldPosition(destination), fallDistance);

        return true;
    }

    /// <summary>落下距離に応じた時間をかけてBombの表示位置を補間します。</summary>
    private async Awaitable FallAwaitable(Vector3 destination, int fallDistance)
    {
        State = BombState.Falling;
        float duration = _fallDurationPerCell * Mathf.Max(1, fallDistance);

        try
        {
            if (duration <= 0f)
            {
                transform.position = destination;
                return;
            }

            Vector3 start = transform.position;
            float elapsed = 0f;

            while (elapsed < duration && State != BombState.Exploded)
            {
                elapsed += Time.deltaTime;
                float rate = Mathf.Clamp01(elapsed / duration);
                transform.position = Vector3.Lerp(start, destination, rate);
                await Awaitable.NextFrameAsync();
            }

            if (State != BombState.Exploded)
                transform.position = destination;
        }
        finally
        {
            if (State != BombState.Exploded)
                State = BombState.Armed;
        }
    }
}
