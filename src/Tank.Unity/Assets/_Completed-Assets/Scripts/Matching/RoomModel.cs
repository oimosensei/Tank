using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Nakatani.Matching
{
    public class RoomModel : MonoBehaviour
    {
        public static RoomModel Instance { get; private set; }

        private readonly ReactiveCollection<RoomInfo> _rooms = new ReactiveCollection<RoomInfo>();
        public IReadOnlyReactiveCollection<RoomInfo> Rooms => _rooms;


        private MatchingHubClient _matchingClient;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                // DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        async void Start()
        {
            _matchingClient = MatchingHubClient.Instance;
            
            // Wait for MatchingHubClient to connect
            while (_matchingClient == null || !_matchingClient.IsConnected)
            {
                await UniTask.Delay(100);
                if (_matchingClient == null)
                    _matchingClient = MatchingHubClient.Instance;
            }
            
            RefreshRoomList().Forget();
        }

        public async UniTask RefreshRoomList()
        {
            if (_matchingClient == null)
            {
                Debug.LogError("MatchingHubClient is not available");
                return;
            }

            try
            {
                var rooms = await _matchingClient.GetRoomList();

                _rooms.Clear();
                foreach (var room in rooms)
                {
                    if (room.RoomName == null)
                    {
                        room.RoomName = "Unknown";
                    }
                    _rooms.Add(room);
                }

                Debug.Log($"Refreshed room list: {rooms.Length} rooms");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to refresh room list: {e.Message}");
            }
        }

        public async UniTask<RoomInfo> CreateRoom(string roomName)
        {
            if (_matchingClient == null)
            {
                Debug.LogError("MatchingHubClient is not available");
                return null;
            }

            try
            {
                var newRoom = await _matchingClient.CreateRoom(roomName);
                if (newRoom != null)
                {
                    if (!_rooms.Any(r => r.RoomId == newRoom.RoomId))
                    {
                        _rooms.Add(newRoom);
                    }
                    Debug.Log($"Created room: {newRoom.RoomName}");
                }
                return newRoom;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create room: {e.Message}");
                return null;
            }
        }


        void OnDestroy()
        {
            _rooms?.Dispose();
        }
    }
}