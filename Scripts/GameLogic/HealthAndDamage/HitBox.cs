using System;
using forloopcowboy_unity_tools.Scripts.Bullet;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.GameLogic
{
    public class HitBox : MonoBehaviour
    {
        public HealthComponent healthComponent;

        private void OnCollisionEnter(Collision other)
        {
            
            var bulletController = other.gameObject.GetComponent<BulletController>() ??
                                   other.gameObject.GetComponentInChildren<BulletController>();
            
            if (bulletController != null && healthComponent != null) healthComponent.Damage(bulletController.Settings.GetDamageAmount(), bulletController);
            else
            {
                
                var dmgProvider = other.gameObject.GetComponent<SimpleDamageProvider>() ??
                                  other.gameObject.GetComponentInChildren<SimpleDamageProvider>();

                if (dmgProvider != null && healthComponent != null) healthComponent.Damage(dmgProvider.GetDamageAmount(), dmgProvider);

            }
        }
    }
}