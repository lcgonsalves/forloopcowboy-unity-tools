using System;
using System.Collections.Generic;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Core
{
    /// <summary>
    /// Exposes SafeExecute methods for preventing
    /// functions from running too often.
    /// </summary>
    public class SpamProtection
    {
        public readonly float debounceInSeconds;
        public DateTime LastExecution { get; private set; }
        
        public SpamProtection(float debounceSeconds)
        {
            debounceInSeconds = debounceSeconds;
            LastExecution = DateTime.MinValue;
        }

        /// <summary>
        /// Executes function if enough time has passed
        /// since the last execution.
        /// </summary>
        /// <param name="fn">Function to be executed</param>
        /// <param name="returnValue">Return value of the function, if it was executed</param>
        /// <typeparam name="T">Return type of function.</typeparam>
        /// <returns>True if successfully executed, false if debounced.</returns>
        public bool SafeExecute<T>(Func<T> fn, out T returnValue)
        {
            DateTime now = DateTime.Now;
            bool canExecute = (now - LastExecution).TotalSeconds >= debounceInSeconds;
            if (canExecute)
            {
                LastExecution = now;
                returnValue = fn();
            }
            else returnValue = default(T);
            
            return canExecute;
        }
        
        /// <summary>
        /// Executes function if enough time has passed
        /// since the last execution.
        /// </summary>
        /// <param name="fn">Function to be executed</param>
        /// <returns>True if successfully executed, false if debounced.</returns>
        public bool SafeExecute(Action fn)
        {
            DateTime now = DateTime.Now;
            bool canExecute = (now - LastExecution).TotalSeconds >= debounceInSeconds;
            if (canExecute)
            {
                LastExecution = now; 
                fn();
            }
            
            return canExecute;
        }

        /// <summary>Returns a new instance of an object instantiator.</summary>
        public static SpamProtectedGameObjectInstantiator ObjectInstantiator(float defaultDebounceInSeconds) =>
            new SpamProtectedGameObjectInstantiator(defaultDebounceInSeconds);

    }

    /// <summary>
    /// Exposes instantiation methods that are spam protected for a given object.
    /// </summary>
    public class SpamProtectedGameObjectInstantiator
    {
        public float defaultDebounceInSeconds;
        private Dictionary<GameObject, SpamProtection> fxSpamProtection = new Dictionary<GameObject, SpamProtection>();
        
        public SpamProtectedGameObjectInstantiator(float defaultDebounceInSeconds)
        {
            this.defaultDebounceInSeconds = defaultDebounceInSeconds;
        }

        public bool SafeInstantiate(
            GameObject prefab,
            Vector3 position,
            out GameObject instance
        ) => SafeInstantiate(prefab, position, Quaternion.identity, defaultDebounceInSeconds, out instance);
        
        /// <summary>
        /// Instantiates the game object with spam protection.
        /// If this is the first time instantiating this prefab, it will use the default debounce value.
        /// </summary>
        /// <param name="prefab">Prefab to spawn</param>
        /// <param name="position">Where to spawn</param>
        /// <param name="rotation">Rotation to spawn</param>
        /// <param name="instance">The instance, if one was created.</param>
        /// <returns>True if an instance was created</returns>
        public bool SafeInstantiate(
            GameObject prefab,
            Vector3 position,
            Quaternion rotation,
            out GameObject instance
        ) => SafeInstantiate(prefab, position, rotation, defaultDebounceInSeconds, out instance);
        
        /// <summary>
        /// Instantiates the game object with spam protection.
        /// </summary>
        /// <param name="prefab">Prefab to spawn</param>
        /// <param name="position">Where to spawn</param>
        /// <param name="rotation">Rotation to spawn</param>
        /// <param name="withDebounce">If this is the first time instantiating a prefab like this, defines what debounce value to use.</param>
        /// <param name="instance">The instance, if one was created.</param>
        /// <returns>True if an instance was created</returns>
        public bool SafeInstantiate(
            GameObject prefab,
            Vector3 position,
            Quaternion rotation,
            float withDebounce,
            out GameObject instance
        )
        {
            if (!fxSpamProtection.ContainsKey(prefab))
                fxSpamProtection.Add(prefab, new SpamProtection(withDebounce));

            return fxSpamProtection[prefab].SafeExecute(
                () => UnityEngine.Object.Instantiate(prefab, position, rotation),
                out instance
            );
        }
    }
}