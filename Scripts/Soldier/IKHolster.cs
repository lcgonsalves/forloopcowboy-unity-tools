using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Soldier
{
    /// <summary>
    /// Defines the properties of an IK holster.
    /// </summary>
    [ExecuteInEditMode]
    public class IKHolster : MonoBehaviour
    {
        public WeaponUser.WeaponType type;

        private void Start()
        {
            UpdateTag();
        }

        private void Awake()
        {
            UpdateTag();
        }

        private void OnEnable()
        {
            UpdateTag();
        }

        private void UpdateTag()
        {
            tag = "IKHolster";
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.08f, 0.83f);
            Gizmos.DrawWireSphere(transform.position, 0.08f);
        }
    }
}