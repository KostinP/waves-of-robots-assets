using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct JoinLobbySystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ReceiveRpcCommandRequest>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var em = state.EntityManager;

        foreach (var (cmd, req, entity) in SystemAPI
                 .Query<JoinLobbyCommand, ReceiveRpcCommandRequest>()
                 .WithEntityAccess())
        {
            UnityEngine.Debug.Log($"[Server] JoinLobby RPC: {cmd.PlayerName} with weapon {cmd.Weapon}");

            var query = em.CreateEntityQuery(typeof(LobbyDataComponent), typeof(LobbyPlayerBuffer));
            if (query.IsEmptyIgnoreFilter)
            {
                ecb.DestroyEntity(entity);
                continue;
            }

            var lobbyEntity = query.GetSingletonEntity();
            var lobbyData = em.GetComponentData<LobbyDataComponent>(lobbyEntity);
            var buffer = em.GetBuffer<LobbyPlayerBuffer>(lobbyEntity);

            // проверяем пароль и лимит
            if ((lobbyData.Password.Length == 0 || lobbyData.Password.Equals(cmd.Password))
                && buffer.Length < lobbyData.MaxPlayers)
            {
                var connId = (ulong)em.GetComponentData<NetworkId>(req.SourceConnection).Value;
                buffer.Add(new LobbyPlayerBuffer
                {
                    PlayerName = cmd.PlayerName,
                    Weapon = cmd.Weapon,
                    ConnectionId = connId
                });

                UnityEngine.Debug.Log($"[Server] Added player {cmd.PlayerName} (Conn={connId}) with weapon {cmd.Weapon}");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[Server] JoinLobby rejected (password or full)");
            }

            ecb.DestroyEntity(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}