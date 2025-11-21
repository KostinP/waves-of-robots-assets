using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct LobbyRPCServerSystem : ISystem
{
    private EntityQuery _createLobbyQuery;
    private EntityQuery _joinLobbyQuery;
    private EntityQuery _kickLobbyQuery;
    private EntityQuery _startGameQuery;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<LobbyDataComponent>();

        _createLobbyQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<CreateLobbyRPC>(),
            ComponentType.ReadOnly<ReceiveRpcCommandRequest>()
        );

        _joinLobbyQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<JoinLobbyRPC>(),
            ComponentType.ReadOnly<ReceiveRpcCommandRequest>()
        );

        _kickLobbyQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<KickLobbyRPC>()
        );

        _startGameQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<StartGameRPC>()
        );
    }

    public void OnUpdate(ref SystemState state)
    {
        var em = state.EntityManager;

        // ---------------- GET LOBBY ----------------
        var lobbyEntity = SystemAPI.GetSingletonEntity<LobbyDataComponent>();
        var lobbyData = em.GetComponentData<LobbyDataComponent>(lobbyEntity);
        var players = em.GetBuffer<LobbyPlayerElement>(lobbyEntity);

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // ------------------------------------------------------
        // CREATE LOBBY (host)
        // ------------------------------------------------------
        var createEntities = _createLobbyQuery.ToEntityArray(Allocator.Temp);
        foreach (var entity in createEntities)
        {
            var rpc = em.GetComponentData<CreateLobbyRPC>(entity);
            var req = em.GetComponentData<ReceiveRpcCommandRequest>(entity);

            // Convert Entity → NetworkId
            int connectionId = em.GetComponentData<NetworkId>(req.SourceConnection).Value;

            lobbyData.MaxPlayers = rpc.MaxPlayers;
            lobbyData.Password = rpc.Password;
            lobbyData.CurrentPlayers = 1;
            lobbyData.IsStarted = 0;

            em.SetComponentData(lobbyEntity, lobbyData);

            players.Clear();
            players.Add(new LobbyPlayerElement
            {
                PlayerName = rpc.Name,
                Weapon = rpc.Character,
                ConnectionId = connectionId,
                Ping = 0
            });

            Debug.Log($"Lobby created by host: {rpc.Name}");

            ecb.DestroyEntity(entity);
        }
        createEntities.Dispose();

        // ------------------------------------------------------
        // JOIN LOBBY
        // ------------------------------------------------------
        var joinEntities = _joinLobbyQuery.ToEntityArray(Allocator.Temp);
        foreach (var entity in joinEntities)
        {
            var rpc = em.GetComponentData<JoinLobbyRPC>(entity);
            var req = em.GetComponentData<ReceiveRpcCommandRequest>(entity);

            int connectionId = em.GetComponentData<NetworkId>(req.SourceConnection).Value;

            // Password check
            if (lobbyData.Password.Length > 0 &&
                rpc.Password.ToString() != lobbyData.Password.ToString())
            {
                Debug.Log("JOIN FAILED: wrong password.");
                ecb.DestroyEntity(entity);
                continue;
            }

            // Max players check
            if (players.Length >= lobbyData.MaxPlayers)
            {
                Debug.Log("JOIN FAILED: lobby full.");
                ecb.DestroyEntity(entity);
                continue;
            }

            players.Add(new LobbyPlayerElement
            {
                PlayerName = rpc.PlayerName,
                Weapon = rpc.Weapon,
                ConnectionId = connectionId,
                Ping = 0
            });

            lobbyData.CurrentPlayers = players.Length;
            em.SetComponentData(lobbyEntity, lobbyData);

            Debug.Log($"Player joined lobby: {rpc.PlayerName}");

            ecb.DestroyEntity(entity);
        }
        joinEntities.Dispose();

        // ------------------------------------------------------
        // KICK
        // ------------------------------------------------------
        var kickEntities = _kickLobbyQuery.ToEntityArray(Allocator.Temp);
        foreach (var entity in kickEntities)
        {
            var rpc = em.GetComponentData<KickLobbyRPC>(entity);
            int targetId = rpc.TargetConnectionId;

            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].ConnectionId == targetId)
                {
                    players.RemoveAt(i);
                    break;
                }
            }

            lobbyData.CurrentPlayers = players.Length;
            em.SetComponentData(lobbyEntity, lobbyData);

            Debug.Log($"Player kicked: {targetId}");

            ecb.DestroyEntity(entity);
        }
        kickEntities.Dispose();

        // ------------------------------------------------------
        // START GAME
        // ------------------------------------------------------
        var startEntities = _startGameQuery.ToEntityArray(Allocator.Temp);
        foreach (var entity in startEntities)
        {
            lobbyData.IsStarted = 1;
            em.SetComponentData(lobbyEntity, lobbyData);

            Debug.Log("StartGameRPC received — marking lobby as started.");

            ecb.DestroyEntity(entity);
        }
        startEntities.Dispose();

        // APPLY
        ecb.Playback(em);
    }
}
