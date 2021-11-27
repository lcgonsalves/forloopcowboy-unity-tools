using System;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.Soldier;
using Sirenix.OdinInspector;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Weapon
{
    [CreateAssetMenu]
    public class Weapon : SerializedScriptableObject
    {
        public float bulletsPerMinute = 10f;
        
        public int clipSize = 30;

        public int minimumDamage = 10;
        public int maximumDamage = 50;

        public WeaponSavedIKSettings ikSettings;
        public WeaponUser.WeaponItem inventorySettings;

        public GameObject prefab;
        public GameObject muzzleEffect;

        [InlineEditor(InlineEditorModes.FullEditor)]
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

    [Serializable]
    public class WeaponSavedIKSettings : IKSettings
    {
        public WeaponSavedIKSettings(WeaponIKSettings from)
        {
            this._limb = from.limb;
            this._path = from.target.name;
            this._translation = from.translation;
            this._rotation = from.rotation;
        }
        
        public AvatarIKGoal _limb;
        public string _path;
        public IKWeightSettings<Vector3> _translation;
        public IKWeightSettings<Vector3> _rotation;

        public AvatarIKGoal limb { get => _limb; }
        public string path { get => _path; }
        public IKWeightSettings<Vector3> translation { get => _translation; }
        public IKWeightSettings<Vector3> rotation { get => _rotation; }
    }
    
}
