using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

public class UISettingsManager
{
    private readonly VisualElement _root;
    private readonly UIDocument _uiDocument;

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

    // Данные
    private List<Resolution> _resolutions;
    private string[] _qualityNames;

    public UISettingsManager(VisualElement root, UIDocument uiDocument)
    {
        _root = root;
        _uiDocument = uiDocument;

        // Ждем один кадр для инициализации SettingsManager
        InitializeWithDelay();
    }

    private void InitializeWithDelay()
    {
        // Ждем конца кадра для инициализации
        _uiDocument.StartCoroutine(InitializeCoroutine());
    }

    private System.Collections.IEnumerator InitializeCoroutine()
    {
        yield return null; // Ждем один кадр

        Initialize();
    }

    private void Initialize()
    {
        FindUIElements();
        SetupData();
        SetupEventHandlers();
        LoadCurrentSettings();
        Debug.Log("Settings manager initialized");
    }

    private void FindUIElements()
    {
        // Находим элементы в settings_screen
        var settingsScreen = _root.Q<VisualElement>("settings_screen");

        if (settingsScreen == null)
        {
            Debug.LogError("Settings screen not found!");
            return;
        }

        // Ищем элементы внутри settings_screen
        _playerNameField = settingsScreen.Q<TextField>("createLobbyName");
        _musicSlider = settingsScreen.Q<Slider>("musicSlider");
        _musicValue = settingsScreen.Q<Label>("musicVolumeLabel");
        _soundSlider = settingsScreen.Q<Slider>("soundVolumeSlider");
        _soundValue = settingsScreen.Q<Label>("soundVolumeLabel");
        _resolutionDropdown = settingsScreen.Q<DropdownField>("resolutionDropdown");
        _qualityDropdown = settingsScreen.Q<DropdownField>("qualityDropdown");
        _btnRussian = settingsScreen.Q<Button>("russianButton");
        _btnEnglish = settingsScreen.Q<Button>("englishButton");
        _btnSaveSettings = settingsScreen.Q<Button>("btnSave");
        _btnCancelSettings = settingsScreen.Q<Button>("cancelBtn");

        // Ищем RadioButtonGroup для оружия
        var rightPanel = settingsScreen.Q<VisualElement>("right-panel");
        if (rightPanel != null)
        {
            _weaponRadioGroup = rightPanel.Query<RadioButtonGroup>().First();
        }

        Debug.Log($"UI Elements found - PlayerName: {_playerNameField != null}, MusicSlider: {_musicSlider != null}, SaveBtn: {_btnSaveSettings != null}");
    }

    private void SetupData()
    {
        // Получаем доступные разрешения
        _resolutions = new List<Resolution>(Screen.resolutions);
        var resolutionOptions = new List<string>();
        for (int i = 0; i < _resolutions.Count; i++)
        {
            var res = _resolutions[i];
            resolutionOptions.Add($"{res.width} x {res.height} ({res.refreshRate}Hz)");
        }

        if (_resolutionDropdown != null)
        {
            _resolutionDropdown.choices = resolutionOptions;
            Debug.Log($"Resolutions loaded: {resolutionOptions.Count}");
        }
        else
        {
            Debug.LogWarning("Resolution dropdown not found!");
        }

        // Получаем названия качеств
        _qualityNames = QualitySettings.names;
        if (_qualityDropdown != null)
        {
            _qualityDropdown.choices = new List<string>(_qualityNames);
            Debug.Log($"Quality levels loaded: {_qualityNames.Length}");
        }
        else
        {
            Debug.LogWarning("Quality dropdown not found!");
        }
    }

    private void SetupEventHandlers()
    {
        // Слайдеры громкости
        if (_musicSlider != null)
        {
            _musicSlider.RegisterValueChangedCallback(OnMusicVolumeChanged);
            Debug.Log("Music slider event handler registered");
        }

        if (_soundSlider != null)
        {
            _soundSlider.RegisterValueChangedCallback(OnSoundVolumeChanged);
            Debug.Log("Sound slider event handler registered");
        }

        // Кнопки языка
        if (_btnRussian != null)
        {
            _btnRussian.clicked += () => OnLanguageChanged(SystemLanguage.Russian);
            Debug.Log("Russian button event handler registered");
        }

        if (_btnEnglish != null)
        {
            _btnEnglish.clicked += () => OnLanguageChanged(SystemLanguage.English);
            Debug.Log("English button event handler registered");
        }

        // Кнопки сохранения/отмены
        if (_btnSaveSettings != null)
        {
            _btnSaveSettings.clicked += OnSaveSettings;
            Debug.Log("Save button event handler registered");
        }

        if (_btnCancelSettings != null)
        {
            _btnCancelSettings.clicked += OnCancelSettings;
            Debug.Log("Cancel button event handler registered");
        }

        // Обработка Escape для возврата
        _root.RegisterCallback<KeyDownEvent>(OnKeyDown);
    }

    private System.Collections.IEnumerator RetryLoadSettings()
    {
        yield return new WaitForSeconds(0.1f); // Ждем 100ms

        if (SettingsManager.IsReady())
        {
            LoadCurrentSettings();
        }
        else
        {
            Debug.LogError("SettingsManager still not ready after retry!");
        }
    }

    private void LoadCurrentSettings()
    {
        // Ждем пока SettingsManager будет готов
        if (!SettingsManager.IsReady())
        {
            Debug.LogWarning("SettingsManager is not ready yet, retrying in next frame...");
            _uiDocument.StartCoroutine(RetryLoadSettings());
            return;
        }

        var settings = SettingsManager.Instance.CurrentSettings;

        Debug.Log($"Loading settings - Player: {settings.playerName}, Music: {settings.musicVolume}");


        if (_playerNameField != null)
        {
            _playerNameField.value = settings.playerName;
            Debug.Log($"Player name set to: {settings.playerName}");
        }

        if (_weaponRadioGroup != null)
        {
            _weaponRadioGroup.value = settings.defaultWeaponIndex;
            Debug.Log($"Weapon index set to: {settings.defaultWeaponIndex}");
        }

        if (_musicSlider != null && _musicValue != null)
        {
            _musicSlider.value = settings.musicVolume * 100;
            UpdateMusicValue(settings.musicVolume * 100);
            Debug.Log($"Music volume set to: {settings.musicVolume * 100}");
        }

        if (_soundSlider != null && _soundValue != null)
        {
            _soundSlider.value = settings.soundVolume * 100;
            UpdateSoundValue(settings.soundVolume * 100);
            Debug.Log($"Sound volume set to: {settings.soundVolume * 100}");
        }

        if (_resolutionDropdown != null)
        {
            if (settings.resolutionIndex < _resolutions.Count && settings.resolutionIndex >= 0)
            {
                _resolutionDropdown.index = settings.resolutionIndex;
                Debug.Log($"Resolution index set to: {settings.resolutionIndex}");
            }
            else
            {
                _resolutionDropdown.index = 0;
                Debug.Log($"Resolution index reset to 0 (was: {settings.resolutionIndex})");
            }
        }

        if (_qualityDropdown != null)
        {
            if (settings.qualityLevel < _qualityNames.Length && settings.qualityLevel >= 0)
            {
                _qualityDropdown.index = settings.qualityLevel;
                Debug.Log($"Quality level set to: {settings.qualityLevel}");
            }
            else
            {
                _qualityDropdown.index = 2; // Среднее качество по умолчанию
                Debug.Log($"Quality level reset to 2 (was: {settings.qualityLevel})");
            }
        }

        UpdateLanguageButtons(settings.language);
    }

    private void UpdateLanguageButtons(SystemLanguage currentLanguage)
    {
        bool isRussian = currentLanguage == SystemLanguage.Russian;

        if (_btnRussian != null)
        {
            _btnRussian.RemoveFromClassList("selected-language");
            if (isRussian) _btnRussian.AddToClassList("selected-language");
        }

        if (_btnEnglish != null)
        {
            _btnEnglish.RemoveFromClassList("selected-language");
            if (!isRussian) _btnEnglish.AddToClassList("selected-language");
        }

        Debug.Log($"Language buttons updated - Russian: {isRussian}");
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

    private void UpdateMusicValue(float value)
    {
        if (_musicValue != null)
            _musicValue.text = Mathf.RoundToInt(value).ToString();
    }

    private void UpdateSoundValue(float value)
    {
        if (_soundValue != null)
            _soundValue.text = Mathf.RoundToInt(value).ToString();
    }

    private void OnLanguageChanged(SystemLanguage language)
    {
        UpdateLanguageButtons(language);
        
        Debug.Log($"Language changed in UI to: {language}");
        
        // НЕМЕДЛЕННО применяем язык для предпросмотра
        if (LocalizationManager.Instance != null)
        {
            // Принудительно обновляем ВСЕ UI элементы
            LocalizationManager.Instance.LoadLanguage(language, true);
        }
        else
        {
            Debug.LogError("LocalizationManager instance is null!");
        }
    }

    private void OnSaveSettings()
    {
        Debug.Log("Save settings clicked");

        if (!SettingsManager.IsReady())
        {
            Debug.LogError("SettingsManager is not ready! Creating emergency settings...");
            CreateEmergencySettingsManager();
            return;
        }

        var newSettings = CreateSettingsFromUI();
        SettingsManager.Instance.SaveSettings(newSettings);

        // Язык уже должен быть применен через OnSettingsChanged,
        // но на всякий случай принудительно обновляем
        if (LocalizationManager.Instance != null)
        {
            // Ждем немного перед обновлением UI
            _uiDocument.StartCoroutine(DelayedUIRefresh());
        }
        else
        {
            ReturnToMainMenu();
        }
    }


    private System.Collections.IEnumerator DelayedUIRefresh()
    {
        yield return new WaitForSeconds(0.1f); // Ждем 100ms
        
        // Принудительно обновляем ВЕСЬ UI
        LocalizationManager.Instance.UpdateAllUIElements();
        
        // Возвращаемся на главный экран
        ReturnToMainMenu();
    }

    private void ReturnToMainMenu()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowScreen(UIScreenManager.MenuScreenName);
        }
    }


    private void CreateEmergencySettingsManager()
    {
        // Создаем временный SettingsManager
        GameObject tempObj = new GameObject("TempSettingsManager");
        var tempManager = tempObj.AddComponent<SettingsManager>();

        // Ждем инициализации
        _uiDocument.StartCoroutine(SaveWithTempManager(tempManager));
    }

    private System.Collections.IEnumerator SaveWithTempManager(SettingsManager tempManager)
    {
        yield return new WaitUntil(() => SettingsManager.IsReady());

        var newSettings = CreateSettingsFromUI();
        SettingsManager.Instance.SaveSettings(newSettings);

        // Удаляем временный объект
        Object.Destroy(tempManager.gameObject);

        // Возвращаемся на предыдущий экран
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowScreen(UIScreenManager.MenuScreenName);
        }
    }


    private GameSettings CreateSettingsFromUI()
    {
        var newSettings = new GameSettings();

        // Собираем настройки из UI
        if (_playerNameField != null)
        {
            newSettings.playerName = string.IsNullOrEmpty(_playerNameField.value) ? "Player" : _playerNameField.value;
            Debug.Log($"Player name: {newSettings.playerName}");
        }

        if (_weaponRadioGroup != null)
        {
            newSettings.defaultWeaponIndex = _weaponRadioGroup.value;
            Debug.Log($"Weapon index: {newSettings.defaultWeaponIndex}");
        }

        if (_musicSlider != null)
        {
            newSettings.musicVolume = _musicSlider.value / 100f;
            Debug.Log($"Music volume: {newSettings.musicVolume}");
        }

        if (_soundSlider != null)
        {
            newSettings.soundVolume = _soundSlider.value / 100f;
            Debug.Log($"Sound volume: {newSettings.soundVolume}");
        }

        if (_resolutionDropdown != null)
        {
            newSettings.resolutionIndex = _resolutionDropdown.index;
            Debug.Log($"Resolution index: {newSettings.resolutionIndex}");
        }

        if (_qualityDropdown != null)
        {
            newSettings.qualityLevel = _qualityDropdown.index;
            Debug.Log($"Quality level: {newSettings.qualityLevel}");
        }

        // Определяем выбранный язык
        newSettings.language = _btnRussian != null && _btnRussian.ClassListContains("selected-language") ?
            SystemLanguage.Russian : SystemLanguage.English;

        Debug.Log($"Selected language: {newSettings.language}");

        return newSettings;
    }

    private void OnCancelSettings()
    {
        Debug.Log("Cancel settings clicked");

        // Возвращаемся на предыдущий экран без сохранения
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowScreen(UIScreenManager.MenuScreenName);
        }
    }

    private void OnKeyDown(KeyDownEvent evt)
    {
        if (evt.keyCode == KeyCode.Escape)
        {
            // Возврат к предыдущему экрану без сохранения
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowScreen(UIScreenManager.MenuScreenName);
                evt.StopPropagation();
            }
        }
    }

    #endregion

    public void Cleanup()
    {
        _root.UnregisterCallback<KeyDownEvent>(OnKeyDown);

        if (_btnRussian != null) _btnRussian.clicked -= () => OnLanguageChanged(SystemLanguage.Russian);
        if (_btnEnglish != null) _btnEnglish.clicked -= () => OnLanguageChanged(SystemLanguage.English);
        if (_btnSaveSettings != null) _btnSaveSettings.clicked -= OnSaveSettings;
        if (_btnCancelSettings != null) _btnCancelSettings.clicked -= OnCancelSettings;
    }
}