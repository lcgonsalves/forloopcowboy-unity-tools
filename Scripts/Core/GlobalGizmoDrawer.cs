using System;
using System.Collections.Generic;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Core
{
    public class GlobalGizmoDrawer : MonoBehaviour
    {
        public Dictionary<string, Action> callbacks = new Dictionary<string, Action>();
        private static SingletonHelper<GlobalGizmoDrawer> _singletonHelper;

        private void OnDrawGizmos()
        {
            foreach (var callback in callbacks)
            {
                callback.Value();
            }
        }

        public static GlobalGizmoDrawer singleton { get; private set; }
        
        public static void CustomGizmo(string key, Action callback)
        {
            singleton = GetSingleton();

            if (!singleton.callbacks.ContainsKey(key))
            {
                singleton.callbacks.Add(key, callback);
            }
            else
            {
                singleton.callbacks[key] = callback;
            }
        }

        public static void ClearGizmo(string key)
        {
            singleton = GetSingleton();
            
            if (singleton.callbacks.ContainsKey(key)) singleton.callbacks.Remove(key);
        }

        private static GlobalGizmoDrawer GetSingleton()
        {
            _singletonHelper = _singletonHelper == null
                ? _singletonHelper = new SingletonHelper<GlobalGizmoDrawer>()
                : _singletonHelper;

            return _singletonHelper.Singleton;
        }
        
        
    }
}