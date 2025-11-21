using UnityEngine;
using Unity.Entities;
using System.Collections.Generic;

public class LobbyUIBridge : MonoBehaviour
{
    void Update()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null) return;

        var em = world.EntityManager;

        // Method 1: Using EntityQuery to find the singleton entity
        var query = em.CreateEntityQuery(typeof(UIEventsSingleton));
        if (query.IsEmpty)
            return;

        var entity = query.GetSingletonEntity();
        var buffer = em.GetBuffer<UIEvent_LobbyUpdated>(entity);

        List<LobbyPlayerInfo> players = new();

        foreach (var item in buffer)
        {
            players.Add(new LobbyPlayerInfo
            {
                Name = item.Name.ToString(),
                Weapon = item.Weapon.ToString(),
                Ping = item.Ping,
                ConnectionId = item.ConnectionId,
                IsLocalPlayer = item.IsLocalPlayer,
                IsHost = item.IsHost
            });
        }

        if (players.Count > 0)
            UILobbyCache.Players = players;
    }
}