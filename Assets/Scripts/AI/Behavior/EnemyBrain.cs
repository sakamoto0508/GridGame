using System.Collections.Generic;
using UnityEngine;

/// <summary>Enemy AIが現在何を目的に行動しているかを表します。</summary>
public enum EnemyAIState
{
    Idle,
    Chase,
    MoveToAttackPosition,
    PlaceBomb,
    Escape
}

/// <summary>
/// Playerへの簡易接近、徘徊、Bomb設置を行う最小構成のEnemy AIです。
/// 経路探索と爆風回避は後からGridPathfindingSystemとGridDangerMapで追加します。
/// </summary>
[RequireComponent(typeof(EnemyCharacter))]
[RequireComponent(typeof(MovementComponent))]
[RequireComponent(typeof(BombComponent))]
[RequireComponent(typeof(BlockPlacementComponent))]
public class EnemyBrain : MonoBehaviour
{
    /// <summary>現在の状態です。実行中はInspectorからも確認できます。</summary>
    public EnemyAIState CurrentState => _currentState;

    private static readonly Vector3Int[] HorizontalDirections =
    {
        Vector3Int.right,
        Vector3Int.left,
        Vector3Int.forward,
        Vector3Int.back
    };

    private GridManager _gridManager;
    private CharacterBase _player;
    private EnemyCharacter _enemy;
    private MovementComponent _movement;
    private BombComponent _bombComponent;
    private BlockPlacementComponent _blockPlacement;
    private MovementComponent _playerMovement;
    private EnemyDifficultyValues _difficultyValues;
    private readonly GridDangerMap _dangerMap = new GridDangerMap();
    private readonly List<Vector3Int> _escapePath = new List<Vector3Int>();
    private Vector3Int? _previousPosition;
    private Vector3Int? _positionBeforePrevious;
    private Vector3Int? _temporarilyAvoidedCell;
    private float _avoidCellUntil;
    private Vector3Int _lastDecisionPosition;
    private int _sameCellDecisionCount;
    private float _reconsiderUntil;
    private bool _hasDecisionPosition;
    private float _escapeSafeSince = -1f;
    private float _nextActionTime;
    private bool _isInitialized;

    [Header("Debug")]
    [SerializeField] private EnemyAIState _currentState = EnemyAIState.Idle;

    /// <summary>Spawnerから盤面、追跡対象、難易度を受け取って思考を開始します。</summary>
    public bool Init(
        GridManager gridManager,
        CharacterBase player,
        EnemyDifficulty difficulty,
        EnemyAISettings settings)
    {
        if (player == null)
        {
            Debug.LogError("EnemyBrainの初期化に失敗しました: Playerがnullです。", this);
            return false;
        }

        _enemy = GetComponent<EnemyCharacter>();
        _movement = GetComponent<MovementComponent>();
        _bombComponent = GetComponent<BombComponent>();
        _blockPlacement = GetComponent<BlockPlacementComponent>();
        _playerMovement = player.GetComponent<MovementComponent>();

        if (gridManager == null || player == null ||
            _enemy == null || _movement == null || _bombComponent == null ||
            _blockPlacement == null || settings == null)
        {
            Debug.LogError("EnemyBrainの初期化に失敗しました: 必要な参照が不足しています。", this);
            enabled = false;
            return false;
        }

        _gridManager = gridManager;
        _player = player;
        _difficultyValues = settings.GetValues(difficulty);

        if (_difficultyValues == null)
        {
            Debug.LogError($"Enemy AI Settingsに{difficulty}の設定がありません。", this);
            enabled = false;
            return false;
        }

        _nextActionTime = Time.time + _difficultyValues.ActionInterval;
        _isInitialized = true;
        SetState(EnemyAIState.Idle);

        Debug.Log(
            $"Enemy AI initialized: Difficulty={difficulty}, Interval={_difficultyValues.ActionInterval}",
            this);
        return true;
    }

    private void Update()
    {
        if (!_isInitialized || !_enemy.IsAlive || _player == null || !_player.IsAlive)
            return;

        if (_movement.IsBusy || Time.time < _nextActionTime)
            return;

        // 同じ判断を繰り返した直後は、短時間待って盤面やPlayerの変化を待ちます。
        if (Time.time < _reconsiderUntil)
            return;

        _nextActionTime = Time.time + _difficultyValues.ActionInterval;
        ThinkAndAct();
    }

    /// <summary>距離と難易度を基に、Bomb設置または1セル移動を選びます。</summary>
    private void ThinkAndAct()
    {
        Vector3Int enemyPosition = _movement.CurrentGridPosition;

        // 行動するたびに現在のBomb配置から危険範囲を更新します。
        _dangerMap.Rebuild(_gridManager);

        bool currentCellIsDangerous = _dangerMap.IsDangerous(enemyPosition);

        // 状態にかかわらず、生存判断は常に最優先します。
        if (currentCellIsDangerous)
        {
            _escapeSafeSince = -1f;
            ResetStuckCounter(enemyPosition);
            SetState(EnemyAIState.Escape);
            TryEscapeDanger(enemyPosition);
            return;
        }

        // Escapeは1回の安全判定だけでは解除しません。Bomb落下などで危険範囲が
        // 変動している間のEscape/Chase状態振動を、安全確認時間によって防ぎます。
        if (_currentState == EnemyAIState.Escape &&
            !CanLeaveEscapeState(enemyPosition))
        {
            _escapePath.Clear();
            ResetStuckCounter(enemyPosition);
            return;
        }

        _escapeSafeSince = -1f;

        // 移動せず同じセルで判断し続けた場合、通常ロジックを繰り返さず別方向を試します。
        if (RegisterDecisionAndIsStuck(enemyPosition))
        {
            RecoverFromStuck(enemyPosition);
            return;
        }

        // 危険範囲を抜けた時点で、以前の逃走経路には固執しません。
        _escapePath.Clear();

        if (_playerMovement == null)
        {
            SetState(EnemyAIState.Idle);
            return;
        }

        Vector3Int playerPosition = _playerMovement.CurrentGridPosition;
        int distance = GetManhattanDistance(enemyPosition, playerPosition);
        bool detectedPlayer = distance <= _difficultyValues.DetectionRange;

        // 現在位置のBombでPlayerへ爆風が届くなら、安全確認後に攻撃します。
        if (detectedPlayer &&
            distance <= _difficultyValues.BombDistance &&
            CanBombHitPlayer(enemyPosition, playerPosition))
        {
            SetState(EnemyAIState.PlaceBomb);

            if (TryPlaceBombByChance())
            {
                SetState(EnemyAIState.Escape);
                return;
            }
        }

        if (!detectedPlayer)
            SetState(EnemyAIState.Idle);
        else if (HasAttackPositionCandidate(enemyPosition, playerPosition))
            SetState(EnemyAIState.MoveToAttackPosition);
        else
            SetState(EnemyAIState.Chase);

        List<Vector3Int> candidates = CreateDirectionCandidates(
            enemyPosition,
            playerPosition,
            detectedPlayer);

        for (int i = 0; i < candidates.Count; i++)
        {
            Vector3Int destination = enemyPosition + candidates[i];

            // A→B→A→Bの往復になる移動を検知したら、Bを短時間候補から外します。
            // Escape中には適用しないため、爆風回避に必要な後退は妨げません。
            if (WouldRepeatTwoCellOscillation(enemyPosition, destination))
            {
                TemporarilyAvoid(destination);
                continue;
            }

            if (IsTemporarilyAvoided(destination))
                continue;

            if (TryTraverseAndRemember(enemyPosition, candidates[i]))
                return;
        }

        // 歩行・既存Blockへのジャンプで進めない場合だけ、足場を作って上ることを検討します。
        if (detectedPlayer && TryPlaceUsefulChaseBlock(enemyPosition, playerPosition, candidates))
        {
            SetState(EnemyAIState.MoveToAttackPosition);
            return;
        }

        // 全方向を塞がれている場合は、Blockを壊すきっかけとしてBomb設置を試します。
        SetState(EnemyAIState.PlaceBomb);
        if (TryPlaceBombByChance())
            SetState(EnemyAIState.Escape);
        else
            SetState(detectedPlayer ? EnemyAIState.Chase : EnemyAIState.Idle);
    }

    /// <summary>隣接セルにPlayerへ爆風を届かせられる攻撃位置があるか調べます。</summary>
    private bool HasAttackPositionCandidate(Vector3Int enemyPosition, Vector3Int playerPosition)
    {
        for (int i = 0; i < HorizontalDirections.Length; i++)
        {
            Vector3Int candidate = enemyPosition + HorizontalDirections[i];

            if (!_gridManager.CanCharacterEnter(candidate) || _dangerMap.IsDangerous(candidate))
                continue;

            if (CanBombHitPlayer(candidate, playerPosition))
                return true;
        }

        return false;
    }

    /// <summary>状態が変化した場合だけ更新し、遷移をConsoleへ出力します。</summary>
    private void SetState(EnemyAIState nextState)
    {
        if (_currentState == nextState)
            return;

        EnemyAIState previousState = _currentState;
        _currentState = nextState;
        Debug.Log($"Enemy AI State: {previousState} -> {_currentState}", this);
    }

    /// <summary>
    /// 最寄りの安全セルへの経路を探し、その最初の1セルへ移動します。
    /// 次セルが1段高い場合は、通常移動ではなく段差ジャンプを要求します。
    /// </summary>
    private bool TryEscapeDanger(Vector3Int enemyPosition)
    {
        // 前回決めた経路の次セルが現在地とつながっていれば、その方針を維持します。
        RemoveReachedEscapeCells(enemyPosition);

        if (_escapePath.Count == 0)
        {
            List<Vector3Int> newPath = GridPathfindingSystem.FindPathToNearestSafeCell(
                _gridManager,
                _dangerMap,
                enemyPosition,
                _movement.MoveDuration,
                _movement.JumpDuration,
                _difficultyValues.ActionInterval);

            if (newPath.Count < 2)
                return TryCreateEscapeStep(enemyPosition);

            _escapePath.Clear();
            // startは現在地なので、実際にこれから通るセルだけを保持します。
            for (int i = 1; i < newPath.Count; i++)
                _escapePath.Add(newPath[i]);
        }

        if (_escapePath.Count == 0)
            return false;

        Vector3Int difference = _escapePath[0] - enemyPosition;
        Vector3Int horizontalDirection =
            new Vector3Int(difference.x, 0, difference.z);

        if (difference.y == 1)
        {
            if (_movement.TryJump(horizontalDirection))
            {
                RememberMove(enemyPosition);
                return true;
            }
        }

        if (difference.y == 0)
        {
            if (TryMoveAndRemember(enemyPosition, horizontalDirection))
                return true;
        }

        // 経路がBlockやBombで無効になった場合、次回の思考で再探索します。
        _escapePath.Clear();
        return false;
    }

    /// <summary>すでに到達した先頭セルを逃走経路から取り除きます。</summary>
    private void RemoveReachedEscapeCells(Vector3Int currentPosition)
    {
        while (_escapePath.Count > 0 && _escapePath[0] == currentPosition)
            _escapePath.RemoveAt(0);
    }

    /// <summary>
    /// Player検知中は距離が縮む順、未検知または判断ミス時はランダム順を返します。
    /// </summary>
    private List<Vector3Int> CreateDirectionCandidates(Vector3Int enemyPosition,Vector3Int playerPosition,
                                                             bool detectedPlayer)
    {
        List<Vector3Int> candidates = new List<Vector3Int>(HorizontalDirections);
        bool makesMistake = Random.value < _difficultyValues.MistakeChance;

        if (!detectedPlayer || makesMistake)
        {
            Shuffle(candidates);
            MovePreviousCellToEnd(candidates, enemyPosition);
            return candidates;
        }

        candidates.Sort((left, right) =>
        {
            // 直前セルへの後退は、ほかの候補がある限り最後に評価します。
            bool leftReturns = IsPreviousCell(enemyPosition + left);
            bool rightReturns = IsPreviousCell(enemyPosition + right);

            if (leftReturns != rightReturns)
                return leftReturns ? 1 : -1;

            // 次のセルからPlayerを爆風に入れられる方向を最優先します。
            bool leftCanAttack = CanBombHitPlayer(enemyPosition + left, playerPosition);
            bool rightCanAttack = CanBombHitPlayer(enemyPosition + right, playerPosition);

            if (leftCanAttack != rightCanAttack)
                return leftCanAttack ? -1 : 1;

            int leftDistance = GetManhattanDistance(enemyPosition + left, playerPosition);
            int rightDistance = GetManhattanDistance(enemyPosition + right, playerPosition);
            return leftDistance.CompareTo(rightDistance);
        });

        return candidates;
    }

    /// <summary>
    /// 直前セルへ戻る方向を末尾へ移動します。禁止はしないため、行き止まりでは後退できます。
    /// </summary>
    private void MovePreviousCellToEnd(List<Vector3Int> directions, Vector3Int currentPosition)
    {
        if (!_previousPosition.HasValue)
            return;

        for (int i = 0; i < directions.Count; i++)
        {
            if (currentPosition + directions[i] != _previousPosition.Value)
                continue;

            Vector3Int returnDirection = directions[i];
            directions.RemoveAt(i);
            directions.Add(returnDirection);
            return;
        }
    }

    private bool IsPreviousCell(Vector3Int position)
        => _previousPosition.HasValue && position == _previousPosition.Value;

    /// <summary>移動に成功した場合だけ、出発セルを直前位置として記録します。</summary>
    private bool TryMoveAndRemember(Vector3Int currentPosition, Vector3Int direction)
    {
        if (!_movement.TryMove(direction))
            return false;

        RememberMove(currentPosition);
        return true;
    }

    /// <summary>安全な通常移動を優先し、進めなければ同方向の1段上へジャンプします。</summary>
    private bool TryTraverseAndRemember(Vector3Int currentPosition, Vector3Int direction)
    {
        Vector3Int moveDestination = currentPosition + direction;

        if (!_dangerMap.IsDangerous(moveDestination) &&
            TryMoveAndRemember(currentPosition, direction))
            return true;

        if (_gridManager.CanJumpUp(currentPosition, direction, out Vector3Int landing) &&
            !_dangerMap.IsDangerous(landing) &&
            _movement.TryJump(direction))
        {
            RememberMove(currentPosition);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 通常経路がないとき、Block上へ上ることでPlayerへ近づく方向へ足場を作ります。
    /// </summary>
    private bool TryPlaceUsefulChaseBlock(
        Vector3Int currentPosition,
        Vector3Int playerPosition,
        List<Vector3Int> orderedDirections)
    {
        int currentDistance = GetManhattanDistance(currentPosition, playerPosition);

        for (int i = 0; i < orderedDirections.Count; i++)
        {
            Vector3Int direction = orderedDirections[i];
            Vector3Int blockPosition = currentPosition + direction;
            Vector3Int landingPosition = blockPosition + Vector3Int.up;
            Vector3Int supportPosition = blockPosition + Vector3Int.down;

            if (!_gridManager.CanPlaceBlock(blockPosition) ||
                !_gridManager.HasBlock(supportPosition) ||
                !_gridManager.CanCharacterEnter(landingPosition) ||
                _dangerMap.IsDangerous(blockPosition) ||
                _dangerMap.IsDangerous(landingPosition) ||
                GetManhattanDistance(landingPosition, playerPosition) >= currentDistance)
                continue;

            if (_movement.TryFace(direction) && _blockPlacement.TryPlaceBlock())
                return true;
        }

        return false;
    }

    /// <summary>
    /// 通常の逃走経路がないとき、安全な高所へ上るためのBlockを設置します。
    /// 設置と次回ジャンプが爆発時刻に間に合う場合だけ実行します。
    /// </summary>
    private bool TryCreateEscapeStep(Vector3Int currentPosition)
    {
        if (!_dangerMap.TryGetDangerTime(currentPosition, out float dangerTime))
            return false;

        float requiredTime = _difficultyValues.ActionInterval +
                             Mathf.Max(_movement.JumpDuration, _difficultyValues.ActionInterval) +
                             0.05f;

        if (requiredTime >= dangerTime)
            return false;

        for (int i = 0; i < HorizontalDirections.Length; i++)
        {
            Vector3Int direction = HorizontalDirections[i];
            Vector3Int blockPosition = currentPosition + direction;
            Vector3Int landingPosition = blockPosition + Vector3Int.up;
            Vector3Int supportPosition = blockPosition + Vector3Int.down;

            if (!_gridManager.CanPlaceBlock(blockPosition) ||
                !_gridManager.HasBlock(supportPosition) ||
                !_gridManager.CanCharacterEnter(landingPosition) ||
                _dangerMap.IsDangerous(landingPosition))
                continue;

            if (_movement.TryFace(direction) && _blockPlacement.TryPlaceBlock())
                return true;
        }

        return false;
    }

    private void RememberMove(Vector3Int position)
    {
        _positionBeforePrevious = _previousPosition;
        _previousPosition = position;
    }

    /// <summary>同じセルで連続して思考した回数を数え、上限到達時にtrueを返します。</summary>
    private bool RegisterDecisionAndIsStuck(Vector3Int position)
    {
        if (!_hasDecisionPosition || position != _lastDecisionPosition)
        {
            ResetStuckCounter(position);
            return false;
        }

        _sameCellDecisionCount++;
        return _sameCellDecisionCount >= _difficultyValues.MaxSameCellDecisions;
    }

    private void ResetStuckCounter(Vector3Int position)
    {
        _lastDecisionPosition = position;
        _sameCellDecisionCount = 0;
        _hasDecisionPosition = true;
    }

    /// <summary>
    /// 現在地と次に使う隣接セルが一定時間連続して安全な場合だけEscape解除を許可します。
    /// 自分のBombが残っている場合も解除しません。
    /// </summary>
    private bool CanLeaveEscapeState(Vector3Int currentPosition)
    {
        if (_bombComponent.CurrentBombCount > 0 || HasDangerousNeighbor(currentPosition))
        {
            _escapeSafeSince = -1f;
            return false;
        }

        if (_escapeSafeSince < 0f)
        {
            _escapeSafeSince = Time.time;
            return false;
        }

        return Time.time - _escapeSafeSince >=
               _difficultyValues.EscapeSafeConfirmationTime;
    }

    /// <summary>通常移動先と1段ジャンプ先の周囲に危険が残っているか調べます。</summary>
    private bool HasDangerousNeighbor(Vector3Int currentPosition)
    {
        for (int i = 0; i < HorizontalDirections.Length; i++)
        {
            Vector3Int direction = HorizontalDirections[i];
            Vector3Int neighbor = currentPosition + direction;

            if (_dangerMap.IsDangerous(neighbor))
                return true;

            if (_gridManager.CanJumpUp(currentPosition, direction, out Vector3Int landing) &&
                _dangerMap.IsDangerous(landing))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 同じ判断の反復を検出した際、追跡評価を一度捨てて安全な別方向を試します。
    /// 進めない場合はBombやBlockを連続生成せず、短時間待ってから再評価します。
    /// </summary>
    private void RecoverFromStuck(Vector3Int currentPosition)
    {
        SetState(EnemyAIState.Idle);
        _escapePath.Clear();

        List<Vector3Int> alternatives = new List<Vector3Int>(HorizontalDirections);
        Shuffle(alternatives);
        MovePreviousCellToEnd(alternatives, currentPosition);

        for (int i = 0; i < alternatives.Count; i++)
        {
            Vector3Int destination = currentPosition + alternatives[i];

            if (IsPreviousCell(destination) ||
                IsTemporarilyAvoided(destination) ||
                _dangerMap.IsDangerous(destination))
                continue;

            if (TryTraverseAndRemember(currentPosition, alternatives[i]))
            {
                ResetStuckCounter(_movement.CurrentGridPosition);
                Debug.Log($"Enemy AI recovered from repeated action: moved toward {alternatives[i]}", this);
                return;
            }
        }

        _reconsiderUntil = Time.time + _difficultyValues.ReconsiderPause;
        _sameCellDecisionCount = 0;
        _previousPosition = null;
        _positionBeforePrevious = null;
        Debug.Log($"Enemy AI paused repeated action for {_difficultyValues.ReconsiderPause:0.00}s", this);
    }

    /// <summary>次の移動がA→B→A→Bという2セル往復を完成させるか判定します。</summary>
    private bool WouldRepeatTwoCellOscillation(
        Vector3Int currentPosition,
        Vector3Int destination)
    {
        return _previousPosition.HasValue &&
               _positionBeforePrevious.HasValue &&
               destination == _previousPosition.Value &&
               currentPosition == _positionBeforePrevious.Value;
    }

    /// <summary>往復先を数回分の思考時間だけ避け、別ルートや待機を選べるようにします。</summary>
    private void TemporarilyAvoid(Vector3Int position)
    {
        _temporarilyAvoidedCell = position;
        _avoidCellUntil = Time.time + Mathf.Max(0.5f, _difficultyValues.ActionInterval * 3f);

        Debug.Log(
            $"Enemy AI detected oscillation. Temporarily avoiding Cell={position}",
            this);
    }

    private bool IsTemporarilyAvoided(Vector3Int position)
    {
        if (!_temporarilyAvoidedCell.HasValue)
            return false;

        if (Time.time >= _avoidCellUntil)
        {
            _temporarilyAvoidedCell = null;
            return false;
        }

        return position == _temporarilyAvoidedCell.Value;
    }

    /// <summary>
    /// 指定位置へBombを置いた場合、現在のBlock配置でPlayerまで爆風が届くかを返します。
    /// </summary>
    private bool CanBombHitPlayer(Vector3Int bombPosition, Vector3Int playerPosition)
    {
        if (!_gridManager.Contains(bombPosition) || _bombComponent.ExplosionPower <= 0)
            return false;

        IReadOnlyList<Vector3Int> affectedCells =
            ExplosionSystem.CalculateAffectedCells(
                _gridManager,
                bombPosition,
                _bombComponent.ExplosionPower);

        for (int i = 0; i < affectedCells.Count; i++)
        {
            if (affectedCells[i] == playerPosition)
                return true;
        }

        return false;
    }

    /// <summary>最大数に達していなければ、難易度別確率でBomb設置を試します。</summary>
    private bool TryPlaceBombByChance()
    {
        if (_bombComponent.CurrentBombCount >= _bombComponent.MaxBombCount)
            return false;

        if (Random.value > _difficultyValues.BombPlaceChance)
            return false;

        // Bombを仮置きした危険Mapで、安全地帯まで逃げ切れる場合だけ設置します。
        Vector3Int position = _movement.CurrentGridPosition;
        GridDangerMap virtualDangerMap = new GridDangerMap();
        virtualDangerMap.RebuildWithVirtualBomb(
            _gridManager,
            position,
            _bombComponent.ExplosionPower,
            _bombComponent.FuseTime);

        List<Vector3Int> escapePath = GridPathfindingSystem.FindPathToNearestSafeCell(
            _gridManager,
            virtualDangerMap,
            position,
            _movement.MoveDuration,
            _movement.JumpDuration,
            _difficultyValues.ActionInterval);

        if (escapePath.Count < 2)
            return false;

        return _bombComponent.TryPlaceBomb();
    }

    /// <summary>XYZ各軸の差を合計したグリッド上の距離を返します。</summary>
    private static int GetManhattanDistance(Vector3Int from, Vector3Int to)
    {
        Vector3Int difference = from - to;
        return Mathf.Abs(difference.x) +
               Mathf.Abs(difference.y) +
               Mathf.Abs(difference.z);
    }

    /// <summary>Fisher-Yates法で候補方向をランダムに並べ替えます。</summary>
    private static void Shuffle(List<Vector3Int> directions)
    {
        for (int i = directions.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            Vector3Int temporary = directions[i];
            directions[i] = directions[swapIndex];
            directions[swapIndex] = temporary;
        }
    }
}
