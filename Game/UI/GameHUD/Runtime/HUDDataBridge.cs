using System.Collections.Generic;
using UnityEngine;

public class HUDDataBridge : MonoBehaviour
{
    public static HUDDataBridge Instance;

    public readonly List<(string name, int ping)> Players = new();

    private void Awake()
    {
        Instance = this;
    }
}

