using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;
using UnityEngine;
using Unity.Transforms;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial struct GoInGameClientSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        var query = SystemAPI.QueryBuilder().WithAll<NetworkId>().WithNone<NetworkStreamInGame>().Build();

        foreach (var entity in query.ToEntityArray(Unity.Collections.Allocator.Temp))
        {
            ecb.AddComponent(entity, new NetworkStreamInGame());
            Debug.Log("Setting Client as InGame");

            var rpc = ecb.CreateEntity();
            ecb.AddComponent(rpc, new GoInGameRequestRpc());
            ecb.AddComponent(rpc, new SendRpcCommandRequest());
        }

        ecb.Playback(state.EntityManager);
    }
}

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct GoInGameServerSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
        var entitiesRefs = SystemAPI.GetSingleton<EntitiesReferences>();

        var query = SystemAPI.QueryBuilder().WithAll<GoInGameRequestRpc, ReceiveRpcCommandRequest>().Build();

        foreach (var rpcEntity in query.ToEntityArray(Unity.Collections.Allocator.Temp))
        {
            var rec = SystemAPI.GetComponent<ReceiveRpcCommandRequest>(rpcEntity);

            ecb.AddComponent(rec.SourceConnection, new NetworkStreamInGame());
            Debug.Log("Client connected to server via RPC");

            // spawn player as ghost
            var player = ecb.Instantiate(entitiesRefs.playerPrefabEntity);
            ecb.SetComponent(player, LocalTransform.FromPosition(new float3(UnityEngine.Random.Range(-8, 8), 0, UnityEngine.Random.Range(-8, 8))));

            var nid = SystemAPI.GetComponent<NetworkId>(rec.SourceConnection);
            ecb.AddComponent(player, new GhostOwner { NetworkId = nid.Value });

            ecb.AppendToBuffer(rec.SourceConnection, new LinkedEntityGroup { Value = player });

            ecb.DestroyEntity(rpcEntity);
        }

        ecb.Playback(state.EntityManager);
    }
}
