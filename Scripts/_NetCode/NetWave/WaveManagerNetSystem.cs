using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct WaveManagerNetSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        if (!SystemAPI.HasSingleton<WaveState>())
        {
            state.EntityManager.CreateEntity(typeof(WaveState));
            SystemAPI.SetSingleton(new WaveState { CurrentWave = 0, TimeToNextWave = 5f });
        }
    }

    public void OnUpdate(ref SystemState state)
    {
        var wave = SystemAPI.GetSingletonRW<WaveState>();
        wave.ValueRW.TimeToNextWave -= SystemAPI.Time.DeltaTime;
        if (wave.ValueRW.TimeToNextWave <= 0f)
        {
            wave.ValueRW.CurrentWave += 1;
            wave.ValueRW.TimeToNextWave = 30f + wave.ValueRW.CurrentWave * 8f;
            Debug.Log("[Server] New wave: " + wave.ValueRO.CurrentWave);
            // TODO: set parameters / notify enemy spawner
        }
    }
}