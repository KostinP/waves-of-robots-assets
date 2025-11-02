using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    // Добавляем событие для уведомления об изменении языка
    public event Action OnLanguageChanged;

    [SerializeField] private SystemLanguage _defaultLanguage = SystemLanguage.English;

    private Dictionary<string, string> _currentLocalization = new Dictionary<string, string>();
    private UIDocument _uiDocument;
    private SystemLanguage _currentLanguage;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _uiDocument = GetComponent<UIDocument>();
            DetectAndLoadLanguage();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        UpdateUIElements();
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
            UpdateUIElements();

            // Вызываем событие об изменении языка
            OnLanguageChanged?.Invoke();
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
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing localization file: {e.Message}");
        }
    }

    public string GetLocalizedText(string key)
    {
        return _currentLocalization.ContainsKey(key) ? _currentLocalization[key] : $"#{key}";
    }

    public void UpdateUIElements()
    {
        if (_uiDocument == null || _uiDocument.rootVisualElement == null) return;

        UpdateElementsWithPrefix(_uiDocument.rootVisualElement, "#");
    }

    private void UpdateElementsWithPrefix(VisualElement root, string prefix)
    {
        // ── LABELS ─────────────────────────────────────
        root.Query<Label>().ForEach(label =>
        {
            if (!string.IsNullOrEmpty(label.text) && label.text.StartsWith(prefix))
                label.text = GetLocalizedText(label.text.Substring(prefix.Length));
        });

        // ── BUTTONS ────────────────────────────────────
        root.Query<Button>().ForEach(button =>
        {
            if (!string.IsNullOrEmpty(button.text) && button.text.StartsWith(prefix))
                button.text = GetLocalizedText(button.text.Substring(prefix.Length));
        });

        // ── TEXTFIELDS (label + placeholder) ───────────
        root.Query<TextField>().ForEach(textField =>
        {
            // 1. Локализуем обычный label
            if (!string.IsNullOrEmpty(textField.label) && textField.label.StartsWith(prefix))
                textField.label = GetLocalizedText(textField.label.Substring(prefix.Length));
        });

        // ── ОСОБАЯ ОБРАБОТКА ДЛЯ ПОЛЯ ПАРОЛЯ ───────────
        TextField passwordField = root.Q<TextField>("lobbyPassword");
        if (passwordField != null)
        {
            Label passwordPlaceholder = passwordField.Q<Label>(className: "unity-base-text-field__placeholder")
                                      ?? passwordField.Q<Label>(className: "unity-text-field__placeholder");

            if (passwordPlaceholder != null && passwordPlaceholder.text == "#enter_password")
            {
                passwordPlaceholder.text = GetLocalizedText("enter_password");
            }
        }

        // ── INTEGERFIELD ───────────────────────────────
        root.Query<IntegerField>().ForEach(field =>
        {
            if (!string.IsNullOrEmpty(field.label) && field.label.StartsWith(prefix))
                field.label = GetLocalizedText(field.label.Substring(prefix.Length));
        });

        // ── Рекурсия по детям ──────────────────────────
        foreach (VisualElement child in root.Children())
            UpdateElementsWithPrefix(child, prefix);
    }

    public SystemLanguage GetCurrentLanguage()
    {
        return _currentLanguage;
    }
}