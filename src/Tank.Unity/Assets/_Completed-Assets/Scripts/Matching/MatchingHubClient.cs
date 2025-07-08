using UnityEngine;
using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using MagicOnion.Client;
using MagicOnion;
using Cysharp.Threading.Tasks;
using Nakatani.Matching;

public class MatchingHubClient : MonoBehaviour, IMatchingHubReceiver
{
    private GrpcChannelx channel;
    private IMatchingHub hubClient;

    public bool IsConnected => hubClient != null;
    public static MatchingHubClient Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    async UniTaskVoid Start()
    {
        channel = GrpcChannelx.ForAddress("http://localhost:5127");
        hubClient = await StreamingHubClient.ConnectAsync<IMatchingHub, IMatchingHubReceiver>(
            channel, this);

        Debug.Log("Connected to MatchingHub server");
    }

    public async UniTask<RoomInfo> CreateRoom(string roomName, int maxPlayers = 4)
    {
        if (hubClient != null)
        {
            var roomInfo = await hubClient.CreateRoomAsync(roomName, maxPlayers);
            Debug.Log($"Created room: {roomInfo.RoomName} with ID: {roomInfo.RoomId}");
            return roomInfo;
        }
        return null;
    }

    public async UniTask<RoomInfo> JoinRoom(Guid roomId, string playerName)
    {
        if (hubClient != null)
        {
            var roomInfo = await hubClient.JoinRoomAsync(roomId, playerName);
            Debug.Log($"Joined room: {roomInfo.RoomName} as {playerName}");
            return roomInfo;
        }
        return null;
    }

    public async UniTask LeaveRoom(Guid roomId)
    {
        if (hubClient != null)
        {
            await hubClient.LeaveRoomAsync(roomId);
            Debug.Log($"Left room: {roomId}");
        }
    }

    public async UniTask<RoomInfo> StartGame(Guid roomId)
    {
        if (hubClient != null)
        {
            var roomInfo = await hubClient.StartGameAsync(roomId);
            Debug.Log($"Started game in room: {roomInfo.RoomName}");
            return roomInfo;
        }
        return null;
    }

    public async UniTask<RoomInfo> GetRoomStatus(Guid roomId)
    {
        if (hubClient != null)
        {
            var roomInfo = await hubClient.GetRoomStatusAsync(roomId);
            return roomInfo;
        }
        return null;
    }

    public async UniTask SetReadyStatus(Guid roomId, bool isReady)
    {
        if (hubClient != null)
        {
            await hubClient.SetReadyStatusAsync(roomId, isReady);
            Debug.Log($"Set ready status to {isReady} in room {roomId}");
        }
    }

    public async UniTask<RoomInfo[]> GetRoomList()
    {
        if (hubClient != null)
        {
            var rooms = await hubClient.GetRoomListAsync();
            Debug.Log($"Retrieved {rooms.Length} rooms from server");
            return rooms;
        }
        return new RoomInfo[0];
    }

    private async void OnDestroy()
    {
        if (hubClient != null)
        {
            await hubClient.DisposeAsync();
        }
        if (channel != null)
        {
            await channel.ShutdownAsync();
        }
    }

    public void OnRoomCreated(RoomInfo roomInfo)
    {
        Debug.Log($"[MatchingHubClient] OnRoomCreated: Room {roomInfo.RoomName} ({roomInfo.RoomId})");
    }

    public void OnRoomUpdated(RoomInfo roomInfo)
    {
        Debug.Log($"[MatchingHubClient] OnRoomUpdated: Room {roomInfo.RoomName} ({roomInfo.RoomId})");
    }

    public void OnRoomDeleted(Guid roomId)
    {
        Debug.Log($"[MatchingHubClient] OnRoomDeleted: Room {roomId}");
    }

    public void OnPlayerJoinedRoom(PlayerInfo playerInfo, RoomInfo roomInfo)
    {
        Debug.Log($"[MatchingHubClient] OnPlayerJoinedRoom: Player {playerInfo.PlayerName} ({playerInfo.PlayerId}) joined room {roomInfo.RoomName}");

        // RoomModelの現在のルーム情報を更新
        var roomModel = RoomModel.Instance;
        if (roomModel != null && roomModel.CurrentRoom.Value != null && roomModel.CurrentRoom.Value.RoomId == roomInfo.RoomId)
        {
            // 現在のルーム情報を最新の状態に更新
            roomModel.RefreshCurrentRoom().Forget();
        }
    }

    public void OnPlayerLeftRoom(Guid playerId, RoomInfo roomInfo)
    {
        Debug.Log($"[MatchingHubClient] OnPlayerLeftRoom: Player {playerId} left room {roomInfo.RoomName}");

        // RoomModelの現在のルーム情報を更新
        var roomModel = RoomModel.Instance;
        if (roomModel != null && roomModel.CurrentRoom.Value != null && roomModel.CurrentRoom.Value.RoomId == roomInfo.RoomId)
        {
            // 現在のルーム情報を最新の状態に更新
            roomModel.RefreshCurrentRoom().Forget();
        }
    }

    public void OnPlayerReadyChanged(Guid playerId, bool isReady)
    {
        Debug.Log($"[MatchingHubClient] OnPlayerReadyChanged: Player {playerId} set ready to {isReady}");
    }

    public void OnGameStarted(Guid gameContextId, RoomInfo roomInfo)
    {
        Debug.Log($"[MatchingHubClient] OnGameStarted: Game started in room {roomInfo.RoomName} with context {gameContextId}");

        // ゲームシーンに遷移
        CurrentRoomInfo.Instance.RoomInfo = roomInfo;
        CurrentRoomInfo.Instance.StartWithSpectating = false;
        UnityEngine.SceneManagement.SceneManager.LoadScene("_Complete-Game");
    }

    public void OnGameEnded(Guid roomId)
    {
        Debug.Log($"[MatchingHubClient] OnGameEnded: Game ended in room {roomId}");
    }
}