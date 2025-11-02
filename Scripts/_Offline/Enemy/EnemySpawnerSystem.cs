using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
partial struct EnemySpawnerSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EntitiesReferences>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var entitiesRefs = SystemAPI.GetSingleton<EntitiesReferences>();
        var timer = SystemAPI.GetSingletonRW<SpawnTimerComponent>();
        timer.ValueRW.Time -= SystemAPI.Time.DeltaTime;
        if (timer.ValueRO.Time <= 0f)
        {
            timer.ValueRW.Time = timer.ValueRO.RespawnInterval;
            // spawn enemy
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
            Entity enemy = ecb.Instantiate(entitiesRefs.enemyPrefabEntity);
            float3 pos = new float3(UnityEngine.Random.Range(-8f, 8f), 0, UnityEngine.Random.Range(-8f, 8f));
            ecb.SetComponent(enemy, LocalTransform.FromPosition(pos));
            ecb.Playback(state.EntityManager);
        }
    }
}

public struct SpawnTimerComponent : IComponentData
{
    public float Time;
    public float RespawnInterval;
}
