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
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeManagers();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeManagers()
    {
        Debug.Log("Initializing managers...");

        // 1. SettingsManager
        if (SettingsManager.Instance == null)
        {
            if (settingsManagerPrefab != null)
            {
                Instantiate(settingsManagerPrefab);
            }
            else
            {
                GameObject settingsObj = new GameObject("SettingsManager");
                settingsObj.AddComponent<SettingsManager>();
            }
            Debug.Log("SettingsManager created");
        }

        // 2. LocalizationManager
        if (LocalizationManager.Instance == null)
        {
            if (localizationManagerPrefab != null)
            {
                Instantiate(localizationManagerPrefab);
            }
            else
            {
                // LocalizationManager уже должен быть на сцене
                LocalizationManager existing = FindObjectOfType<LocalizationManager>();
                if (existing == null)
                {
                    Debug.LogError("LocalizationManager not found!");
                }
            }
            Debug.Log("LocalizationManager checked");
        }

        // 3. UIManager
        if (UIManager.Instance == null)
        {
            if (uiManagerPrefab != null)
            {
                Instantiate(uiManagerPrefab);
            }
            else
            {
                // UIManager уже должен быть на сцене
                UIManager existing = FindObjectOfType<UIManager>();
                if (existing == null)
                {
                    Debug.LogError("UIManager not found!");
                }
            }
            Debug.Log("UIManager checked");
        }

        Debug.Log("All managers initialized");
    }
}