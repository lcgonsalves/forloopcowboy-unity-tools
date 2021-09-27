using System.Collections.Generic;
using forloopcowboy_unity_tools.Scripts.Core;
using UnityEngine;

// Object recycler or whatever this fuck is called
namespace forloopcowboy_unity_tools.Scripts.Bullet
{
    public class BulletSystem : MonoBehaviour
    {
        [Tooltip("Number of bullets kind that are spawned before previous bullet is recycled.")]
        public int maximum = 100;

        [SerializeField, ReadOnly]
        static private int currentActive = 0;

        public int NumberOfActiveBullets { get => currentActive; }

        public List<Bullet> bullets = new List<Bullet>();

        private Dictionary<int, Queue<BulletController>> queueDictionary = new Dictionary<int, Queue<BulletController>>();

        private void OnEnable() {

            // instantiate queue
            foreach (var bullet in bullets)
            {
                queueDictionary.Add(bullet.GetHashCode(), new Queue<BulletController>());
            }

        }

        public BulletController Spawn(Bullet bulletAsset, Vector3 position, Vector3 direction)
        {
        
            if (!queueDictionary.ContainsKey(bulletAsset.GetHashCode())) queueDictionary.Add(bulletAsset.GetHashCode(), new Queue<BulletController>());
        
            Queue<BulletController> queue;
            if (queueDictionary.TryGetValue(bulletAsset.GetHashCode(), out queue))
            {
                // get or else create instance
                GameObject instance;
                BulletController controller;

                if (currentActive < maximum) {

                    instance = Instantiate(bulletAsset.prefab, position, Quaternion.identity); 
                    controller = instance.gameObject.GetOrElseAddComponent<BulletController>();
                    controller.Settings = bulletAsset;
                    currentActive++;
                
                } else { controller = queue.Dequeue(); instance = controller.gameObject; }

                instance.transform.position = position;
                instance.transform.rotation = Quaternion.LookRotation(direction);
                instance.SetActive(true);

                queue.Enqueue(controller);

                return controller;
            
            } else throw new System.Exception("Could not find bullet asset in queue Dictionary.");

        }

        // Either spawns or repossesses a bullet
        public BulletController SpawnAndFire(Bullet bulletAsset, Vector3 position, Vector3 direction) {

            if (!queueDictionary.ContainsKey(bulletAsset.GetHashCode())) queueDictionary.Add(bulletAsset.GetHashCode(), new Queue<BulletController>());
        
            Queue<BulletController> queue;
            if (queueDictionary.TryGetValue(bulletAsset.GetHashCode(), out queue))
            {
                // get or else create instance
                GameObject instance;
                BulletController controller;

                if (currentActive < maximum) {

                    instance = Instantiate(bulletAsset.prefab, position, Quaternion.identity); 
                    controller = instance.gameObject.GetOrElseAddComponent<BulletController>();
                    controller.Settings = bulletAsset;
                    currentActive++;
                
                } else { controller = queue.Dequeue(); instance = controller.gameObject; } // when reached maximum, take from active queue

                controller.ResetBullet();
                instance.transform.position = position;
                instance.transform.rotation = Quaternion.LookRotation(direction);
                instance.SetActive(true);

                controller.Fire(direction);

                queue.Enqueue(controller);

                return controller;

            } else throw new System.Exception("Could not find bullet asset in queue Dictionary.");

        }


    }
}
