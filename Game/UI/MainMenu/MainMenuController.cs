using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuController : MonoBehaviour
{
    private VisualElement _root;

    public UIScreenManager ScreenManager { get; private set; }
    public UIInputManager InputManager { get; private set; }
    public UILobbySetupManager LobbySetupManager { get; private set; }
    public UICharacterSelectionManager CharacterSelectionManager { get; private set; }

    private LobbyDiscoveryService _discovery;

    private const int PlayerVersion = 1;

    private void Awake()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;

        _discovery = FindObjectOfType<LobbyDiscoveryService>();

        ScreenManager = new UIScreenManager(_root, this);
        LobbySetupManager = new UILobbySetupManager(_root, this);
        CharacterSelectionManager = new UICharacterSelectionManager(_root);

        InputManager = new UIInputManager(UIManager.Instance.InputActions, _root);
    }

    // ---------------------- UI Switching ----------------------
    public void ShowScreen(string screenName)
    {
        ScreenManager.ShowScreen(screenName);
    }

    public void HideSettingsScreen()
    {
        ScreenManager.ShowScreen(UIScreenManager.MenuScreenName);
    }

    // ---------------------- Lobby Creation ----------------------
    public void CreateLobby()
    {
        var data = LobbySetupManager.GetLobbyData();

        Debug.Log($"Creating lobby: {data.Name}, players={data.MaxPlayers}");

        LobbyClientRequests.SendCreateLobby(
            data.Name.ToString(),
            data.Password.ToString(),
            data.MaxPlayers,
            CharacterSelectionManager.GetSelectedCharacter()
        );

        ShowScreen(UIScreenManager.LobbySettingsScreenName);
    }

    // ---------------------- Preconnect + Join (NEW) ----------------------
    // Called from UIScreenManager.CreateLobbyItem when player presses Join.
    // info - LobbyInfo from discovery; password - value from TextField (may be empty)
    // onError - callback invoked when preconnect fails (string used to show error / highlight)
    public void JoinLobby(LobbyInfo info, string password, Action<string> onError)
    {
        if (_discovery == null)
        {
            Debug.LogError("MainMenuController: LobbyDiscoveryService not found.");
            onError?.Invoke("internal_error");
            return;
        }

        if (string.IsNullOrEmpty(info.Ip))
        {
            Debug.LogWarning("JoinLobby: info.Ip is empty");
            onError?.Invoke("invalid_address");
            return;
        }

        // Determine query port: discovery.broadcastPort + queryPortOffset (same as server)
        int queryPort = _discovery.broadcastPort + _discovery.queryPortOffset;

        string playerName = SettingsManager.Instance?.CurrentSettings?.playerName ?? "Player";

        // Start preconnect coroutine
        StartCoroutine(LobbyPreconnectClient.StartJoinQueryCoroutine(
            this,
            info.Ip,
            queryPort,
            password,
            (result) =>
            {
                // Run on main thread (we are in coroutine callback)
                switch (result)
                {
                    case PreconnectResult.Ok:
                        Debug.Log($"Preconnect OK -> connecting to host {info.Ip}:{info.Port}");

                        // Here we assume LobbyClientRequests will perform NetCode connect + send JoinLobbyRPC once connected.
                        LobbyClientRequests.SendJoinLobby(
                            playerName,
                            CharacterSelectionManager.GetSelectedCharacter(),
                            password
                        );

                        ShowScreen(UIScreenManager.LobbySettingsScreenName);
                        break;

                    case PreconnectResult.BadPassword:
                        Debug.Log("Preconnect: Bad password");
                        onError?.Invoke("bad_password");
                        break;

                    case PreconnectResult.LobbyFull:
                        Debug.Log("Preconnect: Lobby full");
                        onError?.Invoke("lobby_full");
                        break;

                    case PreconnectResult.VersionMismatch:
                        Debug.Log("Preconnect: Version mismatch");
                        onError?.Invoke("version_mismatch");
                        break;

                    case PreconnectResult.Timeout:
                        Debug.Log("Preconnect: Timeout");
                        onError?.Invoke("timeout");
                        break;

                    case PreconnectResult.NetworkError:
                        Debug.Log("Preconnect: Network error");
                        onError?.Invoke("network_error");
                        break;

                    default:
                        Debug.Log("Preconnect: Unknown result");
                        onError?.Invoke("unknown");
                        break;
                }
            },
            timeout: 0.6f,                   // timeout seconds
            playerVersion: PlayerVersion,
            playerName: playerName
        ));
    }

    // ---------------------- Legacy direct Join (kept for compatibility) ----------------------
    // This bypasses preconnect flow — keep for debug or quick testing if needed.
    public void JoinLobby(string name, string password)
    {
        LobbyClientRequests.SendJoinLobby(
            SettingsManager.Instance.CurrentSettings.playerName,
            CharacterSelectionManager.GetSelectedCharacter(),
            password
        );

        ShowScreen(UIScreenManager.LobbySettingsScreenName);
    }

    // ---------------------- From ECS → UI ----------------------
    public void UpdatePlayerListFromData(List<LobbyPlayerInfo> players)
    {
        ScreenManager.PopulatePlayerScroll(players);
    }

    public void OnLobbyListUpdated()
    {
        ScreenManager.RefreshLobbyList();
    }

    public void OnJoinedAsClient()
    {
        ShowScreen(UIScreenManager.LobbySettingsScreenName);
    }
}
