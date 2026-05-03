using System;
using ExtensionKit;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;

namespace FishNet.Component.Spawning
{

    public class PlayerSpawner : MonoBehaviour
    {
        [SerializeField]
        private GameObject regularPlayer;
        [SerializeField]
        private NetworkObject networkPlayer;
        
        public PrefabPair playerPrefabs;
        public Placement placement;

        // Pending network spawn requested while client was still connecting.

        void Start()
        {
            NetworkManager networkManager = GetActiveNetworkManager();
            if (networkManager == null)
            {
                Debug.LogWarning("No active NetworkManager found. Spawning regular player.");
                // SpawnRegularPlayer();
                return;
            }
            networkManager.SceneManager.OnClientLoadedStartScenes += OnClientLoadedStartScenes;
            networkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
        }

        void Update()
        {
        }

        [ContextMenu("Spawn Player")]
        private void SpawnPlayer()
        {
            NetworkManager networkManager = GetActiveNetworkManager();

            // No NetworkManager at all — offline fallback.
            if (networkManager == null)
            {
                SpawnRegularPlayer();
                return;
            }

            if (networkPlayer == null)
            {
                Debug.LogWarning("Network player prefab is not assigned.");
                SpawnRegularPlayer();
                return;
            }

            if (networkManager.IsServerStarted)
            {
                // This instance is the server or host.
                // If its own client is already connected the spawn happens immediately via
                // SpawnNetworkPlayer; otherwise start the client and let OnClientLoadedStartScenes fire.
                if (networkManager.IsClientStarted)
                    SpawnNetworkPlayer(networkManager);
                else
                    networkManager.ClientManager.StartConnection();
            }
            else
            {
                // Pure client (e.g. Player 2 in MPPM).
                // Just connect — the server's OnClientLoadedStartScenes will spawn for this connection.
                if (!networkManager.IsClientStarted)
                    networkManager.ClientManager.StartConnection();
            }
        }

        private void OnClientConnectionState(ClientConnectionStateArgs args)
        {
            Debug.LogError("Client connection state changed: " + args.ConnectionState);
            // if (!_pendingNetworkSpawn) return;

        }

        void OnClientLoadedStartScenes(NetworkConnection connection, bool asServer)
        {
            Debug.LogError("Client loaded start scenes for connection " + connection.ClientId + ", asServer: " + asServer);
            if (!asServer) return;
            NetworkManager networkManager = GetActiveNetworkManager();
            SpawnNetworkPlayer(networkManager, connection);
        }

        private void SpawnNetworkPlayer(NetworkManager networkManager, NetworkConnection connection)
        {
            NetworkObject nob = networkManager.GetPooledInstantiated(networkPlayer, placement.position, placement.Rotation, true);
            networkManager.ServerManager.Spawn(nob, connection);
            networkManager.SceneManager.AddOwnerToDefaultScene(nob);
        }

        private void SpawnNetworkPlayer(NetworkManager networkManager)
        {
            NetworkConnection connection = null;
            foreach (var kvp in networkManager.ServerManager.Clients)
            {
                connection = kvp.Value;
                break;
            }
            SpawnNetworkPlayer(networkManager, connection);
            // networkManager._spawner.Spawn(networkPlayer, null, owner);
            // NetworkObject nob = networkManager.GetPooledInstantiated(networkPlayer, Vector3.zero, Quaternion.identity, true);
            // networkManager.ServerManager.Spawn(nob, connection);
        }

        private NetworkManager GetActiveNetworkManager()
        {
            if (NetworkManager.Instances.Count == 0)
                return null;

            NetworkManager manager = NetworkManager.Instances[0];
            if (manager == null || !manager.isActiveAndEnabled)
                return null;

            return manager;
        }

        private void SpawnRegularPlayer()
        {
            if (regularPlayer == null)
            {
                Debug.LogWarning("Regular player prefab is not assigned.");
                return;
            }

            Instantiate(regularPlayer, placement.position, placement.Rotation);
        }
    }
}