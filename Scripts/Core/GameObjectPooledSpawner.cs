using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Core
{
    /// <summary>
    /// Exposes a single object pool for game objects.
    /// </summary>
    public class GameObjectPooledSpawner : SerializedMonoBehaviour, ISpawner<GameObject>
    {
        public GameObject prefab;
        public Vector3 spawnPosition;
        
        [SerializeField, MinValue(1), OnValueChanged("UpdateCapacity")] private int _capacity;

        private void UpdateCapacity()
        {
            _pool.capacity = _capacity;
        }
        
        public int capacity
        {
            get => _capacity;
            set
            {
                _capacity = value;
                UpdateCapacity();
            }
        }

        private GameObjectPool _pool;
        
        [Button]
        public GameObject GetInstance() => _pool.SpawnAt(GetSpawnPosition());

        private Vector3 GetSpawnPosition()
        {
            return transform.TransformPoint(spawnPosition);
        }

        private void Awake()
        {
            _pool = new GameObjectPool(prefab, capacity);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(GetSpawnPosition(), 0.5f);
        }

        public GameObject SpawnAt(Transform locationAndRotation)
        {
            return _pool.SpawnAt(locationAndRotation);
        }

        public GameObject SpawnAt(Vector3 position, Quaternion? rotation = null)
        {
            return _pool.SpawnAt(position, rotation);
        }
    }
}