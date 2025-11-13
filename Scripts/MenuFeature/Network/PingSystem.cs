using Unity.Entities;
using Unity.NetCode;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct PingSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Используем NetworkSnapshotAck.EstimatedRTT (в секундах) и переводим в миллисекунды.
        // NetworkSnapshotAck доступен как singleton в NetCode; на клиенте он хранит RTT оценки.
        if (state.WorldUnmanaged.IsClient())
        {
            if (SystemAPI.HasSingleton<NetworkSnapshotAck>())
            {
                var ack = SystemAPI.GetSingleton<NetworkSnapshotAck>();
                int ms = (int)(ack.EstimatedRTT * 1000f);
                foreach (var player in SystemAPI.Query<RefRW<PlayerComponent>>())
                {
                    player.ValueRW.Ping = ms;
                }
            }
            else
            {
                foreach (var player in SystemAPI.Query<RefRW<PlayerComponent>>())
                {
                    player.ValueRW.Ping = 0;
                }
            }
        }
        else if (state.WorldUnmanaged.IsServer())
        {
            // Серверная оценка RTT на клиентах требует доступа к транспортной статистике.
            // Для простоты здесь ставим 0 (или можно интегрировать с конкретным транспортом).
            foreach (var player in SystemAPI.Query<RefRW<PlayerComponent>>())
            {
                player.ValueRW.Ping = 0;
            }
        }
    }
}
