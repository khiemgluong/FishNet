using UnityEngine;
namespace FishNet.Managing
{
    using Component.Spawning;
    public sealed partial class NetworkManager : MonoBehaviour
    {
        /// <summary>
        /// PlayerSpawner for this NetworkManager.
        /// </summary>
        public PlayerSpawner _spawner;
    }
}