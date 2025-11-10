using UnityEngine;
using UnityEngine.UIElements;

public struct LobbyData
{
    public string name;
    public bool isOpen;
    public string password;
    public int maxPlayers;
}

public class UILobbySetupManager
{
    private readonly VisualElement _root;
    private readonly MainMenuController _controller;

    // UI Elements
    private Slider _playerCountSlider;
    private Label _playerCountValue;
    private Slider _waveCountSlider;
    private Label _waveCountValue;
    private RadioButton _waveCountRadio;
    private RadioButton _infinityRadio;
    private RadioButton _radioOpen;
    private RadioButton _radioClosed;
    private TextField _createLobbyName;
    private TextField _lobbyPassword;
    private Button _togglePasswordBtn;
    private bool _isPasswordVisible = false;

    public UILobbySetupManager(VisualElement root, MainMenuController controller)
    {
        _root = root;
        _controller = controller;
        Initialize();
    }

    public LobbyData GetLobbyData()
    {
        return new LobbyData
        {
            name = _createLobbyName?.value ?? "My Lobby",
            isOpen = _radioOpen?.value ?? true,
            password = _radioClosed?.value == true ? (_lobbyPassword?.value ?? "") : "",
            maxPlayers = _playerCountSlider != null ? Mathf.RoundToInt(_playerCountSlider.value) : 4
        };
    }

    private void Initialize()
    {
        FindUIElements();
        SetupSliders();
        SetupRadioButtons();
        SetupPasswordField();
        SetupTextFields();
        Debug.Log("UILobbySetupManager initialized");
    }

    private void FindUIElements()
    {
        var lobbySettingsScreen = _root.Q<VisualElement>(UIScreenManager.LobbySettingsScreenName);
        if (lobbySettingsScreen == null) return;

        _playerCountSlider = lobbySettingsScreen.Q<Slider>("playerCountSlider");
        _playerCountValue = lobbySettingsScreen.Q<Label>("playerCountValue");
        _waveCountSlider = lobbySettingsScreen.Q<Slider>("waveCountSlider");
        _waveCountValue = lobbySettingsScreen.Q<Label>("waveCountValue");
        _waveCountRadio = lobbySettingsScreen.Q<RadioButton>("waveCountRadio");
        _infinityRadio = lobbySettingsScreen.Q<RadioButton>("infinityRadio");
        _radioOpen = lobbySettingsScreen.Q<RadioButton>("radioOpen");
        _radioClosed = lobbySettingsScreen.Q<RadioButton>("radioClosed");
        _createLobbyName = lobbySettingsScreen.Q<TextField>("createLobbyName");
        _lobbyPassword = lobbySettingsScreen.Q<TextField>("lobbyPassword");
        _togglePasswordBtn = lobbySettingsScreen.Q<Button>("togglePasswordVisibility");
    }

    private void SetupTextFields()
    {
        if (_createLobbyName != null)
        {
            _createLobbyName.RegisterCallback<FocusInEvent>(evt => OnTextFieldFocus(_createLobbyName, true));
            _createLobbyName.RegisterCallback<FocusOutEvent>(evt => OnTextFieldFocus(_createLobbyName, false));
            SetupTextFieldStyle(_createLobbyName);
        }
        if (_lobbyPassword != null)
        {
            _lobbyPassword.RegisterCallback<FocusInEvent>(evt => OnTextFieldFocus(_lobbyPassword, true));
            _lobbyPassword.RegisterCallback<FocusOutEvent>(evt => OnTextFieldFocus(_lobbyPassword, false));
            SetupTextFieldStyle(_lobbyPassword);
        }
    }

    private void SetupTextFieldStyle(TextField textField)
    {
        textField.style.color = Color.white;
        textField.style.opacity = 1f;
        var input = textField.Q(className: "unity-base-text-field__input");
        if (input != null)
        {
            input.style.color = Color.white;
            input.style.backgroundColor = Color.clear;
        }
    }

    private void OnTextFieldFocus(TextField textField, bool focused)
    {
        Debug.Log($"TextField {textField.name} focused: {focused}");
    }

    #region Sliders
    private void SetupSliders()
    {
        SetupPlayerCountSlider();
        SetupWaveCountSlider();
    }

    private void SetupPlayerCountSlider()
    {
        if (_playerCountSlider != null && _playerCountValue != null)
        {
            _playerCountSlider.lowValue = 2;
            _playerCountSlider.highValue = 8;
            _playerCountSlider.value = 4;
            _playerCountSlider.RegisterValueChangedCallback(OnPlayerCountChanged);
            UpdatePlayerCountValue(_playerCountSlider.value);
        }
    }

    private void SetupWaveCountSlider()
    {
        if (_waveCountSlider != null && _waveCountValue != null)
        {
            _waveCountSlider.lowValue = 10;
            _waveCountSlider.highValue = 50;
            _waveCountSlider.value = 10;
            _waveCountSlider.RegisterValueChangedCallback(OnWaveCountChanged);
            UpdateWaveCountValue(_waveCountSlider.value);
        }
    }

    private void OnPlayerCountChanged(ChangeEvent<float> evt)
    {
        UpdatePlayerCountValue(evt.newValue);
    }

    private void OnWaveCountChanged(ChangeEvent<float> evt)
    {
        float rounded = Mathf.Round(evt.newValue / 10f) * 10f;
        if (Mathf.Abs(rounded - evt.newValue) > 0.1f)
            _waveCountSlider.value = rounded;
        UpdateWaveCountValue(_waveCountSlider.value);
    }

    private void UpdatePlayerCountValue(float value)
    {
        if (_playerCountValue != null)
            _playerCountValue.text = Mathf.RoundToInt(value).ToString();
    }

    private void UpdateWaveCountValue(float value)
    {
        if (_waveCountValue != null)
            _waveCountValue.text = Mathf.RoundToInt(value).ToString();
    }
    #endregion

    #region Radio Buttons
    private void SetupRadioButtons()
    {
        SetupWaveRadioButtons();
        SetupLobbyRadioButtons();
    }

    private void SetupWaveRadioButtons()
    {
        if (_waveCountRadio != null && _infinityRadio != null)
        {
            _waveCountRadio.SetValueWithoutNotify(true);
            _infinityRadio.SetValueWithoutNotify(false);

            _waveCountRadio.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    _infinityRadio.SetValueWithoutNotify(false);
                    _waveCountSlider?.SetEnabled(true);
                }
                else if (!_infinityRadio.value)
                {
                    _waveCountRadio.SetValueWithoutNotify(true);
                }
            });

            _infinityRadio.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    _waveCountRadio.SetValueWithoutNotify(false);
                    _waveCountSlider?.SetEnabled(false);
                }
                else if (!_waveCountRadio.value)
                {
                    _infinityRadio.SetValueWithoutNotify(true);
                }
            });

            _waveCountSlider?.SetEnabled(true);
        }
    }

    private void SetupLobbyRadioButtons()
    {
        if (_radioOpen != null && _radioClosed != null)
        {
            _radioOpen.SetValueWithoutNotify(true);
            _radioClosed.SetValueWithoutNotify(false);

            _radioOpen.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    _radioClosed.SetValueWithoutNotify(false);
                    if (_lobbyPassword?.parent != null)
                        _lobbyPassword.parent.style.display = DisplayStyle.None;
                }
                else if (!_radioClosed.value)
                {
                    _radioOpen.SetValueWithoutNotify(true);
                }
            });

            _radioClosed.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    _radioOpen.SetValueWithoutNotify(false);
                    if (_lobbyPassword?.parent != null)
                        _lobbyPassword.parent.style.display = DisplayStyle.None;
                }
                else if (!_radioOpen.value)
                {
                    _radioClosed.SetValueWithoutNotify(true);
                }
            });

            if (_lobbyPassword?.parent != null)
                _lobbyPassword.parent.style.display = DisplayStyle.None;
        }
    }
    #endregion

    #region Password Field
    private void SetupPasswordField()
    {
        if (_togglePasswordBtn != null)
        {
            _togglePasswordBtn.clicked += TogglePasswordVisibility;
            UpdatePasswordEyeIcon();
        }
    }

    public void TogglePasswordVisibility()
    {
        _isPasswordVisible = !_isPasswordVisible;
        if (_lobbyPassword != null)
            _lobbyPassword.isPasswordField = !_isPasswordVisible;
        UpdatePasswordEyeIcon();
    }

    private void UpdatePasswordEyeIcon()
    {
        if (_togglePasswordBtn == null) return;
        _togglePasswordBtn.RemoveFromClassList("show-password");
        if (_isPasswordVisible)
            _togglePasswordBtn.AddToClassList("show-password");
    }
    #endregion
}