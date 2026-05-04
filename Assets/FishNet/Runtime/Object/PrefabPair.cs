using UnityEngine;
namespace FishNet.Object
{
    using Managing;
    /// <summary>
    /// Used to pair a regular GameObject with a NetworkObject prefab.
    /// </summary>
    [System.Serializable]
    public class PrefabPair
    {
        [SerializeField]
        GameObject regular;
        public GameObject Regular => regular;

        [SerializeField]
        NetworkObject network;
        public NetworkObject Network => network;

        public GameObject Get()
        {
            NetworkManager networkManager = GetActiveNetworkManager();

            // No NetworkManager at all — offline fallback.
            if (networkManager == null || network == null)
                return regular;

            if (networkManager.IsServerStarted)
                return network.gameObject;
            return regular;

            NetworkManager GetActiveNetworkManager()
            {
                if (NetworkManager.Instances.Count == 0)
                    return null;

                NetworkManager manager = NetworkManager.Instances[0];
                if (manager == null || !manager.isActiveAndEnabled)
                    return null;

                return manager;
            }
        }


    }
}