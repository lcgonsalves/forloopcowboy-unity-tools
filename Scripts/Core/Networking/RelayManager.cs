using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;


namespace forloopcowboy_unity_tools.Scripts.Core.Networking
{
    public class RelayManager : SingletonMonoBehaviour<RelayManager>
    {
        // ReSharper disable once InconsistentNaming
        private const string UDP = "udp";
        
        [SerializeField] private string environment = "production";
        [SerializeField] private int maxConnections = 10;

        public UnityTransport Transport =>
            NetworkManager.Singleton.gameObject.GetComponent<UnityTransport>();

        public bool IsRelayEnabled => Transport != null &&
                                      Transport.Protocol == UnityTransport.ProtocolType.RelayUnityTransport;

        // Set variables
        public NetworkDriver HostDriver;
        public NetworkDriver PlayerDriver;
        public string JoinCode;

        private NetworkConnection clientConnection;
        private bool isRelayServerConnected = false;

        // Set utility functions for constructing server data objects
        private static RelayAllocationId ConvertFromAllocationIdBytes(byte[] allocationIdBytes)
        {
          unsafe
          {
              fixed (byte* ptr = allocationIdBytes)
              {
                  return RelayAllocationId.FromBytePointer(ptr, allocationIdBytes.Length);
              }
          }
        }

        private static RelayConnectionData ConvertConnectionData(byte[] connectionData)
        {
          unsafe
          {
              fixed (byte* ptr = connectionData)
              {
                  return RelayConnectionData.FromBytePointer(ptr, RelayConnectionData.k_Length);
              }
          }
        }

        private static RelayHMACKey ConvertFromHMAC(byte[] hmac)
        {
          unsafe
          {
              fixed (byte* ptr = hmac)
              {
                  return RelayHMACKey.FromBytePointer(ptr, RelayHMACKey.k_Length);
              }
          }
        }
        
        private static RelayServerEndpoint GetEndpointForConnectionType(List<RelayServerEndpoint> endpoints, string connectionType)
        {
           foreach (var endpoint in endpoints)
           {
               if (endpoint.ConnectionType == connectionType)
               {
                   return endpoint;
               }
           }

           return null;
        }
        public static RelayServerData HostRelayData(Allocation allocation, string connectionType = UDP)
        {
           // Select endpoint based on desired connectionType
           var endpoint = GetEndpointForConnectionType(allocation.ServerEndpoints, connectionType);
           if (endpoint == null)
           {
               throw new Exception($"endpoint for connectionType {connectionType} not found");
           }

           // Prepare the server endpoint using the Relay server IP and port
           var serverEndpoint = NetworkEndPoint.Parse(endpoint.Host, (ushort) endpoint.Port);

           // UTP uses pointers instead of managed arrays for performance reasons, so we use these helper functions to convert them
           var allocationIdBytes = ConvertFromAllocationIdBytes(allocation.AllocationIdBytes);
           var connectionData = ConvertConnectionData(allocation.ConnectionData);
           var key = ConvertFromHMAC(allocation.Key);

           // Prepare the Relay server data and compute the nonce value
           // The host passes its connectionData twice into this function
           var relayServerData = new RelayServerData(ref serverEndpoint, 0, ref allocationIdBytes, ref connectionData,
               ref connectionData, ref key, connectionType == "dtls");
           relayServerData.ComputeNewNonce();

           return relayServerData;
        }

        public static RelayServerData PlayerRelayData(JoinAllocation allocation, string connectionType = UDP)
        {
           // Select endpoint based on desired connectionType
           var endpoint = GetEndpointForConnectionType(allocation.ServerEndpoints, connectionType);
           if (endpoint == null)
           {
               throw new Exception($"endpoint for connectionType {connectionType} not found");
           }

           // Prepare the server endpoint using the Relay server IP and port
           var serverEndpoint = NetworkEndPoint.Parse(endpoint.Host, (ushort) endpoint.Port);

           // UTP uses pointers instead of managed arrays for performance reasons, so we use these helper functions to convert them
           var allocationIdBytes = ConvertFromAllocationIdBytes(allocation.AllocationIdBytes);
           var connectionData = ConvertConnectionData(allocation.ConnectionData);
           var hostConnectionData = ConvertConnectionData(allocation.HostConnectionData);
           var key = ConvertFromHMAC(allocation.Key);

           // Prepare the Relay server data and compute the nonce values
           // A player joining the host passes its own connectionData as well as the host's
           var relayServerData = new RelayServerData(ref serverEndpoint, 0, ref allocationIdBytes, ref connectionData,
               ref hostConnectionData, ref key, connectionType == "dtls");
           relayServerData.ComputeNewNonce();

           return relayServerData;
        }
        
        public async Task<RelayServerData> SetupRelay()
        {
            InitializationOptions initializationOptions = new InitializationOptions().SetEnvironmentName(environment);

            await UnityServices.InitializeAsync(options: initializationOptions);

            await AuthenticateIfNeeded();

            Allocation allocation = await Relay.Instance.CreateAllocationAsync(maxConnections);
            
            string joinCode = await Relay.Instance.GetJoinCodeAsync(allocation.AllocationId);

            return HostRelayData(allocation);
        }
        
        // Launch this method as a coroutine
        private IEnumerator ServerBindAndListen(RelayServerData relayServerData)
        {
            // Create the NetworkDriver using the Relay server data
            var settings = new NetworkSettings();
            settings.WithRelayParameters(serverData: ref relayServerData);
            
            HostDriver = NetworkDriver.Create(settings);

            // Bind the NetworkDriver to the local endpoint
            if (HostDriver.Bind(NetworkEndPoint.AnyIpv4) != 0)
            {
                Debug.LogError("Server failed to bind");
            }
            else
            {
                // The binding process is an async operation; wait until bound
                while (!HostDriver.Bound)
                {
                    HostDriver.ScheduleUpdate().Complete();
                    yield return null;
                }

                // Once the driver is bound you can start listening for connection requests
                if (HostDriver.Listen() != 0)
                {
                    Debug.LogError("Server failed to listen");
                }
                else
                {
                    isRelayServerConnected = true;
                }
            }
        }
        
        // Launch this method as a coroutine
        private IEnumerator StartClient(string relayJoinCode)
        {
            // Send the join request to the Relay service
            var joinTask = Relay.Instance.JoinAllocationAsync(relayJoinCode);

            while(!joinTask.IsCompleted)
                yield return null;

            if (joinTask.IsFaulted)
            {
                Debug.LogError("Join Relay request failed");
                yield break;
            }

            // Collect and convert the Relay data from the join response
            var allocation = joinTask.Result;

            // Format the server data, based on desired connectionType
            var relayServerData = PlayerRelayData(allocation, "udp");

            yield return ClientBindAndConnect(relayServerData);
        }
        
        private IEnumerator ClientBindAndConnect(RelayServerData relayServerData)
        {
            // Create the NetworkDriver using the Relay server data
            var settings = new NetworkSettings();
            settings.WithRelayParameters(serverData: ref relayServerData);
            PlayerDriver = NetworkDriver.Create(settings);

            // Bind the NetworkDriver to the available local endpoint.
            // This will send the bind request to the Relay server
            if (PlayerDriver.Bind(NetworkEndPoint.AnyIpv4) != 0)
            {
                Debug.LogError("Client failed to bind");
            }
            else
            {
                while (!PlayerDriver.Bound)
                {
                    PlayerDriver.ScheduleUpdate().Complete();
                    yield return null;
                }

                // Once the client is bound to the Relay server, you can send a connection request
                clientConnection = PlayerDriver.Connect(relayServerData.Endpoint);
            }
        }

        private static async Task AuthenticateIfNeeded()
        {
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }
    }
}