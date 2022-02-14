using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.Core.Networking;
using forloopcowboy_unity_tools.Scripts.Core.Networking.forloopcowboy_unity_tools.Scripts.Core.Networking;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Spell.Implementations
{
    [CreateAssetMenu(fileName = "Projectile Spell", menuName = "Spells/Projectile Spell", order = 0)]
    public class ProjectileSpellDefinition : SpellSettings
    {
        public float cooldownInSeconds = 0.5f;
        public float projectileVelocity = 15f;

        public LayerMask previewCollidesWithLayers;
        
        [ValidateInput("IsNetworkedRigidbody", "Prefab should be a network rigidbody.")]
        public GameObject projectilePrefab;

        public override INetworkSpell GetNewSpellInstance() => new ProjectileSpell(this);
        
        public override void RegisterPrefabsInPool()
        {
            // register in pool with prewarm
            NetworkObjectPool.Singleton.RegisterPrefab(projectilePrefab);
        }
    }

    public class ProjectileSpell : INetworkSpell
    {
        public ProjectileSpellDefinition Settings { get; }

        private SpamProtection withCooldown;
        
        private BallisticTrajectoryPreview preview;

        public ProjectileSpell(ProjectileSpellDefinition settings)
        {
            this.Settings = settings;
            withCooldown = new SpamProtection(settings.cooldownInSeconds);
        }

        public bool TryCast(
            ISpellCaster caster,
            CastSettings castSettings,
            out NetworkObject locallySpawnedObject
        ) {
            return withCooldown.SafeExecute(() =>
            {
                var position = castSettings.position;
                var direction = castSettings.direction;

                var obj = NetworkObjectPool.Singleton.GetNetworkObject(
                    Settings.projectilePrefab,
                    position,
                    Quaternion.LookRotation(direction)
                );
                
                if (!obj.IsSpawned) obj.Spawn( true);

                var projectile = obj.GetComponent<NetworkProjectile>();
                
                projectile.Fire(GetStartingVelocity(castSettings), caster.NetworkObject);
                projectile.prefab = Settings.projectilePrefab; // so pool works :)
                
                return obj;
                
            }, out locallySpawnedObject);
        }

        public Vector3 GetStartingVelocity(CastSettings cs) => cs.direction * Settings.projectileVelocity;
        
        public IPreview GetPreview(ISpellCaster caster, CastSettings castSettings)
        {
            if (preview == null)
            {
                preview = new BallisticTrajectoryPreview(
                    GetStartingVelocity(castSettings),
                    Settings.previewCollidesWithLayers,
                    startingVelocityGetter: () => caster.GetCastDirection(this) * Settings.projectileVelocity
                );
            }
            
            return preview;
        }
    }
}