using UnityEngine;
using UnityEngine.SceneManagement;

public class GridBomberGameMode : MonoBehaviour
{
    [SerializeField] private StageGenerator _stageGenerator;
    [SerializeField] private CharacterSpawner _characterSpawner;
    [SerializeField] private GridBomberGameState _gameState;
    [SerializeField] private GameHud _gameHud;
    [Header("End Phase")]
    [SerializeField] private EndPhaseSettings _endPhaseSettings;

    [Header("Enemy AI")]
    [SerializeField] private EnemyDifficulty _enemyDifficulty = EnemyDifficulty.Normal;
    [SerializeField] private EnemyAISettings _enemyAISettings;
    private bool _startAttempted;
    private bool _autoStart;
    private bool _reloadRequested;
    public EnemyDifficulty SelectedDifficulty => _enemyDifficulty;
    public bool CanStart => !_startAttempted && _gameState != null && _gameState.State == MatchState.Waiting;

    private void Awake()
    {
        if (MatchLaunchRequest.Consume(gameObject.scene.path, out EnemyDifficulty difficulty, out bool autoStart))
        {
            _enemyDifficulty = difficulty;
            _autoStart = autoStart;
        }
    }

    /// <summary>初回は設定画面で待機。リトライ要求のときだけ自動開始します。</summary>
    private void Start()
    {
        if (_autoStart) StartMatch(_enemyDifficulty);
    }

    /// <summary>設定画面で選ばれた難易度により、1回だけステージとCharacterを生成します。</summary>
    public bool StartMatch(EnemyDifficulty difficulty)
    {
        if (!CanStart) return false;
        if (!System.Enum.IsDefined(typeof(EnemyDifficulty), difficulty) ||
            _stageGenerator == null || _characterSpawner == null || _enemyAISettings == null)
        {
            Debug.LogError(
                "GameModeを開始できません: StageGenerator、CharacterSpawner、GameStateを設定してください。",
                this);
            return false;
        }

        _enemyDifficulty = difficulty;
        // 生成途中の失敗でも連打で二重生成させません。再試行はScene再読込で行います。
        _startAttempted = true;

        if (!_stageGenerator.GenerateStage()) return false;

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
            if (player != null) player.gameObject.SetActive(false);
            if (testEnemy != null) testEnemy.gameObject.SetActive(false);
            return false;
        }

        // HUDは生成済みPlayerの実際の能力/位置を表示します。Sceneに1つなら未指定でも取得。
        if (_gameHud == null) _gameHud = FindFirstObjectByType<GameHud>();
        if (_gameHud != null) _gameHud.BindPlayer(player);

        GridManager grid = _stageGenerator.Grid;
        if (grid != null)
        {
            EndPhaseManager endPhase = grid.GetComponent<EndPhaseManager>();
            if (endPhase == null) endPhase = grid.gameObject.AddComponent<EndPhaseManager>();
            endPhase.Init(grid, _gameState, _endPhaseSettings, _stageGenerator.UnbreakableBlockPrefab);
        }
        return _gameState.StartMatch(player, testEnemy);
    }

    /// <summary>再戦は同じ難易度で即開始。設定へ戻る場合は次のSceneでWaitingを維持します。</summary>
    public void ReloadMatch(bool autoStart)
    {
        if (_reloadRequested) return;
        Scene scene = gameObject.scene;
        if (!scene.IsValid() || string.IsNullOrEmpty(scene.path))
        {
            Debug.LogError("Sceneを保存し、Build ProfilesのScene Listへ追加してください。", this);
            return;
        }
        _reloadRequested = true;
        MatchLaunchRequest.Set(scene.path, _enemyDifficulty, autoStart);
        float previousTimeScale = Time.timeScale;
        Time.timeScale = 1f;
        try { SceneManager.LoadScene(scene.path); }
        catch (System.Exception exception)
        {
            _reloadRequested = false;
            MatchLaunchRequest.Clear();
            Time.timeScale = previousTimeScale;
            Debug.LogException(exception, this);
        }
    }
}
