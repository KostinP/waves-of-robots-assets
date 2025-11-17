using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections;

public class SettingsFeature : MonoBehaviour
{
    public static SettingsFeature Instance { get; private set; }

    public static event Action OnRequestOpenSettings;
    public static event Action OnRequestCloseSettings;
    public static event Action<GameSettings> OnRequestSaveSettings;

    [Header("UXML Template for Settings UI")]
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
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Initialize(VisualElement mainRoot, UIDocument mainUiDoc)
    {
        if (isInitialized) return;

        if (mainRoot == null)
        {
            Debug.LogError("SettingsFeature.Initialize: mainRoot is NULL!");
            return;
        }

        settingsRoot = settingsUxml.Instantiate();
        mainRoot.Add(settingsRoot);
        settingsRoot.style.display = DisplayStyle.None;

        StartCoroutine(DelayedUISetup(mainUiDoc));
    }

    private IEnumerator DelayedUISetup(UIDocument mainUiDoc)
    {
        yield return null;

        uiSettingsManager = new UISettingsManager(settingsRoot, mainUiDoc);
        isInitialized = true;

        Debug.Log("SettingsFeature: Initialized.");
    }

    public void ShowSettingsScreen()
    {
        if (!isInitialized) return;

        settingsRoot.style.display = DisplayStyle.Flex;
        uiSettingsManager.LoadCurrentSettings();

        OnRequestOpenSettings?.Invoke();
    }

    public void HideSettingsScreen()
    {
        if (!isInitialized) return;

        settingsRoot.style.display = DisplayStyle.None;
        OnRequestCloseSettings?.Invoke();
    }

    public void RequestSave(GameSettings settings)
    {
        OnRequestSaveSettings?.Invoke(settings);
    }
}
