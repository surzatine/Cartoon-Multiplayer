using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingletonBehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T instance;
    public static T Instance
    {
        get
        {
            if (instance == null)
                SetInstance();
            return instance;
        }

    }
    public virtual void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        DontDestroyOnLoad(this.gameObject);
        SetInstance();

    }
    private static void SetInstance()
    {

        if (instance != null)
        {
            return;
        }
        var instanceActiveInScene = FindObjectOfType<T>();
        if (instanceActiveInScene == null)
        {
            var item = new GameObject(typeof(T).Name);
            instanceActiveInScene = item.AddComponent<T>();
        }
        instance = instanceActiveInScene;


    }

}

