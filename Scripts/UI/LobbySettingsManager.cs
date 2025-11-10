using UnityEngine;
using UnityEngine.UIElements;
using Unity.Collections;
using Unity.Entities;

public class LobbySettingsManager
{
    private readonly VisualElement _root;
    private readonly MainMenuController _controller; // Добавляем

    // lobby_list_screen (источник)
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

    // lobby_settings_screen (приёмник)
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

    public LobbySettingsManager(VisualElement root, MainMenuController controller)
    {
        _root = root;
        _controller = controller;
        InitializeElements();
        SetupEventListeners();
    }

    private void InitializeElements()
    {
        var lobbyList = _root.Q<VisualElement>("lobby_list_screen");
        _createLobbyName = lobbyList.Q<TextField>("createLobbyName");
        _radioOpen = lobbyList.Q<RadioButton>("radioOpen");
        _radioClosed = lobbyList.Q<RadioButton>("radioClosed");
        _lobbyPassword = lobbyList.Q<TextField>("lobbyPassword");
        _playerCountSlider = lobbyList.Q<Slider>("playerCountSlider");
        _playerCountValue = lobbyList.Q<Label>("playerCountValue");
        _charVacuum = lobbyList.Q<VisualElement>("charVacuum");
        _charToaster = lobbyList.Q<VisualElement>("charToaster");
        _charGPT = lobbyList.Q<VisualElement>("charGPT");
        _waveCountSlider = lobbyList.Q<Slider>("waveCountSlider");
        _waveCountValue = lobbyList.Q<Label>("waveCountValue");

        var lobbySettings = _root.Q<VisualElement>("lobby_settings_screen");
        _lobbyNameField = lobbySettings.Q<TextField>("lobbyNameField");
        _settingsRadioOpen = lobbySettings.Q<RadioButton>("radioOpen");
        _settingsRadioClosed = lobbySettings.Q<RadioButton>("radioClosed");
        _lobbyPasswordField = lobbySettings.Q<TextField>("lobbyPasswordField");
        _settingsPlayerCountSlider = lobbySettings.Q<Slider>("playerCountSlider");
        _settingsPlayerCountValue = lobbySettings.Q<Label>("playerCountValue");
        _settingsCharVacuum = lobbySettings.Q<VisualElement>("charVacuum");
        _settingsCharToaster = lobbySettings.Q<VisualElement>("charToaster");
        _settingsCharGPT = lobbySettings.Q<VisualElement>("charGPT");
        _settingsWaveCountSlider = lobbySettings.Q<Slider>("waveCountSlider");
        _settingsWaveCountValue = lobbySettings.Q<Label>("waveCountValue");
    }

    private void SetupEventListeners()
    {
        _createLobbyName?.RegisterValueChangedCallback(_ => SyncLobbyName());
        _radioOpen?.RegisterValueChangedCallback(_ => SyncLobbyType());
        _radioClosed?.RegisterValueChangedCallback(_ => SyncLobbyType());
        _lobbyPassword?.RegisterValueChangedCallback(_ => SyncPassword());
        _playerCountSlider?.RegisterValueChangedCallback(_ => SyncPlayerCount());
        _waveCountSlider?.RegisterValueChangedCallback(_ => SyncWaveCount());

        // Синхронизация при смене персонажа
        _charVacuum?.RegisterCallback<ClickEvent>(_ => SyncCharacterSelection());
        _charToaster?.RegisterCallback<ClickEvent>(_ => SyncCharacterSelection());
        _charGPT?.RegisterCallback<ClickEvent>(_ => SyncCharacterSelection());
    }

    public void SyncAllSettings()
    {
        SyncLobbyName();
        SyncLobbyType();
        SyncPassword();
        SyncPlayerCount();
        SyncCharacterSelection();
        SyncWaveCount();

        // Синхронизация с NetCode
        var lobbyData = _controller.LobbySetupManager.GetLobbyData();
        var world = World.DefaultGameObjectInjectionWorld;
        var em = world.EntityManager;
        var query = em.CreateEntityQuery(ComponentType.ReadOnly<LobbyDataComponent>());
        if (query.TryGetSingletonEntity<LobbyDataComponent>(out var entity))
        {
            var lobby = em.GetComponentData<LobbyDataComponent>(entity);
            lobby.Name = new FixedString128Bytes(lobbyData.name);
            lobby.Password = new FixedString64Bytes(lobbyData.password);
            lobby.MaxPlayers = lobbyData.maxPlayers;
            lobby.IsOpen = lobbyData.isOpen;
            em.SetComponentData(entity, lobby);
        }
    }

    private void SyncLobbyName() => _lobbyNameField.value = _createLobbyName.value;
    private void SyncLobbyType()
    {
        _settingsRadioOpen.SetValueWithoutNotify(_radioOpen.value);
        _settingsRadioClosed.SetValueWithoutNotify(_radioClosed.value);
    }
    private void SyncPassword() => _lobbyPasswordField.value = _lobbyPassword.value;
    private void SyncPlayerCount()
    {
        _settingsPlayerCountSlider.SetValueWithoutNotify(_playerCountSlider.value);
        _settingsPlayerCountValue.text = _playerCountValue.text;
    }
    private void SyncWaveCount()
    {
        _settingsWaveCountSlider.SetValueWithoutNotify(_waveCountSlider.value);
        _settingsWaveCountValue.text = _waveCountValue.text;
    }
    private void SyncCharacterSelection()
    {
        _settingsCharVacuum.RemoveFromClassList("selected");
        _settingsCharToaster.RemoveFromClassList("selected");
        _settingsCharGPT.RemoveFromClassList("selected");

        if (_charVacuum.ClassListContains("selected")) _settingsCharVacuum.AddToClassList("selected");
        else if (_charToaster.ClassListContains("selected")) _settingsCharToaster.AddToClassList("selected");
        else if (_charGPT.ClassListContains("selected")) _settingsCharGPT.AddToClassList("selected");
    }
}