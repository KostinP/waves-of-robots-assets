using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct LobbySystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        var em = state.EntityManager;

        // 🔹 ИСПРАВЛЕНИЕ: Проверяем существование лобби перед обработкой
        var lobbyQuery = SystemAPI.QueryBuilder().WithAll<LobbyDataComponent, LobbyPlayerBuffer>().Build();
        if (lobbyQuery.IsEmpty)
        {
            ecb.Dispose();
            return;
        }

        var lobbyEntity = lobbyQuery.GetSingletonEntity();
        var lobbyBuffer = em.GetBuffer<LobbyPlayerBuffer>(lobbyEntity);
        var lobbyData = em.GetComponentData<LobbyDataComponent>(lobbyEntity);

        // 🔹 Обработка команд JoinLobbyCommand
        foreach (var (joinCmd, req, entity) in SystemAPI
                     .Query<JoinLobbyCommand, ReceiveRpcCommandRequest>()
                     .WithEntityAccess())
        {
            var netId = em.GetComponentData<NetworkId>(req.SourceConnection).Value;
            var connId = (ulong)netId;

            if ((lobbyData.Password.Length == 0 || lobbyData.Password.Equals(joinCmd.Password))
                && lobbyBuffer.Length < lobbyData.MaxPlayers)
            {
                lobbyBuffer.Add(new LobbyPlayerBuffer
                {
                    PlayerName = joinCmd.PlayerName,
                    Weapon = joinCmd.Weapon,
                    ConnectionId = connId
                });

                UnityEngine.Debug.Log($"[Server] Added player {joinCmd.PlayerName} with weapon {joinCmd.Weapon} (Conn={connId})");

                // Создаём сущность для SpawnPlayerCommand
                var spawn = ecb.CreateEntity();
                ecb.AddComponent(spawn, new SpawnPlayerCommand
                {
                    ConnectionId = connId,
                    PlayerName = joinCmd.PlayerName,
                    Weapon = joinCmd.Weapon
                });

                // 🔹 ВЫЗЫВАЕМ ОБНОВЛЕНИЕ UI
                UIManager.Instance?.OnPlayersUpdated();
            }
            else
            {
                // Отказ — удаляем соединение
                var connQuery = em.CreateEntityQuery(ComponentType.ReadOnly<NetworkId>());
                using var entities = connQuery.ToEntityArray(Allocator.Temp);
                foreach (var e in entities)
                {
                    if ((ulong)em.GetComponentData<NetworkId>(e).Value == connId)
                    {
                        em.DestroyEntity(e);
                        break;
                    }
                }
            }

            ecb.DestroyEntity(entity);
        }

        // 🔹 Обработка KickPlayerCommand
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

            var connQuery = em.CreateEntityQuery(ComponentType.ReadOnly<NetworkId>());
            using var entities = connQuery.ToEntityArray(Allocator.Temp);
            foreach (var e in entities)
            {
                if ((ulong)em.GetComponentData<NetworkId>(e).Value == kickCmd.ValueRO.ConnectionId)
                {
                    em.DestroyEntity(e);
                    break;
                }
            }

            ecb.DestroyEntity(entity);

            // 🔹 ВЫЗЫВАЕМ ОБНОВЛЕНИЕ UI
            UIManager.Instance?.OnPlayersUpdated();
        }

        ecb.Playback(em);
        ecb.Dispose();
    }
}