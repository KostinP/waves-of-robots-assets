using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;

    [Header("Manager Prefabs")]
    public GameObject settingsManagerPrefab;
    public GameObject localizationManagerPrefab;
    public GameObject uiManagerPrefab;

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
    }

    private void InitializeManagers()
    {
        CreateIfMissing<SettingsManager>(settingsManagerPrefab);
        CreateIfMissing<LocalizationManager>(localizationManagerPrefab);
        CreateIfMissing<UIManager>(uiManagerPrefab);
    }

    private void CreateIfMissing<T>(GameObject prefab) where T : MonoBehaviour
    {
        if (FindObjectOfType<T>() != null) return;

        GameObject go;
        if (prefab != null)
        {
            go = Instantiate(prefab);
        }
        else
        {
            go = new GameObject(typeof(T).Name);
            go.AddComponent<T>();
        }
        DontDestroyOnLoad(go);
    }
}