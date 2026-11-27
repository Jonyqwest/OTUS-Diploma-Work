using System;
using System.Collections.Concurrent;
using UnityEngine;

[DefaultExecutionOrder(-5000)]
public class MainThreadDispatcher : MonoBehaviour
{
    static MainThreadDispatcher _instance;
    public static MainThreadDispatcher Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("MainThreadDispatcher");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<MainThreadDispatcher>();
            }
            return _instance;
        }
    }

    private readonly ConcurrentQueue<Action> _actions = new ConcurrentQueue<Action>();

    public static void Enqueue(Action action)
    {
        if (action == null) return;
        Instance._actions.Enqueue(action);
    }

    void Update()
    {
        while (_actions.TryDequeue(out var a))
        {
            try { a(); }
            catch (Exception ex) { Debug.LogError("[MainThreadDispatcher] Action error: " + ex); }
        }
    }

    private void OnDestroy()
    {
        if (_instance != this)
            return;

        while (_actions.TryDequeue(out _))
        {
        }
        _instance = null;
    }
}
