// Assets/Scripts/ECSComponents.cs
using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;
using Unity.Collections;

[GhostComponent(PrefabType = GhostPrefabType.All)]
public struct PlayerComponent : IComponentData
{
    [GhostField] public FixedString128Bytes Name;
    [GhostField] public ulong ConnectionId;
    [GhostField] public int Ping;
}

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct PlayerInputComponent : IInputComponentData
{
    public bool IsClicked;
    public float2 MouseClickPosition;
}


public struct JoinLobbyCommand : IComponentData
{
    public FixedString128Bytes PlayerName;
    public FixedString64Bytes Password;
    public ulong ConnectionId;
}

public struct StartGameCommand : IComponentData
{
}

public struct KickPlayerCommand : IComponentData
{
    public ulong ConnectionId;
}

public struct SpawnPlayerCommand : IComponentData
{
    public ulong ConnectionId;
}

public struct LobbyDataComponent : IComponentData
{
    public FixedString128Bytes Name;
    public FixedString64Bytes Password;
    public int MaxPlayers;
    public bool IsOpen;
}

public struct LobbyPlayerBuffer : IBufferElementData
{
    public FixedString128Bytes PlayerName;
    public ulong ConnectionId;
}

public struct PlayerPrefabComponent : IComponentData
{
    public Entity Prefab;
}