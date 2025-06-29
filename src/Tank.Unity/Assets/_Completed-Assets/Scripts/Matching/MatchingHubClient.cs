using UnityEngine;
using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using MagicOnion.Client;
using MagicOnion;
using Cysharp.Threading.Tasks;

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

    public async UniTask<RoomInfo> CreateRoom(string roomName)
    {
        if (hubClient != null)
        {
            var roomInfo = await hubClient.CreateRoomAsync(roomName);
            Debug.Log($"Created room: {roomInfo.RoomName} with ID: {roomInfo.RoomId}");
            return roomInfo;
        }
        return null;
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

    public void OnPlayerJoinedRoom(Guid playerId, RoomInfo roomInfo)
    {
        Debug.Log($"[MatchingHubClient] OnPlayerJoinedRoom: Player {playerId} joined room {roomInfo.RoomName}");
    }

    public void OnPlayerLeftRoom(Guid playerId, RoomInfo roomInfo)
    {
        Debug.Log($"[MatchingHubClient] OnPlayerLeftRoom: Player {playerId} left room {roomInfo.RoomName}");
    }

    public void OnGameStarted(Guid gameContextId)
    {
        Debug.Log($"[MatchingHubClient] OnGameStarted: Game started with context {gameContextId}");
    }
}