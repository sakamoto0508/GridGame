using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Playerへの簡易接近、徘徊、Bomb設置を行う最小構成のEnemy AIです。
/// 経路探索と爆風回避は後からGridPathfindingSystemとGridDangerMapで追加します。
/// </summary>
[RequireComponent(typeof(EnemyCharacter))]
[RequireComponent(typeof(MovementComponent))]
[RequireComponent(typeof(BombComponent))]
public class EnemyBrain : MonoBehaviour
{
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
    private MovementComponent _playerMovement;
    private EnemyDifficultyValues _difficultyValues;
    private float _nextActionTime;
    private bool _isInitialized;

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
        _playerMovement = player.GetComponent<MovementComponent>();

        if (gridManager == null || player == null ||
            _enemy == null || _movement == null || _bombComponent == null || settings == null)
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

        if (_playerMovement == null)
            return;

        Vector3Int playerPosition = _playerMovement.CurrentGridPosition;
        int distance = GetManhattanDistance(enemyPosition, playerPosition);
        bool detectedPlayer = distance <= _difficultyValues.DetectionRange;

        // Playerが近距離なら、難易度別確率でBomb設置を優先します。
        if (detectedPlayer && distance <= _difficultyValues.BombDistance && TryPlaceBombByChance())
            return;

        List<Vector3Int> candidates = CreateDirectionCandidates(
            enemyPosition,
            playerPosition,
            detectedPlayer);

        for (int i = 0; i < candidates.Count; i++)
        {
            if (_movement.TryMove(candidates[i]))
                return;
        }

        // 全方向を塞がれている場合は、Blockを壊すきっかけとしてBomb設置を試します。
        TryPlaceBombByChance();
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
            return candidates;
        }

        candidates.Sort((left, right) =>
        {
            int leftDistance = GetManhattanDistance(enemyPosition + left, playerPosition);
            int rightDistance = GetManhattanDistance(enemyPosition + right, playerPosition);
            return leftDistance.CompareTo(rightDistance);
        });

        return candidates;
    }

    /// <summary>最大数に達していなければ、難易度別確率でBomb設置を試します。</summary>
    private bool TryPlaceBombByChance()
    {
        if (_bombComponent.CurrentBombCount >= _bombComponent.MaxBombCount)
            return false;

        if (Random.value > _difficultyValues.BombPlaceChance)
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
