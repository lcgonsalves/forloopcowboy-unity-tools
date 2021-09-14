using System;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Weapon
{
    [CreateAssetMenu]
    public class Weapon : ScriptableObject
    {
        public float bulletsPerMinute = 10f;

        public int clipSize = 30;

        public int minimumDamage = 10;
        public int maximumDamage = 50;

        public GameObject prefab;

        public Bullet.Bullet ammo;

        [Obsolete]
        public static Transform GrabPointA(GameObject obj)
        {
            return obj.transform.Find("GrabPointA");
        }
        
        [Obsolete]
        public static Transform GrabPointB(GameObject obj)
        {
            return obj.transform.Find("GrabPointB");
        }

        [Obsolete]
        public static Transform MuzzlePosition(GameObject obj)
        {
            return obj.transform.Find("Muzzle");
        }

        public Vector3 weaponPosition = new Vector3(-0.1f, 0.25f, 0.04f);

        public Quaternion weaponRotation = Quaternion.Euler(-90, 0, 90);

    }
}
