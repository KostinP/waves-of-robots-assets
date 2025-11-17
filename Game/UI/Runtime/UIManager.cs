using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    public InputActionAsset InputActions => inputActions;

    [SerializeField] private InputActionAsset inputActions;

    private HUDController _hud;
    private PauseMenuController _pause;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetHUDController(HUDController hud) => _hud = hud;
    public void SetPauseController(PauseMenuController pause) => _pause = pause;

    public void LeaveGame()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void UpdatePlayerListUI(System.Collections.Generic.List<LobbyPlayerInfo> players)
    {
        FindObjectOfType<MainMenuController>()?.UpdatePlayerListFromData(players);
    }
}
