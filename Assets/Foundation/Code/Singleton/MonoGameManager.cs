using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 游戏初始化的事件
/// </summary>
class GameInitEvent
{
    public MonoGameManager gameManager;

    public GameInitEvent(MonoGameManager gameManager)
    {
        this.gameManager = gameManager;
    }
}

class PostGameInitEvent
{
    public MonoGameManager gameManager;

    public PostGameInitEvent(MonoGameManager gameManager)
    {
        this.gameManager = gameManager;
    }
}

public interface ISingleton
{
    void Init(MonoGameManager manager);
}

public class MonoGameManager : MonoBehaviour
{
    public static MonoGameManager Instance { get; private set; }

    private readonly List<ISingleton> _singletons = new List<ISingleton>();

    IEnumerator Start()
    {
        yield return null;
        Instance = this;

        foreach (var singleton in GetComponentsInChildren<ISingleton>())
        {
            singleton.Init(this);
            AddSingleton(singleton);
        }

        EventBus.Post(new GameInitEvent(this));
        EventBus.Post(new PostGameInitEvent(this));
    }

    public void AddSingleton(ISingleton obj)
    {
        _singletons.Add(obj);
    }

    public T GetSingleton<T>(bool required = true)
    {
        for (var i = 0; i < _singletons.Count; i++)
        {
            var item = _singletons[i];
            if (item is T ret)
                return ret;
        }
        if (required)
            throw new Exception("Can't find singleton of " + typeof(T));
        else
            return default(T);
    }
}
