using UnityEngine;
using UnityEngine.UIElements;
using UiLabel = UnityEngine.UIElements.Label;

public class UILobbySetupManager
{
    private readonly VisualElement _root;
    private readonly MainMenuController _controller;

    private Slider _playerCount;
    private UiLabel _playerCountValue;

    private Slider _waveCount;
    private UiLabel _waveCountValue;

    private RadioButton _openRadio;
    private RadioButton _closedRadio;

    private TextField _lobbyName;
    private TextField _password;

    private Button _togglePassword;
    private Button _randomName;

    private bool _pwVisible = false;

    public UILobbySetupManager(VisualElement root, MainMenuController controller)
    {
        _root = root;
        _controller = controller;
        Init();
    }

    private void Init()
    {
        _playerCount = _root.Q<Slider>("playerCountSlider");
        _playerCountValue = _root.Q<UiLabel>("playerCountValue");

        _waveCount = _root.Q<Slider>("waveCountSlider");
        _waveCountValue = _root.Q<UiLabel>("waveCountValue");

        _openRadio = _root.Q<RadioButton>("radioOpen");
        _closedRadio = _root.Q<RadioButton>("radioClosed");

        _lobbyName = _root.Q<TextField>("playerNameField");
        _password = _root.Q<TextField>("lobbyPassword");

        _togglePassword = _root.Q<Button>("togglePasswordVisibility");
        _randomName = _root.Q<Button>("randomNameBtn");

        SetupSliders();
        SetupPassword();
        SetupRandomName();
    }

    private void SetupSliders()
    {
        _playerCount.RegisterValueChangedCallback(e =>
            _playerCountValue.text = Mathf.RoundToInt(e.newValue).ToString());

        _waveCount.RegisterValueChangedCallback(e =>
            _waveCountValue.text = Mathf.RoundToInt(e.newValue).ToString());
    }

    private void SetupPassword()
    {
        _togglePassword.clicked += () =>
        {
            _pwVisible = !_pwVisible;
            _password.isPasswordField = !_pwVisible;
        };
    }

    private void SetupRandomName()
    {
        _randomName.clicked += () =>
        {
            string[] A = { "Alpha", "Bravo", "Gamma", "Neon", "Orbit" };
            string[] B = { "Room", "Zone", "Arena", "Hall" };
            _lobbyName.value = $"{A[Random.Range(0, A.Length)]} {B[Random.Range(0, B.Length)]}-{Random.Range(1, 999)}";
        };
    }

    public LobbyDataComponent GetLobbyData()
    {
        return new LobbyDataComponent
        {
            Name = _lobbyName.value,
            Password = _closedRadio.value ? _password.value : "",
            MaxPlayers = Mathf.RoundToInt(_playerCount.value),
        };
    }

    public string GetLobbyName() => _lobbyName.value;
}
