using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

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
    [Header("横並び難易度（3個設定するとDropdownより優先）")]
    [SerializeField] private Button _easyButton;
    [SerializeField] private Button _normalButton;
    [SerializeField] private Button _hardButton;
    [SerializeField] private CyberpunkUITheme _theme;
    [SerializeField] private Button _startButton;
    [SerializeField] private TMP_Text _controlsText;
    [SerializeField] private TMP_Text _errorText;
    private EnemyDifficulty _selectedDifficulty;
    private MatchSetupSettings _runtimeSettings;
    [Header("メニュー専用Input Actions（空ならWASD/矢印/Enterを設定）")]
    [SerializeField] private InputAction _menuMove = new InputAction("MenuMove", InputActionType.Value, expectedControlType: "Vector2");
    [SerializeField] private InputAction _menuSubmit = new InputAction("MenuSubmit", InputActionType.Button);
    private EventSystem _navigationSystem;
    private bool _previousNavigation;
    private bool _startFocused;
    private bool HasDifficultyButtons => _easyButton != null && _normalButton != null && _hardButton != null;

    private void Awake()
    {
        if (_menuMove.bindings.Count == 0)
        {
            _menuMove.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            _menuMove.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        }
        if (_menuSubmit.bindings.Count == 0)
        {
            _menuSubmit.AddBinding("<Keyboard>/enter");
            _menuSubmit.AddBinding("<Keyboard>/numpadEnter");
        }
        if (_settings == null)
        {
            _runtimeSettings = ScriptableObject.CreateInstance<MatchSetupSettings>();
            _settings = _runtimeSettings;
        }
        if (_controlsText != null) _controlsText.text = _settings.ControlsText;
        if (_errorText != null) _errorText.text = string.Empty;
        if (_gameMode == null || _gameState == null || _setupPanel == null ||
            (!HasDifficultyButtons && (_difficultyDropdown == null || _difficultyDropdown.options.Count != 3)) || _startButton == null)
        {
            Debug.LogError("MatchSetupUI: GameMode/GameState/Panel/Start Buttonと、難易度Button3個（または旧Dropdown）を設定してください。", this);
            if (_startButton != null) _startButton.interactable = false;
            enabled = false;
        }
    }

    private void Start()
    {
        // 全ObjectのAwake後なので、Scene再読込で引き継いだ難易度も反映できます。
        _selectedDifficulty = _gameMode.SelectedDifficulty;
        if (_difficultyDropdown != null) _difficultyDropdown.SetValueWithoutNotify((int)_selectedDifficulty);
        RefreshDifficultyButtons();
        ApplyState(_gameState.State);
        // UI用Input Actionsの左右ナビゲーションを最初から使えるようにします。
        if (HasDifficultyButtons && _gameState.State == MatchState.Waiting && EventSystem.current != null)
        {
            Button current = _selectedDifficulty == EnemyDifficulty.Easy ? _easyButton :
                _selectedDifficulty == EnemyDifficulty.Hard ? _hardButton : _normalButton;
            EventSystem.current.SetSelectedGameObject(current.gameObject);
        }
    }

    private void OnEnable()
    {
        _menuMove.performed += HandleMenuMove;
        _menuSubmit.performed += HandleMenuSubmit;
        if (_difficultyDropdown != null) _difficultyDropdown.onValueChanged.AddListener(HandleDifficultyChanged);
        if (_startButton != null) _startButton.onClick.AddListener(HandleStartClicked);
        if (_easyButton != null) _easyButton.onClick.AddListener(SelectEasy);
        if (_normalButton != null) _normalButton.onClick.AddListener(SelectNormal);
        if (_hardButton != null) _hardButton.onClick.AddListener(SelectHard);
        if (_gameState != null)
        {
            _gameState.StateChanged += ApplyState;
            ApplyState(_gameState.State);
        }
    }

    private void OnDisable()
    {
        SetMenuInput(false);
        _menuMove.performed -= HandleMenuMove;
        _menuSubmit.performed -= HandleMenuSubmit;
        if (_difficultyDropdown != null) _difficultyDropdown.onValueChanged.RemoveListener(HandleDifficultyChanged);
        if (_startButton != null) _startButton.onClick.RemoveListener(HandleStartClicked);
        if (_easyButton != null) _easyButton.onClick.RemoveListener(SelectEasy);
        if (_normalButton != null) _normalButton.onClick.RemoveListener(SelectNormal);
        if (_hardButton != null) _hardButton.onClick.RemoveListener(SelectHard);
        if (_gameState != null) _gameState.StateChanged -= ApplyState;
    }

    private void HandleDifficultyChanged(int index)
    {
        if (_gameMode == null || !_gameMode.CanStart || index < 0 || index > 2) return;
        _selectedDifficulty = (EnemyDifficulty)index;
        if (_difficultyDropdown != null) _difficultyDropdown.SetValueWithoutNotify(index);
        RefreshDifficultyButtons();
    }

    private void SelectEasy() => HandleDifficultyChanged(0);
    private void SelectNormal() => HandleDifficultyChanged(1);
    private void SelectHard() => HandleDifficultyChanged(2);

    /// <summary>フォーカスとは別に選択状態を保持し、マウスを離しても緑の強調を残します。</summary>
    private void RefreshDifficultyButtons()
    {
        RefreshDifficultyButton(_easyButton, EnemyDifficulty.Easy);
        RefreshDifficultyButton(_normalButton, EnemyDifficulty.Normal);
        RefreshDifficultyButton(_hardButton, EnemyDifficulty.Hard);
    }

    private void RefreshDifficultyButton(Button button, EnemyDifficulty difficulty)
    {
        if (button == null) return;
        bool selected = difficulty == _selectedDifficulty;
        CyberpunkUIStyle style = button.GetComponent<CyberpunkUIStyle>();
        if (style != null)
        {
            CyberpunkUITheme theme = _theme != null ? _theme : style.Theme;
            if (theme != null)
            {
                style.SetSpriteOverride(null);
                style.Configure(theme, selected ? CyberpunkUIColor.Success : CyberpunkUIColor.Muted);
            }
        }
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
        SetMenuInput(waiting && HasDifficultyButtons);
        if (_setupPanel != null) _setupPanel.SetActive(waiting);
        // 難易度選択中は操作説明も確実に有効にします。実行時には生成しません。
        if (_controlsText != null) _controlsText.gameObject.SetActive(waiting);
        if (_playingPanel != null) _playingPanel.SetActive(!waiting);
        if (_difficultyDropdown != null) _difficultyDropdown.interactable = waiting;
        if (_difficultyDropdown != null && HasDifficultyButtons) _difficultyDropdown.gameObject.SetActive(false);
        bool canSelect = waiting && _gameMode != null && _gameMode.CanStart;
        if (_easyButton != null) _easyButton.interactable = canSelect;
        if (_normalButton != null) _normalButton.interactable = canSelect;
        if (_hardButton != null) _hardButton.interactable = canSelect;
        RefreshDifficultyButtons();
        if (_startButton != null) _startButton.interactable = waiting && _gameMode != null && _gameMode.CanStart;
    }

    private void OnDestroy()
    {
        SetMenuInput(false);
        _menuMove.Dispose();
        _menuSubmit.Dispose();
        if (_runtimeSettings != null) Destroy(_runtimeSettings);
    }

    private void SetMenuInput(bool enabledInput)
    {
        if (enabledInput)
        {
            // UI ModuleのNavigate/Submitと専用Actionsの二重実行を防ぎます。Pointerは維持。
            if (_navigationSystem == null && EventSystem.current != null)
            {
                _navigationSystem = EventSystem.current;
                _previousNavigation = _navigationSystem.sendNavigationEvents;
                _navigationSystem.sendNavigationEvents = false;
            }
            _menuMove.Enable();
            _menuSubmit.Enable();
        }
        else
        {
            _menuMove.Disable();
            _menuSubmit.Disable();
            if (_navigationSystem != null) _navigationSystem.sendNavigationEvents = _previousNavigation;
            _navigationSystem = null;
        }
    }

    private void HandleMenuMove(InputAction.CallbackContext context)
    {
        if (_gameMode == null || !_gameMode.CanStart || !HasDifficultyButtons) return;
        Vector2 input = context.ReadValue<Vector2>();
        if (input.sqrMagnitude < 0.25f) return;
        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
        {
            _startFocused = false;
            HandleDifficultyChanged(Mathf.Clamp((int)_selectedDifficulty + (input.x > 0 ? 1 : -1), 0, 2));
        }
        else _startFocused = input.y < 0;
        Button target = _startFocused ? _startButton : _selectedDifficulty == EnemyDifficulty.Easy ? _easyButton :
            _selectedDifficulty == EnemyDifficulty.Hard ? _hardButton : _normalButton;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(target.gameObject);
    }

    private void HandleMenuSubmit(InputAction.CallbackContext context)
    {
        if (_gameMode != null && _gameMode.CanStart && _gameState.State == MatchState.Waiting)
            HandleStartClicked();
    }
}
