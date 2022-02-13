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
    /// <summary>
    /// Handles spawning of the master player object that
    /// puppets player characters.
    /// </summary>
    public class PlayerMasterSpawner : SingletonMonoBehaviour<PlayerMasterSpawner>, INetworkPrefabInstanceHandler
    {
        public GameObject playerPrefab;
        public GameObject playerCharacterPrefab;
        public float respawnDelay = 2f;
        
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
            instance.name = "Player " + ownerClientId;
            var networkObj = instance.GetComponent<NetworkObject>();
            var player = instance.GetComponent<NetworkedPlayer>();
            
            player.cameraController = localPlayerCameraController;

            return networkObj;
        }

        public void Destroy(NetworkObject networkObject)
        {
            Object.Destroy(networkObject.gameObject);
        }
        
    }
}