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

    // Navigation
    private UIScreenManager _screenManager;

    public UIInputManager(InputActionAsset inputActions, VisualElement root)
    {
        _inputActions = inputActions;
        _root = root;
        _screenManager = new UIScreenManager(root);
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

        if (_navigateAction == null) Debug.LogError("Navigate action not found!");
        if (_submitAction == null) Debug.LogError("Submit action not found!");
        if (_cancelAction == null) Debug.LogError("Cancel action not found!");
        if (_openSettingsAction == null) Debug.LogWarning("OpenSettings action not found!");
        if (_openStatisticsAction == null) Debug.LogWarning("OpenStatistics action not found!");
        if (_tabAction == null) Debug.LogWarning("Tab action not found!");
    }

    private void SubscribeToInputEvents()
    {
        if (_cancelAction != null) _cancelAction.performed += OnCancelPerformed;
        if (_navigateAction != null) _navigateAction.performed += OnNavigatePerformed;
        if (_submitAction != null) _submitAction.performed += OnSubmitPerformed;
        if (_tabAction != null) _tabAction.performed += OnTabPerformed;
        if (_openSettingsAction != null) _openSettingsAction.performed += OnOpenSettingsPerformed;
        if (_openStatisticsAction != null) _openStatisticsAction.performed += OnOpenStatisticsPerformed;
    }

    public void Enable()
    {
        if (_inputActions == null) return;
        _inputActions.Disable();
        _uiActionMap?.Enable();
    }

    public void Disable() => _uiActionMap?.Disable();

    public void Cleanup()
    {
        if (_cancelAction != null) _cancelAction.performed -= OnCancelPerformed;
        if (_navigateAction != null) _navigateAction.performed -= OnNavigatePerformed;
        if (_submitAction != null) _submitAction.performed -= OnSubmitPerformed;
        if (_tabAction != null) _tabAction.performed -= OnTabPerformed;
        if (_openSettingsAction != null) _openSettingsAction.performed -= OnOpenSettingsPerformed;
        if (_openStatisticsAction != null) _openStatisticsAction.performed -= OnOpenStatisticsPerformed;
    }

    #region Input Event Handlers

    private void OnCancelPerformed(InputAction.CallbackContext context)
    {
        if (!context.performed || _isTextFieldFocused) return;
        HandleEscapeKey();
    }

    private void OnNavigatePerformed(InputAction.CallbackContext context)
    {
        if (_isSettingFocus || _isTextFieldFocused) return;

        Vector2 direction = context.ReadValue<Vector2>();
        if (Mathf.Abs(direction.y) > 0.1f)
        {
            NavigateUI(Mathf.RoundToInt(-direction.y));
        }
        else if (Mathf.Abs(direction.x) > 0.1f)
        {
            NavigateHorizontal(Mathf.RoundToInt(direction.x));
        }
    }

    private void OnTabPerformed(InputAction.CallbackContext context)
    {
        if (!context.performed || _isTextFieldFocused) return;
        NavigateUI(1);
    }

    private void OnSubmitPerformed(InputAction.CallbackContext context)
    {
        if (!context.performed || _isTextFieldFocused) return;

        if (_currentlyFocusedElement is Button focusedButton && focusedButton.enabledSelf)
        {
            HandleButtonClick(focusedButton);
        }
    }

    private void OnOpenSettingsPerformed(InputAction.CallbackContext context)
    {
        if (!context.performed || _isTextFieldFocused) return;
        Debug.Log("OpenSettings action triggered");
    }

    private void OnOpenStatisticsPerformed(InputAction.CallbackContext context)
    {
        if (!context.performed || _isTextFieldFocused) return;
        Debug.Log("OpenStatistics action triggered");
        OnStatistics();
    }

    #endregion

    #region Navigation

    private void NavigateUI(int direction)
    {
        var interactiveElements = GetInteractiveElements();
        if (interactiveElements.Count == 0) return;

        int currentIndex = _currentlyFocusedElement != null ?
            interactiveElements.IndexOf(_currentlyFocusedElement) : -1;

        int newIndex = currentIndex + direction;
        if (newIndex < 0) newIndex = interactiveElements.Count - 1;
        if (newIndex >= interactiveElements.Count) newIndex = 0;

        SetFocusToElement(interactiveElements[newIndex]);
    }

    private void NavigateHorizontal(int direction)
    {
        // Логика для горизонтальной навигации (например, между радио-кнопками)
    }

    private List<VisualElement> GetInteractiveElements()
    {
        var elements = new List<VisualElement>();

        elements.AddRange(_root.Query<Button>().Where(btn =>
            btn.resolvedStyle.display == DisplayStyle.Flex &&
            btn.enabledSelf &&
            btn.visible &&
            btn.focusable).ToList());

        elements.AddRange(_root.Query<TextField>().Where(tf =>
            tf.resolvedStyle.display == DisplayStyle.Flex &&
            tf.enabledSelf &&
            tf.visible &&
            tf.focusable).ToList());

        elements.AddRange(_root.Query<RadioButton>().Where(rb =>
            rb.resolvedStyle.display == DisplayStyle.Flex &&
            rb.enabledSelf &&
            rb.visible &&
            rb.focusable).ToList());

        return elements;
    }

    private void SetFocusToElement(VisualElement element)
    {
        if (_isSettingFocus || element == null) return;

        _isSettingFocus = true;

        try
        {
            if (_currentlyFocusedElement != null && _currentlyFocusedElement != element)
            {
                _currentlyFocusedElement.Blur();
                _currentlyFocusedElement.RemoveFromClassList("focused");

                if (_currentlyFocusedElement is TextField)
                {
                    _isTextFieldFocused = false;
                }
            }

            _currentlyFocusedElement = element;

            if (!_currentlyFocusedElement.focusable)
                _currentlyFocusedElement.focusable = true;

            _currentlyFocusedElement.Focus();
            _currentlyFocusedElement.AddToClassList("focused");

            if (_currentlyFocusedElement is TextField textField)
            {
                _isTextFieldFocused = true;
                textField.Q<TextElement>().style.color = Color.white;
            }

            Debug.Log($"Focused on: {element.name} (Type: {element.GetType().Name})");
        }
        finally
        {
            _isSettingFocus = false;
        }
    }

    #endregion

    #region Button Handlers

    private void HandleButtonClick(Button button)
    {
        string buttonName = button.name;

        switch (buttonName)
        {
            case "btnSingle":
                OnSinglePlayer();
                break;
            case "btnCreateLobby":
                OnCreateLobby();
                break;
            case "btnStatistics":
                OnStatistics();
                break;
            case "btnQuit":
                OnQuit();
                break;
            default:
                SimulateButtonClick(button);
                break;
        }
    }

    private void SimulateButtonClick(Button button)
    {
        using (var clickEvent = NavigationSubmitEvent.GetPooled())
        {
            clickEvent.target = button;
            button.SendEvent(clickEvent);
        }
    }

    private void HandleEscapeKey()
    {
        string currentScreen = _screenManager.GetCurrentScreen();

        switch (currentScreen)
        {
            case UIScreenManager.LobbyListScreenName:
                _screenManager.ReturnToMainMenu();
                Debug.Log("Returned to main menu from lobby");
                break;
            case UIScreenManager.MenuScreenName:
                // StartCoroutine(QuitWithConfirmation()); // Нужно перенести в UIManager
                break;
            default:
                _screenManager.ReturnToMainMenu();
                break;
        }
    }

    private void OnSinglePlayer()
    {
        Debug.Log("Starting single player...");
        SwitchToPlayerInput();
        _screenManager.ShowScreen(UIScreenManager.MenuScreenName);
    }

    private void OnCreateLobby()
    {
        Debug.Log("Creating lobby...");
        _screenManager.ShowScreen(UIScreenManager.LobbyListScreenName);
    }

    private void OnStatistics()
    {
        Debug.Log("Opening statistics...");
        // _screenManager.ShowScreen(UIScreenManager.StatisticsScreenName);
    }

    private void OnQuit()
    {
        // StartCoroutine(QuitWithConfirmation()); // Нужно перенести в UIManager
        Debug.Log("Quit requested");
    }

    #endregion

    #region Input Switching

    public void SwitchToPlayerInput()
    {
        if (_inputActions == null) return;

        _uiActionMap?.Disable();
        var playerActionMap = _inputActions.FindActionMap("Player");
        playerActionMap?.Enable();
        Debug.Log("Switched to Player input");
    }

    public void SwitchToUIInput()
    {
        if (_inputActions == null) return;

        var playerActionMap = _inputActions.FindActionMap("Player");
        playerActionMap?.Disable();
        _uiActionMap?.Enable();
        Debug.Log("Switched to UI input");
    }

    #endregion
}