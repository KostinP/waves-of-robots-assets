// Assets/Scripts/ECSComponents.cs
using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Networking.Transport;
using System;

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct PlayerInputComponent : IInputComponentData
{
    public bool IsClicked;
    public float2 MouseClickPosition;
}

public struct StartGameCommand : IComponentData
{
}

public struct SpawnPlayerCommand : IComponentData
{
    public ulong ConnectionId;
    public FixedString128Bytes PlayerName;
    public FixedString64Bytes Weapon;
}

public struct PlayerPrefabComponent : IComponentData
{
    public Entity Prefab;
}