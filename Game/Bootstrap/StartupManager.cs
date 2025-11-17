using UnityEngine;
using UnityEngine.SceneManagement;

public class StartupManager : MonoBehaviour
{
    [SerializeField] private string mainMenuScene = "MainMenu";

    private void Start()
    {
        // Все менеджеры УЖЕ в этой сцене (в инспекторе)
        // Просто загружаем главное меню
        SceneManager.LoadSceneAsync(mainMenuScene, LoadSceneMode.Single);
    }
}