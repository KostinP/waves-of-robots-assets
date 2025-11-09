using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class LobbySettingsManager
{
    private VisualElement _root;

    // Элементы lobby_list_screen (источник)
    private TextField _createLobbyName;
    private RadioButton _radioOpen;
    private RadioButton _radioClosed;
    private TextField _lobbyPassword;
    private Slider _playerCountSlider;
    private Label _playerCountValue;
    private VisualElement _charVacuum;
    private VisualElement _charToaster;
    private VisualElement _charGPT;
    private Slider _waveCountSlider;
    private Label _waveCountValue;

    // Элементы lobby_settings_screen (приёмник)
    private TextField _lobbyNameField;
    private RadioButton _settingsRadioOpen;
    private RadioButton _settingsRadioClosed;
    private TextField _lobbyPasswordField;
    private Slider _settingsPlayerCountSlider;
    private Label _settingsPlayerCountValue;
    private VisualElement _settingsCharVacuum;
    private VisualElement _settingsCharToaster;
    private VisualElement _settingsCharGPT;
    private Slider _settingsWaveCountSlider;
    private Label _settingsWaveCountValue;

    public LobbySettingsManager(VisualElement root)
    {
        _root = root;
        InitializeElements();
        SetupEventListeners();
    }

    private void InitializeElements()
    {
        // lobby_list_screen (создание)
        var lobbyListScreen = _root.Q<VisualElement>("lobby_list_screen");
        _createLobbyName = lobbyListScreen.Q<TextField>("createLobbyName");
        _radioOpen = lobbyListScreen.Q<RadioButton>("radioOpen");
        _radioClosed = lobbyListScreen.Q<RadioButton>("radioClosed");
        _lobbyPassword = lobbyListScreen.Q<TextField>("lobbyPassword");
        _playerCountSlider = lobbyListScreen.Q<Slider>("playerCountSlider");
        _playerCountValue = lobbyListScreen.Q<Label>("playerCountValue");
        _charVacuum = lobbyListScreen.Q<VisualElement>("charVacuum");
        _charToaster = lobbyListScreen.Q<VisualElement>("charToaster");
        _charGPT = lobbyListScreen.Q<VisualElement>("charGPT");
        _waveCountSlider = lobbyListScreen.Q<Slider>("waveCountSlider");
        _waveCountValue = lobbyListScreen.Q<Label>("waveCountValue");

        // lobby_settings_screen (настройки)
        var lobbySettingsScreen = _root.Q<VisualElement>("lobby_settings_screen");
        _lobbyNameField = lobbySettingsScreen.Q<TextField>("lobbyNameField");
        _settingsRadioOpen = lobbySettingsScreen.Q<RadioButton>("radioOpen");
        _settingsRadioClosed = lobbySettingsScreen.Q<RadioButton>("radioClosed");
        _lobbyPasswordField = lobbySettingsScreen.Q<TextField>("lobbyPasswordField");
        _settingsPlayerCountSlider = lobbySettingsScreen.Q<Slider>("playerCountSlider");
        _settingsPlayerCountValue = lobbySettingsScreen.Q<Label>("playerCountValue");
        _settingsCharVacuum = lobbySettingsScreen.Q<VisualElement>("charVacuum");
        _settingsCharToaster = lobbySettingsScreen.Q<VisualElement>("charToaster");
        _settingsCharGPT = lobbySettingsScreen.Q<VisualElement>("charGPT");
        _settingsWaveCountSlider = lobbySettingsScreen.Q<Slider>("waveCountSlider");
        _settingsWaveCountValue = lobbySettingsScreen.Q<Label>("waveCountValue");
    }

    private void SetupEventListeners()
    {
        // Синхронизация при изменении формы создания
        if (_createLobbyName != null) _createLobbyName.RegisterValueChangedCallback(evt => SyncLobbyName());
        if (_radioOpen != null) _radioOpen.RegisterValueChangedCallback(evt => SyncLobbyType());
        if (_radioClosed != null) _radioClosed.RegisterValueChangedCallback(evt => SyncLobbyType());
        if (_lobbyPassword != null) _lobbyPassword.RegisterValueChangedCallback(evt => SyncPassword());
        if (_playerCountSlider != null) _playerCountSlider.RegisterValueChangedCallback(evt => SyncPlayerCount());
        if (_playerCountValue != null) _playerCountValue.RegisterValueChangedCallback(evt => SyncPlayerCount());

        SyncCharacterSelection();
        if (_waveCountSlider != null) _waveCountSlider.RegisterValueChangedCallback(evt => SyncWaveCount());
        if (_waveCountValue != null) _waveCountValue.RegisterValueChangedCallback(evt => SyncWaveCount());
    }

    public void SyncAllSettings()
    {
        SyncLobbyName();
        SyncLobbyType();
        SyncPassword();
        SyncPlayerCount();
        SyncCharacterSelection();
        SyncWaveCount();
    }

    #region Синхронизация
    private void SyncLobbyName()
    {
        if (_lobbyNameField != null && _createLobbyName != null)
            _lobbyNameField.value = _createLobbyName.value;
    }

    private void SyncLobbyType()
    {
        if (_settingsRadioOpen != null && _radioOpen != null)
            _settingsRadioOpen.SetValueWithoutNotify(_radioOpen.value);
        if (_settingsRadioClosed != null && _radioClosed != null)
            _settingsRadioClosed.SetValueWithoutNotify(_radioClosed.value);
    }

    private void SyncPassword()
    {
        if (_lobbyPasswordField != null && _lobbyPassword != null)
            _lobbyPasswordField.value = _lobbyPassword.value;
    }

    private void SyncPlayerCount()
    {
        if (_settingsPlayerCountSlider != null && _playerCountSlider != null)
            _settingsPlayerCountSlider.SetValueWithoutNotify(_playerCountSlider.value);
        if (_settingsPlayerCountValue != null && _playerCountValue != null)
            _settingsPlayerCountValue.text = _playerCountValue.text;
    }

    private void SyncCharacterSelection()
    {
        // Убираем selected со всех
        _settingsCharVacuum?.RemoveFromClassList("selected");
        _settingsCharToaster?.RemoveFromClassList("selected");
        _settingsCharGPT?.RemoveFromClassList("selected");

        // Добавляем selected к активному
        if (_charVacuum != null && _charVacuum.ClassListContains("selected"))
            _settingsCharVacuum?.AddToClassList("selected");
        else if (_charToaster != null && _charToaster.ClassListContains("selected"))
            _settingsCharToaster?.AddToClassList("selected");
        else if (_charGPT != null && _charGPT.ClassListContains("selected"))
            _settingsCharGPT?.AddToClassList("selected");
    }

    private void SyncWaveCount()
    {
        if (_settingsWaveCountSlider != null && _waveCountSlider != null)
            _settingsWaveCountSlider.SetValueWithoutNotify(_waveCountSlider.value);
        if (_settingsWaveCountValue != null && _waveCountValue != null)
            _settingsWaveCountValue.text = _waveCountValue.text;
    }
    #endregion
}