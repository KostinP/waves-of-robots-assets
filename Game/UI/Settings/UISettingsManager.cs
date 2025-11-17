using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
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

    // Data
    private List<Resolution> _resolutions;
    private string[] _qualityNames;

    public UISettingsManager(VisualElement settingsRoot, UIDocument uiDocument)
    {
        _root = settingsRoot;
        _uiDocument = uiDocument;

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
            _uiDocument.StartCoroutine(InitializeCoroutine());
        else
            Debug.LogError("UISettingsManager: _uiDocument is null!");
    }

    private IEnumerator InitializeCoroutine()
    {
        yield return null; // wait for UI construction
        Initialize();
    }


    // ─────────────────────────────────────────────
    // INITIALIZATION
    // ─────────────────────────────────────────────

    private void Initialize()
    {
        FindUIElements();
        SetupData();
        SetupEventHandlers();
        LoadCurrentSettings();

        Debug.Log("UISettingsManager initialized successfully");
    }


    private void FindUIElements()
    {
        Debug.Log($"[UISettingsManager] Finding elements under root: {_root.name}");

        _playerNameField = _root.Q<TextField>("playerNameField");
        _weaponRadioGroup = _root.Q<RadioButtonGroup>();
        _musicSlider = _root.Q<Slider>("musicSlider");
        _musicValue = _root.Q<Label>("musicVolumeLabel");
        _soundSlider = _root.Q<Slider>("soundVolumeSlider");
        _soundValue = _root.Q<Label>("soundVolumeLabel");
        _resolutionDropdown = _root.Q<DropdownField>("resolutionDropdown");
        _qualityDropdown = _root.Q<DropdownField>("qualityDropdown");

        _btnRussian = _root.Q<Button>("russianButton");
        _btnEnglish = _root.Q<Button>("englishButton");
        _btnSaveSettings = _root.Q<Button>("btnSave");
        _btnCancelSettings = _root.Q<Button>("cancelBtn");

        // Debug logs
        if (_playerNameField == null) Debug.LogError("playerNameField not found in UXML");
        if (_weaponRadioGroup == null) Debug.LogWarning("RadioButtonGroup not found");
        if (_musicSlider == null) Debug.LogWarning("musicSlider missing");
        if (_soundSlider == null) Debug.LogWarning("soundSlider missing");
        if (_resolutionDropdown == null) Debug.LogWarning("resolutionDropdown missing");
        if (_qualityDropdown == null) Debug.LogWarning("qualityDropdown missing");

        if (_btnSaveSettings == null) Debug.LogError("btnSave missing");
        if (_btnCancelSettings == null) Debug.LogError("cancelBtn missing");
    }


    private void SetupData()
    {
        // RESOLUTIONS
        if (_resolutionDropdown != null)
        {
            _resolutions = new List<Resolution>(Screen.resolutions);
            _resolutionDropdown.choices =
                _resolutions.Select(r => $"{r.width} x {r.height} ({r.refreshRate}Hz)").ToList();
        }

        // QUALITY
        if (_qualityDropdown != null)
        {
            _qualityNames = QualitySettings.names;
            _qualityDropdown.choices = new List<string>(_qualityNames);
        }
    }


    private void SetupEventHandlers()
    {
        if (_musicSlider != null)
            _musicSlider.RegisterValueChangedCallback(evt => UpdateMusicValue(evt.newValue));

        if (_soundSlider != null)
            _soundSlider.RegisterValueChangedCallback(evt => UpdateSoundValue(evt.newValue));

        if (_btnRussian != null)
            _btnRussian.clicked += () => OnLanguageChanged(SystemLanguage.Russian);

        if (_btnEnglish != null)
            _btnEnglish.clicked += () => OnLanguageChanged(SystemLanguage.English);

        if (_btnSaveSettings != null)
            _btnSaveSettings.clicked += OnSaveSettings;

        if (_btnCancelSettings != null)
            _btnCancelSettings.clicked += () =>
                SettingsFeature.Instance?.HideSettingsScreen();

        // ESC KEY
        _root.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (evt.keyCode == KeyCode.Escape)
            {
                SettingsFeature.Instance?.HideSettingsScreen();
                evt.StopPropagation();
            }
        });
    }


    // ─────────────────────────────────────────────
    // LOAD
    // ─────────────────────────────────────────────

    public void LoadCurrentSettings()
    {
        if (!SettingsManager.IsReady())
        {
            Debug.LogWarning("SettingsManager not ready → retry");
            if (_uiDocument != null)
                _uiDocument.StartCoroutine(RetryLoadSettings());
            return;
        }

        var s = SettingsManager.Instance.CurrentSettings;

        if (_playerNameField != null)
            _playerNameField.value = s.playerName ?? "Player";

        if (_weaponRadioGroup != null)
            _weaponRadioGroup.value = s.defaultWeaponIndex;

        if (_musicSlider != null)
        {
            _musicSlider.value = s.musicVolume * 100f;
            UpdateMusicValue(s.musicVolume * 100f);
        }

        if (_soundSlider != null)
        {
            _soundSlider.value = s.soundVolume * 100f;
            UpdateSoundValue(s.soundVolume * 100f);
        }

        if (_resolutionDropdown != null && _resolutions != null && _resolutions.Count > 0)
            _resolutionDropdown.index = Mathf.Clamp(s.resolutionIndex, 0, _resolutions.Count - 1);

        if (_qualityDropdown != null && _qualityNames != null)
            _qualityDropdown.index = Mathf.Clamp(s.qualityLevel, 0, _qualityNames.Length - 1);

        UpdateLanguageButtons(s.language);
    }


    private IEnumerator RetryLoadSettings()
    {
        yield return new WaitForSeconds(0.1f);
        LoadCurrentSettings();
    }


    // ─────────────────────────────────────────────
    // EVENT HANDLERS
    // ─────────────────────────────────────────────

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
        LocalizationManager.Instance?.LoadLanguage(lang);
    }

    private void UpdateLanguageButtons(SystemLanguage lang)
    {
        bool isRu = lang == SystemLanguage.Russian;

        if (_btnRussian != null)
        {
            _btnRussian.RemoveFromClassList("selected-language");
            if (isRu)
                _btnRussian.AddToClassList("selected-language");
        }

        if (_btnEnglish != null)
        {
            _btnEnglish.RemoveFromClassList("selected-language");
            if (!isRu)
                _btnEnglish.AddToClassList("selected-language");
        }
    }


    private void OnSaveSettings()
    {
        if (!SettingsManager.IsReady())
        {
            Debug.LogWarning("SettingsManager not ready → emergency instance created");
            CreateEmergencySettingsManager();
            return;
        }

        var settings = CreateSettingsFromUI();
        SettingsFeature.Instance?.RequestSave(settings);

        // Close settings window
        _uiDocument.StartCoroutine(CloseAfterDelay());
    }

    private IEnumerator CloseAfterDelay()
    {
        yield return new WaitForSeconds(0.1f);
        LocalizationManager.Instance?.UpdateAllUIElements();
        SettingsFeature.Instance?.HideSettingsScreen();
    }


    // ─────────────────────────────────────────────
    // DATA BUILDING
    // ─────────────────────────────────────────────

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


    // ─────────────────────────────────────────────
    // EMERGENCY FALLBACK
    // ─────────────────────────────────────────────

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
        SettingsFeature.Instance?.RequestSave(settings);

        Object.Destroy(mgr.gameObject);
        SettingsFeature.Instance?.HideSettingsScreen();
    }


    // ─────────────────────────────────────────────
    // CLEANUP
    // ─────────────────────────────────────────────

    public void Cleanup()
    {
        // remove events if needed
    }
}
