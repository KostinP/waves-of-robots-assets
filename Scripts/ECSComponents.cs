// Assets/Scripts/ECSComponents.cs
using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Networking.Transport;
using System;

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

/// <summary>
/// Информация о лобби, рассылаемая по LAN.
/// </summary>
[Serializable]
public struct LobbyInfo
{
    public string name;
    public int currentPlayers;
    public int maxPlayers;
    public bool isOpen;
    public string password;
    public string ip;
    public int port;
    public string uniqueId;
}


public struct ConnectToServerCommand : IComponentData
{
    public FixedString128Bytes ServerIP;
    public ushort ServerPort;
    public FixedString128Bytes PlayerName;
    public FixedString64Bytes Password;
}

public struct LobbyData
{
    public string name;
    public string password;
    public int maxPlayers;
    public bool isOpen;
}

public struct NetworkStreamRequestConnect : IComponentData
{
    public NetworkEndpoint Endpoint;
}

public struct PlayerData
{
    public FixedString128Bytes name;
    public string selectedCharacter; // "Vacuum", "Toaster", "GPT"
}

/// <summary>
/// RPC-команда от клиента: присоединение к лобби.
/// </summary>
public struct JoinLobbyCommand : IRpcCommand
{
    public FixedString128Bytes PlayerName;
    public FixedString64Bytes Weapon;
    public FixedString64Bytes Password;
}