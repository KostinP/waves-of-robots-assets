using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
partial struct WaveManagerSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        // Create singleton initial wave data
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
            wave.ValueRW.TimeToNextWave = 30f + wave.ValueRW.CurrentWave * 10f; // simple scale
            Debug.Log($"Wave started: {wave.ValueRO.CurrentWave}");
            // TODO: Notify spawner by setting spawn rate
        }
    }
}

public struct WaveState : IComponentData
{
    public int CurrentWave;
    public float TimeToNextWave;
}
