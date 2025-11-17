using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class UIInputManager
{
    private readonly VisualElement _root;
    private readonly InputActionAsset _input;

    private InputActionMap _ui;
    private InputAction _cancel;

    // сохраняем обработчик, чтобы корректно отписываться
    private System.Action<InputAction.CallbackContext> _cancelHandler;

    public UIInputManager(InputActionAsset actions, VisualElement root)
    {
        _input = actions;
        _root = root;

        Initialize();
    }

    private void Initialize()
    {
        if (_input == null) return;

        _input.Enable();

        _ui = _input.FindActionMap("UI");
        if (_ui == null) return;

        _cancel = _ui.FindAction("Cancel");
        if (_cancel == null) return;

        // сохраняем лямбду, чтобы потом можно было отписаться
        _cancelHandler = ctx => SettingsFeature.Instance?.HideSettingsScreen();

        _cancel.performed += _cancelHandler;
    }

    public void Cleanup()
    {
        if (_cancel != null && _cancelHandler != null)
            _cancel.performed -= _cancelHandler;
    }
}
