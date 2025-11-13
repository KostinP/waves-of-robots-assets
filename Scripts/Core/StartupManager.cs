using UnityEngine;
using UnityEngine.SceneManagement;

public class StartupManager : MonoBehaviour
{
    [SerializeField] private string managersScene = "ManagersScene";
    [SerializeField] private string mainMenuScene = "MainMenuScene";

    private void Start()
    {
        SceneManager.LoadSceneAsync(managersScene, LoadSceneMode.Additive).completed += _ => LoadMainMenu();
    }

    private void LoadMainMenu()
    {
        SceneManager.LoadSceneAsync(mainMenuScene, LoadSceneMode.Additive);
    }
}