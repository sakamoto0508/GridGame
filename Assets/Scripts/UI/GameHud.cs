using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 試合中の生存人数と終了結果を表示し、現在Sceneのリスタートを受け付けます。
/// ゲームルールは持たず、GridBomberGameStateの通知だけを表示へ反映します。
/// </summary>
public class GameHud : MonoBehaviour
{
    [Header("Game State")]
    [SerializeField] private GridBomberGameState _gameState;
    [SerializeField] private GameHudSettings _settings;

    [Header("Playing UI")]
    [SerializeField] private TMP_Text _aliveCountText;

    [Header("Result UI")]
    [SerializeField] private GameObject _resultPanel;
    [SerializeField] private CanvasGroup _resultCanvasGroup;
    [SerializeField] private TMP_Text _resultText;
    [SerializeField] private Button _restartButton;

    private int _resultSequenceVersion;

    private void Awake()
    {
        if (_settings == null)
            Debug.LogError("GameHudのGame HUD Settingsが未設定です。", this);

        PrepareResultCanvasGroup();
        HideResult();

        UpdateAliveCount(_gameState != null ? _gameState.AliveCharacterCount : 0);
    }

    private void OnEnable()
    {
        if (_gameState != null)
        {
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
    }

    private void OnDisable()
    {
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
    }

    /// <summary>現在の生存Character数を表示します。</summary>
    private void UpdateAliveCount(int aliveCount)
    {
        if (_aliveCountText != null)
            _aliveCountText.text = _settings != null
                ? string.Format(_settings.AliveFormat, aliveCount)
                : $"ALIVE: {aliveCount}";
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
        if (_resultText != null)
        {
            if (winner == null)
                _resultText.text = _settings != null ? _settings.DrawText : "DRAW";
            else if (winner is PlayerCharacter)
                _resultText.text = _settings != null ? _settings.WinText : "YOU WIN";
            else
                _resultText.text = _settings != null ? _settings.LoseText : "YOU LOSE";
        }

        int sequenceVersion = ++_resultSequenceVersion;
        _ = ShowResultAsync(sequenceVersion);
    }

    /// <summary>指定時間待った後、Result Panelを透明状態から徐々に表示します。</summary>
    private async Awaitable ShowResultAsync(int sequenceVersion)
    {
        float elapsedTime = 0f;

        float resultDelay = _settings != null ? _settings.ResultDelay : 0f;
        float fadeDuration = _settings != null ? _settings.FadeDuration : 0f;

        while (elapsedTime < resultDelay)
        {
            // 試合終了時にTimeScaleを止めても結果UIを表示できるようにします。
            elapsedTime += Time.unscaledDeltaTime;
            await Awaitable.NextFrameAsync();

            if (!CanContinueResultSequence(sequenceVersion))
                return;
        }

        if (_resultPanel == null)
            return;

        _resultPanel.SetActive(true);

        if (_resultCanvasGroup == null)
            return;

        _resultCanvasGroup.alpha = 0f;
        _resultCanvasGroup.interactable = false;
        _resultCanvasGroup.blocksRaycasts = false;

        if (fadeDuration <= 0f)
        {
            CompleteResultFade();
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

        CompleteResultFade();
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

    /// <summary>CanvasGroupが未設定ならResult Panelから取得または自動追加します。</summary>
    private void PrepareResultCanvasGroup()
    {
        if (_resultCanvasGroup != null || _resultPanel == null)
            return;

        _resultCanvasGroup = _resultPanel.GetComponent<CanvasGroup>();

        if (_resultCanvasGroup == null)
            _resultCanvasGroup = _resultPanel.AddComponent<CanvasGroup>();
    }

    /// <summary>現在の非同期表示処理がまだ有効か確認します。</summary>
    private bool CanContinueResultSequence(int sequenceVersion)
    {
        return this != null &&
               isActiveAndEnabled &&
               sequenceVersion == _resultSequenceVersion;
    }

    /// <summary>完全表示にしてResult Panel内のUI操作を有効化します。</summary>
    private void CompleteResultFade()
    {
        _resultCanvasGroup.alpha = 1f;
        _resultCanvasGroup.interactable = true;
        _resultCanvasGroup.blocksRaycasts = true;
    }

    /// <summary>現在開いているSceneを読み込み直して試合を最初から開始します。</summary>
    public void RestartGame()
    {
        Scene activeScene = SceneManager.GetActiveScene();

        if (!activeScene.IsValid())
        {
            Debug.LogError("現在Sceneを取得できないため、リスタートできません。", this);
            return;
        }

        SceneManager.LoadScene(activeScene.name);
    }
}
