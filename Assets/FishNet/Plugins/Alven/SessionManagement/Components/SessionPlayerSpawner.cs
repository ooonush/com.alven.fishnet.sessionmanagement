﻿using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Alven.SessionManagement;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using UnityEngine;

namespace DefaultNamespace
{
    [AddComponentMenu("FishNet/Component/SessionPlayerSpawner")]
    public class SessionPlayerSpawner : MonoBehaviour
    {
        #region Public.
        /// <summary>
        /// Called on the server when a player is spawned.
        /// </summary>
        public event Action<NetworkObject> OnSpawned;
        #endregion

        #region Serialized.
        /// <summary>
        /// Prefab to spawn for the session player.
        /// </summary>
        [Tooltip("Prefab to spawn for the session player.")]
        [SerializeField]
        private NetworkPlayerObject _playerPrefab;
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
        public Transform[] Spawns = new Transform[0];
        #endregion

        #region Private.
        /// <summary>
        /// NetworkManager on this object or within this objects parents.
        /// </summary>
        private NetworkManager _networkManager;
        /// <summary>
        /// Next spawns to use.
        /// </summary>
        private int _nextSpawn;
        #endregion

        private readonly Dictionary<SessionPlayer, NetworkPlayerObject> _playerObjects = new Dictionary<SessionPlayer, NetworkPlayerObject>();
        /// <summary>
        /// Spawned Players.
        /// </summary>
        public IReadOnlyDictionary<SessionPlayer, NetworkPlayerObject> SpawnedPlayers => _playerObjects;

        private void Start()
        {
            InitializeOnce();
        }

        private void OnDestroy()
        {
            if (_networkManager != null)
            {
                _networkManager.SceneManager.OnClientLoadedStartScenes -= SceneManager_OnClientLoadedStartScenes;
                ServerSessionManager serverSessionManager = _networkManager.GetServerSessionManager();
                serverSessionManager.OnRemotePlayerConnectionState -= ServerSessionManager_OnRemotePlayerConnectionState;
            }
        }

        private void InitializeOnce()
        {
            _networkManager = InstanceFinder.NetworkManager;
            if (_networkManager == null)
            {
                Debug.LogWarning($"PlayerSpawner on {gameObject.name} cannot work as NetworkManager wasn't found on this object or within parent objects.");
                return;
            }

            _networkManager.SceneManager.OnClientLoadedStartScenes += SceneManager_OnClientLoadedStartScenes;
            ServerSessionManager serverSessionManager = _networkManager.GetServerSessionManager();
            serverSessionManager.OnRemotePlayerConnectionState += ServerSessionManager_OnRemotePlayerConnectionState;
        }

        /// <summary>
        /// Called when a client loads initial scenes after connecting.
        /// </summary>
        private void SceneManager_OnClientLoadedStartScenes(NetworkConnection conn, bool asServer)
        {
            if (!asServer)
                return;
            SessionPlayer player = conn.GetSessionPlayer();
            if (_playerObjects.ContainsKey(player)) // PlayerObject for this session player is already spawned.
            {
                return;
            }

            if (_playerPrefab == null)
            {
                Debug.LogWarning($"Player prefab is empty and cannot be spawned for connection {conn.ClientId}.");
                return;
            }

            Vector3 position;
            Quaternion rotation;
            SetSpawn(_playerPrefab.transform, out position, out rotation);

            NetworkObject nob = _networkManager.GetPooledInstantiated(_playerPrefab.NetworkObject, position, rotation, true);
            var networkPlayerObject = nob.GetComponent<NetworkPlayerObject>();
            _playerObjects.Add(player, networkPlayerObject);
            _networkManager.ServerManager.Spawn(networkPlayerObject, player);

            //If there are no global scenes 
            if (_addToDefaultScene)
                _networkManager.SceneManager.AddOwnerToDefaultScene(nob);

            OnSpawned?.Invoke(nob);
        }

        /// <summary>
        /// Called when a player connection state changed.
        /// </summary>
        private void ServerSessionManager_OnRemotePlayerConnectionState(SessionPlayer player, RemotePlayerConnectionStateArgs args)
        {
            if (args.State == PlayerConnectionState.PermanentlyDisconnected)
            {
                _playerObjects.Remove(player);
            }
        }

        /// <summary>
        /// Sets a spawn position and rotation.
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="pos"></param>
        /// <param name="rot"></param>
        private void SetSpawn(Transform prefab, out Vector3 pos, out Quaternion rot)
        {
            //No spawns specified.
            if (Spawns.Length == 0)
            {
                SetSpawnUsingPrefab(prefab, out pos, out rot);
                return;
            }

            Transform result = Spawns[_nextSpawn];
            if (result == null)
            {
                SetSpawnUsingPrefab(prefab, out pos, out rot);
            }
            else
            {
                pos = result.position;
                rot = result.rotation;
            }

            //Increase next spawn and reset if needed.
            _nextSpawn++;
            if (_nextSpawn >= Spawns.Length)
                _nextSpawn = 0;
        }

        /// <summary>
        /// Sets spawn using values from prefab.
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="pos"></param>
        /// <param name="rot"></param>
        private void SetSpawnUsingPrefab(Transform prefab, out Vector3 pos, out Quaternion rot)
        {
            pos = prefab.position;
            rot = prefab.rotation;
        }
    }
}