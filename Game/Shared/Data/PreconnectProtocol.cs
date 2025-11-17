using System;
using UnityEngine;

[System.Serializable]
public class PreconnectQuery
{
    public string type = "QueryJoin";
    public string password;
    public int playerVersion;
    public string playerName;
}

[System.Serializable]
public class PreconnectResponse
{
    public string result; // "OK", "BadPassword", "LobbyFull", "VersionMismatch"
    public string reason; // optional human text
}

public enum PreconnectResult
{
    Ok,
    BadPassword,
    LobbyFull,
    VersionMismatch,
    Timeout,
    NetworkError,
    Unknown
}
