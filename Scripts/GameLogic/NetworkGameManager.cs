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

        public static SpawnPoint GetRandomSpawnPoint()
        {
            var s = Singleton;
            return s.spawnPoints[Random.Range(0, s.spawnPoints.Count)];
        }
        
        public static void TrySpawnWithDelay(ulong ownerClientId, float respawnDelay) =>
            Singleton.TrySpawnPlayerWithDelayServerRpc(ownerClientId, respawnDelay);

        public static bool TrySpawnPlayer(ulong ownerClientId)
        {
            if (TryGetFirstAvailableSpawnPoint(out var spawnPoint))
            {
                Singleton.SpawnPlayerServerRpc(ownerClientId, spawnPoint.location.position, spawnPoint.location.rotation);
                return true;
            }

            return false;
        }

        public static void DespawnPlayerWithDelay(NetworkedPlayer player, float delayInSeconds = 0f)
        {
            if (player != null && player.NetworkObject != null)
                Singleton.DespawnPlayerWithDelayServerRpc(player.NetworkObject, delayInSeconds);
        }

        [ServerRpc(RequireOwnership = false)]
        private void DespawnPlayerWithDelayServerRpc(NetworkObjectReference playerNetworkObject, float delayInSeconds)
        {
            if (delayInSeconds == 0f && playerNetworkObject.TryGet(out var instance)) PlayerSpawnInstanceHandler.Singleton.Destroy(instance);
            else
                this.RunAsyncWithDelay(
                    delayInSeconds,
                    () =>
                    {
                        if (playerNetworkObject.TryGet(out var instanceAfterDelay))
                            PlayerSpawnInstanceHandler.Singleton.Destroy(instanceAfterDelay);
                    }
                );
        }

        private HashSet<ulong> tryingToSpawn = new HashSet<ulong>(); 

        [ServerRpc(RequireOwnership = false)]
        private void TrySpawnPlayerWithDelayServerRpc(ulong ownerClientId, float respawnDelay)
        {
            if (tryingToSpawn.Contains(ownerClientId)) return;

            tryingToSpawn.Add(ownerClientId);
            
            // waits respawn delay before trying to spawn player
            this.RunAsyncWithDelay(
                respawnDelay,
                () => TrySpawnPlayer(ownerClientId)
            );
        }
        

        [ServerRpc(RequireOwnership = false)]
        private void SpawnPlayerServerRpc(ulong ownerClientId, Vector3 position, Quaternion rotation)
        {
            NetworkObject networkInstance = 
                PlayerSpawnInstanceHandler.Singleton.Instantiate(ownerClientId, position, rotation);

            networkInstance.SpawnAsPlayerObject(ownerClientId);
            tryingToSpawn.Remove(ownerClientId);
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