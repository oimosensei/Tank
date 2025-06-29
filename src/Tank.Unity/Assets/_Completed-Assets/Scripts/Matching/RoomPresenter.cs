using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UniRx;

namespace Nakatani.Matching
{
    public class RoomPresenter : MonoBehaviour
    {
        private const string GAME_SCENE_NAME = "_Complete-Game";

        [SerializeField] private RoomListView roomListViewPrefab;
        [SerializeField] private Transform parentTransform;

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
                    .Subscribe(room => OnRoomJoinClicked(room))
                    .AddTo(viewInstance);

                _currentViews.Add(viewInstance);
            }
        }

        private void OnRoomJoinClicked(RoomInfo roomInfo)
        {
            Debug.Log($"Room join clicked: {roomInfo.RoomName} ({roomInfo.RoomId})");

            CurrentRoomInfo.Instance.RoomInfo = roomInfo;
            SceneManager.LoadScene(GAME_SCENE_NAME);
        }
    }
}