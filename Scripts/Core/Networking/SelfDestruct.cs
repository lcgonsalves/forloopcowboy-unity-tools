using Unity.Netcode;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Core.Networking
{
    [RequireComponent(typeof(NetworkObject))]
    public class SelfDestruct : NetworkBehaviour
    {
        [SerializeField]
        private float despawnDelay = 3f;
        
        private void Start()
        {
            if (IsOwner)
            {
                this.RunAsync(
                    () =>
                    {
                        var shouldDestroy = NetworkObject.IsSpawned;

                        if (shouldDestroy)
                            Destroy(gameObject, despawnDelay);

                        return shouldDestroy;
                    }
                );
            }
        }
    }
}