using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct BulletSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        float dt = SystemAPI.Time.DeltaTime;

        // Кэшируем данные о врагах для простого расстояния
        var enemyPositions = new NativeList<float3>(Allocator.Temp);
        var enemyEntities = new NativeList<Entity>(Allocator.Temp);

        foreach (var (enemyLocal, enemyEntity) in
                 SystemAPI.Query<RefRO<LocalTransform>>()
                          .WithAll<EnemyTag>()
                          .WithEntityAccess())
        {
            enemyPositions.Add(enemyLocal.ValueRO.Position);
            enemyEntities.Add(enemyEntity);
        }

        // Движение и столкновения пуль
        foreach (var (local, bullet, entity) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRW<Bullet>>()
                          .WithEntityAccess())
        {
            // Перемещаем пулю
            local.ValueRW.Position += new float3(0, 0, 1) * bullet.ValueRW.Speed * dt;
            bullet.ValueRW.TimeToLive -= dt;

            // Удаляем, если TTL закончился
            if (bullet.ValueRW.TimeToLive <= 0f)
            {
                ecb.DestroyEntity(entity);
                continue;
            }

            // Проверяем столкновения
            for (int i = 0; i < enemyPositions.Length; i++)
            {
                float distSq = math.distancesq(local.ValueRO.Position, enemyPositions[i]);
                if (distSq < (1.2f * 1.2f))
                {
                    // Здесь можно применить урон (через буфер событий или напрямую)
                    // Для простоты: уничтожим пулю
                    ecb.DestroyEntity(entity);
                    break;
                }
            }
        }

        ecb.Playback(state.EntityManager);
        enemyPositions.Dispose();
        enemyEntities.Dispose();
    }
}
