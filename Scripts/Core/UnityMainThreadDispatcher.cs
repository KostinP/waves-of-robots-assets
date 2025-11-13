using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher _instance;
    private readonly Queue<Action> _actions = new Queue<Action>();
    private static readonly object _lockObject = new object();

    public static UnityMainThreadDispatcher Instance
    {
        get
        {
            if (_instance == null)
            {
                // ФИКС: Используем FindObjectOfType только в главном потоке
                if (Application.isPlaying)
                {
                    _instance = FindObjectOfType<UnityMainThreadDispatcher>();
                    if (_instance == null)
                    {
                        var obj = new GameObject("UnityMainThreadDispatcher");
                        _instance = obj.AddComponent<UnityMainThreadDispatcher>();
                        DontDestroyOnLoad(obj);
                    }
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void Enqueue(Action action)
    {
        if (action == null) return;

        // ФИКС: Проверяем, находимся ли мы уже в главном потоке
        if (Thread.CurrentThread.ManagedThreadId == 1) // Главный поток Unity обычно имеет ID = 1
        {
            action?.Invoke();
        }
        else
        {
            lock (_lockObject)
            {
                _actions.Enqueue(action);
            }
        }
    }

    private void Update()
    {
        lock (_lockObject)
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
                    Debug.LogError($"Error in main thread action: {e}");
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}