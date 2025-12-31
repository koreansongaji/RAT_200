using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;

    public static T Instance
    {
        get
        {
            if(applicationIsQuitting)
            {
                return null;
            }

            if (instance == null)
            {
                // 해당 컴포넌트를 가지고 있는 게임 오브젝트를 찾아서 반환한다.
                instance = (T)FindObjectOfType(typeof(T));

                if (instance == null) // 인스턴스를 찾지 못한 경우
                {
                    // 새로운 게임 오브젝트를 생성하여 해당 컴포넌트를 추가한다.
                    var obj = new GameObject(typeof(T).Name);
                    // 생성된 게임 오브젝트에서 해당 컴포넌트를 instance에 저장한다.
                    instance = obj.AddComponent<T>();

                    DontDestroyOnLoad(obj);
                }
            }
            return instance;
        }
    }

    private static bool applicationIsQuitting = false;

    protected void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(this.gameObject); // 중복 인스턴스를 방지하기 위해 파괴
        }
    }

    protected void OnApplicationQuit()
    {
        applicationIsQuitting = true;
    }
}
