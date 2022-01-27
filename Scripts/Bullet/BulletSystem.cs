using System;
using System.Collections.Generic;
using forloopcowboy_unity_tools.Scripts.Core;
using UnityEngine;

// Object recycler or whatever this fuck is called
namespace forloopcowboy_unity_tools.Scripts.Bullet
{
    public class BulletSystem : MonoBehaviour
    {
        [Tooltip("Number of bullets kind that are spawned before previous bullet is recycled.")]
        public int maximum = 30;

        /// <summary>
        /// Accessing this gets you all the active bullet game objects.
        /// Setting this object updates the serialized count.
        /// </summary>
        public int NumberOfActiveBullets
        {
            get
            {
                int newActiveCount = 0;
                
                foreach (var bulletQueue in queueDictionary)
                {
                    foreach (var bulletController in bulletQueue.Value)
                    {
                        if (bulletController.gameObject.activeInHierarchy) newActiveCount++;
                    }
                }
                
                return newActiveCount;
            }
        }

        public List<Bullet> bullets = new List<Bullet>();

        private Dictionary<int, Queue<BulletController>> queueDictionary = new Dictionary<int, Queue<BulletController>>();

        private void OnEnable() {

            // instantiate queue
            foreach (var bullet in bullets)
            {
                queueDictionary.Add(bullet.GetHashCode(), new Queue<BulletController>());
            }

        }

        /// <summary>
        /// Either spawns or repossesses a bullet.
        /// </summary>
        public BulletController Spawn(Bullet bulletAsset, Vector3 position, Vector3 direction)
        {
            var hashCode = bulletAsset.GetHashCode();
            if (!queueDictionary.ContainsKey(hashCode)) queueDictionary.Add(hashCode, new Queue<BulletController>());
        
            Queue<BulletController> queue;
            if (queueDictionary.TryGetValue(hashCode, out queue))
            {
                // get or else create instance
                GameObject instance;
                BulletController controller;

                if (NumberOfActiveBullets < maximum) {

                    instance = Instantiate(bulletAsset.prefab, position, Quaternion.identity); 
                    controller = instance.gameObject.GetOrElseAddComponent<BulletController>();
                    controller.Settings = bulletAsset;
                
                } else { controller = queue.Dequeue(); instance = controller.gameObject; }

                controller.ResetBullet();
                instance.transform.position = position;
                instance.transform.rotation = Quaternion.LookRotation(direction);
                instance.SetActive(true);

                queue.Enqueue(controller);

                return controller;
            
            } else throw new System.Exception("Could not find bullet asset in queue Dictionary.");

        }

        public BulletController SpawnAndFire(
            Bullet bulletAsset, 
            Vector3 position,
            Vector3 direction,
            GameObject firedFrom
        )
        {
            var spawned = SpawnAndFire(bulletAsset, position, direction);
            spawned.firedBy = firedFrom;

            return spawned;
        }

        /// <summary>
        /// Either spawns or repossesses a bullet, calling BulletController.Fire immediately.
        /// </summary>
        public BulletController SpawnAndFire(Bullet bulletAsset, Vector3 position, Vector3 direction)
        {

            var spawned = Spawn(bulletAsset, position, direction);
            spawned.Fire(direction);
            spawned.firedBy = null;
            
            return spawned;

        }

        private void OnDestroy()
        {
            float delayIncrement = 0.2f;
            float delay = 0f;
            
            // Destroy all cached bullets
            foreach (var keyValuePair in queueDictionary)
            {
                foreach (var bulletController in keyValuePair.Value)
                {
                    if (bulletController == null) continue;
                    
                    if (Application.isEditor) DestroyImmediate(bulletController.gameObject);
                    else Destroy(bulletController.gameObject, delay += delayIncrement);
                }
            }
        }
    }
}
