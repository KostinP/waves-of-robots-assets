using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;
using UnityEngine;

public static class LobbyClientRequests
{
    private static World GetClientWorld()
    {
        foreach (var w in World.All)
            if (w.IsClient()) return w;

        return null;
    }

    private static World GetServerWorld()
    {
        foreach (var w in World.All)
            if (w.IsServer()) return w;

        return null;
    }

    private static Entity GetConnection(World w)
    {
        var em = w.EntityManager;

        var q = em.CreateEntityQuery(
            ComponentType.ReadOnly<NetworkId>(),
            ComponentType.ReadOnly<NetworkStreamInGame>()
        );

        var arr = q.ToEntityArray(Allocator.Temp);
        var result = arr.Length > 0 ? arr[0] : Entity.Null;
        arr.Dispose();

        return result;
    }

    // JOIN
    public static void SendJoinLobby(string playerName, string weapon, string password)
    {
        var w = GetClientWorld();
        if (w == null) return;

        var em = w.EntityManager;
        var rpc = em.CreateEntity();

        em.AddComponentData(rpc, new JoinLobbyRPC
        {
            PlayerName = new FixedString64Bytes(playerName),
            Weapon = new FixedString64Bytes(weapon),
            Password = new FixedString64Bytes(password)
        });

        em.AddComponentData(rpc, new SendRpcCommandRequest
        {
            TargetConnection = GetConnection(w)
        });
    }

    // CREATE
    public static void SendCreateLobby(string name, string password, int maxPlayers, string character)
    {
        var w = GetClientWorld();
        if (w == null) return;

        var em = w.EntityManager;
        var rpc = em.CreateEntity();

        em.AddComponentData(rpc, new CreateLobbyRPC
        {
            Name = new FixedString64Bytes(name),
            Password = new FixedString64Bytes(password),
            MaxPlayers = maxPlayers,
            Character = new FixedString64Bytes(character)
        });

        em.AddComponentData(rpc, new SendRpcCommandRequest
        {
            TargetConnection = GetConnection(w)
        });
    }

    // KICK
    public static void SendKick(int id)
    {
        var w = GetServerWorld() ?? GetClientWorld();
        if (w == null) return;

        var em = w.EntityManager;
        var rpc = em.CreateEntity();

        em.AddComponentData(rpc, new KickLobbyRPC
        {
            TargetConnectionId = id
        });

        em.AddComponentData(rpc, new SendRpcCommandRequest
        {
            TargetConnection = GetConnection(w)
        });
    }

    // START GAME
    public static void SendStartGame()
    {
        var w = GetServerWorld() ?? GetClientWorld();
        if (w == null) return;

        var em = w.EntityManager;
        var rpc = em.CreateEntity();

        em.AddComponentData(rpc, new StartGameRPC());

        em.AddComponentData(rpc, new SendRpcCommandRequest
        {
            TargetConnection = GetConnection(w)
        });
    }
}
