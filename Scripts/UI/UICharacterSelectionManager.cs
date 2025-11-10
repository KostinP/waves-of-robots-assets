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
        Debug.Log("UICharacterSelectionManager initialized");
    }

    private void FindCharacterElements()
    {
        _charVacuum = _root.Q<VisualElement>("charVacuum");
        _charToaster = _root.Q<VisualElement>("charToaster");
        _charGPT = _root.Q<VisualElement>("charGPT");
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

    private void SelectCharacter(VisualElement selectedCharacter)
    {
        // Снимаем выделение со всех персонажей
        if (_charVacuum != null) _charVacuum.RemoveFromClassList("selected");
        if (_charToaster != null) _charToaster.RemoveFromClassList("selected");
        if (_charGPT != null) _charGPT.RemoveFromClassList("selected");

        // Выделяем выбранного персонажа
        selectedCharacter.AddToClassList("selected");
        Debug.Log($"Selected character: {selectedCharacter.name}");
    }

    public string GetSelectedCharacter()
    {
        if (_charVacuum != null && _charVacuum.ClassListContains("selected"))
            return "Vacuum";
        if (_charToaster != null && _charToaster.ClassListContains("selected"))
            return "Toaster";
        if (_charGPT != null && _charGPT.ClassListContains("selected"))
            return "GPT";

        return "None";
    }
}