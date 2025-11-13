using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

public class PauseMenuController : MonoBehaviour
{
    private VisualElement _root;
    private bool _paused = false;

    void Start()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;
        _root.style.display = DisplayStyle.None;

        _root.Q<Button>("btnResume").clicked += Resume;
        _root.Q<Button>("btnLeaveGame").clicked += LeaveGame;
    }

    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            TogglePause();
    }

    void TogglePause()
    {
        _paused = !_paused;
        _root.style.display = _paused ? DisplayStyle.Flex : DisplayStyle.None;
        Time.timeScale = _paused ? 0f : 1f;
        AudioListener.pause = _paused;
    }

    public void ShowMenu()
    {
        // ваша логика показа меню паузы
        gameObject.SetActive(true);
    }

    public void HideMenu()
    {
        // ваша логика скрытия меню паузы
        gameObject.SetActive(false);
    }

    void Resume() => TogglePause();

    void LeaveGame()
    {
        TogglePause();
        UIManager.Instance.LeaveGame();
    }
}