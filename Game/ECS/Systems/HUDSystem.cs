using Unity.Burst;
using Unity.Entities;
using Unity.Collections;

[BurstCompile]
public partial struct HUDSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Если PlayerComponent содержит Name и Ping, синхронизируем имя в HUDData.
        // Health в HUDData оставляем как есть — источник Health не предоставлен.
        foreach (var (player, hudData) in SystemAPI.Query<PlayerComponent, RefRW<HUDData>>())
        {
            hudData.ValueRW.Name = player.Name; // FixedString64Bytes <- FixedString64Bytes
            // Если в будущем захочешь показывать ping в HUDData, добавь поле Ping в HUDData и раскомментируй:
            // hudData.ValueRW.Ping = player.Ping;
        }
    }
}
