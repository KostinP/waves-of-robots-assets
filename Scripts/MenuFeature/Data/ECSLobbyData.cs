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
    [GhostField] public FixedString64Bytes Weapon;
    [GhostField] public ulong ConnectionId;
    [GhostField] public int Ping;
}

public struct KickPlayerCommand : IComponentData
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
    public FixedString64Bytes Weapon;
    public ulong ConnectionId;
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

    public override string ToString()
    {
        return $"LobbyInfo: {name}, Players: {currentPlayers}/{maxPlayers}, Open: {isOpen}, IP: {ip}:{port}, ID: {uniqueId}";
    }
}

public struct LobbyData
{
    public string name;
    public string password;
    public int maxPlayers;
    public bool isOpen;
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

public struct ConnectToServerCommand : IComponentData
{
    public FixedString128Bytes ServerIP;
    public ushort ServerPort;
    public FixedString128Bytes PlayerName;
    public FixedString64Bytes Password;
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

// Структура для информации об игроке в лобби
public struct LobbyPlayerInfo
{
    public string Name;
    public string Weapon;
    public ulong ConnectionId;
    public int Ping;
}

public struct LobbyPlayersRequest : IComponentData { }

public struct LobbyPlayersResponse : IComponentData
{
    public Entity LobbyEntity;
}

// Маркер для синхронизированных данных клиента
public struct SyncedLobbyData : IComponentData 
{
    public bool IsInitialized;
}