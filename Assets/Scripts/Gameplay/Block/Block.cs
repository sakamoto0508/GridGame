using UnityEngine;

/// <summary>
/// 破壊可能かどうかを示すブロックの種類
/// </summary>
public enum BlockType
{
    Breakable,
    Unbreakable
}

/// <summary>
/// グリッド上のブロックを表すクラス
/// </summary>
public class Block : MonoBehaviour
{
    [SerializeField] private BlockSettings _settings;
    private GridManager _gridManager;
    private bool _isFalling;
    private bool _isDestroyed;

    public BlockType Type => _settings != null ? _settings.Type : BlockType.Unbreakable;
    public Vector3Int GridPosition { get; private set; }

    /// <summary>生成されたBlockへ論理グリッド座標を設定します。</summary>
    public void Initialize(GridManager gridManager, Vector3Int gridPosition)
    {
        if (_settings == null)
        {
            Debug.LogError("BlockのBlock Settingsが未設定です。", this);
            enabled = false;
            return;
        }

        _gridManager = gridManager;
        GridPosition = gridPosition;

        TryStartFall();
    }

    /// <summary>
    /// 破壊可能Blockをグリッドから解除して破壊します。
    /// 解除後は同じ列の上側にあるBlockへ重力再判定を要求します。
    /// </summary>
    public bool BlockBreak()
    {
        if (_isDestroyed || Type != BlockType.Breakable || _gridManager == null)
            return false;

        Vector3Int destroyedPosition = GridPosition;

        if (!_gridManager.TryUnregisterBlock(destroyedPosition, this))
            return false;

        _isDestroyed = true;

        // Blockを論理グリッドから外してから、上のBlockを下側から順番に落とします。
        ReevaluateBlocksAbove(_gridManager, destroyedPosition);

        // TODO: 破壊時のEffectとSoundを追加する。
        Destroy(gameObject);
        return true;
    }

    /// <summary>
    /// 足場が失われた可能性があるときに、外部から重力を再判定します。
    /// すでに落下中なら新しい落下は開始しません。
    /// </summary>
    public bool ReevaluateGravity()
    {
        return TryStartFall();
    }

    private bool TryStartFall()
    {
        if (_isDestroyed || _isFalling)
            return false;

        if (!GridGravitySystem.TryGetBlockFallDestination(_gridManager,GridPosition,
                out Vector3Int destination))
        {
            return false;
        }

        Vector3Int startPosition = GridPosition;

        if (!_gridManager.TryMoveBlock(startPosition,destination,this))
        {
            return false;
        }

        GridPosition = destination;

        _ = FallAwaitable(
            startPosition,
            destination,
            _gridManager.GetWorldPosition(destination));

        return true;
    }

    /// <summary>
    /// Blockを着地点まで補間し、通過した各グリッドセルのCharacterを押し潰します。
    /// Colliderやフレームレートに依存せず、各セルを一度ずつ判定します。
    /// </summary>
    private async Awaitable FallAwaitable(
        Vector3Int startGridPosition,
        Vector3Int destinationGridPosition,
        Vector3 targetWorldPosition)
    {
        _isFalling = true;
        float elapsedTime = 0f;
        Vector3 startWorldPosition = transform.position;
        int nextGridYToCheck = startGridPosition.y - 1;

        while (!_isDestroyed && elapsedTime < _settings.FallDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = _settings.FallDuration <= 0f
                ? 1f
                : Mathf.Clamp01(elapsedTime / _settings.FallDuration);
            transform.position = Vector3.Lerp(startWorldPosition, targetWorldPosition, t);

            // 補間位置が次のセル高さを越えたら、そのセルのCharacterを判定します。
            float currentGridY = Mathf.Lerp(
                startGridPosition.y,
                destinationGridPosition.y,
                t);

            while (nextGridYToCheck >= destinationGridPosition.y &&
                   currentGridY <= nextGridYToCheck)
            {
                CrushCharacterAt(new Vector3Int(
                    startGridPosition.x,
                    nextGridYToCheck,
                    startGridPosition.z));

                nextGridYToCheck--;
            }

            await Awaitable.NextFrameAsync();
        }

        if (_isDestroyed)
            return;

        // durationが0に近い場合や最終フレームの丸め誤差でも、経路全体を取りこぼさない。
        while (nextGridYToCheck >= destinationGridPosition.y)
        {
            CrushCharacterAt(new Vector3Int(
                startGridPosition.x,
                nextGridYToCheck,
                startGridPosition.z));

            nextGridYToCheck--;
        }

        transform.position = targetWorldPosition;
        _isFalling = false;
        // 落下後に再度落下可能か確認
        TryStartFall();
    }

    /// <summary>指定セルに生存Characterがいれば、落下Blockによる死亡を要求します。</summary>
    private void CrushCharacterAt(Vector3Int position)
    {
        CharacterBase character = _gridManager.GetCharacter(position);

        if (character == null)
            return;

        Debug.Log(
            $"Falling Block crushed Character: Block={name}, Cell={position}, Character={character.name}",
            this);

        character.Kill(DeathCause.FallingBlock);
    }

    /// <summary>
    /// 破壊セルと同じX/Z列にある上側のBlockを、下から順番に再評価します。
    /// 各Blockは落下開始時に論理セルを移すため、積み重なったBlockも順番に落下できます。
    /// </summary>
    private static void ReevaluateBlocksAbove(
        GridManager gridManager,
        Vector3Int destroyedPosition)
    {
        for (int y = destroyedPosition.y + 1; y < gridManager.Size.y; y++)
        {
            Vector3Int position = new Vector3Int(
                destroyedPosition.x,
                y,
                destroyedPosition.z);

            Block blockAbove = gridManager.GetBlock(position);

            if (blockAbove != null)
                blockAbove.ReevaluateGravity();
        }
    }
}
