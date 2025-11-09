using UnityEngine;
using System;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    public GameSettings CurrentSettings { get; private set; }
    public event Action<GameSettings> OnSettingsChanged;

    private const string SETTINGS_KEY = "GameSettings";

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();

            if (debugMode)
            {
                Debug.Log("=== SettingsManager Initialized ===");
                Debug.Log($"Instance: {Instance != null}");
                Debug.Log($"CurrentSettings: {CurrentSettings != null}");
                Debug.Log($"Language: {CurrentSettings?.language}");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadSettings()
    {
        string json = PlayerPrefs.GetString(SETTINGS_KEY, "");

        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                CurrentSettings = JsonUtility.FromJson<GameSettings>(json);
                if (debugMode) Debug.Log("Settings loaded from PlayerPrefs");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading settings: {e.Message}");
                CreateDefaultSettings();
            }
        }
        else
        {
            CreateDefaultSettings();
            if (debugMode) Debug.Log("Default settings created");
        }

        ApplySettings();
    }

    private void CreateDefaultSettings()
    {
        CurrentSettings = new GameSettings();
        // Определяем язык системы
        CurrentSettings.language = Application.systemLanguage == SystemLanguage.Russian ?
            SystemLanguage.Russian : SystemLanguage.English;

        if (debugMode) Debug.Log($"Default language set to: {CurrentSettings.language}");
    }

    public void SaveSettings(GameSettings newSettings)
    {
        if (newSettings == null)
        {
            Debug.LogError("Cannot save null settings!");
            return;
        }

        CurrentSettings = newSettings;
        string json = JsonUtility.ToJson(CurrentSettings);
        PlayerPrefs.SetString(SETTINGS_KEY, json);
        PlayerPrefs.Save();

        ApplySettings();
        OnSettingsChanged?.Invoke(CurrentSettings);

        if (debugMode)
        {
            Debug.Log("=== Settings Saved ===");
            Debug.Log($"Language: {CurrentSettings.language}");
            Debug.Log($"Music: {CurrentSettings.musicVolume}, Sound: {CurrentSettings.soundVolume}");
            Debug.Log($"Player: {CurrentSettings.playerName}");
        }
    }

    private void ApplySettings()
    {
        if (CurrentSettings == null)
        {
            Debug.LogError("CurrentSettings is null!");
            return;
        }

        // Применяем настройки аудио
        AudioListener.volume = CurrentSettings.soundVolume;

        // Применяем настройки графики
        if (CurrentSettings.qualityLevel >= 0 && CurrentSettings.qualityLevel < QualitySettings.names.Length)
        {
            QualitySettings.SetQualityLevel(CurrentSettings.qualityLevel);
        }

        Screen.fullScreen = CurrentSettings.fullscreen;

        // Применяем язык - ВАЖНО: всегда применяем язык, даже если LocalizationManager еще не готов
        ApplyLanguage();

        // Применяем разрешение экрана
        ApplyResolution();
    }

    private void ApplyLanguage()
    {
        if (LocalizationManager.Instance != null)
        {
            // Всегда принудительно обновляем язык
            LocalizationManager.Instance.LoadLanguage(CurrentSettings.language, true);
            Debug.Log($"Language applied: {CurrentSettings.language}");
        }
        else
        {
            Debug.LogWarning("LocalizationManager not found, retrying...");
            // Пробуем еще раз через кадр
            StartCoroutine(DelayedLanguageApply());
        }
    }

    private System.Collections.IEnumerator DelayedLanguageApply()
    {
        yield return new WaitForEndOfFrame();
        
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.LoadLanguage(CurrentSettings.language, true);
            Debug.Log($"Language applied after delay: {CurrentSettings.language}");
        }
        else
        {
            Debug.LogError("LocalizationManager still not found after delay!");
        }
    }

    private void RetryApplyLanguage()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.LoadLanguage(CurrentSettings.language);
        }
    }

    private void ApplyResolution()
    {
        Resolution[] resolutions = Screen.resolutions;
        if (CurrentSettings.resolutionIndex >= 0 && CurrentSettings.resolutionIndex < resolutions.Length)
        {
            Resolution resolution = resolutions[CurrentSettings.resolutionIndex];
            Screen.SetResolution(resolution.width, resolution.height, CurrentSettings.fullscreen);

            if (debugMode) Debug.Log($"Resolution set to: {resolution.width}x{resolution.height}");
        }
    }

    // Метод для быстрого доступа из других скриптов
    public static bool IsReady()
    {
        return Instance != null && Instance.CurrentSettings != null;
    }
}