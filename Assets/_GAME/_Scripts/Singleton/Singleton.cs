using UnityEngine;

namespace GameYT
{
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T instance;
        public static T Ins
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<T>();

                    if (instance == null)
                    {
                        GameObject obj = new GameObject(typeof(T).Name);
                        instance = obj.AddComponent<T>();
                    }
                }
                return instance;
            }
        }

        protected void Awake()
        {
            if (instance == null)
            {
                instance = this as T;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
    }
}
