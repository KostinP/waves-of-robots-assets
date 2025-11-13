using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class SettingsFeature : MonoBehaviour
{
    public static SettingsFeature Instance { get; private set; }

    [SerializeField] private VisualTreeAsset settingsUxml;

    private UISettingsManager uiSettingsManager;
    private VisualElement settingsRoot;
    private bool isInitialized = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Проверяем настройки сразу
            if (settingsUxml == null)
            {
                Debug.LogError("SettingsFeature: settingsUxml не назначен в инспекторе!");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Initialize(VisualElement mainRoot, UIDocument mainUiDoc, MainMenuController controller)
    {
        if (isInitialized) return;

        if (settingsUxml == null)
        {
            Debug.LogError("SettingsFeature: Не могу инициализироваться - settingsUxml is null!");
            return;
        }

        // Клонируем UXML для настроек в основной root
        settingsRoot = settingsUxml.Instantiate();
        mainRoot.Add(settingsRoot);
        settingsRoot.style.display = DisplayStyle.None;

        // Ждем кадр для инициализации UI
        StartCoroutine(DelayedUISetup(mainUiDoc, controller));
    }

    private IEnumerator DelayedUISetup(UIDocument mainUiDoc, MainMenuController controller)
    {
        yield return new WaitForEndOfFrame();

        uiSettingsManager = new UISettingsManager(settingsRoot, mainUiDoc, controller);
        isInitialized = true;

        Debug.Log("SettingsFeature: Инициализация завершена успешно");
    }

    public void ShowSettingsScreen()
    {
        if (!isInitialized)
        {
            Debug.LogError("SettingsFeature: Не инициализирован! Вызовите Initialize() сначала.");
            return;
        }

        if (settingsRoot != null && uiSettingsManager != null)
        {
            settingsRoot.style.display = DisplayStyle.Flex;
            uiSettingsManager.LoadCurrentSettings();
            Debug.Log("SettingsFeature: Экран настроек показан");
        }
        else
        {
            Debug.LogError($"SettingsFeature: settingsRoot={settingsRoot != null}, uiSettingsManager={uiSettingsManager != null}");
        }
    }

    public void HideSettingsScreen()
    {
        if (settingsRoot != null)
        {
            settingsRoot.style.display = DisplayStyle.None;
        }
    }
}