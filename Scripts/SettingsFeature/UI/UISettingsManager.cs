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

    public UISettingsManager(VisualElement root, UIDocument uiDocument, MainMenuController controller)
    {
        _root = root;
        _uiDocument = uiDocument;
        _controller = controller;
        InitializeWithDelay();
    }

    private void InitializeWithDelay()
    {
        _uiDocument.StartCoroutine(InitializeCoroutine());
    }

    private IEnumerator InitializeCoroutine()
    {
        yield return null; // Ждём один кадр
        Initialize();
    }

    private void Initialize()
    {
        FindUIElements();
        SetupData();
        SetupEventHandlers();
        LoadCurrentSettings();
        Debug.Log("UISettingsManager initialized");
    }

    private void FindUIElements()
    {
        var settingsScreen = _root.Q<VisualElement>(UIScreenManager.SettingsScreenName);
        if (settingsScreen == null)
        {
            Debug.LogError($"[UISettingsManager] Экран '{UIScreenManager.SettingsScreenName}' не найден в UIDocument!");
            return;
        }

        _playerNameField = settingsScreen.Q<TextField>("playerNameField");
        if (_playerNameField == null) Debug.LogWarning("[UISettingsManager] playerNameField не найден!");

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

        var rightPanel = settingsScreen.Q<VisualElement>("right-panel");
        if (rightPanel != null)
        {
            _weaponRadioGroup = rightPanel.Query<RadioButtonGroup>().First();
            if (_weaponRadioGroup == null)
                Debug.LogWarning("[UISettingsManager] RadioButtonGroup не найден в right-panel!");
        }
        else
        {
            Debug.LogWarning("[UISettingsManager] right-panel не найден в settings_screen!");
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

        _root.RegisterCallback<KeyDownEvent>(OnKeyDown);
    }

    private void LoadCurrentSettings()
    {
        if (!SettingsManager.IsReady())
        {
            Debug.LogWarning("[UISettingsManager] SettingsManager не готов. Повторная попытка через 0.1с...");
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
        if (!SettingsManager.IsReady())
        {
            Debug.LogWarning("[UISettingsManager] SettingsManager не готов → создаём временный");
            CreateEmergencySettingsManager();
            return;
        }

        var settings = CreateSettingsFromUI();
        SettingsManager.Instance.SaveSettings(settings);

        _uiDocument.StartCoroutine(DelayedReturn());
    }

    private IEnumerator DelayedReturn()
    {
        yield return new WaitForSeconds(0.1f);
        LocalizationManager.Instance?.UpdateAllUIElements();
        _controller.ShowScreen(UIScreenManager.MenuScreenName);
    }

    private void OnCancelSettings()
    {
        _controller.ShowScreen(UIScreenManager.MenuScreenName);
    }

    private void OnKeyDown(KeyDownEvent evt)
    {
        if (evt.keyCode == KeyCode.Escape)
        {
            _controller.ShowScreen(UIScreenManager.MenuScreenName);
            evt.StopPropagation();
        }
    }

    #endregion

    #region Emergency & Helpers

    private void CreateEmergencySettingsManager()
    {
        var obj = new GameObject("TempSettingsManager");
        var mgr = obj.AddComponent<SettingsManager>();
        _uiDocument.StartCoroutine(SaveWithTempManager(mgr));
    }

    private IEnumerator SaveWithTempManager(SettingsManager mgr)
    {
        yield return new WaitUntil(() => SettingsManager.IsReady());
        var settings = CreateSettingsFromUI();
        SettingsManager.Instance.SaveSettings(settings);
        Object.Destroy(mgr.gameObject);
        _controller.ShowScreen(UIScreenManager.MenuScreenName);
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

    #endregion

    public void Cleanup()
    {
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