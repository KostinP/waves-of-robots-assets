using System;
using System.Collections.Generic;
using UnityEngine;

public class MainThreadDispatcher : MonoBehaviour
{
    private static MainThreadDispatcher _instance;
    private readonly Queue<Action> _actions = new Queue<Action>();

    public static MainThreadDispatcher Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<MainThreadDispatcher>();
                if (_instance == null)
                {
                    var go = new GameObject("MainThreadDispatcher");
                    _instance = go.AddComponent<MainThreadDispatcher>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    public void Enqueue(Action action)
    {
        lock (_actions)
        {
            _actions.Enqueue(action);
        }
    }

    void Update()
    {
        lock (_actions)
        {
            while (_actions.Count > 0)
            {
                var action = _actions.Dequeue();
                try
                {
                    action?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Exception in main thread dispatcher: {e}");
                }
            }
        }
    }

    void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}