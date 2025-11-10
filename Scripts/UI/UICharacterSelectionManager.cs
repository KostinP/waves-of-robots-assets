using UnityEngine;
using UnityEngine.UIElements;

public class UICharacterSelectionManager
{
    private readonly VisualElement _root;
    private readonly MainMenuController _controller;

    private VisualElement _charVacuum;
    private VisualElement _charToaster;
    private VisualElement _charGPT;

    public UICharacterSelectionManager(VisualElement root, MainMenuController controller)
    {
        _root = root;
        _controller = controller;
        Initialize();
    }

    public PlayerData GetPlayerData()
    {
        var playerNameField = _root.Q<TextField>("playerNameField");
        return new PlayerData
        {
            name = playerNameField?.value ?? "Player",
            selectedCharacter = GetSelectedCharacter()
        };
    }

    private void Initialize()
    {
        FindCharacterElements();
        SetupCharacterSelection();
        SelectDefaultCharacter();
        Debug.Log("UICharacterSelectionManager initialized");
    }

    private void FindCharacterElements()
    {
        var lobbySettingsScreen = _root.Q<VisualElement>(UIScreenManager.LobbySettingsScreenName);
        if (lobbySettingsScreen == null) return;

        _charVacuum = lobbySettingsScreen.Q<VisualElement>("charVacuum");
        _charToaster = lobbySettingsScreen.Q<VisualElement>("charToaster");
        _charGPT = lobbySettingsScreen.Q<VisualElement>("charGPT");
    }

    private void SetupCharacterSelection()
    {
        if (_charVacuum != null)
            _charVacuum.RegisterCallback<ClickEvent>(evt => SelectCharacter(_charVacuum));
        if (_charToaster != null)
            _charToaster.RegisterCallback<ClickEvent>(evt => SelectCharacter(_charToaster));
        if (_charGPT != null)
            _charGPT.RegisterCallback<ClickEvent>(evt => SelectCharacter(_charGPT));
    }

    private void SelectCharacter(VisualElement selected)
    {
        _charVacuum?.RemoveFromClassList("selected");
        _charToaster?.RemoveFromClassList("selected");
        _charGPT?.RemoveFromClassList("selected");

        selected.AddToClassList("selected");
        Debug.Log($"Selected character: {selected.name}");
    }

    private void SelectDefaultCharacter()
    {
        _charVacuum?.AddToClassList("selected");
    }

    public string GetSelectedCharacter()
    {
        if (_charVacuum?.ClassListContains("selected") == true) return "Vacuum";
        if (_charToaster?.ClassListContains("selected") == true) return "Toaster";
        if (_charGPT?.ClassListContains("selected") == true) return "GPT";
        return "Vacuum";
    }
}