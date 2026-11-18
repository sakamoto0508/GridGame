using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 生存人数・Playerの能力/座標・終了結果を表示し、Sceneのリスタートを受け付けます。
/// ゲームルールは持たず、試合通知とPlayerの状態を表示へ反映します。
/// </summary>
public class GameHud : MonoBehaviour
{
    /// <summary>数値を2桁ゼロ埋めで表示する書式です。</summary>
    private const string TwoDigitFormat = "00";

    // Playerが未設定のときに表示するプレースホルダーです。
    private const string EmptyValueText = "--";
    private const string EmptyPositionText = "-- / -- / --";

    [Header("Game State")]
    [SerializeField] private GridBomberGameState _gameState;
    [SerializeField] private GameHudSettings _settings;

    [Header("Playing UI")]
    [SerializeField] private TMP_Text _aliveCountText;
    [Tooltip("Sceneに配置したTMP Textを指定してください。UIは自動生成しません。")]
    [SerializeField] private TMP_Text _playerStatusText;

    [Header("NEON HUD（任意・Editor配置）")]
    [SerializeField] private TMP_Text _aliveValueText;
    [SerializeField] private TMP_Text _powerValueText;
    [SerializeField] private TMP_Text _limitValueText;
    [SerializeField] private TMP_Text _placedValueText;
    [SerializeField] private TMP_Text _positionValueText;

    [Header("Result UI")]
    [SerializeField] private GameObject _resultPanel;
    [SerializeField] private CanvasGroup _resultCanvasGroup;
    [SerializeField] private TMP_Text _resultText;
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _returnToSetupButton;
    [SerializeField] private GridBomberGameMode _gameMode;

    private int _resultSequenceVersion;
    private PlayerCharacter _player;
    private BombComponent _playerBombs;
    private MovementComponent _playerMovement;
    private int _lastPower = -1;
    private int _lastLimit = -1;
    private int _lastPlaced = -1;
    private Vector3Int _lastPosition;
    private string _lastStatusFormat;
    private bool _statusValid;
    private GameHudSettings _runtimeSettings;

    private void Awake()
    {
        // 以降の処理は_settingsがnullでない前提です。未設定ならここで既定値のSettingsを作ります。
        if (_settings == null)
        {
            Debug.LogWarning("GameHudのSettingsが未設定のため既定値を使用します。", this);
            _runtimeSettings = ScriptableObject.CreateInstance<GameHudSettings>();
            _settings = _runtimeSettings;
        }

        PrepareResultCanvasGroup();

        if (_playerStatusText == null)
            Debug.LogWarning("GameHudのPlayer Status Textに、SceneのTMP Textを設定してください。", this);

        HideResult();

        UpdateAliveCount(_gameState != null ? _gameState.AliveCharacterCount : 0);
    }

    /// <summary>GameModeが生成したPlayerを渡します。Enemyを誤って表示対象にしません。</summary>
    public void BindPlayer(PlayerCharacter player)
    {
        UnsubscribePlayer();
        _player = player;
        _playerBombs = player != null ? player.GetComponent<BombComponent>() : null;
        _playerMovement = player != null ? player.GetComponent<MovementComponent>() : null;
        _statusValid = false;

        if (isActiveAndEnabled)
            SubscribePlayer();

        RefreshPlayerStatus();
    }

    private void SubscribePlayer()
    {
        // BindとOnEnableの両方から呼ばれても二重購読しません。
        UnsubscribePlayer();

        if (_playerBombs != null)
            _playerBombs.StatsChanged += RefreshPlayerStatus;

        if (_playerMovement != null)
            _playerMovement.GridPositionChanged += RefreshPlayerStatus;
    }

    private void UnsubscribePlayer()
    {
        if (_playerBombs != null)
            _playerBombs.StatsChanged -= RefreshPlayerStatus;

        if (_playerMovement != null)
            _playerMovement.GridPositionChanged -= RefreshPlayerStatus;
    }

    /// <summary>値を比較し、変化したときだけ文字列を更新。死亡後も最後の座標と残存Bomb数を表示します。</summary>
    private void RefreshPlayerStatus()
    {
        if (_player == null || _playerBombs == null || _playerMovement == null)
        {
            if (_playerStatusText != null)
                _playerStatusText.text = string.Empty;

            SetValue(_powerValueText, EmptyValueText);
            SetValue(_limitValueText, EmptyValueText);
            SetValue(_placedValueText, EmptyValueText);
            SetValue(_positionValueText, EmptyPositionText);
            _statusValid = false;
            return;
        }

        int power = _playerBombs.ExplosionPower;
        int limit = _playerBombs.MaxBombCount;
        int placed = _playerBombs.CurrentBombCount;
        Vector3Int position = _playerMovement.CurrentGridPosition;
        string format = _settings.PlayerStatusFormat ?? string.Empty;

        bool unchanged =
            _statusValid &&
            power == _lastPower &&
            limit == _lastLimit &&
            placed == _lastPlaced &&
            position == _lastPosition &&
            format == _lastStatusFormat;
        if (unchanged)
            return;

        _lastPower = power;
        _lastLimit = limit;
        _lastPlaced = placed;
        _lastPosition = position;
        _lastStatusFormat = format;
        _statusValid = true;

        SetValue(_powerValueText, power.ToString(TwoDigitFormat));
        SetValue(_limitValueText, limit.ToString(TwoDigitFormat));
        SetValue(_placedValueText, placed.ToString(TwoDigitFormat));
        SetValue(_positionValueText, $"{position.x:00} / {position.y:00} / {position.z:00}");

        if (_playerStatusText == null || !_playerStatusText.gameObject.activeSelf)
            return;

        try
        {
            _playerStatusText.text = string.Format(
                format,
                power,
                limit,
                placed,
                position.x,
                position.y,
                position.z);
        }
        catch (System.FormatException)
        {
            // 書式が壊れていても最低限の情報は表示します（ゲーム内UIは英語表記）。
            Debug.LogWarning("Player Status Formatの書式が不正です。{0}～{5}を使用してください。", this);
            _playerStatusText.text =
                $"RANGE: {power}\n" +
                $"BOMB LIMIT: {limit}\n" +
                $"BOMBS PLACED: {placed}\n" +
                $"GRID: {position}";
        }
    }

    private void OnDestroy()
    {
        UnsubscribePlayer();

        if (_runtimeSettings != null)
            Destroy(_runtimeSettings);
    }

    private void OnEnable()
    {
        SubscribePlayer();
        _statusValid = false;
        RefreshPlayerStatus();

        if (_gameState != null)
        {
            UpdateAliveCount(_gameState.AliveCharacterCount);
            _gameState.StateChanged += HandleStateChanged;
            _gameState.MatchFinished += HandleMatchFinished;
            _gameState.AliveCharacterCountChanged += UpdateAliveCount;
        }
        else
        {
            Debug.LogError("GameHudのGame Stateが未設定です。", this);
        }

        if (_restartButton != null)
            _restartButton.onClick.AddListener(RestartGame);

        if (_returnToSetupButton != null)
            _returnToSetupButton.onClick.AddListener(ReturnToSetup);
    }

    private void OnDisable()
    {
        UnsubscribePlayer();
        // 待機中またはフェード中の非同期処理を無効化します。
        _resultSequenceVersion++;

        if (_gameState != null)
        {
            _gameState.StateChanged -= HandleStateChanged;
            _gameState.MatchFinished -= HandleMatchFinished;
            _gameState.AliveCharacterCountChanged -= UpdateAliveCount;
        }

        if (_restartButton != null)
            _restartButton.onClick.RemoveListener(RestartGame);

        if (_returnToSetupButton != null)
            _returnToSetupButton.onClick.RemoveListener(ReturnToSetup);
    }

    /// <summary>現在の生存Character数を表示します。</summary>
    private void UpdateAliveCount(int aliveCount)
    {
        SetValue(_aliveValueText, aliveCount.ToString(TwoDigitFormat));

        if (_aliveCountText != null)
            _aliveCountText.text = string.Format(_settings.AliveFormat, aliveCount);
    }

    private static void SetValue(TMP_Text text, string value)
    {
        if (text != null && text.text != value)
            text.text = value;
    }

    /// <summary>新しい試合が始まったとき、前回の結果表示を閉じます。</summary>
    private void HandleStateChanged(MatchState state)
    {
        if (state == MatchState.Playing)
            HideResult();
    }

    /// <summary>勝者がPlayerかどうかを判定し、勝敗または引き分けを表示します。</summary>
    private void HandleMatchFinished(CharacterBase winner)
    {
        // 勝者の参照が後から消えても結果を維持し、SEはフェード完了まで待ちます。
        SoundId? resultSound = winner == null ? (SoundId?)null
            : winner is PlayerCharacter ? SoundId.Win
            : SoundId.Lose;

        if (_resultText != null)
        {
            if (winner == null)
                _resultText.text = _settings.DrawText;
            else if (winner is PlayerCharacter)
                _resultText.text = _settings.WinText;
            else
                _resultText.text = _settings.LoseText;
        }

        int sequenceVersion = ++_resultSequenceVersion;
        _ = ShowResultAsync(sequenceVersion, resultSound);
    }

    /// <summary>指定時間待った後、Result Panelを透明状態から徐々に表示します。</summary>
    private async Awaitable ShowResultAsync(int sequenceVersion, SoundId? resultSound)
    {
        float elapsedTime = 0f;

        float resultDelay = _settings.ResultDelay;
        float fadeDuration = _settings.FadeDuration;

        while (elapsedTime < resultDelay)
        {
            // 試合終了時にTimeScaleを止めても結果UIを表示できるようにします。
            elapsedTime += Time.unscaledDeltaTime;
            await Awaitable.NextFrameAsync();

            if (!CanContinueResultSequence(sequenceVersion))
                return;
        }

        if (!CanContinueResultSequence(sequenceVersion) || _resultPanel == null)
            return;

        _resultPanel.SetActive(true);

        if (_resultCanvasGroup == null)
            return;

        _resultCanvasGroup.alpha = 0f;
        _resultCanvasGroup.interactable = false;
        _resultCanvasGroup.blocksRaycasts = false;

        if (fadeDuration <= 0f)
        {
            CompleteResultFade(sequenceVersion, resultSound);
            return;
        }

        elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            _resultCanvasGroup.alpha = Mathf.Clamp01(elapsedTime / fadeDuration);
            await Awaitable.NextFrameAsync();

            if (!CanContinueResultSequence(sequenceVersion))
                return;
        }

        CompleteResultFade(sequenceVersion, resultSound);
    }

    /// <summary>Result Panelを非表示にして操作も無効化します。</summary>
    private void HideResult()
    {
        _resultSequenceVersion++;

        if (_resultCanvasGroup != null)
        {
            _resultCanvasGroup.alpha = 0f;
            _resultCanvasGroup.interactable = false;
            _resultCanvasGroup.blocksRaycasts = false;
        }

        if (_resultPanel != null)
            _resultPanel.SetActive(false);
    }

    /// <summary>CanvasGroupが未指定なら配置済みのものを取得します。自動追加はしません。</summary>
    private void PrepareResultCanvasGroup()
    {
        if (_resultCanvasGroup != null || _resultPanel == null)
            return;

        _resultCanvasGroup = _resultPanel.GetComponent<CanvasGroup>();

        if (_resultCanvasGroup == null)
            Debug.LogWarning("Result PanelにCanvasGroupを追加し、GameHudへ設定してください。", this);
    }

    /// <summary>現在の非同期表示処理がまだ有効か確認します。</summary>
    private bool CanContinueResultSequence(int sequenceVersion)
    {
        return this != null &&
               isActiveAndEnabled &&
               sequenceVersion == _resultSequenceVersion;
    }

    /// <summary>完全表示にして操作を有効化し、勝敗SEを再生します。中断済みなら鳴らしません。</summary>
    private void CompleteResultFade(int sequenceVersion, SoundId? resultSound)
    {
        if (!CanContinueResultSequence(sequenceVersion) || _resultCanvasGroup == null)
            return;

        _resultCanvasGroup.alpha = 1f;
        _resultCanvasGroup.interactable = true;
        _resultCanvasGroup.blocksRaycasts = true;

        if (resultSound.HasValue)
            AudioManager.Play(resultSound.Value);
    }

    /// <summary>現在開いているSceneを読み込み直して試合を最初から開始します。</summary>
    public void RestartGame()
    {
        Reload(true);
    }

    public void ReturnToSetup() => Reload(false);

    private void Reload(bool autoStart)
    {
        if (_gameMode == null)
            _gameMode = FindFirstObjectByType<GridBomberGameMode>();

        if (_gameMode != null)
            _gameMode.ReloadMatch(autoStart);
        else
            Debug.LogWarning("再読込にはGameModeを指定してください。", this);
    }
}
