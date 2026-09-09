using System.Collections.Generic;
using UnityEngine;

/// <summary>Enemy AIが現在何を目的に行動しているかを表します。</summary>
public enum EnemyAIState
{
    Idle,
    Chase,
    MoveToAttackPosition,
    PlaceBomb,
    Escape,
    CollectItem
}

/// <summary>
/// 危険回避、Item取得、Playerへの接近・攻撃を優先順に判断します。
/// 経路探索はGridPathfindingSystem、爆発予測はGridDangerMapへ委譲します。
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
    private readonly List<GridPathStep> _attackPath = new List<GridPathStep>();
    private Vector3Int _plannedPlayerPosition;
    private bool _hasAttackPlan;
    private bool _isWaitingForPlacedBlockJump;
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
    private Item _itemTarget;
    private int _itemTargetVersion;
    private Vector3Int _itemTargetPosition;
    private readonly List<GridPathStep> _itemPath = new();
    private float _itemPlanUntil;
    private float _nextItemSearch;

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
            ClearItemPlan(true);
            ClearAttackPath();
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

        // 行き詰まり後の待機中でも上の危険判定は実行し、爆風回避を止めません。
        if (Time.time < _reconsiderUntil) return;

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

        // Block設置後の複合行動は、ほかの攻撃判断を挟まず次のジャンプまで完了させます。
        if (_isWaitingForPlacedBlockJump)
        {
            SetState(EnemyAIState.MoveToAttackPosition);
            TryFollowAttackPath(enemyPosition, playerPosition);
            return;
        }

        // 逃走と実行中の設置→ジャンプの完了後に、安全なItem取得を攻撃より優先します。
        if (TryCollectItem(enemyPosition)) return;

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
                ClearAttackPath();
                SetState(EnemyAIState.Escape);
            }

            // すでに攻撃位置なので、確率判定に外れた回に別の攻撃位置へ移動し直しません。
            return;
        }

        // Player検知中の追跡は、攻撃可能セルをゴールにしたA*の行動列を使います。
        // 経路を毎回選び直さず保持するため、高低差による局所的な往復も抑えられます。
        if (detectedPlayer)
        {
            SetState(EnemyAIState.MoveToAttackPosition);
            if (TryFollowAttackPath(enemyPosition, playerPosition))
                return;

            // 攻撃位置への経路がない場合だけ、その場のBlockを壊すBombを検討します。
            SetState(EnemyAIState.PlaceBomb);
            if (TryPlaceBombByChance())
            {
                ClearAttackPath();
                SetState(EnemyAIState.Escape);
            }
            else
            {
                SetState(EnemyAIState.Chase);
            }
            return;
        }

        ClearAttackPath();
        SetState(EnemyAIState.Idle);

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
        // 全方向を塞がれている場合は、Blockを壊すきっかけとしてBomb設置を試します。
        SetState(EnemyAIState.PlaceBomb);
        if (TryPlaceBombByChance())
            SetState(EnemyAIState.Escape);
        else
            SetState(detectedPlayer ? EnemyAIState.Chase : EnemyAIState.Idle);
    }

    /// <summary>
    /// 攻撃可能セルまでのA*経路を保持し、1回の思考につき1行動だけ実行します。
    /// PlaceBlockAndJumpはBlock設置とジャンプの2回に分けて実行します。
    /// </summary>
    private bool TryFollowAttackPath(Vector3Int currentPosition, Vector3Int playerPosition)
    {
        bool targetChanged = !_hasAttackPlan || _plannedPlayerPosition != playerPosition;
        bool nextBecameDangerous = _attackPath.Count > 0 &&
                                   _dangerMap.IsDangerous(_attackPath[0].Position);

        if (targetChanged || nextBecameDangerous ||
            (_attackPath.Count == 0 && !_isWaitingForPlacedBlockJump))
        {
            ClearAttackPath();
            _plannedPlayerPosition = playerPosition;
            _hasAttackPlan = true;
            _attackPath.AddRange(GridPathfindingSystem.FindPathToAttackPosition(
                _gridManager,
                _dangerMap,
                currentPosition,
                playerPosition,
                _bombComponent.ExplosionPower,
                _difficultyValues.BombDistance,
                _movement.MoveDuration,
                _movement.JumpDuration,
                _movement.FallDurationPerCell,
                _difficultyValues.ActionInterval));
        }

        if (_attackPath.Count == 0)
            return false;

        GridPathStep step = _attackPath[0];

        if (_isWaitingForPlacedBlockJump)
        {
            if (step.Action == GridPathActionType.PlaceBlockAndJump &&
                _movement.TryJump(step.Direction))
            {
                RememberMove(currentPosition);
                _attackPath.RemoveAt(0);
                _isWaitingForPlacedBlockJump = false;
                return true;
            }

            ClearAttackPath();
            return false;
        }

        bool succeeded;
        switch (step.Action)
        {
            case GridPathActionType.Move:
                succeeded = _movement.TryMove(step.Direction);
                break;

            case GridPathActionType.JumpUp:
                succeeded = _movement.TryJump(step.Direction);
                break;

            case GridPathActionType.MoveAndFall:
                succeeded = _movement.TryMoveAndFall(step.Direction);
                break;

            case GridPathActionType.PlaceBlockAndJump:
                succeeded = _movement.TryFace(step.Direction) &&
                            _blockPlacement.TryPlaceBlock();
                if (succeeded)
                    _isWaitingForPlacedBlockJump = true;
                return succeeded;

            default:
                succeeded = false;
                break;
        }

        if (!succeeded)
        {
            ClearAttackPath();
            return false;
        }

        RememberMove(currentPosition);
        _attackPath.RemoveAt(0);
        return true;
    }

    private void ClearAttackPath()
    {
        _attackPath.Clear();
        _hasAttackPlan = false;
        _isWaitingForPlacedBlockJump = false;
    }

    /// <summary>有効な目標は固定し、新しく近いItemが現れても途中で目標を変えません。</summary>
    private bool TryCollectItem(Vector3Int current)
    {
        if (!_difficultyValues.CollectItems)
        {
            ClearItemPlan(false);
            return false;
        }
        if (_itemTarget != null &&
            (_itemTarget.SpawnVersion != _itemTargetVersion ||
             _itemTarget.GridPosition != _itemTargetPosition || !CanTargetItem(_itemTarget) ||
             Time.time >= _itemPlanUntil))
        {
            ClearItemPlan(true);
            return false;
        }

        if (_itemTarget == null)
        {
            _itemPath.Clear();
            if (Time.time < _nextItemSearch) return false;
            _nextItemSearch = Time.time + _difficultyValues.ItemSearchInterval;
            float bestTime = float.PositiveInfinity;
            Vector3Int size = _gridManager.Size;
            for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
            for (int z = 0; z < size.z; z++)
            {
                Vector3Int position = new Vector3Int(x, y, z);
                if (GetManhattanDistance(current, position) > _difficultyValues.DetectionRange) continue;
                Item item = _gridManager.GetItem(position);
                if (!CanTargetItem(item)) continue;
                List<GridPathStep> path = GridPathfindingSystem.FindSafePathToItem(
                    _gridManager, _dangerMap, current, position, _movement.MoveDuration,
                    _movement.JumpDuration, _movement.FallDurationPerCell,
                    _difficultyValues.ActionInterval, out float travelTime);
                if (travelTime >= bestTime || travelTime > _difficultyValues.ItemPlanTimeout ||
                    (path.Count == 0 && position != current)) continue;
                bestTime = travelTime;
                _itemTarget = item;
                _itemTargetVersion = item.SpawnVersion;
                _itemTargetPosition = position;
                _itemPath.Clear();
                _itemPath.AddRange(path);
            }
            if (_itemTarget == null) return false;
            _itemPlanUntil = Time.time + _difficultyValues.ItemPlanTimeout;
            ClearAttackPath();
        }

        if (current == _itemTargetPosition)
        {
            bool collected = _itemTarget.TryCollect(_enemy);
            ClearItemPlan(!collected);
            if (collected) SetState(EnemyAIState.CollectItem);
            return collected;
        }
        while (_itemPath.Count > 0 && _itemPath[0].Position == current) _itemPath.RemoveAt(0);

        // 毎思考で残り経路を再検証。移動先だけでなく途中の降下列や足場の変化も確認します。
        Vector3Int cursor = current;
        float arrival = 0f;
        foreach (GridPathStep step in _itemPath)
        {
            if (!GridPathfindingSystem.IsSafeItemStep(_gridManager, _dangerMap, cursor, step,
                    _movement.MoveDuration, _movement.JumpDuration, _movement.FallDurationPerCell,
                    _difficultyValues.ActionInterval, arrival, out float duration))
            {
                ClearItemPlan(true);
                return false;
            }
            cursor = step.Position;
            arrival += duration;
        }
        if (_itemPath.Count == 0)
        {
            ClearItemPlan(true);
            return false;
        }

        GridPathStep next = _itemPath[0];
        bool moved = next.Action == GridPathActionType.Move ? _movement.TryMove(next.Direction) :
            next.Action == GridPathActionType.JumpUp ? _movement.TryJump(next.Direction) :
            next.Action == GridPathActionType.MoveAndFall && _movement.TryMoveAndFall(next.Direction);
        if (!moved)
        {
            ClearItemPlan(true);
            return false;
        }
        SetState(EnemyAIState.CollectItem);
        RememberMove(current);
        return true;
    }

    /// <summary>着地済み・取得による強化あり・危険予測なしのItemだけを対象にします。</summary>
    private bool CanTargetItem(Item item)
    {
        if (item == null || !item.IsAvailable || item.IsFalling || item.Settings == null ||
            _gridManager.GetItem(item.GridPosition) != item ||
            !_gridManager.HasBlock(item.GridPosition + Vector3Int.down) ||
            _dangerMap.IsDangerous(item.GridPosition)) return false;
        InventoryComponent inventory = _enemy.GetComponent<InventoryComponent>();
        int bonus = inventory == null ? 0 : item.Settings.Type == ItemType.BombPower ?
            inventory.BombPowerBonus : inventory.BombCountBonus;
        // 上限品は消費する目的で追わず、強化できるItemを優先します。
        return bonus < item.Settings.MaxBonus;
    }

    private void ClearItemPlan(bool retryDelay)
    {
        _itemTarget = null;
        _itemPath.Clear();
        if (retryDelay)
            _nextItemSearch = Time.time + _difficultyValues.ItemRetryDelay;
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
        ClearItemPlan(true);
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
