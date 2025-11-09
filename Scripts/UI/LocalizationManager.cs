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

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _uiDocument = GetComponent<UIDocument>();

            // Подписываемся на событие изменения настроек
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.OnSettingsChanged += OnSettingsChanged;
            }

            // Не загружаем язык здесь - ждем SettingsManager
            Debug.Log("LocalizationManager initialized, waiting for SettingsManager");
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

        // Если SettingsManager уже готов, загружаем язык
        if (SettingsManager.IsReady())
        {
            LoadLanguage(SettingsManager.Instance.CurrentSettings.language);
        }
        else
        {
            // Иначе используем системный язык
            DetectAndLoadLanguage();
        }
    }

    private void OnSettingsChanged(GameSettings newSettings)
    {
        // При изменении настроек меняем язык
        if (newSettings.language != _currentLanguage)
        {
            LoadLanguage(newSettings.language);
        }
    }

    private void DetectAndLoadLanguage()
    {
        SystemLanguage systemLanguage = Application.systemLanguage;

        // Поддерживаемые языки
        if (systemLanguage == SystemLanguage.Russian || systemLanguage == SystemLanguage.English)
        {
            LoadLanguage(systemLanguage);
        }
        else
        {
            LoadLanguage(_defaultLanguage);
        }
    }

    public void LoadLanguage(SystemLanguage language)
    {
        Debug.Log($"Loading language: {language}");

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
            UpdateAllUIElements();

            // Вызываем событие об изменении языка
            OnLanguageChanged?.Invoke();

            Debug.Log($"Language changed to: {language}");
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
        LoadLanguage(newLanguage);
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
        if (_uiDocument == null || _uiDocument.rootVisualElement == null)
        {
            Debug.LogWarning("UI Document or root visual element is null");
            return;
        }

        UpdateElementsWithPrefix(_uiDocument.rootVisualElement, "#");
        Debug.Log("UI elements updated with new language");
    }

    private void UpdateElementsWithPrefix(VisualElement root, string prefix)
    {
        if (root == null) return;

        // ── LABELS ─────────────────────────────────────
        root.Query<Label>().ForEach(label =>
        {
            if (!string.IsNullOrEmpty(label.text) && label.text.StartsWith(prefix))
            {
                string key = label.text.Substring(prefix.Length);
                label.text = GetLocalizedText(key);
            }
        });

        // ── BUTTONS ────────────────────────────────────
        root.Query<Button>().ForEach(button =>
        {
            if (!string.IsNullOrEmpty(button.text) && button.text.StartsWith(prefix))
            {
                string key = button.text.Substring(prefix.Length);
                button.text = GetLocalizedText(key);
            }
        });

        // ── TEXTFIELDS (label) ─────────────────────────
        root.Query<TextField>().ForEach(textField =>
        {
            if (!string.IsNullOrEmpty(textField.label) && textField.label.StartsWith(prefix))
            {
                string key = textField.label.Substring(prefix.Length);
                textField.label = GetLocalizedText(key);
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
                placeholder.text = GetLocalizedText(key);
            }
        });

        // ── INTEGERFIELD ───────────────────────────────
        root.Query<IntegerField>().ForEach(field =>
        {
            if (!string.IsNullOrEmpty(field.label) && field.label.StartsWith(prefix))
            {
                string key = field.label.Substring(prefix.Length);
                field.label = GetLocalizedText(key);
            }
        });

        // ── DROPDOWNFIELD ──────────────────────────────
        root.Query<DropdownField>().ForEach(dropdown =>
        {
            // Обновляем выбранное значение если оно локализовано
            if (!string.IsNullOrEmpty(dropdown.value) && dropdown.value.StartsWith(prefix))
            {
                string key = dropdown.value.Substring(prefix.Length);
                dropdown.value = GetLocalizedText(key);
            }
        });

        // ── Рекурсия по детям ──────────────────────────
        foreach (VisualElement child in root.Children())
            UpdateElementsWithPrefix(child, prefix);
    }

    public SystemLanguage GetCurrentLanguage()
    {
        return _currentLanguage;
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