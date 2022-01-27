using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace forloopcowboy_unity_tools.Scripts.Core
{
    /// <summary>
    /// Generic definition of an object pool
    /// </summary>
    public interface IObjectPool<out T> where T : class
    {
        /// <summary>
        /// Maximum number of objects in the pool that will be instantiated
        /// before it starts recycling objects.
        /// </summary>
        int capacity { get; set; }
        
        /// <summary>
        /// All objects ins the pool, active or not.
        /// </summary>
        IEnumerable<T> all { get; }

        /// <summary>
        /// Instantiates or repossesses an object instance.
        /// </summary>
        T GetInstance();

        /// <summary>
        /// Destroys all objects in pool.
        /// </summary>
        /// <returns>Number of objects cleared.</returns>
        int Clear(float delay = 0.0f);

    }

    public interface ISpawner<out T> where T : class
    {
        public T SpawnAt(Transform locationAndRotation);
        
        public T SpawnAt(Vector3 position, Quaternion? rotation = null);
    }
    
    public class ObjectPool<T> : IObjectPool<T> where T : class
    {
        public static int DEFAULT_CAPACITY = 10;

        public int capacity { get; set; } = DEFAULT_CAPACITY;

        public IEnumerable<T> all => pooledObjects.FindAll(_ => _.reference != null).Select(_ => _.reference);

        private readonly Func<T> Builder;
        private readonly Func<T, bool> IsActive;

        protected class PooledObject
        {
            public T reference;
            
            /// <summary>
            /// The last time the object was enabled.
            /// </summary>
            public DateTime lastEnabled;

            public PooledObject(T reference, DateTime lastEnabled)
            {
                this.reference = reference;
                this.lastEnabled = lastEnabled;
            }
        }

        protected List<PooledObject> pooledObjects = new List<PooledObject>();

        public ObjectPool(Func<T> builder, Func<T, bool> isActive)
        {
            Builder = builder;
            IsActive = isActive;
        }
        
        public ObjectPool(Func<T> builder, Func<T, bool> isActive, int capacity)
        {
            Builder = builder;
            IsActive = isActive;
            
            this.capacity = capacity;
        }

        public T GetInstance()
        {
            if (capacity <= 0) throw new ArgumentException($"Capacity {capacity} is not valid.");
            
            var inactive = pooledObjects.Find(_ => !IsActive(_.reference));
            if (inactive is {reference: { }})
            {
                inactive.lastEnabled = DateTime.Now;
                return inactive.reference;
            }
            
            // be sure to clear nulls before getting instance, as we cannot guarantee object won't be deleted by external process
            pooledObjects.RemoveAll(_ => _ == null);

            // if no inactive object is available, and we are still under capacity, we instantiate a new object.
            if (pooledObjects.Count < capacity)
            {
                var instance = Builder();
                
                pooledObjects.Add(
                    new PooledObject(
                        instance,
                        DateTime.Now
                    )
                );
                
                return instance;
            }
            
            // otherwise we reposses the oldest active object.
            var allActive = pooledObjects.FindAll(_ => IsActive(_.reference));
            
            if (allActive.Count == 0)
                throw new NullReferenceException(
                    "Invalid state: No active objects to repossess, no inactive objects, and max capacity.");
            
            PooledObject oldestActivePooledObject = allActive[0];

            foreach (var pooledObject in allActive)
            {
                var isOlder = DateTime.Compare(pooledObject.lastEnabled, oldestActivePooledObject.lastEnabled) < 0;
                if (isOlder) 
                    oldestActivePooledObject = pooledObject;
            }
            
            oldestActivePooledObject.lastEnabled = DateTime.Now;
            return oldestActivePooledObject.reference;
        }

        public virtual int Clear(float delay = 0.0f)
        {
            int totalObjects = pooledObjects.Count;
            pooledObjects.Clear();
            return totalObjects;
        }
    }

    public class GameObjectPool : ObjectPool<GameObject>, ISpawner<GameObject>
    {
        public GameObjectPool(GameObject prefab, int capacity) : 
            base(
                () => Object.Instantiate(prefab),
                _ => _.activeInHierarchy,
                capacity
            ) { }

        public GameObject SpawnAt(Transform locationAndRotation) =>
            SpawnAt(locationAndRotation.position, locationAndRotation.rotation);

        public GameObject SpawnAt(Vector3 position, Quaternion? rotation = null)
        {
            var instance = GetInstance();
            return UpdateInstancePositionAndRotationAndActivate(instance, position, rotation);
        }

        public override int Clear(float delay = 0.0f)
        {
            foreach (var pooledObject in pooledObjects)
                Object.Destroy(pooledObject.reference, delay);
            
            return base.Clear(delay);
        }

        public static GameObject UpdateInstancePositionAndRotationAndActivate(
            GameObject instance,
            Vector3 position,
            Quaternion? rotation = null
        ) {
            instance.SetActive(true);
            instance.transform.position = position;
            if (rotation.HasValue) instance.transform.rotation = rotation.Value;

            return instance;
        }
    }
    
    /// <summary>
    /// Same as game object pool, but with the benefit of caching
    /// the component directly in the pool, preventing the
    /// need to get component every time.
    /// </summary>
    /// <typeparam name="TC">Component type</typeparam>
    public class ComponentPool<TC> : ObjectPool<TC>, ISpawner<TC>
        where TC : Component
    {
        public ComponentPool(TC prefab, int capacity) : 
            base(
                () => Object.Instantiate(prefab),
                _ => _.gameObject.activeInHierarchy,
                capacity
            ) { }
        
        /// <summary>
        /// This constructor adds the component to the prefab if one is not added at spawn
        /// time.
        /// </summary>
        public ComponentPool(GameObject prefab, int capacity) : 
            base(
                () => Object.Instantiate(prefab).GetOrElseAddComponent<TC>(),
                _ => _.gameObject.activeInHierarchy,
                capacity
            ) { }

        public TC SpawnAt(Transform locationAndRotation) =>
            SpawnAt(locationAndRotation.position, locationAndRotation.rotation);

        public TC SpawnAt(Vector3 position, Quaternion? rotation = null)
        {
            var instance = GetInstance();
            GameObjectPool.UpdateInstancePositionAndRotationAndActivate(instance.gameObject, position, rotation);
            return instance;
        }
        
        public override int Clear(float delay = 0.0f)
        {
            foreach (var pooledObject in pooledObjects)
                Object.Destroy(pooledObject.reference.gameObject, delay);
            
            return base.Clear(delay);
        }
    }

}