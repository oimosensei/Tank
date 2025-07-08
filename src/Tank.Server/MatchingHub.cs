
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using MagicOnion;
using MagicOnion.Server.Hubs;
// using Tank.Shared;

public class MatchingHub : StreamingHubBase<IMatchingHub, IMatchingHubReceiver>, IMatchingHub
{
    private readonly GameContextRepository _gameContextRepository;
    private static readonly ConcurrentDictionary<Guid, Guid> _playerToRoom = new();

    public MatchingHub(GameContextRepository gameContextRepository)
    {
        _gameContextRepository = gameContextRepository;
    }

    protected override ValueTask OnConnected()
    {
        Console.WriteLine($"[MatchingHub] Player connected: {ConnectionId}");
        return default;
    }

    protected override ValueTask OnDisconnected()
    {
        Console.WriteLine($"[MatchingHub] Player disconnected: {ConnectionId}");

        // プレイヤーが参加していたルームから自動退出
        if (_playerToRoom.TryRemove(ConnectionId, out var roomId))
        {
            LeaveRoomInternal(roomId, ConnectionId);
            Console.WriteLine($"[MatchingHub] Player {ConnectionId} automatically left room {roomId} due to disconnection");
        }

        return default;
    }

    public ValueTask<RoomInfo> CreateRoomAsync(string roomName, int maxPlayers = 4)
    {
        var context = _gameContextRepository.CreateRoomWithMatchingInfo(roomName, maxPlayers);

        Console.WriteLine($"[MatchingHub] Room created: {roomName} ({context.Id}) by {ConnectionId}");
        return new ValueTask<RoomInfo>(context.RoomInfo);
    }

    public ValueTask<RoomInfo[]> GetRoomListAsync()
    {
        var rooms = _gameContextRepository.GetAll()
            .Where(c => c.RoomInfo.Status == RoomStatus.Waiting || c.RoomInfo.Status == RoomStatus.Playing)
            .Select(c => c.RoomInfo)
            .ToArray();
        return new ValueTask<RoomInfo[]>(rooms);
    }

    public ValueTask<RoomInfo> JoinRoomAsync(Guid roomId, string playerName)
    {
        if (!_gameContextRepository.TryGet(roomId, out var context))
        {
            throw new InvalidOperationException("Room not found");
        }

        if (!context.TryJoinRoom(ConnectionId, playerName, out var playerInfo))
        {
            throw new InvalidOperationException("Failed to join room (room full, wrong status, or already joined)");
        }

        // プレイヤーとルームの関連付けを記録
        _playerToRoom[ConnectionId] = roomId;

        Console.WriteLine($"[MatchingHub] Player {playerName} ({ConnectionId}) joined room {context.RoomInfo.RoomName}");
        
        // TODO: 他のクライアントにプレイヤー参加を通知
        // Broadcast(context.RoomInfo).OnPlayerJoinedRoom(playerInfo, context.RoomInfo);
        
        return new ValueTask<RoomInfo>(context.RoomInfo);
    }

    public ValueTask LeaveRoomAsync(Guid roomId)
    {
        // プレイヤーとルームの関連付けを削除
        _playerToRoom.TryRemove(ConnectionId, out _);

        LeaveRoomInternal(roomId, ConnectionId);
        return default;
    }

    private void LeaveRoomInternal(Guid roomId, Guid playerId)
    {
        if (!_gameContextRepository.TryGet(roomId, out var context))
        {
            return;
        }

        if (!context.TryLeaveRoom(playerId))
        {
            return;
        }

        // ルームが空になった場合は削除
        // if (context.IsEmpty)
        // {
        //     _gameContextRepository.Remove(roomId);
        //     Console.WriteLine($"[MatchingHub] Room {context.RoomInfo.RoomName} deleted (empty)");
        // }

        Console.WriteLine($"[MatchingHub] Player {playerId} left room {context.RoomInfo.RoomName}");
        
        // TODO: 他のクライアントにプレイヤー退出を通知
        // Broadcast(context.RoomInfo).OnPlayerLeftRoom(playerId, context.RoomInfo);
    }

    public ValueTask<RoomInfo> StartGameAsync(Guid roomId)
    {
        if (!_gameContextRepository.TryGet(roomId, out var context))
        {
            throw new InvalidOperationException("Room not found");
        }

        if (!context.TryStartGame(ConnectionId))
        {
            throw new InvalidOperationException("Only host can start the game or game is not in waiting state");
        }

        Console.WriteLine($"[MatchingHub] Game started in room {context.RoomInfo.RoomName} by host {ConnectionId}");
        
        // TODO: 他のクライアントにゲーム開始を通知
        // Broadcast(context.RoomInfo).OnGameStarted(context.Id, context.RoomInfo);
        
        return new ValueTask<RoomInfo>(context.RoomInfo);
    }

    public ValueTask<RoomInfo> GetRoomInfoAsync(Guid roomId)
    {
        if (_gameContextRepository.TryGetRoomInfo(roomId, out var roomInfo))
        {
            return new ValueTask<RoomInfo>(roomInfo);
        }

        throw new InvalidOperationException("Room not found");
    }

    public ValueTask<RoomInfo> GetRoomStatusAsync(Guid roomId)
    {
        if (_gameContextRepository.TryGetRoomInfo(roomId, out var roomInfo))
        {
            return new ValueTask<RoomInfo>(roomInfo);
        }

        throw new InvalidOperationException("Room not found");
    }

    public ValueTask SetReadyStatusAsync(Guid roomId, bool isReady)
    {
        if (!_gameContextRepository.TryGet(roomId, out var context))
        {
            throw new InvalidOperationException("Room not found");
        }

        if (!context.TrySetReady(ConnectionId, isReady))
        {
            throw new InvalidOperationException("Player not in room");
        }

        Console.WriteLine($"[MatchingHub] Player {ConnectionId} set ready status to {isReady} in room {context.RoomInfo.RoomName}");
        return default;
    }
}
