using UnityEngine;

/// <summary>キャラクターが現在行っている移動の種類を表します。</summary>
public enum CharacterMoveState
{
    Grounded,
    Moving,
    Jumping,
    Falling
}

/// <summary>
/// グリッド上の通常移動、段差ジャンプ、その場ジャンプを管理します。
/// 入力元には依存しないため、PlayerとEnemyの両方で利用できます。
/// </summary>
public class MovementComponent : MonoBehaviour
{
    /// <summary>現在占有している論理グリッド座標です。</summary>
    public Vector3Int CurrentGridPosition => _currentGridPosition;

    /// <summary>最後に入力された水平4方向です。</summary>
    public Vector3Int FacingDirection => _facingDirection;

    /// <summary>現在の移動状態です。</summary>
    public CharacterMoveState State => _state;

    /// <summary>ジャンプまたは落下中ならtrueです。</summary>
    public bool IsAirborne =>
        _state == CharacterMoveState.Jumping ||
        _state == CharacterMoveState.Falling;

    /// <summary>新しい移動要求を受け付けられない状態ならtrueです。</summary>
    public bool IsBusy => _state != CharacterMoveState.Grounded;

    [Header("Move")]
    [SerializeField, Min(0f)] private float _moveDuration = 0.15f;

    [Header("Jump")]
    [SerializeField, Min(0f)] private float _jumpUpDuration = 0.15f;
    [SerializeField, Min(0f)] private float _airTime = 0.15f;
    [SerializeField, Min(0f)] private float _fallDuration = 0.15f;
    [SerializeField, Min(0f)] private float _jumpArcHeight = 0.5f;

    private GridManager _gridManager;
    private CharacterBase _character;
    private Vector3Int _currentGridPosition;
    private Vector3Int _facingDirection = Vector3Int.forward;
    private CharacterMoveState _state = CharacterMoveState.Grounded;
    private bool _isInitialized;

    private void Awake()
    {
        _character = GetComponent<CharacterBase>();
    }

    private void Update()
    {
        if (!_isInitialized || _state != CharacterMoveState.Grounded)
            return;

        // 足場Blockが破壊された場合も検知できるよう、待機中は毎フレーム直下を確認します。
        TryStartFall();
    }

    /// <summary>
    /// Spawnerから必要な参照と開始位置を受け取り、開始セルへCharacterを登録します。
    /// </summary>
    public bool Init(GridManager gridManager, Vector3Int startPosition)
    {
        _gridManager = gridManager;
        _currentGridPosition = startPosition;
        transform.position = _gridManager.GetWorldPosition(startPosition);

        if (_character == null ||
            !_gridManager.TryRegisterCharacter(startPosition, _character))
        {
            Debug.LogError("キャラクターの初期登録に失敗しました。", this);
            enabled = false;
            return false;
        }

        _isInitialized = true;
        return true;
    }

    /// <summary>
    /// 死亡・退場時に現在占有しているGridCellからCharacterを登録解除します。
    /// すでに未登録の場合はfalseを返します。
    /// </summary>
    public bool UnregisterFromGrid()
    {
        if (!_isInitialized || _gridManager == null || _character == null)
            return false;

        bool wasUnregistered = _gridManager.TryUnregisterCharacter(
            _currentGridPosition,
            _character);

        if (wasUnregistered)
            _isInitialized = false;

        return wasUnregistered;
    }

    /// <summary>
    /// 水平4方向の隣接セルへの移動を試みます。
    /// 要求を受理できた時点でtrueを返し、表示位置は非同期で補間します。
    /// </summary>
    public bool TryMove(Vector3Int direction)
    {
        if (IsBusy || !IsHorizontalDirection(direction))
            return false;

        // 壁に阻まれた場合でも、入力した方向へ向きを更新します。
        _facingDirection = direction;
        Vector3Int destination = _currentGridPosition + direction;

        if (!_gridManager.TryMoveCharacter(
                _currentGridPosition, destination, _character))
        {
            return false;
        }

        _currentGridPosition = destination;
        _ = MoveAwaitable(_gridManager.GetWorldPosition(destination));
        return true;
    }

    /// <summary>
    /// ゼロ方向ならその場ジャンプ、水平4方向なら1段高いBlockへのジャンプを試みます。
    /// </summary>
    public bool TryJump(Vector3Int direction)
    {
        if (IsBusy)
            return false;

        if (direction == Vector3Int.zero)
            return TryJumpInPlace();

        if (!IsHorizontalDirection(direction))
            return false;

        _facingDirection = direction;
        return TryJumpUp(direction);
    }

    /// <summary>
    /// 現在位置の1セル上へ移動し、直下にBlockが置かれなければ元のセルへ戻ります。
    /// </summary>
    private bool TryJumpInPlace()
    {
        Vector3Int groundPosition = _currentGridPosition;
        Vector3Int airPosition = groundPosition + Vector3Int.up;

        if (!_gridManager.TryMoveCharacter(
                groundPosition, airPosition, _character))
        {
            return false;
        }

        _currentGridPosition = airPosition;
        _ = JumpInPlaceAwaitable(groundPosition, airPosition);
        return true;
    }

    /// <summary>方向先にある1段高いBlockの上へジャンプします。</summary>
    private bool TryJumpUp(Vector3Int direction)
    {
        if (!_gridManager.CanJumpUp(
                _currentGridPosition, direction, out Vector3Int landingPosition))
        {
            return false;
        }

        if (!_gridManager.TryMoveCharacter(
                _currentGridPosition, landingPosition, _character))
        {
            return false;
        }

        _currentGridPosition = landingPosition;
        _ = JumpUpAwaitable(_gridManager.GetWorldPosition(landingPosition));
        return true;
    }

    /// <summary>通常移動の表示位置を目的地まで補間します。</summary>
    private async Awaitable MoveAwaitable(Vector3 destination)
    {
        _state = CharacterMoveState.Moving;

        try
        {
            await MoveToAwaitable(destination, _moveDuration);
        }
        finally
        {
            _state = CharacterMoveState.Grounded;
        }
    }

    /// <summary>
    /// その場ジャンプを再生します。空中待機中に直下へBlockが置かれた場合は、
    /// 現在の高さへ留まってそのBlockの上に着地します。
    /// </summary>
    private async Awaitable JumpInPlaceAwaitable(
        Vector3Int groundPosition,
        Vector3Int airPosition)
    {
        _state = CharacterMoveState.Jumping;

        try
        {
            Vector3 airWorldPosition = _gridManager.GetWorldPosition(airPosition);
            await MoveToAwaitable(airWorldPosition, _jumpUpDuration);
            await WaitAwaitable(_airTime);

            // 元のセルへBlockが置かれた場合、現在セルがそのBlock上の着地点になります。
            if (_gridManager.HasBlock(groundPosition))
                return;

            _state = CharacterMoveState.Falling;

            if (!_gridManager.TryMoveCharacter(
                    airPosition, groundPosition, _character))
            {
                Debug.LogWarning("その場ジャンプ後の着地セルを確保できませんでした。", this);
                return;
            }

            _currentGridPosition = groundPosition;
            await MoveToAwaitable(
                _gridManager.GetWorldPosition(groundPosition), _fallDuration);
        }
        finally
        {
            _state = CharacterMoveState.Grounded;
        }
    }

    /// <summary>段差ジャンプの放物線移動を再生します。</summary>
    private async Awaitable JumpUpAwaitable(Vector3 destination)
    {
        _state = CharacterMoveState.Jumping;

        try
        {
            float duration = _jumpUpDuration + _fallDuration;

            if (duration <= 0f)
            {
                transform.position = destination;
                return;
            }

            Vector3 start = transform.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float rate = Mathf.Clamp01(elapsed / duration);
                Vector3 position = Vector3.Lerp(start, destination, rate);

                // 0→1→0と変化する値を加え、直線移動をジャンプ軌道にします。
                position.y += 4f * rate * (1f - rate) * _jumpArcHeight;
                transform.position = position;
                await Awaitable.NextFrameAsync();
            }

            transform.position = destination;
        }
        finally
        {
            _state = CharacterMoveState.Grounded;
        }
    }

    /// <summary>
    /// 現在セルの下に足場がない場合、最下部の着地可能セルまで落下を開始します。
    /// </summary>
    private bool TryStartFall()
    {
        if (_state != CharacterMoveState.Grounded || _gridManager == null)
            return false;

        if (!GridGravitySystem.TryGetFallDestination(
                _gridManager,
                _currentGridPosition,
                out Vector3Int destination))
        {
            return false;
        }

        Vector3Int startPosition = _currentGridPosition;

        // 落下開始時に着地点を論理上占有し、他Characterとの重複を防ぎます。
        if (!_gridManager.TryMoveCharacter(
                startPosition,
                destination,
                _character))
        {
            Debug.LogWarning(
                $"落下先セル {destination} の確保に失敗しました。開始セル={startPosition}",
                this);
            return false;
        }

        _currentGridPosition = destination;
        int fallDistance = startPosition.y - destination.y;
        _ = FallAwaitable(
            _gridManager.GetWorldPosition(destination),
            fallDistance);

        return true;
    }

    /// <summary>落下距離に応じた時間をかけて着地点まで表示位置を移動します。</summary>
    private async Awaitable FallAwaitable(Vector3 destination, int fallDistance)
    {
        _state = CharacterMoveState.Falling;

        try
        {
            float duration = _fallDuration * Mathf.Max(1, fallDistance);
            await MoveToAwaitable(destination, duration);
        }
        finally
        {
            _state = CharacterMoveState.Grounded;
        }
    }

    /// <summary>指定時間をかけて現在の表示位置から目的地へ移動します。</summary>
    private async Awaitable MoveToAwaitable(Vector3 destination, float duration)
    {
        if (duration <= 0f)
        {
            transform.position = destination;
            return;
        }

        Vector3 start = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float rate = Mathf.Clamp01(elapsed / duration);
            transform.position = Vector3.Lerp(start, destination, rate);
            await Awaitable.NextFrameAsync();
        }

        transform.position = destination;
    }

    /// <summary>Time.deltaTimeを使って指定秒数だけ待機します。</summary>
    private static async Awaitable WaitAwaitable(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            await Awaitable.NextFrameAsync();
        }
    }

    /// <summary>値がX/Z平面上の単位4方向かを判定します。</summary>
    private static bool IsHorizontalDirection(Vector3Int direction)
    {
        return direction.y == 0 &&
               Mathf.Abs(direction.x) + Mathf.Abs(direction.z) == 1;
    }
}
