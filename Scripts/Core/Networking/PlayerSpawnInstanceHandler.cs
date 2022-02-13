using System;
using System.Net;
using forloopcowboy_unity_tools.Scripts.GameLogic;
using forloopcowboy_unity_tools.Scripts.Player;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace forloopcowboy_unity_tools.Scripts.Core.Networking
{
    public class PlayerSpawnInstanceHandler : SingletonMonoBehaviour<PlayerSpawnInstanceHandler>, INetworkPrefabInstanceHandler
    {
        public GameObject playerPrefab;
        public float respawnDelay = 2f;
        
        [InfoBox("When a player spawns, the local camera controller is assigned to it.")]
        public PlayerCameraController localPlayerCameraController;

        private void Start()
        {
            NetworkManager.Singleton.PrefabHandler.AddHandler(playerPrefab, this);
        }

        /// <summary>
        /// Instantiates object and assigns local player camera controller to it.
        /// If no spawn point is available, object is spawned at the default pos/rot
        /// and is set to disabled.
        /// </summary>
        public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
        {
            bool canSpawn = NetworkGameManager.TryGetFirstAvailableSpawnPoint(out var spawnPoint);
            if (canSpawn)
            {
                position = spawnPoint.location.position;
                rotation = spawnPoint.location.rotation;
            }
            
            var instance = Instantiate(playerPrefab, position, rotation);
            var networkObj = instance.GetComponent<NetworkObject>();

            if (canSpawn) InitializePlayer(ownerClientId, networkObj);
            else instance.SetActive(false);

            return networkObj;
        }

        private void InitializePlayer(ulong ownerClientId, NetworkObject instance)
        {
            var player = instance.GetComponent<NetworkedPlayer>();
            var healthComponent = instance.GetComponent<NetworkHealthComponent>();

            // assign camera
            player.cameraController = localPlayerCameraController;

            // add it to network health tracker
            NetworkHealthTracker.AssociateReactiveUpdateAndTrack(healthComponent, instance.transform);
            
            // when player dies, make it respawn
            healthComponent.NetworkCurrent.OnValueChanged += (value, newValue) =>
            {
                // delay respawn by value
                if (newValue <= 0)
                {
                    NetworkGameManager.TrySpawnWithDelay(ownerClientId, respawnDelay);
                    NetworkGameManager.DespawnPlayerWithDelay(player, respawnDelay * 1.5f);
                }
            };
        }

        public void Destroy(NetworkObject networkObject)
        {
            Object.Destroy(networkObject.gameObject);
        }
        
    }
}