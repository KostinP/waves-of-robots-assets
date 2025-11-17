using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class UISettingsManager
{
    private readonly VisualElement _root;
    private readonly UIDocument _uiDocument;
    private readonly MainMenuController _controller;

    // UI Elements
    private TextField _playerNameField;
    private RadioButtonGroup _weaponRadioGroup;
    private Slider _musicSlider;
    private Label _musicValue;
    private Slider _soundSlider;
    private Label _soundValue;
    private DropdownField _resolutionDropdown;
    private DropdownField _qualityDropdown;
    private Button _btnRussian;
    private Button _btnEnglish;
    private Button _btnSaveSettings;
    private Button _btnCancelSettings;

    // Data
    private List<Resolution> _resolutions;
    private string[] _qualityNames;

    public UISettingsManager(VisualElement settingsRoot, UIDocument uiDocument, MainMenuController controller)
    {
        _root = settingsRoot;
        _uiDocument = uiDocument;
        _controller = controller;

        if (_root == null)
        {
            Debug.LogError("UISettingsManager: settingsRoot is null!");
            return;
        }

        InitializeWithDelay();
    }

    private void InitializeWithDelay()
    {
        if (_uiDocument != null)
        {
            _uiDocument.StartCoroutine(InitializeCoroutine());
        }
        else
        {
            Debug.LogError("UISettingsManager: _uiDocument is null!");
        }
    }

    private IEnumerator InitializeCoroutine()
    {
        yield return null; // Ждём один кадр
        Initialize();
    }

    private void Initialize()
    {
        if (_root == null)
        {
            Debug.LogError("UISettingsManager: Cannot initialize - _root is null");
            return;
        }

        FindUIElements();
        SetupData();
        SetupEventHandlers();
        LoadCurrentSettings();
        Debug.Log("UISettingsManager initialized successfully");
    }

    private void FindUIElements()
    {
        if (_root == null)
        {
            Debug.LogError("UISettingsManager: _root is null in FindUIElements");
            return;
        }

        try
        {
            Debug.Log($"[UISettingsManager] Searching for UI elements in root: {_root.name}");

            _playerNameField = _root.Q<TextField>("playerNameField");
            if (_playerNameField == null)
            {
                Debug.LogError("[UISettingsManager] playerNameField не найден! Проверьте имя в UXML.");
                // Выведем все TextField для отладки
                var allTextFields = _root.Query<TextField>().ToList();
                Debug.Log($"[UISettingsManager] Найдено TextField: {allTextFields.Count}");
                foreach (var field in allTextFields)
                {
                    Debug.Log($"[UISettingsManager] TextField: {field.name}");
                }
            }
            else
            {
                Debug.Log("[UISettingsManager] playerNameField найден успешно");
            }

            // Остальные элементы...
            _musicSlider = _root.Q<Slider>("musicSlider");
            Debug.Log($"[UISettingsManager] musicSlider: {_musicSlider != null}");

            _musicValue = _root.Q<Label>("musicVolumeLabel");
            Debug.Log($"[UISettingsManager] musicVolumeLabel: {_musicValue != null}");

            _soundSlider = _root.Q<Slider>("soundVolumeSlider");
            Debug.Log($"[UISettingsManager] soundVolumeSlider: {_soundSlider != null}");

            _soundValue = _root.Q<Label>("soundVolumeLabel");
            Debug.Log($"[UISettingsManager] soundVolumeLabel: {_soundValue != null}");

            _resolutionDropdown = _root.Q<DropdownField>("resolutionDropdown");
            Debug.Log($"[UISettingsManager] resolutionDropdown: {_resolutionDropdown != null}");

            _qualityDropdown = _root.Q<DropdownField>("qualityDropdown");
            Debug.Log($"[UISettingsManager] qualityDropdown: {_qualityDropdown != null}");

            _btnRussian = _root.Q<Button>("russianButton");
            Debug.Log($"[UISettingsManager] russianButton: {_btnRussian != null}");

            _btnEnglish = _root.Q<Button>("englishButton");
            Debug.Log($"[UISettingsManager] englishButton: {_btnEnglish != null}");

            _btnSaveSettings = _root.Q<Button>("btnSave");
            Debug.Log($"[UISettingsManager] btnSave: {_btnSaveSettings != null}");

            _btnCancelSettings = _root.Q<Button>("cancelBtn");
            Debug.Log($"[UISettingsManager] cancelBtn: {_btnCancelSettings != null}");

            var rightPanel = _root.Q<VisualElement>("right-panel");
            if (rightPanel != null)
            {
                _weaponRadioGroup = rightPanel.Query<RadioButtonGroup>().First();
                if (_weaponRadioGroup == null)
                    Debug.LogWarning("[UISettingsManager] RadioButtonGroup не найден в right-panel!");
                else
                    Debug.Log("[UISettingsManager] RadioButtonGroup найден");
            }
            else
            {
                Debug.LogError("[UISettingsManager] right-panel не найден!");
            }

        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UISettingsManager] Error in FindUIElements: {e.Message}");
        }
    }

    private void SetupData()
    {
        if (_resolutionDropdown != null)
        {
            _resolutions = new List<Resolution>(Screen.resolutions);
            var options = _resolutions.Select(r => $"{r.width} x {r.height} ({r.refreshRate}Hz)").ToList();
            _resolutionDropdown.choices = options;
        }

        if (_qualityDropdown != null)
        {
            _qualityNames = QualitySettings.names;
            _qualityDropdown.choices = new List<string>(_qualityNames);
        }
    }

    private void SetupEventHandlers()
    {
        if (_musicSlider != null)
            _musicSlider.RegisterValueChangedCallback(OnMusicVolumeChanged);

        if (_soundSlider != null)
            _soundSlider.RegisterValueChangedCallback(OnSoundVolumeChanged);

        if (_btnRussian != null)
            _btnRussian.clicked += () => OnLanguageChanged(SystemLanguage.Russian);

        if (_btnEnglish != null)
            _btnEnglish.clicked += () => OnLanguageChanged(SystemLanguage.English);

        if (_btnSaveSettings != null)
            _btnSaveSettings.clicked += OnSaveSettings;

        if (_btnCancelSettings != null)
            _btnCancelSettings.clicked += OnCancelSettings;

        if (_root != null)
            _root.RegisterCallback<KeyDownEvent>(OnKeyDown);
    }

    public void LoadCurrentSettings()
    {
        if (!SettingsManager.IsReady())
        {
            Debug.LogWarning("[UISettingsManager] SettingsManager не готов. Повторная попытка через 0.1с...");
            if (_uiDocument != null)
                _uiDocument.StartCoroutine(RetryLoadSettings());
            return;
        }

        var s = SettingsManager.Instance.CurrentSettings;

        if (_playerNameField != null)
            _playerNameField.value = s.playerName ?? "Player";
        else
            Debug.LogWarning("[UISettingsManager] playerNameField == null → имя игрока не установлено");

        if (_weaponRadioGroup != null)
            _weaponRadioGroup.value = s.defaultWeaponIndex;
        else
            Debug.LogWarning("[UISettingsManager] weaponRadioGroup == null → оружие не выбрано");

        if (_musicSlider != null && _musicValue != null)
        {
            _musicSlider.value = s.musicVolume * 100f;
            UpdateMusicValue(s.musicVolume * 100f);
        }

        if (_soundSlider != null && _soundValue != null)
        {
            _soundSlider.value = s.soundVolume * 100f;
            UpdateSoundValue(s.soundVolume * 100f);
        }

        if (_resolutionDropdown != null && _resolutions != null && _resolutions.Count > 0)
        {
            int index = Mathf.Clamp(s.resolutionIndex, 0, _resolutions.Count - 1);
            _resolutionDropdown.index = index;
        }

        if (_qualityDropdown != null && _qualityNames != null && _qualityNames.Length > 0)
        {
            int index = Mathf.Clamp(s.qualityLevel, 0, _qualityNames.Length - 1);
            _qualityDropdown.index = index;
        }

        UpdateLanguageButtons(s.language);
    }

    private IEnumerator RetryLoadSettings()
    {
        yield return new WaitForSeconds(0.1f);
        if (SettingsManager.IsReady())
            LoadCurrentSettings();
        else
            Debug.LogError("[UISettingsManager] SettingsManager всё ещё не готов после задержки!");
    }

    private void UpdateLanguageButtons(SystemLanguage lang)
    {
        bool isRu = lang == SystemLanguage.Russian;

        if (_btnRussian != null)
        {
            _btnRussian.RemoveFromClassList("selected-language");
            if (isRu) _btnRussian.AddToClassList("selected-language");
        }

        if (_btnEnglish != null)
        {
            _btnEnglish.RemoveFromClassList("selected-language");
            if (!isRu) _btnEnglish.AddToClassList("selected-language");
        }
    }

    #region Event Handlers
    private void OnMusicVolumeChanged(ChangeEvent<float> evt)
    {
        UpdateMusicValue(evt.newValue);
    }

    private void OnSoundVolumeChanged(ChangeEvent<float> evt)
    {
        UpdateSoundValue(evt.newValue);
    }

    private void UpdateMusicValue(float v)
    {
        if (_musicValue != null)
            _musicValue.text = Mathf.RoundToInt(v).ToString();
    }

    private void UpdateSoundValue(float v)
    {
        if (_soundValue != null)
            _soundValue.text = Mathf.RoundToInt(v).ToString();
    }

    private void OnLanguageChanged(SystemLanguage lang)
    {
        UpdateLanguageButtons(lang);
        LocalizationManager.Instance?.LoadLanguage(lang, true);
    }

    private void OnSaveSettings()
    {
        SaveSettings();
    }

    private void OnCancelSettings()
    {
        CancelSettings();
    }

    private void OnKeyDown(KeyDownEvent evt)
    {
        if (evt.keyCode == KeyCode.Escape)
        {
            CancelSettings();
            evt.StopPropagation();
        }
    }
    #endregion

    #region Public Methods for MainMenuController
    public void SaveSettings()
    {
        Debug.Log("UISettingsManager: Saving settings...");
        
        if (!SettingsManager.IsReady())
        {
            Debug.LogWarning("[UISettingsManager] SettingsManager не готов → создаём временный");
            CreateEmergencySettingsManager();
            return;
        }

        var settings = CreateSettingsFromUI();
        SettingsManager.Instance.SaveSettings(settings);
        
        // Применяем настройки графики
        ApplyGraphicsSettings(settings);
        
        if (_uiDocument != null)
            _uiDocument.StartCoroutine(DelayedReturn());
    }

    public void CancelSettings()
    {
        Debug.Log("UISettingsManager: Cancelling settings changes");
        
        // Восстанавливаем оригинальные настройки
        LoadCurrentSettings();
        
        // Уведомляем MainMenuController о закрытии
        _controller?.HideSettingsScreen();
    }
    #endregion

    #region Emergency & Helpers
    private void CreateEmergencySettingsManager()
    {
        var obj = new GameObject("TempSettingsManager");
        var mgr = obj.AddComponent<SettingsManager>();
        if (_uiDocument != null)
            _uiDocument.StartCoroutine(SaveWithTempManager(mgr));
    }

    private IEnumerator SaveWithTempManager(SettingsManager mgr)
    {
        yield return new WaitUntil(() => SettingsManager.IsReady());
        var settings = CreateSettingsFromUI();
        SettingsManager.Instance.SaveSettings(settings);
        ApplyGraphicsSettings(settings);
        Object.Destroy(mgr.gameObject);
        _controller?.HideSettingsScreen();
    }

    private GameSettings CreateSettingsFromUI()
    {
        return new GameSettings
        {
            playerName = _playerNameField?.value ?? "Player",
            defaultWeaponIndex = _weaponRadioGroup?.value ?? 0,
            musicVolume = _musicSlider != null ? _musicSlider.value / 100f : 0.8f,
            soundVolume = _soundSlider != null ? _soundSlider.value / 100f : 0.8f,
            resolutionIndex = _resolutionDropdown?.index ?? 0,
            qualityLevel = _qualityDropdown?.index ?? 2,
            language = (_btnRussian != null && _btnRussian.ClassListContains("selected-language"))
                ? SystemLanguage.Russian
                : SystemLanguage.English
        };
    }

    private void ApplyGraphicsSettings(GameSettings settings)
    {
        // Применяем разрешение
        if (_resolutions != null && settings.resolutionIndex >= 0 && settings.resolutionIndex < _resolutions.Count)
        {
            var resolution = _resolutions[settings.resolutionIndex];
            Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreenMode, resolution.refreshRate);
            Debug.Log($"Applied resolution: {resolution.width}x{resolution.height}@{resolution.refreshRate}Hz");
        }

        // Применяем качество графики
        if (settings.qualityLevel >= 0 && settings.qualityLevel < QualitySettings.names.Length)
        {
            QualitySettings.SetQualityLevel(settings.qualityLevel, true);
            Debug.Log($"Applied quality level: {QualitySettings.names[settings.qualityLevel]}");
        }

        // Применяем звуковые настройки
        ApplyAudioSettings(settings);
    }

    private void ApplyAudioSettings(GameSettings settings)
    {
        // Здесь можно добавить логику применения звуковых настроек
        // Например, через AudioManager если он есть
        Debug.Log($"Audio settings - Music: {settings.musicVolume}, Sound: {settings.soundVolume}");
    }

    private IEnumerator DelayedReturn()
    {
        yield return new WaitForSeconds(0.1f);
        LocalizationManager.Instance?.UpdateAllUIElements();
        _controller?.HideSettingsScreen();
    }
    #endregion

    public void Cleanup()
    {
        if (_root != null)
            _root.UnregisterCallback<KeyDownEvent>(OnKeyDown);

        if (_btnRussian != null)
            _btnRussian.clicked -= () => OnLanguageChanged(SystemLanguage.Russian);

        if (_btnEnglish != null)
            _btnEnglish.clicked -= () => OnLanguageChanged(SystemLanguage.English);

        if (_btnSaveSettings != null)
            _btnSaveSettings.clicked -= OnSaveSettings;

        if (_btnCancelSettings != null)
            _btnCancelSettings.clicked -= OnCancelSettings;
    }
}