using System;
using UnityEngine;

/// <summary>1難易度分のEnemy AI調整値です。</summary>
[Serializable]
public class EnemyDifficultyValues
{
    [SerializeField, Min(0.05f)] private float _actionInterval = 0.55f;
    [SerializeField, Range(0f, 1f)] private float _mistakeChance = 0.12f;
    [SerializeField, Range(0f, 1f)] private float _bombPlaceChance = 0.4f;
    [SerializeField, Min(0)] private int _detectionRange = 6;
    [SerializeField, Min(0)] private int _bombDistance = 2;

    public float ActionInterval => _actionInterval;
    public float MistakeChance => _mistakeChance;
    public float BombPlaceChance => _bombPlaceChance;
    public int DetectionRange => _detectionRange;
    public int BombDistance => _bombDistance;

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
    [SerializeField] private EnemyDifficultyValues _easy =
        new EnemyDifficultyValues(0.9f, 0.35f, 0.2f, 3, 1);
    [SerializeField] private EnemyDifficultyValues _normal =
        new EnemyDifficultyValues(0.55f, 0.12f, 0.4f, 6, 2);
    [SerializeField] private EnemyDifficultyValues _hard =
        new EnemyDifficultyValues(0.3f, 0f, 0.65f, 999, 2);

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
