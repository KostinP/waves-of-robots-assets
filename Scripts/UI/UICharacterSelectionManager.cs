using UnityEngine;
using UnityEngine.UIElements;

public class UICharacterSelectionManager
{
    private readonly VisualElement _root;

    // Character Elements
    private VisualElement _charVacuum;
    private VisualElement _charToaster;
    private VisualElement _charGPT;

    public UICharacterSelectionManager(VisualElement root)
    {
        _root = root;
        Initialize();
    }

    private void Initialize()
    {
        FindCharacterElements();
        SetupCharacterSelection();
    }

    private void FindCharacterElements()
    {
        _charVacuum = _root.Q<VisualElement>("charVacuum");
        _charToaster = _root.Q<VisualElement>("charToaster");
        _charGPT = _root.Q<VisualElement>("charGPT");
    }

    private void SetupCharacterSelection()
    {
        _charVacuum?.RegisterCallback<ClickEvent>(evt => SelectCharacter(_charVacuum));
        _charToaster?.RegisterCallback<ClickEvent>(evt => SelectCharacter(_charToaster));
        _charGPT?.RegisterCallback<ClickEvent>(evt => SelectCharacter(_charGPT));
    }

    private void SelectCharacter(VisualElement selectedCharacter)
    {
        _charVacuum?.RemoveFromClassList("selected");
        _charToaster?.RemoveFromClassList("selected");
        _charGPT?.RemoveFromClassList("selected");

        selectedCharacter.AddToClassList("selected");
    }

    public string GetSelectedCharacter()
    {
        if (_charVacuum?.ClassListContains("selected") == true) return "Vacuum";
        if (_charToaster?.ClassListContains("selected") == true) return "Toaster";
        if (_charGPT?.ClassListContains("selected") == true) return "GPT";
        return "None";
    }
}