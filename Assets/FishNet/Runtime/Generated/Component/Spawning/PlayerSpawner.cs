using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using System;
using System.Collections.Generic;
using UnityEngine;
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
        /// True to auto-spawn when a client has finished loading start scenes.
        /// </summary>
        [Tooltip("True to auto-spawn when a client has finished loading start scenes.")]
        [SerializeField]
        private bool _spawnOnClientLoadedStartScenes = true;

        /// <summary>
        /// Sets if automatic spawning on start-scene load should be used.
        /// </summary>
        /// <param name = "value"></param>
        public void SetSpawnOnClientLoadedStartScenes(bool value) => _spawnOnClientLoadedStartScenes = value;

        /// <summary>
        /// Areas in which players may spawn.
        /// </summary>
        [Tooltip("Areas in which players may spawn.")]
        // public Transform[] Spawns = new Transform[0];
        public Placement[] Spawns;
        #endregion


        /// <summary>
        /// Next spawns to use.
        /// </summary>
        private int _nextSpawn;

        /// <summary>
        /// Connections waiting to spawn once their start scenes are loaded.
        /// Key is the connection; value is an optional Placement override (may be null).
        /// </summary>
        readonly Dictionary<NetworkConnection, Placement> _pendingSpawns = new();

        public PlayerSpawner(NetworkManager manager, PlayerSpawner other)
        {
            _networkManager = manager;
            _playerPrefab = other._playerPrefab;
            _addToDefaultScene = other._addToDefaultScene;
            _spawnOnClientLoadedStartScenes = other._spawnOnClientLoadedStartScenes;
            Spawns = other.Spawns;
            _networkManager.SceneManager.OnClientLoadedStartScenes += SceneManager_OnClientLoadedStartScenes;
        }

        public void OnDestroy()
        {
            if (_networkManager != null)
                _networkManager.SceneManager.OnClientLoadedStartScenes -= SceneManager_OnClientLoadedStartScenes;
            _pendingSpawns.Clear();
        }

        void SceneManager_OnClientLoadedStartScenes(NetworkConnection conn, bool asServer)
        {
            if (!asServer)
                return;

            // Drain any pending manual spawn queued before scenes were ready.
            if (_pendingSpawns.TryGetValue(conn, out Placement pendingPlacement))
            {
                _pendingSpawns.Remove(conn);
                SpawnNow(conn, pendingPlacement);
                return;
            }

            if (!_spawnOnClientLoadedStartScenes)
                return;

            SpawnNow(conn, null);
        }

        /// <summary>
        /// Spawns a player object for a connection.
        /// If the connection has not yet finished loading start scenes the spawn is
        /// deferred and will execute automatically once that condition is met.
        /// </summary>
        /// <param name = "placement">Optional placement override. If null, configured spawn locations are used.</param>
        /// <returns>The spawned NetworkObject if spawn happened immediately, otherwise null (deferred).</returns>

        public NetworkObject SpawnPlayer(NetworkConnection conn, Placement placement = null)
        {
            if (conn == null)
            {
                _networkManager.LogWarning("Connection is null and player cannot be spawned.");
                return null;
            }

            // If start scenes are already loaded for this connection, spawn immediately.
            if (conn.LoadedStartScenes(true))
                return SpawnNow(conn, placement);

            // Otherwise defer until OnClientLoadedStartScenes fires.
            _pendingSpawns[conn] = placement;
            return null;
        }


        public NetworkObject Spawn(NetworkConnection conn, Placement placement = null)
        {
            if (conn == null)
            {
                _networkManager.LogWarning("Connection is null and player cannot be spawned.");
                return null;
            }

            // If start scenes are already loaded for this connection, spawn immediately.
            if (conn.LoadedStartScenes(true))
                return SpawnNow(conn, placement);

            // Otherwise defer until OnClientLoadedStartScenes fires.
            _pendingSpawns[conn] = placement;
            return null;
        }


        /// <summary>
        /// Spawns a specific NetworkObject prefab at a placement with an optional owner.
        /// Spawns immediately — no deferral. Use only when scenes are already loaded.
        /// </summary>
        /// <param name="prefab">Prefab to instantiate and spawn.</param>
        /// <param name="placement">Placement to use for position and rotation.</param>
        /// <param name="owner">Optional owner connection. Null means no owner.</param>
        public NetworkObject Spawn(NetworkObject prefab, Placement placement, NetworkConnection owner = null)
        {
            if (prefab == null)
            {
                _networkManager.LogWarning("Prefab is null and cannot be spawned.");
                return null;
            }

            Vector3 position;
            Quaternion rotation;

            if (placement == null)
            {
                position = prefab.transform.position;
                rotation = prefab.transform.rotation;
            }
            else
            {
                position = placement.position;
                rotation = Quaternion.Euler(0f, placement.yRotation, 0f);
            }

            NetworkObject nob = _networkManager.GetPooledInstantiated(prefab, position, rotation, true);
            _networkManager.ServerManager.Spawn(nob, owner);

            if (_addToDefaultScene)
                _networkManager.SceneManager.AddOwnerToDefaultScene(nob);

            OnSpawned?.Invoke(nob);
            return nob;
        }

        /// <summary>
        /// Immediately spawns a player object without any deferral check.
        /// Only call this once start scenes are confirmed loaded for the connection.
        /// </summary>
        private NetworkObject SpawnNow(NetworkConnection conn, Placement placement)
        {
            if (_playerPrefab == null)
            {
                _networkManager.LogWarning($"Player prefab is empty and cannot be spawned for connection {conn.ClientId}.");
                return null;
            }

            Vector3 position;
            Quaternion rotation;

            if (placement == null)
            {
                SetSpawn(_playerPrefab.transform, out position, out rotation);
            }
            else
            {
                position = placement.position;
                rotation = Quaternion.Euler(0f, placement.yRotation, 0f);
            }

            NetworkObject nob = _networkManager.GetPooledInstantiated(_playerPrefab, position, rotation, true);
            _networkManager.ServerManager.Spawn(nob, conn);

            // If there are no global scenes.
            if (_addToDefaultScene)
                _networkManager.SceneManager.AddOwnerToDefaultScene(nob);

            OnSpawned?.Invoke(nob);
            return nob;
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

            Placement result = Spawns[_nextSpawn];
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