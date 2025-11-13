using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct LobbyDataResponseSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Обрабатываем запросы данных от клиентов
        foreach (var (request, rpcCommand, entity) in
            SystemAPI.Query<LobbyPlayersRequest, ReceiveRpcCommandRequest>()
                     .WithEntityAccess())
        {
            // Находим лобби данные
            var lobbyQuery = SystemAPI.QueryBuilder().WithAll<LobbyDataComponent, LobbyPlayerBuffer>().Build();
            if (!lobbyQuery.IsEmptyIgnoreFilter)
            {
                var lobbyEntity = lobbyQuery.GetSingletonEntity();
                var lobbyBuffer = state.EntityManager.GetBuffer<LobbyPlayerBuffer>(lobbyEntity);

                // Получаем ConnectionId из RPC запроса
                var sourceConnectionEntity = rpcCommand.SourceConnection;
                var sourceNetId = state.EntityManager.GetComponentData<NetworkId>(sourceConnectionEntity).Value;

                // Находим entity клиента для синхронизации
                var connections = state.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkId>())
                    .ToEntityArray(Allocator.Temp);

                foreach (var connection in connections)
                {
                    var netId = state.EntityManager.GetComponentData<NetworkId>(connection);
                    if (netId.Value == sourceNetId)
                    {
                        if (state.EntityManager.HasComponent<CommandTarget>(connection))
                        {
                            var commandTarget = state.EntityManager.GetComponentData<CommandTarget>(connection);
                            if (commandTarget.targetEntity != Entity.Null)
                            {
                                // Синхронизируем данные
                                if (!state.EntityManager.HasBuffer<LobbyPlayerBuffer>(commandTarget.targetEntity))
                                {
                                    state.EntityManager.AddBuffer<LobbyPlayerBuffer>(commandTarget.targetEntity);
                                }

                                var clientBuffer = state.EntityManager.GetBuffer<LobbyPlayerBuffer>(commandTarget.targetEntity);
                                clientBuffer.Clear();
                                for (int i = 0; i < lobbyBuffer.Length; i++)
                                {
                                    clientBuffer.Add(lobbyBuffer[i]);
                                }

                                // Добавляем маркер
                                if (!state.EntityManager.HasComponent<SyncedLobbyData>(commandTarget.targetEntity))
                                {
                                    state.EntityManager.AddComponent<SyncedLobbyData>(commandTarget.targetEntity);
                                }

                                var syncedData = new SyncedLobbyData { IsInitialized = true };
                                state.EntityManager.SetComponentData(commandTarget.targetEntity, syncedData);

                                Debug.Log($"[Server] Responded to data request from connection {sourceNetId} with {lobbyBuffer.Length} players");
                            }
                        }
                        break;
                    }
                }
                connections.Dispose();
            }

            ecb.DestroyEntity(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}