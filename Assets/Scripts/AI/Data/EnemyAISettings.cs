using System;
using UnityEngine;

/// <summary>1難易度分のEnemy AI調整値です。</summary>
[Serializable]
public class EnemyDifficultyValues
{
    // Inspectorの下限値です。プロパティでも同じ値で補正し、古いAssetの不正値を防ぎます。
    private const int MinSameCellDecisions = 2;
    private const float MinIntervalSeconds = 0.05f;
    private const float MinItemPlanTimeout = 0.1f;

    [SerializeField, Min(0.05f)] private float _actionInterval = 0.55f;
    [SerializeField, Range(0f, 1f)] private float _mistakeChance = 0.12f;
    [SerializeField, Range(0f, 1f)] private float _bombPlaceChance = 0.4f;
    [SerializeField, Min(0)] private int _detectionRange = 6;
    [SerializeField, Min(0)] private int _bombDistance = 2;

    [Header("Stuck Recovery")]
    [SerializeField, Min(2)] private int _maxSameCellDecisions = 3;
    [SerializeField, Min(0.05f)] private float _reconsiderPause = 0.6f;
    [SerializeField, Min(0.05f)] private float _escapeSafeConfirmationTime = 0.6f;

    [Header("Item Collection")]
    [SerializeField] private bool _collectItems = true;
    [SerializeField, Min(0.05f)] private float _itemSearchInterval = 0.8f;
    [SerializeField, Min(0.05f)] private float _itemRetryDelay = 2f;
    [SerializeField, Min(0.1f)] private float _itemPlanTimeout = 10f;

    public float ActionInterval => _actionInterval;
    public float MistakeChance => _mistakeChance;
    public float BombPlaceChance => _bombPlaceChance;
    public int DetectionRange => _detectionRange;
    public int BombDistance => _bombDistance;
    public int MaxSameCellDecisions => Mathf.Max(MinSameCellDecisions, _maxSameCellDecisions);
    public float ReconsiderPause => Mathf.Max(MinIntervalSeconds, _reconsiderPause);
    public float EscapeSafeConfirmationTime => Mathf.Max(MinIntervalSeconds, _escapeSafeConfirmationTime);
    public bool CollectItems => _collectItems;
    public float ItemSearchInterval => Mathf.Max(MinIntervalSeconds, _itemSearchInterval);
    public float ItemRetryDelay => Mathf.Max(MinIntervalSeconds, _itemRetryDelay);
    public float ItemPlanTimeout => Mathf.Max(MinItemPlanTimeout, _itemPlanTimeout);

    public EnemyDifficultyValues() { }

    public EnemyDifficultyValues(
        float actionInterval,
        float mistakeChance,
        float bombPlaceChance,
        int detectionRange,
        int bombDistance)
    {
        _actionInterval = actionInterval;
        _mistakeChance = mistakeChance;
        _bombPlaceChance = bombPlaceChance;
        _detectionRange = detectionRange;
        _bombDistance = bombDistance;
    }
}

/// <summary>Easy、Normal、HardのEnemy AI調整値をまとめます。</summary>
[CreateAssetMenu(fileName = "EnemyAISettings", menuName = "3D Grid Bomber/Settings/Enemy AI")]
public class EnemyAISettings : ScriptableObject
{
    // 新規Asset作成時の初期値です（行動間隔, ミス率, 爆弾設置率, 索敵距離, 爆弾設置距離）。
    // Hardの索敵距離999は「実質無制限」を表します。
    [SerializeField] private EnemyDifficultyValues _easy = new EnemyDifficultyValues(0.9f, 0.35f, 0.2f, 3, 1);
    [SerializeField] private EnemyDifficultyValues _normal = new EnemyDifficultyValues(0.55f, 0.12f, 0.4f, 6, 2);
    [SerializeField] private EnemyDifficultyValues _hard = new EnemyDifficultyValues(0.3f, 0f, 0.65f, 999, 2);

    public EnemyDifficultyValues Easy => _easy;
    public EnemyDifficultyValues Normal => _normal;
    public EnemyDifficultyValues Hard => _hard;

    public EnemyDifficultyValues GetValues(EnemyDifficulty difficulty)
    {
        switch (difficulty)
        {
            case EnemyDifficulty.Easy:
                return Easy;

            case EnemyDifficulty.Hard:
                return Hard;

            default:
                return Normal;
        }
    }
}
