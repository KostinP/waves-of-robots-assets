using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance => _instance;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeManagers();

        // Загружаем главное меню
        SceneManager.LoadScene("MainMenu");
    }

    private void InitializeManagers()
    {
        // Создаем только отсутствующие менеджеры
        CreateIfMissing<LocalizationManager>();
        CreateIfMissing<UIManager>();
        CreateIfMissing<SettingsManager>();
        CreateIfMissing<LobbyManager>(); // Добавляем LobbyManager
    }

    private void CreateIfMissing<T>() where T : MonoBehaviour
    {
        if (FindObjectOfType<T>() != null) return;

        GameObject go = new GameObject(typeof(T).Name);
        go.AddComponent<T>();
        DontDestroyOnLoad(go);
    }

    public void LoadMainMenu() => SceneManager.LoadScene("MainMenu");
    public void LoadGame() => SceneManager.LoadScene("Game");
}