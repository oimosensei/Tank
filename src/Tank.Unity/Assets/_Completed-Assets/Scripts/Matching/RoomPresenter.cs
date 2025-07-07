using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UniRx;
using Cysharp.Threading.Tasks;

namespace Nakatani.Matching
{
    public class RoomPresenter : MonoBehaviour
    {
        private const string GAME_SCENE_NAME = "_Complete-Game";

        [SerializeField] private RoomListView roomListViewPrefab;
        [SerializeField] private Transform parentTransform;
        [SerializeField] private InputField roomNameInputField;
        [SerializeField] private Button createRoomButton;

        private RoomModel _roomModel;
        private List<RoomListView> _currentViews = new List<RoomListView>();

        void Start()
        {
            _roomModel = RoomModel.Instance;

            if (_roomModel != null)
            {
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
            }

            if (createRoomButton != null)
            {
                createRoomButton.onClick.AddListener(OnCreateRoomButtonClicked);
            }
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
                // 通常参加の場合、MatchingHubを通じてルームに参加
                await JoinRoomViaMatchingHub(roomInfo);
            }
            else
            {
                // 観戦の場合は従来通り直接ゲームシーンへ
                CurrentRoomInfo.Instance.RoomInfo = roomInfo;
                CurrentRoomInfo.Instance.StartWithSpectating = isSpectating;
                SceneManager.LoadScene(GAME_SCENE_NAME);
            }
        }

        private async UniTask JoinRoomViaMatchingHub(RoomInfo roomInfo)
        {
            var matchingClient = MatchingHubClient.Instance;
            if (matchingClient == null)
            {
                Debug.LogError("MatchingHubClient is not available");
                return;
            }

            try
            {
                // プレイヤー名を入力する簡易的な方法（実際のUIでは専用の入力フィールドを作成）
                string playerName = "Player_" + System.DateTime.Now.Ticks.ToString().Substring(10);
                
                var joinedRoom = await matchingClient.JoinRoom(roomInfo.RoomId, playerName);
                if (joinedRoom != null)
                {
                    Debug.Log($"Successfully joined room: {joinedRoom.RoomName}");
                    
                    // 待機室シーンに遷移（現在はゲームシーンに直接遷移）
                    CurrentRoomInfo.Instance.RoomInfo = joinedRoom;
                    CurrentRoomInfo.Instance.StartWithSpectating = false;
                    SceneManager.LoadScene(GAME_SCENE_NAME);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to join room: {e.Message}");
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

    }
}