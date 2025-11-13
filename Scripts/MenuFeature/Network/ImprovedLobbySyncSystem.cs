using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct ImprovedLobbySyncSystem : ISystem
{
    private EntityQuery _lobbyQuery;
    private EntityQuery _connectionQuery;
    private bool _hasRequestedData;
    
    public void OnCreate(ref SystemState state)
    {
        _lobbyQuery = state.GetEntityQuery(ComponentType.ReadWrite<LobbyDataComponent>(),
                                         ComponentType.ReadWrite<LobbyPlayerBuffer>());
        _connectionQuery = state.GetEntityQuery(ComponentType.ReadWrite<NetworkId>());
        _hasRequestedData = false;
    }

    public void OnUpdate(ref SystemState state)
    {
        if (state.WorldUnmanaged.IsServer())
        {
            SyncServerToClients(ref state);
        }
        else if (state.WorldUnmanaged.IsClient())
        {
            ProcessClientUpdates(ref state);
        }
    }

    private void SyncServerToClients(ref SystemState state)
    {
        if (_lobbyQuery.IsEmptyIgnoreFilter) return;

        var lobbyEntity = _lobbyQuery.GetSingletonEntity();
        var lobbyBuffer = state.EntityManager.GetBuffer<LobbyPlayerBuffer>(lobbyEntity);
        
        var connections = _connectionQuery.ToEntityArray(Allocator.Temp);
        
        foreach (var connection in connections)
        {
            if (!state.EntityManager.HasComponent<CommandTarget>(connection)) continue;
            
            var commandTarget = state.EntityManager.GetComponentData<CommandTarget>(connection);
            if (commandTarget.targetEntity == Entity.Null) continue;

            // Добавляем маркер синхронизированных данных
            if (!state.EntityManager.HasComponent<SyncedLobbyData>(commandTarget.targetEntity))
            {
                state.EntityManager.AddComponent<SyncedLobbyData>(commandTarget.targetEntity);
            }

            // Синхронизируем буфер
            if (!state.EntityManager.HasBuffer<LobbyPlayerBuffer>(commandTarget.targetEntity))
            {
                state.EntityManager.AddBuffer<LobbyPlayerBuffer>(commandTarget.targetEntity);
            }

            var clientBuffer = state.EntityManager.GetBuffer<LobbyPlayerBuffer>(commandTarget.targetEntity);
            SyncBuffers(lobbyBuffer, clientBuffer);

            // Помечаем как инициализированные
            var syncedData = state.EntityManager.GetComponentData<SyncedLobbyData>(commandTarget.targetEntity);
            syncedData.IsInitialized = true;
            state.EntityManager.SetComponentData(commandTarget.targetEntity, syncedData);
        }
        
        connections.Dispose();
    }

    private void ProcessClientUpdates(ref SystemState state)
    {
        // Ищем синхронизированные данные с маркером
        bool hasSyncedData = false;
        
        foreach (var (buffer, syncedData, entity) in 
            SystemAPI.Query<DynamicBuffer<LobbyPlayerBuffer>, RefRO<SyncedLobbyData>>().WithEntityAccess())
        {
            if (syncedData.ValueRO.IsInitialized)
            {
                hasSyncedData = true;
                Debug.Log($"[Client] Found {buffer.Length} players in SYNCED data (Entity: {entity})");
                
                // Обновляем UI только если есть реальные изменения
                if (buffer.Length > 0)
                {
                    UnityMainThreadDispatcher.Instance?.Enqueue(() =>
                    {
                        UIManager.Instance?.OnPlayersUpdated();
                    });
                }
                break;
            }
        }

        // Запрашиваем данные только один раз при подключении
        if (!hasSyncedData && !_hasRequestedData)
        {
            RequestLobbyData(ref state);
            _hasRequestedData = true;
        }
    }

    private void RequestLobbyData(ref SystemState state)
    {
        var requestEntity = state.EntityManager.CreateEntity();
        state.EntityManager.AddComponent<LobbyPlayersRequest>(requestEntity);
        state.EntityManager.AddComponent<SendRpcCommandRequest>(requestEntity);
        
        Debug.Log("[Client] Requesting lobby data from server (first time)");
    }

    private void SyncBuffers(DynamicBuffer<LobbyPlayerBuffer> source, DynamicBuffer<LobbyPlayerBuffer> destination)
    {
        destination.Clear();
        
        for (int i = 0; i < source.Length; i++)
        {
            destination.Add(source[i]);
        }
        
        Debug.Log($"[Server] Synced {source.Length} players to client");
    }
}