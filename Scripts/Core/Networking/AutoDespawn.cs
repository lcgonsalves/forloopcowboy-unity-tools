using Unity.Netcode;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Core.Networking
{
    public class AutoDespawn : NetworkBehaviour
    {
        public void DespawnIn(float seconds, bool destroy = true) => 
            this.RunAsyncWithDelay(seconds, () => DespawnServerRpc(destroy));

        [ServerRpc]
        private void DespawnServerRpc(bool destroy)
        {
            var no = GetComponent<NetworkObject>();
            Debug.Log("Despawning... " + no.name);
            no.Despawn(destroy);
        }
    }
}