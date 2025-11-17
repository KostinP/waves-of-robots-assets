using UnityEngine;
using UnityEngine.UIElements;

public class PauseMenuController : MonoBehaviour
{
    private VisualElement _root;
    private bool _paused;

    private Button _resumeButton;
    private Button _leaveButton;

    private void Start()
    {
        var doc = GetComponent<UIDocument>();
        _root = doc.rootVisualElement;

        _resumeButton = _root.Q<Button>("btnResume");
        _leaveButton = _root.Q<Button>("btnLeaveGame");

        if (_resumeButton != null)
            _resumeButton.clicked += Resume;

        if (_leaveButton != null)
            _leaveButton.clicked += Leave;

        _root.style.display = DisplayStyle.None;

        Debug.Log("[PauseMenuController] Initialized");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Toggle();
    }

    private void Toggle()
    {
        _paused = !_paused;

        _root.style.display = _paused ? DisplayStyle.Flex : DisplayStyle.None;
        Time.timeScale = _paused ? 0f : 1f;
    }

    private void Resume()
    {
        Toggle();
    }

    private void Leave()
    {
        Toggle();
        Time.timeScale = 1f;

        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
