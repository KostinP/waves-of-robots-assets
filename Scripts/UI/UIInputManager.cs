using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class UIInputManager
{
    private readonly InputActionAsset _inputActions;
    private readonly VisualElement _root;
    private InputActionMap _uiActionMap;
    private InputAction _navigateAction, _submitAction, _cancelAction;
    private InputAction _openSettingsAction, _openStatisticsAction, _tabAction;
    private bool _isTextFieldFocused = false;
    private VisualElement _currentlyFocusedElement;
    private bool _isSettingFocus = false;
    private bool _cancelSubscribed = false;
    private bool _navigateSubscribed = false;
    private bool _submitSubscribed = false;
    private bool _tabSubscribed = false;
    private bool _settingsSubscribed = false;
    private bool _statsSubscribed = false;
    private UIScreenManager _screenManager;
    private MainMenuController _controller;

    public UIInputManager(InputActionAsset inputActions, VisualElement root, MainMenuController controller)
    {
        _inputActions = inputActions;
        _root = root;
        _controller = controller;
        _screenManager = new UIScreenManager(root, controller);
        Initialize();
    }

    private void Initialize()
    {
        if (_inputActions == null)
        {
            Debug.LogWarning("InputActionAsset is not assigned!");
            return;
        }
        _uiActionMap = _inputActions.FindActionMap("UI");
        if (_uiActionMap == null)
        {
            Debug.LogError("UI Action Map not found!");
            return;
        }
        SetupInputActions();
        SubscribeToInputEvents();
    }

    private void SetupInputActions()
    {
        _navigateAction = _uiActionMap.FindAction("Navigate");
        _submitAction = _uiActionMap.FindAction("Submit");
        _cancelAction = _uiActionMap.FindAction("Cancel");
        _openSettingsAction = _uiActionMap.FindAction("OpenSettings");
        _openStatisticsAction = _uiActionMap.FindAction("OpenStatistics");
        _tabAction = _uiActionMap.FindAction("Tab");
    }

    private void SubscribeToInputEvents()
    {
        if (_cancelAction != null)
        {
            _cancelAction.performed += OnCancelPerformed;
            _cancelSubscribed = true;
        }
        if (_navigateAction != null)
        {
            _navigateAction.performed += OnNavigatePerformed;
            _navigateSubscribed = true;
        }
        if (_submitAction != null)
        {
            _submitAction.performed += OnSubmitPerformed;
            _submitSubscribed = true;
        }
        if (_tabAction != null)
        {
            _tabAction.performed += OnTabPerformed;
            _tabSubscribed = true;
        }
        if (_openSettingsAction != null)
        {
            _openSettingsAction.performed += OnOpenSettingsPerformed;
            _settingsSubscribed = true;
        }
        if (_openStatisticsAction != null)
        {
            _openStatisticsAction.performed += OnOpenStatisticsPerformed;
            _statsSubscribed = true;
        }
    }

    public void Cleanup()
    {
        if (_cancelSubscribed && _cancelAction != null) _cancelAction.performed -= OnCancelPerformed;
        if (_navigateSubscribed && _navigateAction != null) _navigateAction.performed -= OnNavigatePerformed;
        if (_submitSubscribed && _submitAction != null) _submitAction.performed -= OnSubmitPerformed;
        if (_tabSubscribed && _tabAction != null) _tabAction.performed -= OnTabPerformed;
        if (_settingsSubscribed && _openSettingsAction != null) _openSettingsAction.performed -= OnOpenSettingsPerformed;
        if (_statsSubscribed && _openStatisticsAction != null) _openStatisticsAction.performed -= OnOpenStatisticsPerformed;

        _cancelSubscribed = _navigateSubscribed = _submitSubscribed = _tabSubscribed = _settingsSubscribed = _statsSubscribed = false;
    }

    public void Enable()
    {
        _inputActions?.Disable();
        _uiActionMap?.Enable();
    }

    public void Disable() => _uiActionMap?.Disable();

    private void OnCancelPerformed(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed || _isTextFieldFocused) return;
        HandleEscapeKey();
    }

    private void OnNavigatePerformed(InputAction.CallbackContext ctx)
    {
        if (_isSettingFocus || _isTextFieldFocused) return;
        Vector2 dir = ctx.ReadValue<Vector2>();
        if (Mathf.Abs(dir.y) > 0.1f) NavigateUI(Mathf.RoundToInt(-dir.y));
        else if (Mathf.Abs(dir.x) > 0.1f) NavigateHorizontal(Mathf.RoundToInt(dir.x));
    }

    private void OnTabPerformed(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed || _isTextFieldFocused) return;
        NavigateUI(1);
    }

    private void OnSubmitPerformed(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed || _isTextFieldFocused) return;
        if (_currentlyFocusedElement is Button btn && btn.enabledSelf)
            HandleButtonClick(btn);
    }

    private void OnOpenSettingsPerformed(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed || _isTextFieldFocused) return;
        string current = _screenManager.GetCurrentScreen();
        if (current != UIScreenManager.SettingsScreenName)
            _controller.ShowScreen(UIScreenManager.SettingsScreenName);
        else
            _screenManager.ReturnToMainMenu();
    }

    private void OnOpenStatisticsPerformed(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed || _isTextFieldFocused) return;
        Debug.Log("Open Statistics");
        // _controller.ShowScreen("statistics_screen");
    }

    private void NavigateUI(int direction)
    {
        var elements = GetInteractiveElements();
        if (elements.Count == 0) return;
        int current = _currentlyFocusedElement != null ? elements.IndexOf(_currentlyFocusedElement) : -1;
        int next = (current + direction + elements.Count) % elements.Count;
        SetFocusToElement(elements[next]);
    }

    private void NavigateHorizontal(int direction)
    {
        // Implement horizontal navigation if needed
    }

    private List<VisualElement> GetInteractiveElements()
    {
        var list = new List<VisualElement>();
        list.AddRange(_root.Query<Button>().Where(b => b.enabledSelf && b.visible && b.focusable && b.resolvedStyle.display == DisplayStyle.Flex).ToList());
        list.AddRange(_root.Query<TextField>().Where(t => t.enabledSelf && t.visible && t.focusable && t.resolvedStyle.display == DisplayStyle.Flex).ToList());
        list.AddRange(_root.Query<RadioButton>().Where(r => r.enabledSelf && r.visible && r.focusable && r.resolvedStyle.display == DisplayStyle.Flex).ToList());
        return list;
    }

    private void SetFocusToElement(VisualElement el)
    {
        if (_isSettingFocus || el == null) return;
        _isSettingFocus = true;
        try
        {
            _currentlyFocusedElement?.Blur();
            _currentlyFocusedElement?.RemoveFromClassList("focused");
            _currentlyFocusedElement = el;
            el.focusable = true;
            el.Focus();
            el.AddToClassList("focused");
            if (el is TextField tf)
            {
                _isTextFieldFocused = true;
                tf.Q<TextElement>().style.color = Color.white;
            }
            else
            {
                _isTextFieldFocused = false;
            }
        }
        finally
        {
            _isSettingFocus = false;
        }
    }

    private void HandleButtonClick(Button btn)
    {
        switch (btn.name)
        {
            case "btnSingle": OnSinglePlayer(); break;
            case "btnCreateLobby": OnCreateLobby(); break;
            case "btnStatistics": OnStatistics(); break;
            case "btnQuit": OnQuit(); break;
            default: SimulateButtonClick(btn); break;
        }
    }

    private void SimulateButtonClick(Button btn)
    {
        using var e = NavigationSubmitEvent.GetPooled();
        e.target = btn;
        btn.SendEvent(e);
    }

    private void HandleEscapeKey()
    {
        string screen = _screenManager.GetCurrentScreen();
        if (screen == UIScreenManager.LobbyListScreenName || screen == UIScreenManager.SettingsScreenName)
            _screenManager.ReturnToMainMenu();
    }

    private void OnSinglePlayer()
    {
        SwitchToPlayerInput();
        _controller.ShowScreen(UIScreenManager.MenuScreenName);
    }

    private void OnCreateLobby()
    {
        _controller.ShowScreen(UIScreenManager.LobbyListScreenName);
    }

    private void OnStatistics() => Debug.Log("Statistics");
    private void OnQuit() => Debug.Log("Quit");

    public void SwitchToPlayerInput()
    {
        _uiActionMap?.Disable();
        var playerMap = _inputActions.FindActionMap("Player");
        playerMap?.Enable();
    }

    public void SwitchToUIInput()
    {
        var playerMap = _inputActions.FindActionMap("Player");
        playerMap?.Disable();
        _uiActionMap?.Enable();
    }
}