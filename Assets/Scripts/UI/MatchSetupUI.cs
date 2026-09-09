using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>事前配置したUIをイベントで制御します。非表示にするPanel自身には付けないでください。</summary>
public class MatchSetupUI : MonoBehaviour
{
    [SerializeField] private GridBomberGameMode _gameMode;
    [SerializeField] private GridBomberGameState _gameState;
    [SerializeField] private MatchSetupSettings _settings;
    [SerializeField] private GameObject _setupPanel;
    [SerializeField] private GameObject _playingPanel;
    [Tooltip("OptionsをEasy、Normal、Hardの順で事前設定してください。")]
    [SerializeField] private TMP_Dropdown _difficultyDropdown;
    [SerializeField] private Button _startButton;
    [SerializeField] private TMP_Text _controlsText;
    [SerializeField] private TMP_Text _errorText;
    private EnemyDifficulty _selectedDifficulty;
    private MatchSetupSettings _runtimeSettings;

    private void Awake()
    {
        if (_settings == null)
        {
            _runtimeSettings = ScriptableObject.CreateInstance<MatchSetupSettings>();
            _settings = _runtimeSettings;
        }
        if (_controlsText != null) _controlsText.text = _settings.ControlsText;
        if (_errorText != null) _errorText.text = string.Empty;
        if (_gameMode == null || _gameState == null || _setupPanel == null ||
            _difficultyDropdown == null || _difficultyDropdown.options.Count != 3 || _startButton == null)
        {
            Debug.LogError("MatchSetupUI: GameMode/GameState/Panel/Start Buttonと、3項目のDropdownを設定してください。", this);
            if (_startButton != null) _startButton.interactable = false;
            enabled = false;
        }
    }

    private void Start()
    {
        // 全ObjectのAwake後なので、Scene再読込で引き継いだ難易度も反映できます。
        _selectedDifficulty = _gameMode.SelectedDifficulty;
        _difficultyDropdown.SetValueWithoutNotify((int)_selectedDifficulty);
        ApplyState(_gameState.State);
    }

    private void OnEnable()
    {
        if (_difficultyDropdown != null) _difficultyDropdown.onValueChanged.AddListener(HandleDifficultyChanged);
        if (_startButton != null) _startButton.onClick.AddListener(HandleStartClicked);
        if (_gameState != null)
        {
            _gameState.StateChanged += ApplyState;
            ApplyState(_gameState.State);
        }
    }

    private void OnDisable()
    {
        if (_difficultyDropdown != null) _difficultyDropdown.onValueChanged.RemoveListener(HandleDifficultyChanged);
        if (_startButton != null) _startButton.onClick.RemoveListener(HandleStartClicked);
        if (_gameState != null) _gameState.StateChanged -= ApplyState;
    }

    private void HandleDifficultyChanged(int index)
    {
        if (index >= 0 && index <= 2) _selectedDifficulty = (EnemyDifficulty)index;
    }

    private void HandleStartClicked()
    {
        if (!_gameMode.CanStart) return;
        _startButton.interactable = false;
        if (_errorText != null) _errorText.text = string.Empty;
        if (!_gameMode.StartMatch(_selectedDifficulty))
        {
            if (_errorText != null) _errorText.text = _settings.StartFailedText;
            _startButton.interactable = _gameMode.CanStart;
        }
    }

    private void ApplyState(MatchState state)
    {
        bool waiting = state == MatchState.Waiting;
        if (_setupPanel != null) _setupPanel.SetActive(waiting);
        if (_playingPanel != null) _playingPanel.SetActive(!waiting);
        if (_difficultyDropdown != null) _difficultyDropdown.interactable = waiting;
        if (_startButton != null) _startButton.interactable = waiting && _gameMode != null && _gameMode.CanStart;
    }

    private void OnDestroy()
    {
        if (_runtimeSettings != null) Destroy(_runtimeSettings);
    }
}
