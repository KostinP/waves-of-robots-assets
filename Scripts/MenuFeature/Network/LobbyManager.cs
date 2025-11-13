using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Unity.Entities;
using Unity.NetCode;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using System.Net;
using System.Net.Sockets;
using System;
using System.Threading;

/// <summary>
/// Управляет локальным лобби: создает серверный мир, запускает discovery broadcast через LobbyDiscovery,
/// отслеживает текущее число игроков и — при достижении maxPlayers — помечает лобби скрытым и стартует игру.
/// </summary>
public class LobbyManager : MonoBehaviour
{
    private LobbyDiscovery _discovery;
    private LobbyData? _currentLobby;
    private LobbyInfo? _currentLobbyInfo;
    private Dictionary<ulong, PlayerData> _players = new();
    private bool _gameStarted = false;
    private Coroutine _lobbyMonitorCoroutine;
    private Coroutine _playerListUpdateCoroutine;

    private void Start()
    {
        _discovery = LobbyDiscovery.Instance;
        if (_discovery == null)
        {
            _discovery = gameObject.AddComponent<LobbyDiscovery>();
        }

        // ФИКС: Убедитесь, что подписка происходит в главном потоке
        _discovery.OnLobbiesUpdated += OnLobbiesUpdated;

        if (LobbyDiscovery.Instance != null)
        {
            LobbyDiscovery.Instance.OnLobbyClosed += OnLobbyClosed;
        }
    }

    private void OnLobbyClosed(string lobbyId)
    {
        Debug.Log($"LobbyManager: Lobby {lobbyId} was closed by host");

        // ФИКС: Упрощенная и надежная логика определения нужно ли возвращаться к списку
        UnityMainThreadDispatcher.Instance.Enqueue(() =>
        {
            StartCoroutine(HandleLobbyClosedCoroutine(lobbyId));
        });
    }

    public ulong GetHostConnectionId()
    {
        // Хост всегда имеет ConnectionId = 0
        return 0;
    }

    // Метод для проверки, является ли игрок хостом
    public bool IsHost(ulong connectionId)
    {
        return connectionId == GetHostConnectionId();
    }

    // Метод для получения всех игроков в лобби
    public List<LobbyPlayerInfo> GetLobbyPlayers()
    {
        var players = new List<LobbyPlayerInfo>();
        Debug.Log("=== GetLobbyPlayers START ===");

        // Пробуем получить данные из всех миров
        foreach (var world in World.All)
        {
            if (!world.IsCreated) continue;

            var em = world.EntityManager;

            // Ищем entity с буфером игроков
            var query = em.CreateEntityQuery(ComponentType.ReadOnly<LobbyPlayerBuffer>());

            if (!query.IsEmptyIgnoreFilter)
            {
                var entities = query.ToEntityArray(Allocator.Temp);
                foreach (var entity in entities)
                {
                    if (em.HasBuffer<LobbyPlayerBuffer>(entity))
                    {
                        var buffer = em.GetBuffer<LobbyPlayerBuffer>(entity);
                        Debug.Log($"Found buffer in world {world.Name}: {buffer.Length} players");

                        for (int i = 0; i < buffer.Length; i++)
                        {
                            var playerBuffer = buffer[i];
                            players.Add(new LobbyPlayerInfo
                            {
                                Name = playerBuffer.PlayerName.ToString(),
                                Weapon = playerBuffer.Weapon.ToString(),
                                ConnectionId = playerBuffer.ConnectionId,
                                Ping = GetPing(playerBuffer.ConnectionId)
                            });
                            Debug.Log($"Added player: {playerBuffer.PlayerName} from world {world.Name}");
                        }
                    }
                }
                entities.Dispose();

                // Если нашли игроков, выходим
                if (players.Count > 0) break;
            }
        }

        Debug.Log($"=== GetLobbyPlayers END: {players.Count} players ===");
        return players;
    }

    private IEnumerator HandleLobbyClosedCoroutine(string lobbyId)
    {
        yield return new WaitForSeconds(0.5f); // Даем время на синхронизацию

        // Проверяем, находимся ли мы в клиентском режиме И в настройках лобби
        var clientWorld = GetClientWorld();
        bool isClient = clientWorld != null && clientWorld.IsCreated && clientWorld.IsClient();
        bool isInLobbySettings = UIManager.Instance?.GetCurrentScreen() == "lobby_settings_screen";

        Debug.Log($"HandleLobbyClosed: isClient={isClient}, isInLobbySettings={isInLobbySettings}, closedLobbyId={lobbyId}");

        // ФИКС: Возвращаемся только если мы клиент И находимся в настройках лобби
        if (isClient && isInLobbySettings)
        {
            Debug.Log("We are a client in lobby settings and host closed the lobby, returning to lobby list");

            // Закрываем клиентское соединение
            ShutdownClientWorld();

            // Очищаем сохраненные данные о подключении
            PlayerPrefs.DeleteKey("JoiningLobbyIP");
            PlayerPrefs.DeleteKey("JoiningLobbyPort");
            PlayerPrefs.Save();

            // Возвращаемся к списку лобби
            var mainMenuController = FindObjectOfType<MainMenuController>();
            if (mainMenuController != null)
            {
                mainMenuController.ReturnToLobbyList();
            }
            else
            {
                UIManager.Instance?.ReturnToLobbyList();
            }
        }
        else
        {
            Debug.Log($"HandleLobbyClosed: Not affected - isClient={isClient}, isInLobbySettings={isInLobbySettings}");
        }
    }

    private void ShutdownClientWorld()
    {
        var clientWorld = GetClientWorld();
        if (clientWorld != null && clientWorld.IsCreated)
        {
            try
            {
                clientWorld.Dispose();
                Debug.Log("Client world disposed");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error disposing client world: {e.Message}");
            }
        }
    }

    private World GetServerWorld()
    {
        foreach (var w in World.All)
        {
            if (w.IsCreated && w.IsServer())
                return w;
        }
        return null;
    }

    private World GetClientWorld()
    {
        foreach (var w in World.All)
        {
            if (w.IsCreated && w.IsClient())
                return w;
        }
        return null;
    }

    public void CreateLobby(LobbyData data, PlayerData hostData)
    {
        _currentLobby = data;

        if (GetServerWorld() == null)
            CreateServer();

        var serverWorld = GetServerWorld();
        if (serverWorld == null)
        {
            Debug.LogError("Failed to create server world!");
            return;
        }

        var em = serverWorld.EntityManager;

        var oldQuery = em.CreateEntityQuery(typeof(LobbyDataComponent));
        if (!oldQuery.IsEmptyIgnoreFilter)
            em.DestroyEntity(oldQuery.GetSingletonEntity());

        var lobbyEntity = em.CreateEntity();
        em.AddComponentData(lobbyEntity, new LobbyDataComponent
        {
            Name = new FixedString128Bytes(data.name),
            Password = new FixedString64Bytes(data.password),
            MaxPlayers = data.maxPlayers,
            IsOpen = data.isOpen
        });

        var buffer = em.AddBuffer<LobbyPlayerBuffer>(lobbyEntity);
        _players.Clear();
        _players[0] = hostData;
        buffer.Add(new LobbyPlayerBuffer
        {
            PlayerName = new FixedString128Bytes(hostData.name),
            Weapon = new FixedString64Bytes(hostData.selectedCharacter ?? "Default"),
            ConnectionId = 0
        });

        _currentLobbyInfo = new LobbyInfo
        {
            name = data.name,
            currentPlayers = 1,
            maxPlayers = data.maxPlayers,
            isOpen = data.isOpen,
            password = data.password,
            ip = GetLocalIPAddress(),
            port = _discovery.gamePort,
            uniqueId = _discovery.UniqueID
        };

        _discovery.StartHosting(_currentLobbyInfo.Value);

        // 🔹 ЗАПУСКАЕМ МОНИТОРИНГ ИГРОКОВ
        StartLobbyMonitoring();

        if (_lobbyMonitorCoroutine != null) StopCoroutine(_lobbyMonitorCoroutine);
        _lobbyMonitorCoroutine = StartCoroutine(LobbyBroadcastAndMonitorLoop());

        UIManager.Instance.OnLobbyListUpdated();
        UIManager.Instance.OnLobbyCreated();
        UIManager.Instance.OnPlayersUpdated();

        Debug.Log($"Lobby created: {data.name}, broadcasting on port {_discovery.broadcastPort}");

        // ПРИНУДИТЕЛЬНАЯ СИНХРОНИЗАЦИЯ СРАЗУ ПОСЛЕ СОЗДАНИЯ
        StartCoroutine(InitialSyncCoroutine());
    }

    private IEnumerator InitialSyncCoroutine()
    {
        yield return new WaitForSeconds(1f); // Даем время на инициализацию
        ForceSyncPlayers();

        // Дополнительная синхронизация
        yield return new WaitForSeconds(2f);
        ForceSyncPlayers();
    }

    private IEnumerator LobbyBroadcastAndMonitorLoop()
    {
        // Проверяем, что лобби существует
        if (!_currentLobbyInfo.HasValue) yield break;

        // Периодически обновляем поле currentPlayers и шлём broadcast
        while (!_gameStarted)
        {
            int count = GetPlayerCount();
            var currentInfo = _currentLobbyInfo.Value;
            currentInfo.currentPlayers = Mathf.Max(1, count); // >=1 (хост)
            // Если стали меньше max — оставляем isOpen как есть (обычно true), если достигли — закрываем
            if (currentInfo.currentPlayers >= currentInfo.maxPlayers)
            {
                // Пометим как закрытое — чтобы клиенты не пытались зайти
                currentInfo.isOpen = false;
                _currentLobbyInfo = currentInfo;
                _discovery.BroadcastLobby(currentInfo); // одноразовое обновление со статусом закрыто
                Debug.Log("Lobby full — broadcasting hide/closed and starting game.");
                // Небольшая задержка, чтобы клиенты успели получить сообщение
                yield return new WaitForSeconds(0.5f);
                StartGame(); // автоматически стартуем игру
                yield break;
            }
            else
            {
                // Обычная регулярная рассылка актуального состояния (имя/кол-во/порт)
                if (_currentLobby.HasValue)
                {
                    currentInfo.isOpen = _currentLobby.Value.isOpen;
                }
                _currentLobbyInfo = currentInfo;
                _discovery.BroadcastLobby(currentInfo);
            }

            yield return new WaitForSeconds(1.0f);
        }
    }

    private void CreateServer()
    {
#if UNITY_EDITOR
        // В редакторе создаем и сервер и клиент
        if (World.All.Count == 0)
        {
            var server = ClientServerBootstrap.CreateServerWorld("ServerWorld");
            var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");
        }
#endif
    }

    private string GetLocalIPAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    Debug.Log($"Found local IP: {ip}");
                    return ip.ToString();
                }
            }

            // Если не нашли подходящий IP, используем локальный
            Debug.LogWarning("No suitable local IP found, using 127.0.0.1");
            return "127.0.0.1";
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error getting local IP: {ex.Message}");
            return "127.0.0.1";
        }
    }

    public void JoinLobby(LobbyInfo lobbyInfo, string playerName, string password = "")
    {
        Debug.Log($"Joining lobby: {lobbyInfo.name} at {lobbyInfo.ip}:{lobbyInfo.port}");

        // Сохраняем данные для подключения
        PlayerPrefs.SetString("JoiningLobbyIP", lobbyInfo.ip);
        PlayerPrefs.SetInt("JoiningLobbyPort", lobbyInfo.port);
        PlayerPrefs.SetString("JoiningPlayerName", playerName);
        PlayerPrefs.Save();

        var mainMenuController = FindObjectOfType<MainMenuController>();
        if (mainMenuController != null)
            mainMenuController.OnJoinedAsClient();

        // Получаем название оружия
        string weaponName = GetWeaponNameFromIndex(SettingsManager.Instance.CurrentSettings.defaultWeaponIndex);

        // Отправляем команду подключения
        var clientWorld = GetClientWorld();
        if (clientWorld != null)
        {
            var em = clientWorld.EntityManager;
            var req = em.CreateEntity();
            em.AddComponentData(req, new JoinLobbyCommand
            {
                PlayerName = new FixedString128Bytes(playerName),
                Weapon = new FixedString64Bytes(weaponName),
                Password = new FixedString64Bytes(password ?? "")
            });
            em.AddComponentData(req, new SendRpcCommandRequest { TargetConnection = Entity.Null });
        }

        // Запускаем усиленное обновление списка игроков
        StartCoroutine(EnhancedPlayerListUpdate());
    }

    private IEnumerator EnhancedPlayerListUpdate()
    {
        Debug.Log("Starting enhanced player list update for client...");

        // Многократные попытки обновления с прогрессивной задержкой
        for (int attempt = 0; attempt < 8; attempt++) // Увеличил до 8 попыток
        {
            yield return new WaitForSeconds(0.5f + attempt * 0.3f); // Более частые попытки

            Debug.Log($"Player list update attempt {attempt + 1}");

            // Принудительно запрашиваем синхронизацию
            if (attempt % 2 == 0) // Каждую вторую попытку
            {
                ForceSyncPlayers();
            }

            UIManager.Instance?.OnPlayersUpdated();

            // Проверяем, есть ли данные
            var players = GetLobbyPlayersForClient();
            if (players.Count > 0)
            {
                Debug.Log($"Successfully found {players.Count} players on attempt {attempt + 1}");

                // Дополнительное обновление после успеха
                yield return new WaitForSeconds(0.5f);
                UIManager.Instance?.OnPlayersUpdated();
                break;
            }

            if (attempt == 7) // Последняя попытка
            {
                Debug.LogWarning("Failed to get player list after all attempts");
            }
        }

        Debug.Log("Enhanced player list update completed");
    }

    public void ForceSyncPlayers()
    {
        var serverWorld = GetServerWorld();
        if (serverWorld == null) return;

        var em = serverWorld.EntityManager;
        var lobbyQuery = em.CreateEntityQuery(ComponentType.ReadOnly<LobbyDataComponent>(),
                                            ComponentType.ReadOnly<LobbyPlayerBuffer>());
    
        if (lobbyQuery.IsEmptyIgnoreFilter) return;

        var lobbyEntity = lobbyQuery.GetSingletonEntity();
        var buffer = em.GetBuffer<LobbyPlayerBuffer>(lobbyEntity);
    
        Debug.Log($"ForceSync: Syncing {buffer.Length} players to all clients");
    
        // Принудительно обновляем UI
        UnityMainThreadDispatcher.Instance.Enqueue(() =>
        {
            UIManager.Instance?.OnPlayersUpdated();
        });
    }

    private string GetWeaponNameFromIndex(int index)
    {
        switch (index)
        {
            case 0: return "Termit\"Flamethrower\"";
            case 1: return "Titan \"Autocannon\"";
            case 2: return "Volt \"Stun Gun\"";
            case 3: return "Vikhr \"Twin Machine Gun\"";
            default: return $"Weapon{index}";
        }
    }

    private IEnumerator LoadLobbySceneWithDelay()
    {
        yield return new WaitForSeconds(1f);

        try
        {
            if (Application.CanStreamedLevelBeLoaded("LobbyScene"))
            {
                SceneManager.LoadScene("LobbyScene");
            }
            else if (Application.CanStreamedLevelBeLoaded("GameCoreScene"))
            {
                SceneManager.LoadScene("GameCoreScene");
            }
            else
            {
                SceneManager.LoadScene(0);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load scene: {e.Message}");
            SceneManager.LoadScene("MainMenu");
        }
    }

    public void KickPlayer(ulong connectionId)
    {
        var serverWorld = GetServerWorld();
        if (serverWorld == null) return;
        var em = serverWorld.EntityManager;
        var query = em.CreateEntityQuery(ComponentType.ReadOnly<NetworkId>());
        using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        foreach (var e in entities)
        {
            if (em.GetComponentData<NetworkId>(e).Value == (int)connectionId)
            {
                em.DestroyEntity(e);
                break;
            }
        }
        _players.Remove(connectionId);
        UIManager.Instance.OnPlayerLeft((int)connectionId);
    }

    private bool _isDisbanding = false;

    public void DisbandLobby()
    {
        if (_isDisbanding)
        {
            Debug.LogWarning("LobbyManager: DisbandLobby already in progress, ignoring duplicate call");
            return;
        }

        _isDisbanding = true;
        Debug.Log("LobbyManager: Starting lobby disband process...");

        try
        {
            // Останавливаем корутины
            if (_lobbyMonitorCoroutine != null)
            {
                Debug.Log("Stopping lobby monitor coroutine...");
                StopCoroutine(_lobbyMonitorCoroutine);
                _lobbyMonitorCoroutine = null;
            }

            // ФИКС: Сначала уведомляем о закрытии
            if (LobbyDiscovery.Instance != null)
            {
                Debug.Log("Sending lobby close notification...");
                LobbyDiscovery.Instance.StopHostingAndNotify();
            }

            // Очищаем данные лобби
            Debug.Log("Clearing lobby data...");
            _players.Clear();
            _gameStarted = false;
            _currentLobby = null;
            _currentLobbyInfo = null;

            // Уничтожаем NetCode миры
            Debug.Log("Shutting down NetCode worlds...");
            ShutdownAllNetCodeWorlds();

            // Останавливаем хостинг в discovery
            if (_discovery != null)
            {
                Debug.Log("Stopping discovery hosting...");
                _discovery.StopHosting();
            }

            // Возвращаем в главное меню
            Debug.Log("Returning to main menu...");
            StartCoroutine(ReturnToMainMenuWithDelay());

            Debug.Log("Lobby disbanded successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error during lobby disband: {e.Message}\n{e.StackTrace}");
            // Все равно пытаемся вернуться в главное меню
            StartCoroutine(ReturnToMainMenuWithDelay());
        }
        finally
        {
            _isDisbanding = false;
        }
    }

    private IEnumerator ReturnToMainMenuWithDelay()
    {
        yield return new WaitForSeconds(0.5f); // Даем время на очистку сетевых соединений

        if (UIManager.Instance != null)
        {
            UIManager.Instance.LeaveGame();
        }
        else
        {
            SceneManager.LoadScene("MainMenu");
        }
    }

    public void StartGame()
    {
        if (_gameStarted) return;
        _gameStarted = true;
        // Остановим broadcasting — discovery сам прекратит, когда мы вызовем StopHosting
        _discovery.StopHosting();

        var serverWorld = GetServerWorld();
        if (serverWorld == null) return;
        var em = serverWorld.EntityManager;
        var startEntity = em.CreateEntity();
        em.AddComponentData(startEntity, new StartGameCommand { });

        StartCoroutine(LoadGameSceneWithDelay());
    }

    private IEnumerator LoadGameSceneWithDelay()
    {
        yield return new WaitForSeconds(1f);

        try
        {
            if (Application.CanStreamedLevelBeLoaded("GameCoreScene"))
            {
                SceneManager.LoadScene("GameCoreScene");
            }
            else
            {
                Debug.LogWarning("GameCoreScene not found, loading first available scene");
                SceneManager.LoadScene(0);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load game scene: {e.Message}");
            SceneManager.LoadScene("MainMenu");
        }
    }

    private int GetPlayerCount()
    {
        var serverWorld = GetServerWorld();
        if (serverWorld == null) return 1; // хотя бы хост
        var em = serverWorld.EntityManager;
        var query = em.CreateEntityQuery(typeof(LobbyPlayerBuffer));
        if (query.IsEmptyIgnoreFilter) return 1;
        return em.GetBuffer<LobbyPlayerBuffer>(query.GetSingletonEntity()).Length;
    }

    // УДАЛЕНО: PopulateLobbyList и CreateLobbyItem - эти методы дублируют функциональность из UIScreenManager

    public void StartLobbyMonitoring()
    {
        if (_playerListUpdateCoroutine != null)
            StopCoroutine(_playerListUpdateCoroutine);

        _playerListUpdateCoroutine = StartCoroutine(PlayerListUpdateLoop());
    }

    private IEnumerator PlayerListUpdateLoop()
    {
        while (!_gameStarted)
        {
            yield return new WaitForSeconds(1f);

            // Обновляем UI списка игроков
            UIManager.Instance?.OnPlayersUpdated();

            // Обновляем информацию о лобби для broadcast
            UpdateLobbyInfoForBroadcast();
        }
    }

    private void UpdateLobbyInfoForBroadcast()
    {
        if (!_currentLobbyInfo.HasValue || !_discovery.isHost) return;

        var currentInfo = _currentLobbyInfo.Value;
        currentInfo.currentPlayers = GetPlayerCount();

        _currentLobbyInfo = currentInfo;
        _discovery.BroadcastLobby(currentInfo);
    }

    public void StopLobbyMonitoring()
    {
        if (_playerListUpdateCoroutine != null)
        {
            StopCoroutine(_playerListUpdateCoroutine);
            _playerListUpdateCoroutine = null;
        }
    }

    public void PopulatePlayerList(ScrollView scroll, ulong localPlayerConnectionId = 0)
    {
        scroll.Clear();
        var serverWorld = GetServerWorld();
        if (serverWorld == null) return;

        var em = serverWorld.EntityManager;
        var query = em.CreateEntityQuery(ComponentType.ReadOnly<LobbyPlayerBuffer>());
        if (query.IsEmptyIgnoreFilter) return;

        var buffer = em.GetBuffer<LobbyPlayerBuffer>(query.GetSingletonEntity());

        for (int i = 0; i < buffer.Length; i++)
        {
            var player = buffer[i];
            CreatePlayerItem(scroll, player, localPlayerConnectionId);
        }
    }

    private void CreatePlayerItem(ScrollView scroll, LobbyPlayerBuffer player, ulong localPlayerConnectionId)
    {
        var item = new VisualElement();
        item.AddToClassList("player-item");

        var nameLabel = new Label(player.PlayerName.ToString());
        nameLabel.AddToClassList("player-name");

        var weaponLabel = new Label(player.Weapon.ToString());
        weaponLabel.AddToClassList("player-weapon");

        var pingLabel = new Label($"{GetPing(player.ConnectionId)} ms");
        pingLabel.AddToClassList("player-ping");

        item.Add(nameLabel);
        item.Add(weaponLabel);
        item.Add(pingLabel);

        // Добавляем кнопку "Выгнать" только если:
        // 1. Локальный игрок - хост (ConnectionId = 0)
        // 2. Игрок не является самим собой
        if (localPlayerConnectionId == 0 && player.ConnectionId != localPlayerConnectionId)
        {
            var kickBtn = new Button(() => KickPlayer(player.ConnectionId))
            {
                text = "Kick"
            };
            kickBtn.AddToClassList("kick-button");
            item.Add(kickBtn);
        }

        scroll.Add(item);
    }

    public void ShowPasswordPrompt(LobbyInfo lobbyInfo)
    {
        string password = "";

#if UNITY_EDITOR
        // В редакторе используем системный диалог — оставляем упрощение
#else
        Debug.Log("Password protected lobby - using temporary password '123'");
        password = "123";
#endif

        if (!string.IsNullOrEmpty(password))
        {
            var mainMenuController = FindObjectOfType<MainMenuController>();
            if (mainMenuController != null)
            {
                mainMenuController.OnJoinedAsClient();
            }

            JoinLobby(lobbyInfo, SettingsManager.Instance.CurrentSettings.playerName, password);
        }
    }

    private int GetPing(ulong id)
    {
        var serverWorld = GetServerWorld();
        if (serverWorld == null) return 0;

        var em = serverWorld.EntityManager;
        var query = em.CreateEntityQuery(ComponentType.ReadOnly<PlayerComponent>());

        if (query.IsEmptyIgnoreFilter) return 0;

        using var players = query.ToComponentDataArray<PlayerComponent>(Allocator.Temp);
        foreach (var player in players)
        {
            if (player.ConnectionId == id)
                return player.Ping;
        }
        return 0;
    }

    private void OnLobbiesUpdated(List<LobbyInfo> lobbies)
    {
        UIManager.Instance.OnLobbyListUpdated();
    }

    private void ShutdownAllNetCodeWorlds()
    {
        // СОЗДАЕМ КОПИЮ СПИСКА МИРОВ ДЛЯ БЕЗОПАСНОЙ ИТЕРАЦИИ
        var worldsToDispose = new List<World>();
        foreach (var w in World.All)
        {
            if (w.IsCreated && (w.IsServer() || w.IsClient() || w.IsThinClient()))
                worldsToDispose.Add(w);
        }

        // ТЕПЕРЬ БЕЗОПАСНО УНИЧТОЖАЕМ МИРЫ
        foreach (var w in worldsToDispose)
        {
            try
            {
                w.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to dispose world {w.Name}: {e.Message}");
            }
        }
    }

    public List<LobbyPlayerInfo> GetLobbyPlayersForClient()
    {
        var players = new List<LobbyPlayerInfo>();
        Debug.Log("=== GetLobbyPlayersForClient START ===");

        // Детально логируем все миры
        foreach (var world in World.All)
        {
            if (!world.IsCreated) continue;

            Debug.Log($"Checking world: {world.Name}, IsClient: {world.IsClient()}, IsServer: {world.IsServer()}");

            var em = world.EntityManager;
            var query = em.CreateEntityQuery(ComponentType.ReadOnly<LobbyPlayerBuffer>());

            if (!query.IsEmptyIgnoreFilter)
            {
                var entities = query.ToEntityArray(Allocator.Temp);
                Debug.Log($"Found {entities.Length} entities with LobbyPlayerBuffer in {world.Name}");

                foreach (var entity in entities)
                {
                    if (em.HasBuffer<LobbyPlayerBuffer>(entity))
                    {
                        var buffer = em.GetBuffer<LobbyPlayerBuffer>(entity);

                        // Проверяем маркер синхронизации
                        bool isSynced = em.HasComponent<SyncedLobbyData>(entity);
                        Debug.Log($"Entity {entity} in {world.Name}: {buffer.Length} players, Synced: {isSynced}");

                        for (int i = 0; i < buffer.Length; i++)
                        {
                            var playerBuffer = buffer[i];
                            players.Add(new LobbyPlayerInfo
                            {
                                Name = playerBuffer.PlayerName.ToString(),
                                Weapon = playerBuffer.Weapon.ToString(),
                                ConnectionId = playerBuffer.ConnectionId,
                                Ping = GetPingForClient(playerBuffer.ConnectionId)
                            });
                            Debug.Log($"Added player: {playerBuffer.PlayerName} from {world.Name}");
                        }
                    }
                }
                entities.Dispose();
            }
            else
            {
                Debug.Log($"No LobbyPlayerBuffer found in {world.Name}");
            }
        }

        Debug.Log($"=== GetLobbyPlayersForClient END: {players.Count} players ===");
        return players;
    }

    private int GetPingForClient(ulong connectionId)
    {
        var clientWorld = GetClientWorld();
        if (clientWorld == null) return 0;

        var em = clientWorld.EntityManager;
        var query = em.CreateEntityQuery(ComponentType.ReadOnly<PlayerComponent>());

        if (query.IsEmptyIgnoreFilter) return 0;

        using var players = query.ToComponentDataArray<PlayerComponent>(Allocator.Temp);
        foreach (var player in players)
        {
            if (player.ConnectionId == connectionId)
                return player.Ping;
        }
        return 0;
    }

    public bool IsConnectedToServer()
    {
        var clientWorld = GetClientWorld();
        if (clientWorld == null) return false;

        var query = clientWorld.EntityManager.CreateEntityQuery(typeof(NetworkStreamConnection));
        return !query.IsEmptyIgnoreFilter;
    }

    private void OnDestroy()
    {
        if (LobbyDiscovery.Instance != null)
        {
            LobbyDiscovery.Instance.OnLobbyClosed -= OnLobbyClosed;
        }
    }
}