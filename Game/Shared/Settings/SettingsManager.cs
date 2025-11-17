using UnityEngine;
using System;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    public GameSettings CurrentSettings { get; private set; }

    /// <summary>
    /// Событие — UI может подписаться и обновить язык
    /// </summary>
    public static event Action<SystemLanguage> OnLanguageChanged;

    /// <summary>
    /// UI может подписаться и обновить разрешение списка
    /// </summary>
    public static event Action<int> OnResolutionChanged;

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
                Debug.Log($"Instance exists: {Instance != null}");
                Debug.Log($"CurrentSettings loaded: {CurrentSettings != null}");
                Debug.Log($"Language: {CurrentSettings?.language}");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // -------------------------------------------------------
    // LOAD / SAVE
    // -------------------------------------------------------

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
            catch
            {
                Debug.LogError("SettingsManager: invalid saved settings, rebuilding defaults.");
                CreateDefaultSettings();
            }
        }
        else
        {
            CreateDefaultSettings();
        }

        ApplySettings();
    }

    private void CreateDefaultSettings()
    {
        CurrentSettings = new GameSettings();

        // язык по умолчанию
        CurrentSettings.language =
            Application.systemLanguage == SystemLanguage.Russian
            ? SystemLanguage.Russian
            : SystemLanguage.English;

        if (debugMode)
            Debug.Log($"Default language chosen: {CurrentSettings.language}");
    }

    public void SaveSettings(GameSettings newSettings)
    {
        if (newSettings == null)
        {
            Debug.LogError("SettingsManager: Cannot save NULL settings!");
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
            Debug.Log($"Player name: {CurrentSettings.playerName}");
        }
    }

    // -------------------------------------------------------
    // APPLY
    // -------------------------------------------------------

    private void ApplySettings()
    {
        if (CurrentSettings == null)
        {
            Debug.LogError("SettingsManager: CurrentSettings is NULL");
            return;
        }

        // Audio
        AudioListener.volume = CurrentSettings.soundVolume;

        // Quality
        if (CurrentSettings.qualityLevel >= 0 &&
            CurrentSettings.qualityLevel < QualitySettings.names.Length)
        {
            QualitySettings.SetQualityLevel(CurrentSettings.qualityLevel);
        }

        // Fullscreen
        Screen.fullScreen = CurrentSettings.fullscreen;

        // Language — только событие
        ApplyLanguage();

        // Resolution
        ApplyResolution();
    }

    private void ApplyLanguage()
    {
        // уведомляем всех слушателей, кто занимается UI
        OnLanguageChanged?.Invoke(CurrentSettings.language);

        if (debugMode)
            Debug.Log($"Language event fired: {CurrentSettings.language}");
    }

    private void ApplyResolution()
    {
        Resolution[] resolutions = Screen.resolutions;

        if (CurrentSettings.resolutionIndex >= 0 &&
            CurrentSettings.resolutionIndex < resolutions.Length)
        {
            var r = resolutions[CurrentSettings.resolutionIndex];
            Screen.SetResolution(r.width, r.height, CurrentSettings.fullscreen);

            if (debugMode)
                Debug.Log($"Resolution set: {r.width}x{r.height}");
        }

        // уведомляем UI менеджер, если ему нужно обновить dropdown
        OnResolutionChanged?.Invoke(CurrentSettings.resolutionIndex);
    }

    // -------------------------------------------------------

    public static bool IsReady()
    {
        return Instance != null && Instance.CurrentSettings != null;
    }
}
