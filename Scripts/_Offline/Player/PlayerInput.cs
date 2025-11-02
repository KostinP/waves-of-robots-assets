using Unity.Entities;
using Unity.Mathematics;

// Оффлайн-версия: обычный IComponentData, заполняется InputBridge (MonoBehaviour или System)
public struct PlayerInput : IComponentData
{
    public float2 Move;
    public bool Shoot;
}