using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UniRx;
using Cysharp.Threading.Tasks;

namespace Nakatani.Matching
{
    public class RoomPresenter : MonoBehaviour
    {
        private const string GAME_SCENE_NAME = "_Complete-Game";

        [SerializeField] private RoomListView roomListViewPrefab;
        [SerializeField] private Transform parentTransform;
        [SerializeField] private TMP_InputField roomNameInputField;
        [SerializeField] private Button createRoomButton;
        
        // 待機室UI
        [SerializeField] private GameObject roomListPanel;
        [SerializeField] private GameObject waitingRoomPanel;
        [SerializeField] private TMP_Text waitingRoomNameText;
        [SerializeField] private RectTransform playerListParent;
        [SerializeField] private TMP_Text playerListItemPrefab;
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button readyButton;
        [SerializeField] private Button leaveRoomButton;
        [SerializeField] private TMP_InputField playerNameInputField;

        private RoomModel _roomModel;
        private List<RoomListView> _currentViews = new List<RoomListView>();
        private List<TMP_Text> _playerListItems = new List<TMP_Text>();

        void Start()
        {
            _roomModel = RoomModel.Instance;

            if (_roomModel != null)
            {
                // ルームリストの変更を監視
                _roomModel.Rooms
                    .ObserveAdd()
                    .Subscribe(_ => RefreshRoomList())
                    .AddTo(this);

                _roomModel.Rooms
                    .ObserveRemove()
                    .Subscribe(_ => RefreshRoomList())
                    .AddTo(this);

                _roomModel.Rooms
                    .ObserveReplace()
                    .Subscribe(_ => RefreshRoomList())
                    .AddTo(this);

                _roomModel.Rooms
                    .ObserveReset()
                    .Subscribe(_ => RefreshRoomList())
                    .AddTo(this);

                // ルーム参加状態の変更を監視
                _roomModel.IsInRoom
                    .Subscribe(isInRoom => {
                        if (roomListPanel != null) roomListPanel.SetActive(!isInRoom);
                        if (waitingRoomPanel != null) waitingRoomPanel.SetActive(isInRoom);
                    })
                    .AddTo(this);

                // 現在のルーム情報の変更を監視
                _roomModel.CurrentRoom
                    .Subscribe(currentRoom => {
                        if (currentRoom != null)
                        {
                            UpdateWaitingRoomUI(currentRoom);
                        }
                    })
                    .AddTo(this);
            }

            // ボタンイベントを設定
            if (createRoomButton != null)
            {
                createRoomButton.onClick.AddListener(OnCreateRoomButtonClicked);
            }
            
            if (startGameButton != null)
            {
                startGameButton.onClick.AddListener(OnStartGameButtonClicked);
            }
            
            if (readyButton != null)
            {
                readyButton.onClick.AddListener(OnReadyButtonClicked);
            }
            
            if (leaveRoomButton != null)
            {
                leaveRoomButton.onClick.AddListener(OnLeaveRoomButtonClicked);
            }

            // 初期状態を設定
            if (roomListPanel != null) roomListPanel.SetActive(true);
            if (waitingRoomPanel != null) waitingRoomPanel.SetActive(false);
        }

        private void RefreshRoomList()
        {
            ClearCurrentViews();
            CreateRoomViews();
        }

        private void ClearCurrentViews()
        {
            foreach (var view in _currentViews)
            {
                if (view != null)
                {
                    Destroy(view.gameObject);
                }
            }
            _currentViews.Clear();
        }

        private void CreateRoomViews()
        {
            if (_roomModel == null || roomListViewPrefab == null || parentTransform == null)
                return;

            foreach (var roomInfo in _roomModel.Rooms)
            {
                var viewInstance = Instantiate(roomListViewPrefab, parentTransform);
                viewInstance.SetText(roomInfo);

                viewInstance.OnJoinClicked
                    .Subscribe(room => OnRoomJoinClicked(room, false))
                    .AddTo(viewInstance);

                viewInstance.OnSpectateClicked
                    .Subscribe(room => OnRoomJoinClicked(room, true))
                    .AddTo(viewInstance);

                _currentViews.Add(viewInstance);
            }
        }

        //観戦で入る場合も同じ関数が呼ばれる
        private async void OnRoomJoinClicked(RoomInfo roomInfo, bool isSpectating)
        {
            Debug.Log($"Room {(isSpectating ? "spectate" : "join")} clicked: {roomInfo.RoomName} ({roomInfo.RoomId})");

            if (!isSpectating)
            {
                // 通常参加の場合、新しいフローでルームに参加（待機室状態に遷移）
                await JoinRoomViaModel(roomInfo);
            }
            else
            {
                // 観戦の場合は従来通り直接ゲームシーンへ
                CurrentRoomInfo.Instance.RoomInfo = roomInfo;
                CurrentRoomInfo.Instance.StartWithSpectating = isSpectating;
                SceneManager.LoadScene(GAME_SCENE_NAME);
            }
        }

        private async UniTask JoinRoomViaModel(RoomInfo roomInfo)
        {
            if (_roomModel == null)
            {
                Debug.LogError("RoomModel is not available");
                return;
            }

            try
            {
                // プレイヤー名を取得（入力フィールドがあれば使用、なければデフォルト値）
                string playerName = GetPlayerName();
                
                bool success = await _roomModel.JoinRoom(roomInfo.RoomId, playerName);
                if (success)
                {
                    Debug.Log($"Successfully joined room: {roomInfo.RoomName}");
                    // RoomModelのIsInRoomプロパティによってUI状態が自動的に切り替わる
                }
                else
                {
                    Debug.LogError("Failed to join room");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to join room: {e.Message}");
            }
        }

        private string GetPlayerName()
        {
            if (playerNameInputField != null && !string.IsNullOrEmpty(playerNameInputField.text))
            {
                return playerNameInputField.text.Trim();
            }
            return "Player_" + System.DateTime.Now.Ticks.ToString().Substring(10);
        }

        private void UpdateWaitingRoomUI(RoomInfo roomInfo)
        {
            if (waitingRoomNameText != null)
            {
                waitingRoomNameText.text = $"Room: {roomInfo.RoomName}";
            }

            UpdatePlayerList(roomInfo);
            UpdateButtonStates(roomInfo);
        }

        private void UpdatePlayerList(RoomInfo roomInfo)
        {
            // 既存のプレイヤーリストアイテムを削除
            foreach (var item in _playerListItems)
            {
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }
            _playerListItems.Clear();

            if (roomInfo.Players == null || playerListParent == null || playerListItemPrefab == null)
                return;

            // 新しいプレイヤーリストを作成
            foreach (var player in roomInfo.Players)
            {
                var playerItem = Instantiate(playerListItemPrefab, playerListParent);
                string displayText = player.PlayerName;
                if (player.IsHost) displayText += " (Host)";
                if (player.IsReady) displayText += " [Ready]";
                
                playerItem.text = displayText;
                _playerListItems.Add(playerItem);
            }
        }

        private void UpdateButtonStates(RoomInfo roomInfo)
        {
            if (roomInfo.Players == null) return;

            // 現在のプレイヤーを取得（プレイヤー名で検索）
            var currentPlayerName = _roomModel.GetCurrentPlayerName();
            var currentPlayer = roomInfo.Players.FirstOrDefault(p => p.PlayerName == currentPlayerName);
            Debug.Log($"Current player: {currentPlayer?.PlayerName}, IsHost: {currentPlayer?.IsHost}, Players count: {roomInfo.Players.Count}");
            
            // スタートボタンはホストのみ表示
            if (startGameButton != null)
            {
                bool isHost = currentPlayer?.IsHost ?? false;
                startGameButton.gameObject.SetActive(isHost);
                startGameButton.interactable = isHost && roomInfo.Status == RoomStatus.Waiting;
            }

            // Readyボタンはホスト以外に表示
            if (readyButton != null)
            {
                bool isHost = currentPlayer?.IsHost ?? false;
                readyButton.gameObject.SetActive(!isHost);
                
                if (!isHost && currentPlayer != null)
                {
                    readyButton.GetComponentInChildren<TMP_Text>().text = currentPlayer.IsReady ? "Not Ready" : "Ready";
                }
            }
        }

        public async UniTask<RoomInfo> CreateRoom(string roomName)
        {
            if (_roomModel == null)
            {
                Debug.LogError("RoomModel is not available");
                return null;
            }

            return await _roomModel.CreateRoom(roomName);
        }

        private async void OnCreateRoomButtonClicked()
        {
            if (roomNameInputField == null)
            {
                Debug.LogError("Room name input field is not assigned");
                return;
            }

            string roomName = roomNameInputField.text.Trim();
            if (string.IsNullOrEmpty(roomName))
            {
                Debug.LogWarning("Room name cannot be empty");
                return;
            }

            createRoomButton.interactable = false;

            try
            {
                var createdRoom = await CreateRoom(roomName);
                if (createdRoom != null)
                {
                    roomNameInputField.text = "";
                    Debug.Log($"Successfully created room: {createdRoom.RoomName}");
                    // RoomModelのCreateRoomで自動的にJoinRoomが呼ばれ、待機室状態に遷移する
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to create room: {e.Message}");
            }
            finally
            {
                createRoomButton.interactable = true;
            }
        }

        private async void OnStartGameButtonClicked()
        {
            if (_roomModel == null) return;

            startGameButton.interactable = false;
            
            try
            {
                bool success = await _roomModel.StartGame();
                if (success)
                {
                    // ゲーム開始成功（シーン遷移はRoomModelで処理）
                    Debug.Log("Game start request sent successfully");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to start game: {e.Message}");
            }
            finally
            {
                startGameButton.interactable = true;
            }
        }

        private async void OnReadyButtonClicked()
        {
            if (_roomModel == null || _roomModel.CurrentRoom.Value == null) return;

            readyButton.interactable = false;
            
            try
            {
                // 現在のプレイヤーを取得してReady状態を切り替え
                var currentPlayerName = _roomModel.GetCurrentPlayerName();
                var currentPlayer = _roomModel.CurrentRoom.Value.Players?.FirstOrDefault(p => p.PlayerName == currentPlayerName);
                bool newReadyStatus = !(currentPlayer?.IsReady ?? false);
                
                await _roomModel.SetReady(newReadyStatus);
                
                // ルーム情報を更新してUIを反映
                await _roomModel.RefreshCurrentRoom();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to set ready status: {e.Message}");
            }
            finally
            {
                readyButton.interactable = true;
            }
        }

        private async void OnLeaveRoomButtonClicked()
        {
            if (_roomModel == null) return;

            leaveRoomButton.interactable = false;
            
            try
            {
                await _roomModel.LeaveRoom();
                // RoomModelのIsInRoomプロパティによってUI状態が自動的に切り替わる
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to leave room: {e.Message}");
            }
            finally
            {
                leaveRoomButton.interactable = true;
            }
        }

    }
}