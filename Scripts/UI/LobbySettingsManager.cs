using UnityEngine;
using UnityEngine.UIElements;

public class LobbySettingsManager
{
    private VisualElement _root;

    // Source elements from lobby_list_screen
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

    // Target elements from lobby_settings_screen
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
        _createLobbyName?.RegisterValueChangedCallback(evt => SyncLobbyName());
        _radioOpen?.RegisterValueChangedCallback(evt => SyncLobbyType());
        _radioClosed?.RegisterValueChangedCallback(evt => SyncLobbyType());
        _lobbyPassword?.RegisterValueChangedCallback(evt => SyncPassword());
        _playerCountSlider?.RegisterValueChangedCallback(evt => SyncPlayerCount());
        _waveCountSlider?.RegisterValueChangedCallback(evt => SyncWaveCount());
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

    private void SyncLobbyName()
    {
        if (_lobbyNameField != null && _createLobbyName != null)
            _lobbyNameField.value = _createLobbyName.value;
    }

    private void SyncLobbyType()
    {
        _settingsRadioOpen?.SetValueWithoutNotify(_radioOpen?.value ?? false);
        _settingsRadioClosed?.SetValueWithoutNotify(_radioClosed?.value ?? false);
    }

    private void SyncPassword()
    {
        if (_lobbyPasswordField != null && _lobbyPassword != null)
            _lobbyPasswordField.value = _lobbyPassword.value;
    }

    private void SyncPlayerCount()
    {
        _settingsPlayerCountSlider?.SetValueWithoutNotify(_playerCountSlider?.value ?? 4);
        _settingsPlayerCountValue.text = _playerCountValue?.text;
    }

    private void SyncCharacterSelection()
    {
        _settingsCharVacuum?.RemoveFromClassList("selected");
        _settingsCharToaster?.RemoveFromClassList("selected");
        _settingsCharGPT?.RemoveFromClassList("selected");

        if (_charVacuum?.ClassListContains("selected") == true) _settingsCharVacuum?.AddToClassList("selected");
        else if (_charToaster?.ClassListContains("selected") == true) _settingsCharToaster?.AddToClassList("selected");
        else if (_charGPT?.ClassListContains("selected") == true) _settingsCharGPT?.AddToClassList("selected");
    }

    private void SyncWaveCount()
    {
        _settingsWaveCountSlider?.SetValueWithoutNotify(_waveCountSlider?.value ?? 10);
        _settingsWaveCountValue.text = _waveCountValue?.text;
    }
}