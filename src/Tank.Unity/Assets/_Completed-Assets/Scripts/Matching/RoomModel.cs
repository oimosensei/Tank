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

        // ルームリスト管理
        private readonly ReactiveCollection<RoomInfo> _rooms = new ReactiveCollection<RoomInfo>();
        public IReadOnlyReactiveCollection<RoomInfo> Rooms => _rooms;

        // 現在参加しているルーム情報
        private readonly ReactiveProperty<RoomInfo> _currentRoom = new ReactiveProperty<RoomInfo>();
        public IReadOnlyReactiveProperty<RoomInfo> CurrentRoom => _currentRoom;

        // 現在のルーム状態（ルームリスト表示 or ジョイン状態表示）
        private readonly ReactiveProperty<bool> _isInRoom = new ReactiveProperty<bool>(false);
        public IReadOnlyReactiveProperty<bool> IsInRoom => _isInRoom;

        private MatchingHubClient _matchingClient;
        private bool _isRefreshingCurrentRoom = false;
        private string _currentPlayerName = string.Empty;

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
            
            // 現在のルーム状態を定期的に更新
            StartCurrentRoomRefreshLoop().Forget();
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
                    
                    // 作成したルームに自動参加
                    await JoinRoom(newRoom.RoomId, "Host");
                }
                return newRoom;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create room: {e.Message}");
                return null;
            }
        }

        // ルームに参加
        public async UniTask<bool> JoinRoom(Guid roomId, string playerName)
        {
            if (_matchingClient == null)
            {
                Debug.LogError("MatchingHubClient is not available");
                return false;
            }

            try
            {
                var joinedRoom = await _matchingClient.JoinRoom(roomId, playerName);
                if (joinedRoom != null)
                {
                    _currentRoom.Value = joinedRoom;
                    _isInRoom.Value = true;
                    _currentPlayerName = playerName; // 現在のプレイヤー名を保存
                    
                    // CurrentRoomInfoに保存
                    CurrentRoomInfo.Instance.RoomInfo = joinedRoom;
                    
                    Debug.Log($"Joined room: {joinedRoom.RoomName}");
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to join room: {e.Message}");
            }
            
            return false;
        }

        // ルームから離脱
        public async UniTask LeaveRoom()
        {
            if (_currentRoom.Value == null || _matchingClient == null)
                return;

            try
            {
                await _matchingClient.LeaveRoom(_currentRoom.Value.RoomId);
                
                _currentRoom.Value = null;
                _isInRoom.Value = false;
                _currentPlayerName = string.Empty;
                CurrentRoomInfo.Instance.RoomInfo = null;
                
                Debug.Log("Left room");
                
                // ルームリストを更新
                await RefreshRoomList();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to leave room: {e.Message}");
            }
        }

        // ゲーム開始
        public async UniTask<bool> StartGame()
        {
            if (_currentRoom.Value == null || _matchingClient == null)
                return false;

            try
            {
                var updatedRoom = await _matchingClient.StartGame(_currentRoom.Value.RoomId);
                if (updatedRoom != null)
                {
                    _currentRoom.Value = updatedRoom;
                    CurrentRoomInfo.Instance.RoomInfo = updatedRoom;
                    
                    Debug.Log($"Game started in room: {updatedRoom.RoomName}");
                    
                    // ゲーム開始と同時にシーン遷移（ホスト用）
                    if (updatedRoom.Status == RoomStatus.Playing)
                    {
                        Debug.Log("Host triggered game start. Transitioning to game scene...");
                        UnityEngine.SceneManagement.SceneManager.LoadScene("_Complete-Game");
                    }
                    
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to start game: {e.Message}");
            }
            
            return false;
        }

        // Ready状態変更
        public async UniTask SetReady(bool isReady)
        {
            if (_currentRoom.Value == null || _matchingClient == null)
                return;

            try
            {
                await _matchingClient.SetReadyStatus(_currentRoom.Value.RoomId, isReady);
                Debug.Log($"Set ready status to: {isReady}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to set ready status: {e.Message}");
            }
        }

        // 現在のルーム情報を更新（リアルタイム更新用）
        public async UniTask RefreshCurrentRoom()
        {
            if (_currentRoom.Value == null || _matchingClient == null || _isRefreshingCurrentRoom)
                return;

            _isRefreshingCurrentRoom = true;
            try
            {
                var updatedRoom = await _matchingClient.GetRoomStatus(_currentRoom.Value.RoomId);
                if (updatedRoom != null)
                {
                    var previousStatus = _currentRoom.Value.Status;
                    _currentRoom.Value = updatedRoom;
                    CurrentRoomInfo.Instance.RoomInfo = updatedRoom;
                    
                    // ゲーム開始を検知してシーン遷移
                    if (previousStatus == RoomStatus.Waiting && updatedRoom.Status == RoomStatus.Playing)
                    {
                        Debug.Log("Game started detected! Transitioning to game scene...");
                        UnityEngine.SceneManagement.SceneManager.LoadScene("_Complete-Game");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to refresh current room: {e.Message}");
            }
            finally
            {
                _isRefreshingCurrentRoom = false;
            }
        }


        // 現在のルーム情報を定期的に更新するループ
        private async UniTask StartCurrentRoomRefreshLoop()
        {
            while (this != null)
            {
                await UniTask.Delay(2000); // 2秒ごとに更新
                
                if (_isInRoom.Value && _currentRoom.Value != null && !_isRefreshingCurrentRoom)
                {
                    await RefreshCurrentRoom();
                }
            }
        }

        // 現在のプレイヤー名を取得するメソッド
        public string GetCurrentPlayerName() => _currentPlayerName;

        void OnDestroy()
        {
            _rooms?.Dispose();
            _currentRoom?.Dispose();
            _isInRoom?.Dispose();
        }
    }
}