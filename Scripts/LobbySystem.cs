using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct LobbySystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkId>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        if (!SystemAPI.HasSingleton<LobbyDataComponent>())
        {
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            return;
        }

        var lobbyEntity = SystemAPI.GetSingletonEntity<LobbyDataComponent>();
        var lobbyBuffer = state.EntityManager.GetBuffer<LobbyPlayerBuffer>(lobbyEntity);
        var lobbyData = state.EntityManager.GetComponentData<LobbyDataComponent>(lobbyEntity);

        // JoinLobbyCommands
        foreach (var (joinCmd, entity) in SystemAPI.Query<RefRO<JoinLobbyCommand>>().WithEntityAccess())
        {
            // сравниваем FixedString с FixedString
            if (joinCmd.ValueRO.Password.Equals(lobbyData.Password) && lobbyBuffer.Length < lobbyData.MaxPlayers)
            {
                lobbyBuffer.Add(new LobbyPlayerBuffer { PlayerName = joinCmd.ValueRO.PlayerName, ConnectionId = joinCmd.ValueRO.ConnectionId });

                var spawnEntity = ecb.CreateEntity();
                ecb.AddComponent(spawnEntity, new SpawnPlayerCommand { ConnectionId = joinCmd.ValueRO.ConnectionId });
            }
            else
            {
                // На отказ: разрушаем сущность подключения с тем же NetworkId
                // Находим сущность с NetworkId == ConnectionId и удаляем её
                var connQuery = state.EntityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<NetworkId>());

                using (var entities = connQuery.ToEntityArray(Allocator.Temp))
                {
                    for (int i = 0; i < entities.Length; i++)
                    {
                        var e = entities[i];
                        var nid = state.EntityManager.GetComponentData<NetworkId>(e).Value;
                        if ((ulong)nid == joinCmd.ValueRO.ConnectionId)
                        {
                            // удаляем сущность подключения
                            state.EntityManager.DestroyEntity(e);
                            break;
                        }
                    }
                }
            }
            ecb.DestroyEntity(entity);
        }

        // KickPlayerCommands
        foreach (var (kickCmd, entity) in SystemAPI.Query<RefRO<KickPlayerCommand>>().WithEntityAccess())
        {
            for (int i = 0; i < lobbyBuffer.Length; i++)
            {
                if (lobbyBuffer[i].ConnectionId == kickCmd.ValueRO.ConnectionId)
                {
                    lobbyBuffer.RemoveAt(i);
                    break;
                }
            }

            // Найдём и удалим сущность подключения
            var connQuery = state.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkId>());
            using (var entities = connQuery.ToEntityArray(Allocator.Temp))
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    var e = entities[i];
                    var nid = state.EntityManager.GetComponentData<NetworkId>(e).Value;
                    if ((ulong)nid == kickCmd.ValueRO.ConnectionId)
                    {
                        state.EntityManager.DestroyEntity(e);
                        break;
                    }
                }
            }

            ecb.DestroyEntity(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
