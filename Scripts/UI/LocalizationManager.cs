using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    public event Action OnLanguageChanged;

    [SerializeField] private SystemLanguage _defaultLanguage = SystemLanguage.English;

    private Dictionary<string, string> _currentLocalization = new Dictionary<string, string>();
    private UIDocument _uiDocument;
    private SystemLanguage _currentLanguage;
    private VisualElement _rootVisualElement;

    // Кэш для всех UIDocument'ов в сцене
    private List<UIDocument> _allUiDocuments = new List<UIDocument>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _uiDocument = GetComponent<UIDocument>();
            
            // Находим все UIDocument'ы в сцене
            RefreshUiDocuments();
            
            // Подписываемся на событие изменения настроек
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.OnSettingsChanged += OnSettingsChanged;
            }
            
            Debug.Log("LocalizationManager initialized");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Получаем root visual element
        if (_uiDocument != null)
        {
            _rootVisualElement = _uiDocument.rootVisualElement;
        }

        // Загружаем язык из настроек или используем системный
        if (SettingsManager.IsReady())
        {
            LoadLanguage(SettingsManager.Instance.CurrentSettings.language, false);
        }
        else
        {
            DetectAndLoadLanguage();
        }
    }

    // Обновляем список всех UIDocument'ов
    private void RefreshUiDocuments()
    {
        _allUiDocuments.Clear();
        _allUiDocuments.AddRange(FindObjectsOfType<UIDocument>());
        Debug.Log($"Found {_allUiDocuments.Count} UI documents in scene");
    }

    private void OnSettingsChanged(GameSettings newSettings)
    {
        // При изменении настроек меняем язык
        if (newSettings.language != _currentLanguage)
        {
            LoadLanguage(newSettings.language, true);
        }
    }

    private void DetectAndLoadLanguage()
    {
        SystemLanguage systemLanguage = Application.systemLanguage;

        if (systemLanguage == SystemLanguage.Russian || systemLanguage == SystemLanguage.English)
        {
            LoadLanguage(systemLanguage, false);
        }
        else
        {
            LoadLanguage(_defaultLanguage, false);
        }
    }

    public void LoadLanguage(SystemLanguage language, bool forceUpdate = true)
    {
        Debug.Log($"Loading language: {language}, forceUpdate: {forceUpdate}");
        
        // Если язык не изменился, выходим
        if (_currentLanguage == language && !forceUpdate)
            return;

        _currentLanguage = language;
        string langCode = GetLanguageCode(language);
        var localizationFile = Resources.Load<TextAsset>($"Localization/{langCode}");

        if (localizationFile == null)
        {
            Debug.LogWarning($"Localization file for {language} not found, using default");
            localizationFile = Resources.Load<TextAsset>($"Localization/en");
        }

        if (localizationFile != null)
        {
            ParseLocalizationFile(localizationFile.text);
            
            // Всегда обновляем UI при смене языка
            UpdateAllUIElements();

            // Вызываем событие об изменении языка
            OnLanguageChanged?.Invoke();
            
            Debug.Log($"Language changed to: {language}, UI updated");
        }
        else
        {
            Debug.LogError("Default localization file not found!");
        }
    }

    public void SwitchLanguage()
    {
        var newLanguage = _currentLanguage == SystemLanguage.English ?
            SystemLanguage.Russian : SystemLanguage.English;
        LoadLanguage(newLanguage, true);
    }

    private string GetLanguageCode(SystemLanguage language)
    {
        return language == SystemLanguage.Russian ? "ru" : "en";
    }

    private void ParseLocalizationFile(string content)
    {
        _currentLocalization.Clear();

        try
        {
            var lines = content.Split('\n');
            foreach (var line in lines)
            {
                if (line.Trim().StartsWith("\"") && line.Contains(":"))
                {
                    var parts = line.Split(new[] { ':' }, 2);
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim().Trim('"', ' ', '\t');
                        var value = parts[1].Trim().Trim('"', ',', ' ', '\t');
                        _currentLocalization[key] = value;
                    }
                }
            }
            Debug.Log($"Parsed {_currentLocalization.Count} localization entries");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing localization file: {e.Message}");
        }
    }

    public string GetLocalizedText(string key)
    {
        if (_currentLocalization.ContainsKey(key))
            return _currentLocalization[key];
        
        Debug.LogWarning($"Localization key not found: {key}");
        return $"#{key}";
    }

    public void UpdateAllUIElements()
    {
        Debug.Log("Updating ALL UI elements with new language");
        
        // Обновляем все UIDocument'ы в сцене
        foreach (var uiDoc in _allUiDocuments)
        {
            if (uiDoc != null && uiDoc.rootVisualElement != null)
            {
                UpdateElementsWithPrefix(uiDoc.rootVisualElement, "#");
            }
        }
        
        Debug.Log($"Updated {_allUiDocuments.Count} UI documents");
    }

    private void UpdateElementsWithPrefix(VisualElement root, string prefix)
    {
        if (root == null) return;

        int updatedCount = 0;

        // ── LABELS ─────────────────────────────────────
        root.Query<Label>().ForEach(label =>
        {
            if (!string.IsNullOrEmpty(label.text) && label.text.StartsWith(prefix))
            {
                string key = label.text.Substring(prefix.Length);
                string localized = GetLocalizedText(key);
                if (label.text != localized)
                {
                    label.text = localized;
                    updatedCount++;
                }
            }
        });

        // ── BUTTONS ────────────────────────────────────
        root.Query<Button>().ForEach(button =>
        {
            if (!string.IsNullOrEmpty(button.text) && button.text.StartsWith(prefix))
            {
                string key = button.text.Substring(prefix.Length);
                string localized = GetLocalizedText(key);
                if (button.text != localized)
                {
                    button.text = localized;
                    updatedCount++;
                }
            }
        });

        // ── TEXTFIELDS (label) ─────────────────────────
        root.Query<TextField>().ForEach(textField =>
        {
            if (!string.IsNullOrEmpty(textField.label) && textField.label.StartsWith(prefix))
            {
                string key = textField.label.Substring(prefix.Length);
                string localized = GetLocalizedText(key);
                if (textField.label != localized)
                {
                    textField.label = localized;
                    updatedCount++;
                }
            }
        });

        // ── TEXTFIELDS (placeholder) ───────────────────
        root.Query<TextField>().ForEach(textField =>
        {
            var placeholder = textField.Q<Label>(className: "unity-base-text-field__placeholder") 
                           ?? textField.Q<Label>(className: "unity-text-field__placeholder");
            
            if (placeholder != null && !string.IsNullOrEmpty(placeholder.text) && placeholder.text.StartsWith(prefix))
            {
                string key = placeholder.text.Substring(prefix.Length);
                string localized = GetLocalizedText(key);
                if (placeholder.text != localized)
                {
                    placeholder.text = localized;
                    updatedCount++;
                }
            }
        });

        // ── INTEGERFIELD ───────────────────────────────
        root.Query<IntegerField>().ForEach(field =>
        {
            if (!string.IsNullOrEmpty(field.label) && field.label.StartsWith(prefix))
            {
                string key = field.label.Substring(prefix.Length);
                string localized = GetLocalizedText(key);
                if (field.label != localized)
                {
                    field.label = localized;
                    updatedCount++;
                }
            }
        });

        // ── Рекурсия по детям ──────────────────────────
        foreach (VisualElement child in root.Children())
            UpdateElementsWithPrefix(child, prefix);

        if (updatedCount > 0)
        {
            Debug.Log($"Updated {updatedCount} elements in {root.name}");
        }
    }

    public SystemLanguage GetCurrentLanguage()
    {
        return _currentLanguage;
    }

    // Метод для принудительного обновления при смене сцены
    public void RefreshForNewScene()
    {
        RefreshUiDocuments();
        UpdateAllUIElements();
    }

    private void OnDestroy()
    {
        // Отписываемся от события
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSettingsChanged -= OnSettingsChanged;
        }
    }
}