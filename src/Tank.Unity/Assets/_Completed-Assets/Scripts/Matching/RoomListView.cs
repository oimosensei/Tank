using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UniRx;
using System;

namespace Nakatani.Matching
{
    public class RoomListView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI roomNameText;
        [SerializeField] private Button joinButton;
        [SerializeField] private Button spectateButton;

        private RoomInfo _currentRoomInfo;

        public IObservable<RoomInfo> OnJoinClicked => joinButton.OnClickAsObservable()
            .Where(_ => _currentRoomInfo != null)
            .Select(_ => _currentRoomInfo);

        public IObservable<RoomInfo> OnSpectateClicked => spectateButton.OnClickAsObservable()
            .Where(_ => _currentRoomInfo != null)
            .Select(_ => _currentRoomInfo);

        public void SetText(RoomInfo roomInfo)
        {
            _currentRoomInfo = roomInfo;

            if (roomNameText != null && roomInfo != null && roomInfo.RoomName != null)
            {
                roomNameText.text = $"{roomInfo.RoomName} ({roomInfo.RoomId})";
            }
        }

    }
}