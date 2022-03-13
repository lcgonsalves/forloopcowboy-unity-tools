using System;
using System.Collections.Generic;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.GameLogic;
using forloopcowboy_unity_tools.Scripts.HUD;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

namespace forloopcowboy_unity_tools.Scripts.GUI
{
    [RequireComponent(typeof(Canvas), typeof(NetworkObject))]
    public class NetworkHealthTracker : SingletonNetworkBehaviour<NetworkHealthTracker>
    {
        public ProgressBar progressBarPrefab;

        private void Awake()
        {
            GetComponent<Canvas>().worldCamera = Camera.main;
        }
    
        public ProgressBar playerProgressBar; 
        private Dictionary<IHealth, ProgressBar> progressBars = new Dictionary<IHealth, ProgressBar>();

        /// <summary>
        /// Updates heath bar on damage and death.
        /// </summary>
        public static void AssociateReactiveUpdate(HealthComponent healthComponent, bool isPlayer = false)
        {
            void DoUpdate() {
                if (isPlayer)
                    UpdatePlayerProgressBar(healthComponent);
                else
                    UpdateProgressbar(healthComponent);
            }

            healthComponent.onDamage += (dmg, _) => DoUpdate();
            healthComponent.onDeath += DoUpdate;

            // initialize it
            DoUpdate();
        }

        /// <summary>
        /// Updates heath bar on damage and death, and follows it's position.
        /// </summary>
        public static void AssociateReactiveUpdateAndTrack(IHealth healthComponent, Transform lookAt)
        {
            UpdateAndTrackProgressbar(healthComponent, lookAt);

            switch (healthComponent)
            {
                case HealthComponent legacyHealthComponent:
                    AssociateReactiveUpdate(legacyHealthComponent);
                    break;
                case NetworkHealthComponent networkHealthComponent:
                    AssociateReactiveNetworkUpdate(networkHealthComponent);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(healthComponent));
            }
        }

        public static void AssociateReactiveNetworkUpdate(NetworkHealthComponent networkHealthComponent)
        {
            networkHealthComponent.NetworkCurrent.OnValueChanged += 
                (_, newValue) => UpdateProgressbar(networkHealthComponent);
        }

        public static void UpdatePlayerProgressBar(HealthComponent playerHealthComponent)
        {
            var ppb = Singleton.playerProgressBar;
            Assert.IsNotNull(ppb, "Trying to update player progress bar, but none has been specified.");
        
            UpdateProgressbarFromHealth(playerHealthComponent, ppb);
        }
    
        public static void UpdateProgressbar(IHealth component)
        {
            Singleton.UpdateValues(component);
        }

        public static void UpdateAndTrackProgressbar(IHealth component, Transform lookAt)
        {
            Singleton.UpdateAndTrack(healthComponent: component, lookAt);
        }

        public void UpdateValues(IHealth healthComponent)
        {
            GetOrCreateHealthBar(healthComponent, (progressBar, _) => UpdateProgressbarFromHealth(healthComponent, progressBar));
        }

        [ClientRpc]
        public void UpdateValuesClientRpc(NetworkBehaviourReference networkBehaviourReference)
        {
            if (networkBehaviourReference.TryGet(out NetworkBehaviour nb))
            {
                UpdateValues((NetworkHealthComponent) nb);
            }
        }
    
        /// <summary>
        /// Gets or creates healthbar.
        /// </summary>
        /// <param name="healthComponent"></param>
        /// <param name="update">Update closure to be done to the progress bar. First param is the progress bar game object component, second is true if the progress bar is a fresh instance.</param>
        private void GetOrCreateHealthBar(IHealth healthComponent, Action<ProgressBar, bool> update)
        {
            var id = healthComponent;
            bool isNewInstance = false;
        
            if (healthComponent.IsAlive)
            {
                if (!progressBars.ContainsKey(id))
                {
                    progressBars.Add(id, Instantiate(progressBarPrefab, transform));
                    isNewInstance = true;
                }

                // update position to follow above head.
                if (progressBars.TryGetValue(id, out var progressBar))
                {
                    update(progressBar, isNewInstance);
                }
            }
            else if (progressBars.ContainsKey(id)) // && component is dead
            {

                if (progressBars.TryGetValue(id, out var progressBar))
                {
                    update(progressBar, false); // expression is always false
                    Destroy(progressBar.gameObject, 5f);
                }

                progressBars.Remove(id);
            }
        }
    
        private void UpdateAndTrack(IHealth healthComponent, Transform lookAt)
        {
            GetOrCreateHealthBar(
                healthComponent,
                (progressBar, isNewInstance) =>
                {
                    // only needs to be done once
                    if (isNewInstance) TrackPosition(lookAt, progressBar);
                    UpdateProgressbarFromHealth(healthComponent, progressBar);
                }
            );
        }

        private void TrackPosition(Transform lookAt, ProgressBar progressBar)
        {
            WorldPositionFollower follower =
                progressBar.gameObject.GetOrElseAddComponent<WorldPositionFollower>();
        
            HideBarIfTrackedTargetIsInvisible(progressBar, follower); // Coroutine

            if (follower.canvas == null) 
                follower.canvas = GetComponent<Canvas>();
        
            follower.lookAt = lookAt;
        }

        private static Coroutine HideBarIfTrackedTargetIsInvisible(ProgressBar progressBar, WorldPositionFollower follower)
        {
            return follower.RunAsync(
                () =>
                {
                    if (follower.lookAtIsNotVisible) progressBar.Hide();
                    else progressBar.Show();
                },
                () => progressBar == null,
                GameObjectHelpers.RoutineTypes.TimeInterval,
                0.1f // not that often but fast enough to disappear when needed without much delay.
            );
        }

        private static void UpdateProgressbarFromHealth(IHealth healthComponent, ProgressBar progressBar)
        {
            progressBar.max = healthComponent.Max;
            progressBar.current = healthComponent.Current;
        }
    }
}
