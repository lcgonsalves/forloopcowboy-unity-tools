using forloopcowboy_unity_tools.Scripts.Bullet;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.GameLogic
{
    public class HitBox : MonoBehaviour
    {
        public HealthComponent legacyHealthComponent;
        public NetworkHealthComponent networkHealthComponent;

        public IHealth HealthComponent => (IHealth) networkHealthComponent ?? legacyHealthComponent;

        private void OnCollisionEnter(Collision other)
        {
            
            var bulletController = other.gameObject.GetComponent<BulletController>() ??
                                   other.gameObject.GetComponentInChildren<BulletController>();
            
            if (bulletController != null && HealthComponent != null) HealthComponent.Damage(bulletController.Settings.GetDamageAmount(), bulletController);
            else
            {
                
                var dmgProvider = other.gameObject.GetComponent<SimpleDamageProvider>() ??
                                  other.gameObject.GetComponentInChildren<SimpleDamageProvider>();

                if (dmgProvider != null && HealthComponent != null) HealthComponent.Damage(dmgProvider.GetDamageAmount(), dmgProvider);

            }
        }
    }
}