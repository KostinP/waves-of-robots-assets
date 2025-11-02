using Unity.Entities;

// При смерти врага этот System создаёт сущность XP (или сразу увеличивает счет игрока)
partial struct ExperienceSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // TODO: hook into EnemyDeath events and spawn Experience or add to Player component
    }
}
