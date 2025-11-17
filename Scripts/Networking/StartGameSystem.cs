using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct StartGameSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Здесь можно загрузить сцену через Server
    }
}
