using System;
using System.Collections.Generic;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.Core.Networking.forloopcowboy_unity_tools.Scripts.Core.Networking;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Spell.Implementations
{
    [CreateAssetMenu(fileName = "Projectile Spell", menuName = "Spells/Projectile Spell", order = 0)]
    public class ProjectileSpellSettings : SpellSettings
    {
        public float cooldownInSeconds = 0.5f;
        public float projectileVelocity = 15f;

        public LayerMask previewCollidesWithLayers;
        
        [ValidateInput("IsNetworkedRigidbody", "Prefab should be a network rigidbody.")]
        public GameObject projectilePrefab;

        public override INetworkSpell GetNewSpellInstance() => new ProjectileSpell(this);
        public override IEnumerable<GameObject> GetPrefabsToBePooled() => new[] {projectilePrefab};
        
    }

    public class ProjectileSpell : INetworkSpell
    {
        public ProjectileSpellSettings Settings { get; }

        private SpamProtection withCooldown;
        
        private BallisticTrajectoryPreview preview;

        public ProjectileSpell(ProjectileSpellSettings settings)
        {
            this.Settings = settings;
            withCooldown = new SpamProtection(settings.cooldownInSeconds);
        }

        public bool TryCast(ISpellCaster caster, out NetworkObject locallySpawnedObject)
        {
            return withCooldown.SafeExecute(() =>
            {
                var position = caster.GetCastPosition(this);
                var direction = caster.GetCastDirection(this);

                var obj = NetworkObjectPool.Singleton.GetNetworkObject(
                    Settings.projectilePrefab,
                    position,
                    Quaternion.LookRotation(direction)
                );

                var rb = obj.GetComponent<Rigidbody>();
            
                rb.AddForce(GetStartingVelocity(caster), ForceMode.VelocityChange);

                return obj;
                
            }, out locallySpawnedObject);
        }

        public Vector3 GetStartingVelocity(ISpellCaster caster) =>
            caster.GetCastDirection(this) * Settings.projectileVelocity;
        
        public IPreview GetPreview(ISpellCaster caster)
        {
            if (preview == null)
            {
                preview = new BallisticTrajectoryPreview(
                    GetStartingVelocity(caster),
                    Settings.previewCollidesWithLayers,
                    startingVelocityGetter: () => GetStartingVelocity(caster)
                );
            }
            
            return preview;
        }
    }
}