//Scripts/Core/UI/Runtime/LocalizationManager.cs
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using System.Collections;
using UnityEngine.SceneManagement;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    private Dictionary<string, string> _currentLocalization = new Dictionary<string, string>();
    private List<UIDocument> _allUiDocuments = new List<UIDocument>();

    private SystemLanguage _currentLanguage = SystemLanguage.English;

    [SerializeField] private SystemLanguage _defaultLanguage = SystemLanguage.English;

    // ---------------------------------------------------------
    // LIFECYCLE
    // ---------------------------------------------------------

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            SceneManager.sceneLoaded += OnSceneLoaded;

            Debug.Log("LocalizationManager initialized");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        // Подписка на изменения языка (только событие!)
        SettingsManager.OnLanguageChanged += LoadLanguage;
    }

    private void OnDisable()
    {
        SettingsManager.OnLanguageChanged -= LoadLanguage;
    }

    private void Start()
    {
        // Если SettingsManager уже готов — загрузим язык
        if (SettingsManager.IsReady())
            LoadLanguage(SettingsManager.Instance.CurrentSettings.language);
        else
            LoadLanguage(DetectLanguage());
    }

    // ---------------------------------------------------------
    // LANGUAGE LOADING
    // ---------------------------------------------------------

    private SystemLanguage DetectLanguage()
    {
        return Application.systemLanguage == SystemLanguage.Russian
            ? SystemLanguage.Russian
            : _defaultLanguage;
    }

    public void LoadLanguage(SystemLanguage language)
    {
        Debug.Log($"Localization: loading language {language}");

        if (_currentLanguage == language)
        {
            Debug.Log("Localization: language already applied, skipping");
            return;
        }

        _currentLanguage = language;

        string langCode = GetLanguageCode(language);
        TextAsset file = Resources.Load<TextAsset>($"Localization/{langCode}");

        if (file == null)
        {
            Debug.LogWarning($"Localization file missing for {language}, using English");
            file = Resources.Load<TextAsset>("Localization/en");
        }

        ParseLocalization(file?.text);
        RefreshUiDocuments();
        UpdateAllUIElements();

        Debug.Log($"Localization applied: {language}");
    }

    private string GetLanguageCode(SystemLanguage lang)
    {
        switch (lang)
        {
            case SystemLanguage.Russian: return "ru";
            case SystemLanguage.English: return "en";
            default: return "en";
        }
    }

    // ---------------------------------------------------------
    // PARSE
    // ---------------------------------------------------------

    private void ParseLocalization(string content)
    {
        _currentLocalization.Clear();

        if (string.IsNullOrEmpty(content))
        {
            Debug.LogError("Localization file is empty!");
            return;
        }

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
                        var key = parts[0].Trim().Trim('"');
                        var value = parts[1].Trim().Trim('"', ',', ' ');
                        _currentLocalization[key] = value;
                    }
                }
            }

            Debug.Log($"Localization parsed: {_currentLocalization.Count} entries");
        }
        catch (Exception e)
        {
            Debug.LogError($"Localization parse error: {e.Message}");
        }
    }

    // ---------------------------------------------------------
    // UI UPDATE
    // ---------------------------------------------------------

    private void RefreshUiDocuments()
    {
        _allUiDocuments.Clear();
        _allUiDocuments.AddRange(FindObjectsOfType<UIDocument>());

        Debug.Log($"Localization: found {_allUiDocuments.Count} UI documents");
    }

    public void UpdateAllUIElements()
    {
        foreach (var doc in _allUiDocuments)
        {
            if (doc?.rootVisualElement != null)
                UpdateElementsWithPrefix(doc.rootVisualElement, "#");
        }
    }

    private void UpdateElementsWithPrefix(VisualElement root, string prefix)
    {
        if (root == null) return;

        int updated = 0;

        // Labels
        root.Query<Label>().ForEach(label =>
        {
            if (label.text != null && label.text.StartsWith(prefix))
                updated += TryLocalize(label, label.text.Substring(prefix.Length));
        });

        // Buttons
        root.Query<Button>().ForEach(btn =>
        {
            if (btn.text != null && btn.text.StartsWith(prefix))
                updated += TryLocalize(btn, btn.text.Substring(prefix.Length));
        });

        // TextField label
        root.Query<TextField>().ForEach(tf =>
        {
            if (tf.label != null && tf.label.StartsWith(prefix))
                updated += TryLocalize(tf, tf.label.Substring(prefix.Length), true);

            var ph = tf.Q<Label>(className: "unity-base-text-field__placeholder")
                  ?? tf.Q<Label>(className: "unity-text-field__placeholder");

            if (ph != null && ph.text.StartsWith(prefix))
                updated += TryLocalize(ph, ph.text.Substring(prefix.Length));
        });

        if (updated > 0)
            Debug.Log($"Localization updated {updated} elements in {root.name}");
    }

    private int TryLocalize(object element, string key, bool isLabel = false)
    {
        if (!_currentLocalization.TryGetValue(key, out string val))
            return 0;

        switch (element)
        {
            case Label lbl:
                lbl.text = val; return 1;
            case Button btn:
                btn.text = val; return 1;
            case TextField tf:
                if (isLabel) tf.label = val;
                return 1;
            default:
                return 0;
        }
    }

    // ---------------------------------------------------------
    // SCENE EVENTS
    // ---------------------------------------------------------

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshUiDocuments();
        UpdateAllUIElements();
    }

    // ---------------------------------------------------------

    public string GetLocalizedText(string key)
    {
        if (_currentLocalization.TryGetValue(key, out var value))
            return value;

        return $"#{key}";
    }
}
