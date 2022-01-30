using System;
using System.Collections.Generic;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.GameLogic;
using forloopcowboy_unity_tools.Scripts.Player;
using Sirenix.OdinInspector;
using Sirenix.Serialization.Utilities;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Environment
{
    
    [InfoBox(
        "This object spawns as the solid object, and upon collisions with said object will switch it for the " +
        "shattered object. Assign child objects to these variables.",
        InfoMessageType.Info)]
    public class DestructableObject : SerializedMonoBehaviour
    {
        [TabGroup("Instance Settings"), SerializeField]
        private List<InstanceConfiguration> instanceConfigurations = new List<InstanceConfiguration>();

        [InfoBox("Shattered element must contain the particles of all other object instances. This is to take" +
                 "advantage of the Rayfire connectivity component.")]
        [SerializeField, TabGroup("Instance Settings")]
        [ValidateInput("IsActive", "Shattered object needs to be active at initialization -- Performance degradation will happen when swapping objects!")]
        private ObjectConfiguration shatteredObject = new ObjectConfiguration();
        private bool IsActive(ObjectConfiguration config) => config.obj.IsNotNull() && config.obj.activeInHierarchy && !Application.isPlaying;
        
        [InfoBox("These settings are applied for all sets of objects. They may be tracked differently for different " +
                 "pairs of objects (i.e. the number of hits is counted per solid object) but their settings is global.")]
        [TabGroup("Destruction Settings")]
        [SerializeField] private DestructableObjectSettings settings;

        [TabGroup("Destruction Settings")]
        [LabelText("Default Settings")]
        [Tooltip("These are the values that will be used if no settings are provided.")]
        [HideIf("HasPrefabSettings")]
        [SerializeField]
        private SimpleDestructableObjectSettings simpleSettings;

        private bool HasPrefabSettings => settings != null;
        
        public IDestructableObjectSettings Settings => settings != null ? (IDestructableObjectSettings) settings : simpleSettings;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {

            for (int i = 0; i < instanceConfigurations.Count; i++)
            {
                var instanceConfiguration = instanceConfigurations[i];
                
                // Hide shattered and show solid
                var solidObject     = instanceConfiguration.SolidObject;
                
                solidObject.obj.IfNotNull(obj =>
                {
                    var detector = solidObject.GetCollisionDetector;
                    detector.onTriggerEnter += (_, __) => OnHitDetectedInSolidObject(_, __, instanceConfiguration);
                }, "Cannot detect collisions.");
                
                shatteredObject.obj.IfNotNull(_ =>
                {
                    if (!_.activeInHierarchy) Debug.LogWarning("Shattered not active -- Performance degradation will happen when activating object!");
                    _.SetActive(false);
                });
            }
        }

        private void OnHitDetectedInSolidObject(CollisionDetector detector, Collider c, InstanceConfiguration configuration)
        {
            // ignore certain hits
            if (configuration.ignoreHitPredicate.Apply(c.gameObject)) return;
            
            configuration.IncrementHitCount();
            if (!Settings.IsReadyToDestroy(configuration.hitsSoFar)) return;
            
            // disable ALL solid objects, as shattered objects contain all the particles for a given object group.
            foreach (var instanceConfiguration in instanceConfigurations)
                instanceConfiguration.SolidObject.obj.IfNotNull(_ => _.SetActive(false));
            
            shatteredObject.obj.IfNotNull(_ => _.SetActive(true), "Shattered object cannot be swapped to.");
        }

        [Serializable]
        public class ObjectConfiguration
        {
            [HideLabel]
            public GameObject obj;
            public bool ignorePlayerHits = true;
            private Core.Cache<CollisionDetector> collisionDetector;

            public CollisionDetector GetCollisionDetector
            {
                get
                {
                    if (collisionDetector == null)
                    {
                        collisionDetector =
                            new Core.Cache<CollisionDetector>(() => obj.GetOrElseAddComponent<CollisionDetector>());
                    }
                    
                    obj.WarnIfIsNull();
                    
                    if (obj != null)
                    {
                        return collisionDetector.Get;
                    }

                    return null;
                }
            }
        }

        [Serializable]
        public class InstanceConfiguration
        {

            [InfoBox("Solid object is displayed by default and each object counts the hits individually. The first object to reach the hit" +
                     " count will trigger the mesh swap for ALL objects.")]
            [SerializeField]
            [ValidateInput("HasTriggerAttached",
                "Solid Object must have a trigger in order to detect an approaching object.", InfoMessageType.Error)]
            [ValidateInput("IsNotNull")]
            private ObjectConfiguration solidObject = new ObjectConfiguration();

            public GameObjectPredicate ignoreHitPredicate;
            
            [ShowInInspector, Sirenix.OdinInspector.ReadOnly, BoxGroup("Debugging Info")]
            public int hitsSoFar { get; private set; }
            public void ResetHitCount() => hitsSoFar = 0;
            public void IncrementHitCount() => hitsSoFar++;
            
            public ObjectConfiguration SolidObject
            {
                get => solidObject;
                set => solidObject = value;
            }

            // validation
            
            private bool IsNotNull(ObjectConfiguration config) => config.obj.IsNotNull();

            private bool HasTriggerAttached(ObjectConfiguration config)
            {
                foreach (var collider in config.obj.GetComponents<Collider>())
                {
                    if (collider.isTrigger) return true;
                }

                return false;
            }
        }
    }
}