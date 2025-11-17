using System.Collections.Generic;
using UnityEngine;

public static class LobbyDiscoverySystem
{
    // UI читает этот список на главном потоке
    public static List<LobbyInfo> LatestDiscovered = new();
}
