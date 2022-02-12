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
    public class PlayerSpawnInstanceHandler : MonoBehaviour, INetworkPrefabInstanceHandler
    {
        public GameObject playerPrefab;
        
        [InfoBox("When a player spawns, the local camera controller is assigned to it.")]
        public PlayerCameraController localPlayerCameraController;

        private void Start()
        {
            NetworkManager.Singleton.PrefabHandler.AddHandler(playerPrefab, this);
        }

        /// <summary>
        /// Instantiates object and assigns local player camera controller to it.
        /// </summary>
        public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
        {
            var instance = Instantiate(playerPrefab, position, rotation);
            var networkObj = instance.GetComponent<NetworkObject>();
            var player = instance.GetComponent<NetworkedPlayer>();
            var healthComponent = instance.GetComponent<NetworkHealthComponent>();

            // assign camera
            player.cameraController = localPlayerCameraController;
            
            // add it to network health tracker
            NetworkHealthTracker.AssociateReactiveUpdateAndTrack(healthComponent, instance.transform);

            return networkObj;
        }

        public void Destroy(NetworkObject networkObject)
        {
            Object.Destroy(networkObject.gameObject);
        }
    }
}