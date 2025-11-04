using UnityEngine;
using UnityEngine.UIElements;

public class UILobbySetupManager
{
    private readonly VisualElement _root;

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

    public UILobbySetupManager(VisualElement root)
    {
        _root = root;
        Initialize();
    }

    private void Initialize()
    {
        FindUIElements();
        SetupSliders();
        SetupRadioButtons();
        SetupPasswordField();
        SetupTextFields();
        Debug.Log("Lobby setup manager initialized");
    }

    private void FindUIElements()
    {
        _playerCountSlider = _root.Q<Slider>("playerCountSlider");
        _playerCountValue = _root.Q<Label>("playerCountValue");
        _waveCountSlider = _root.Q<Slider>("waveCountSlider");
        _waveCountValue = _root.Q<Label>("waveCountValue");
        _waveCountRadio = _root.Q<RadioButton>("waveCountRadio");
        _infinityRadio = _root.Q<RadioButton>("infinityRadio");
        _radioOpen = _root.Q<RadioButton>("radioOpen");
        _radioClosed = _root.Q<RadioButton>("radioClosed");
        _createLobbyName = _root.Q<TextField>("createLobbyName");
        _lobbyPassword = _root.Q<TextField>("lobbyPassword");
        _togglePasswordBtn = _root.Q<Button>("togglePasswordVisibility");
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
        textField.style.color = new StyleColor(Color.white);
        textField.style.opacity = 1f;

        var textInput = textField.Q(className: "unity-base-text-field__input");
        if (textInput != null)
        {
            textInput.style.color = new StyleColor(Color.white);
            textInput.style.opacity = 1f;
            textInput.style.backgroundColor = new StyleColor(Color.clear);
        }
    }

    private void OnTextFieldFocus(TextField textField, bool focused)
    {
        // Логика фокуса должна быть синхронизирована с UIInputManager
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
        float roundedValue = Mathf.Round(evt.newValue / 10f) * 10f;
        if (Mathf.Abs(roundedValue - evt.newValue) > 0.1f)
        {
            _waveCountSlider.value = roundedValue;
        }
        UpdateWaveCountValue(_waveCountSlider.value);
    }

    private void UpdatePlayerCountValue(float value)
    {
        if (_playerCountValue != null)
        {
            _playerCountValue.text = Mathf.RoundToInt(value).ToString();
        }
    }

    private void UpdateWaveCountValue(float value)
    {
        if (_waveCountValue != null)
        {
            _waveCountValue.text = Mathf.RoundToInt(value).ToString();
        }
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
            _waveCountRadio.SetValueWithoutNotify(false);
            _infinityRadio.SetValueWithoutNotify(false);

            _waveCountRadio.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    _infinityRadio.SetValueWithoutNotify(false);
                    if (_waveCountSlider != null)
                        _waveCountSlider.SetEnabled(true);
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
                    if (_waveCountSlider != null)
                        _waveCountSlider.SetEnabled(false);
                }
                else if (!_waveCountRadio.value)
                {
                    _infinityRadio.SetValueWithoutNotify(true);
                }
            });

            _waveCountRadio.value = true;
            _infinityRadio.value = false;
            if (_waveCountSlider != null)
                _waveCountSlider.SetEnabled(true);
        }
    }

    private void SetupLobbyRadioButtons()
    {
        if (_radioOpen != null && _radioClosed != null)
        {
            _radioOpen.SetValueWithoutNotify(false);
            _radioClosed.SetValueWithoutNotify(false);

            _radioOpen.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    _radioClosed.SetValueWithoutNotify(false);
                    if (_lobbyPassword != null && _lobbyPassword.parent != null)
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
                    if (_lobbyPassword != null && _lobbyPassword.parent != null)
                        _lobbyPassword.parent.style.display = DisplayStyle.Flex;
                }
                else if (!_radioOpen.value)
                {
                    _radioClosed.SetValueWithoutNotify(true);
                }
            });

            _radioOpen.value = true;
            _radioClosed.value = false;
            if (_lobbyPassword != null && _lobbyPassword.parent != null)
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
        {
            _lobbyPassword.isPasswordField = !_isPasswordVisible;
        }
        UpdatePasswordEyeIcon();
    }

    private void UpdatePasswordEyeIcon()
    {
        if (_togglePasswordBtn == null) return;

        if (_isPasswordVisible)
        {
            _togglePasswordBtn.RemoveFromClassList("show-password");
            _togglePasswordBtn.AddToClassList("show-password");
        }
        else
        {
            _togglePasswordBtn.RemoveFromClassList("show-password");
        }
    }

    #endregion

    #region Debug Methods

    public void DebugTextFieldState(TextField textField, string fieldName)
    {
        if (textField == null)
        {
            Debug.LogError($"{fieldName} is null!");
            return;
        }

        Debug.Log($"=== {fieldName} State ===");
        Debug.Log($"- Display: {textField.resolvedStyle.display}");
        Debug.Log($"- Visibility: {textField.resolvedStyle.visibility}");
        Debug.Log($"- Opacity: {textField.resolvedStyle.opacity}");
        Debug.Log($"- Enabled: {textField.enabledSelf}");
        Debug.Log($"- Focusable: {textField.focusable}");
        Debug.Log($"- Value: '{textField.value}'");

        var inputElement = textField.Q(className: "unity-base-text-field__input");
        if (inputElement != null)
        {
            Debug.Log($"- Input Display: {inputElement.resolvedStyle.display}");
            Debug.Log($"- Input Color: {inputElement.resolvedStyle.color}");
        }
        else
        {
            Debug.LogError("- Input element not found!");
        }

        Debug.Log($"=== End {fieldName} State ===\n");
    }

    #endregion
}