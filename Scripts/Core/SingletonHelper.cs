using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Core
{
    public class SingletonHelper<T> where T : Component
    {
        private T singletonReference;

        public SingletonHelper() {}

        public SingletonHelper(T singletonReference)
        {
            this.singletonReference = singletonReference;
        }

        public T Singleton => GetSingleton();
        public T GetSingleton()
        {
            var s = singletonReference == null ? Object.FindObjectOfType<T>() : singletonReference;
            if (s == null)
            {
                var instance = new GameObject("GlobalGizmoDrawer");
                s = instance.AddComponent<T>();
            }

            return s;
        }
    }
}