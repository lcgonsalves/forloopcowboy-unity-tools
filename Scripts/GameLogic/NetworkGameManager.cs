using System;
using System.Collections.Generic;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.Core.Networking;
using forloopcowboy_unity_tools.Scripts.Core.Networking.forloopcowboy_unity_tools.Scripts.Core.Networking;
using forloopcowboy_unity_tools.Scripts.Player;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

namespace forloopcowboy_unity_tools.Scripts.GameLogic
{
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkGameManager : SingletonNetworkBehaviour<NetworkGameManager>
    {
        public List<SpawnPoint> spawnPoints = new List<SpawnPoint>();
        public LayerMask playerLayerMask;

        public Dictionary<ulong, NetworkObject> playerCharacters = new Dictionary<ulong, NetworkObject>();

        public static SpawnPoint GetRandomSpawnPoint()
        {
            var s = Singleton;
            return s.spawnPoints[Random.Range(0, s.spawnPoints.Count)];
        }

        /// <summary>
        /// Gets or creates a new instance of the
        /// player character prefab, and assigns it to
        /// the player object.
        /// </summary>
        public static void GetOrCreateCharacterForPlayer(NetworkedPlayer playerMaster) =>
            Singleton.GetOrCreateCharacterForPlayerServerRpc((NetworkBehaviourReference) playerMaster);

        [ServerRpc(RequireOwnership = false)]
        private void GetOrCreateCharacterForPlayerServerRpc(NetworkBehaviourReference playerMasterRef)
        {
            if (playerMasterRef.TryGet(out NetworkedPlayer playerMaster))
            {
                var character = GetOrCreateCharacter(playerMaster);
                character.gameObject.name = "Player " + playerMaster.OwnerClientId + " Character";
                
                character.SpawnWithOwnership(playerMaster.OwnerClientId);
                playerMaster.AssignNewCharacter(character);
            }
        }

        /// <summary>
        /// Gets or creates character, spawning it with ownership to
        /// the player master.
        /// </summary>
        private NetworkObject GetOrCreateCharacter(NetworkedPlayer playerMaster)
        {
            var id = playerMaster.OwnerClientId;
            
            // instantiate new if previous instance has been destroyed
            if (!playerCharacters.ContainsKey(id) || playerCharacters[id] == null)
            {
                var newInstance = Instantiate(PlayerMasterSpawner.Singleton.playerCharacterPrefab);
                var newInstanceNetObj = newInstance.GetComponent<NetworkObject>();
                playerCharacters.Add(id, newInstanceNetObj);
            }

            return playerCharacters[id];
        }

        public static bool TryGetFirstAvailableSpawnPoint(out SpawnPoint spawnPoint)
        {
            foreach (var potentialSpawnPoint in Singleton.spawnPoints)
            {
                if (!SpawnPointIsBusy(potentialSpawnPoint, 5f))
                {
                    spawnPoint = potentialSpawnPoint;
                    return true;
                }
            }

            spawnPoint = default;
            return false;
        }

        /// <returns>True if there is a player in the spawn point.</returns>
        public static bool SpawnPointIsBusy(SpawnPoint spawnPoint, float radius = 2f) =>
            LocationIsBusy(spawnPoint.location.position, radius);
        
        public static bool LocationIsBusy(Vector3 location, float radius) =>
            Physics.CheckSphere(
                location,
                radius,
                Singleton.playerLayerMask
            );
    }

    [Serializable]
    public struct SpawnPoint
    {
        public Transform location;
        public string team;
    }
}