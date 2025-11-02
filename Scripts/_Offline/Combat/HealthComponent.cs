using Unity.Entities;

public struct Health : IComponentData
{
    public int Current;
    public int Max;
}