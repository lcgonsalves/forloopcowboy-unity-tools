using System;
using System.Collections.Generic;
using System.Linq;
using forloopcowboy_unity_tools.Scripts.Core;
using UnityEngine;

// Object recycler or whatever this fuck is called
namespace forloopcowboy_unity_tools.Scripts.Bullet
{
    public class BulletSystem
    {
        public List<Bullet> bullets = new List<Bullet>();

        private static readonly Dictionary<int, ComponentPool<BulletController>> pools =
            new Dictionary<int, ComponentPool<BulletController>>();

        public static IEnumerable<ComponentPool<BulletController>> GetPools => pools.Select(_ => _.Value);

        private static void InitializeComponentPoolFor(Bullet bullet)
        {
            if (!pools.ContainsKey(bullet.GetHashCode()))
                pools.Add(
                    bullet.GetHashCode(),
                    new ComponentPool<BulletController>(bullet.prefab.GetOrElseAddComponent<BulletController>(), bullet.maximumConcurrentSpawnedBullets)
                );
        }

        /// <summary>
        /// Either spawns or repossesses a bullet.
        /// </summary>
        public static BulletController Spawn(Bullet bulletAsset, Vector3 position, Vector3 direction)
        {
            var hashCode = bulletAsset.GetHashCode();
            InitializeComponentPoolFor(bulletAsset);

            if (pools.TryGetValue(hashCode, out var pool))
            {
                var instance = pool.SpawnAt(position, Quaternion.LookRotation(direction));
                instance.Settings = bulletAsset;

                return instance;
            }

            throw new Exception("Could not find bullet asset in queue Dictionary.");

        }

        public static BulletController SpawnAndFire(
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
        public static BulletController SpawnAndFire(Bullet bulletAsset, Vector3 position, Vector3 direction)
        {

            var spawned = Spawn(bulletAsset, position, direction);
            spawned.Fire(direction);
            spawned.firedBy = null;
            
            return spawned;

        }

        private void Destroy()
        {
            float delayIncrement = 0.2f;
            float delay = 0f;
            
            // Destroy all cached bullets
            foreach (var keyValuePair in pools)
            {
                keyValuePair.Value.Clear(delay);
                delay += delayIncrement;
            }
        }
        
    }
}
