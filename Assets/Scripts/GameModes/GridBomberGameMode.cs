using UnityEngine;

public class GridBomberGameMode : MonoBehaviour
{
    [SerializeField] private StageGenerator _stageGenerator;
    [SerializeField] private CharacterSpawner _characterSpawner;
    [SerializeField] private GridBomberGameState _gameState;

    [Header("Enemy AI")]
    [SerializeField] private EnemyDifficulty _enemyDifficulty = EnemyDifficulty.Normal;
    [SerializeField] private EnemyAISettings _enemyAISettings;

    /// <summary>ステージ生成後にPlayerと簡易AI Enemyを生成し、試合へ登録します。</summary>
    private void Start()
    {
        if (_stageGenerator == null || _characterSpawner == null || _gameState == null)
        {
            Debug.LogError(
                "GameModeを開始できません: StageGenerator、CharacterSpawner、GameStateを設定してください。",
                this);
            return;
        }

        _stageGenerator.GenerateStage();

        PlayerCharacter player =
            _characterSpawner.SpawnPlayer(_stageGenerator.PlayerSpawnPosition);
        EnemyCharacter testEnemy =
            _characterSpawner.SpawnTestEnemy(
                _stageGenerator.EnemySpawnPosition,
                player,
                _enemyDifficulty,
                _enemyAISettings);

        if (player == null || testEnemy == null)
        {
            Debug.LogError("Character生成に失敗したため、試合を開始しません。", this);
            return;
        }

        _gameState.StartMatch(player, testEnemy);
    }
}
