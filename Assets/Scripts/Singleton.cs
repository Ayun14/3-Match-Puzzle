using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T m_intance;

    public static T Instance 
    { 
        get 
        {
            if(m_intance == null)
            {
                m_intance = FindObjectOfType<T>();
                if(m_intance == null)
                {
                    GameObject sigleton = new GameObject(typeof(T).Name);
                    m_intance = sigleton.AddComponent<T>();
                }
            }
            return m_intance; 
        } 
    }

    protected virtual void Awake()
    {
        if(m_intance == null )
        {
            m_intance = this as T;            
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
