using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneUIController : MonoBehaviour
{
    void Start()
    {
        // Загружаем UI сцены аддитивно
        SceneManager.LoadSceneAsync("HUDScene", LoadSceneMode.Additive);
        SceneManager.LoadSceneAsync("PauseMenuScene", LoadSceneMode.Additive);
        
        // Подписываемся на события
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "HUDScene")
        {
            var hudRoot = GameObject.Find("HUDRoot")?.GetComponent<HUDController>();
            UIManager.Instance.SetHUDController(hudRoot);
        }
        else if (scene.name == "PauseMenuScene")
        {
            var pauseRoot = GameObject.Find("PauseMenuRoot")?.GetComponent<PauseMenuController>();
            UIManager.Instance.SetPauseController(pauseRoot);
        }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        // Выгружаем UI сцены при выходе, только если загружены
        var hudScene = SceneManager.GetSceneByName("HUDScene");
        if (hudScene.isLoaded) SceneManager.UnloadSceneAsync("HUDScene");

        var pauseScene = SceneManager.GetSceneByName("PauseMenuScene");
        if (pauseScene.isLoaded) SceneManager.UnloadSceneAsync("PauseMenuScene");
    }
}