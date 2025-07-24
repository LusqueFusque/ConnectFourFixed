using System;
using System.Collections.Generic;
using UnityEngine;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();

    private static UnityMainThreadDispatcher _instance;

    public static UnityMainThreadDispatcher Instance()
    {
        if (_instance == null)
        {
            // Tenta achar um objeto existente
            _instance = FindObjectOfType<UnityMainThreadDispatcher>();
            if (_instance == null)
            {
                // Se não existir, cria um novo GameObject com o dispatcher
                GameObject obj = new GameObject("UnityMainThreadDispatcher");
                _instance = obj.AddComponent<UnityMainThreadDispatcher>();
                DontDestroyOnLoad(obj);
            }
        }
        return _instance;
    }

    void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }

    public void Enqueue(Action action)
    {
        if (action == null)
            throw new ArgumentNullException("action");

        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }
}