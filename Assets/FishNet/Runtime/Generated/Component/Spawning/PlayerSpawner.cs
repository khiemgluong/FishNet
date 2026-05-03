using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using System;
using UnityEngine;
using UnityEngine.Serialization;
using ExtensionKit;
namespace FishNet.Component.Spawning
{
    /// <summary>
    /// Spawns a player object for clients when they connect.
    /// </summary>
    // [AddComponentMenu("FishNet/Component/PlayerSpawner")]
    [Serializable]
    public class PlayerSpawner
    {
        /// <summary>
        /// First instance of the NetworkManager found. This will be either the NetworkManager on or above this object, or InstanceFinder.NetworkManager.
        /// </summary>
        private NetworkManager _networkManager;
        #region Public
        /// <summary>
        /// Called on the server when a player is spawned.
        /// </summary>
        public event Action<NetworkObject> OnSpawned;
        #endregion

        #region Serialized
        /// <summary>
        /// Prefab to spawn for the player.
        /// </summary>
        [Tooltip("Prefab to spawn for the player.")]
        [SerializeField]
        private NetworkObject _playerPrefab;

        /// <summary>
        /// Sets the PlayerPrefab to use.
        /// </summary>
        /// <param name = "nob"></param>
        public void SetPlayerPrefab(NetworkObject nob) => _playerPrefab = nob;

        /// <summary>
        /// True to add player to the active scene when no global scenes are specified through the SceneManager.
        /// </summary>
        [Tooltip("True to add player to the active scene when no global scenes are specified through the SceneManager.")]
        [SerializeField]
        private bool _addToDefaultScene = true;
        /// <summary>
        /// Areas in which players may spawn.
        /// </summary>
        [Tooltip("Areas in which players may spawn.")]
        // public Transform[] Spawns = new Transform[0];
        public Transforms.Placement[] Spawns;
        #endregion


        /// <summary>
        /// Next spawns to use.
        /// </summary>
        private int _nextSpawn;

        public PlayerSpawner(NetworkManager manager, PlayerSpawner other)
        {
            _networkManager = manager;
            _playerPrefab = other._playerPrefab;
            _addToDefaultScene = other._addToDefaultScene;
            Spawns = other.Spawns;
            _networkManager.SceneManager.OnClientLoadedStartScenes += SceneManager_OnClientLoadedStartScenes;
        }

        public void OnDestroy()
        {
            if (_networkManager != null)
                _networkManager.SceneManager.OnClientLoadedStartScenes -= SceneManager_OnClientLoadedStartScenes;
        }

        /// <summary>
        /// Initializes this script for use.
        /// </summary>
        // public void Initialize(NetworkManager networkManager)
        // {
        //     _networkManager = networkManager;

        //     if (_networkManager == null)
        //         _networkManager = InstanceFinder.NetworkManager;

        //     if (_networkManager == null)
        //     {
        //         // _networkManager.LogWarning($"PlayerSpawner on {gameObject.name} cannot work as NetworkManager wasn't found on this object or within parent objects.");
        //         return;
        //     }

        // }

        /// <summary>
        /// Called when a client loads initial scenes after connecting.
        /// </summary>
        void SceneManager_OnClientLoadedStartScenes(NetworkConnection conn, bool asServer)
        {
            if (!asServer)
                return;
            if (_playerPrefab == null)
            {
                _networkManager.LogWarning($"Player prefab is empty and cannot be spawned for connection {conn.ClientId}.");
                return;
            }

            Vector3 position;
            Quaternion rotation;
            SetSpawn(_playerPrefab.transform, out position, out rotation);

            NetworkObject nob = _networkManager.GetPooledInstantiated(_playerPrefab, position, rotation, true);
            _networkManager.ServerManager.Spawn(nob, conn);

            // If there are no global scenes 
            if (_addToDefaultScene)
                _networkManager.SceneManager.AddOwnerToDefaultScene(nob);

            OnSpawned?.Invoke(nob);
        }

        /// <summary>
        /// Sets a spawn position and rotation.
        /// </summary>
        /// <param name = "pos"></param>
        /// <param name = "rot"></param>
        private void SetSpawn(Transform prefab, out Vector3 pos, out Quaternion rot)
        {
            // No spawns specified.
            if (Spawns.Length == 0)
            {
                SetSpawnUsingPrefab(prefab, out pos, out rot);
                return;
            }

            Transforms.Placement result = Spawns[_nextSpawn];
            if (result == null)
            {
                SetSpawnUsingPrefab(prefab, out pos, out rot);
            }
            else
            {
                pos = result.position;
                rot = Quaternion.Euler(0, result.yRotation, 0);
            }

            // Increase next spawn and reset if needed.
            _nextSpawn++;
            if (_nextSpawn >= Spawns.Length)
                _nextSpawn = 0;
        }

        /// <summary>
        /// Sets spawn using values from prefab.
        /// </summary>
        /// <param name = "prefab"></param>
        /// <param name = "pos"></param>
        /// <param name = "rot"></param>
        private void SetSpawnUsingPrefab(Transform prefab, out Vector3 pos, out Quaternion rot)
        {
            pos = prefab.position;
            rot = prefab.rotation;
        }
    }
}