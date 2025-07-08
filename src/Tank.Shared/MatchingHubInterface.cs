using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MagicOnion;
using MessagePack;

[MessagePackObject]
public class PlayerInfo
{
    [Key(0)]
    public Guid PlayerId { get; set; }

    [Key(1)]
    public string PlayerName { get; set; } = string.Empty;

    [Key(2)]
    public bool IsHost { get; set; }

    [Key(3)]
    public bool IsReady { get; set; }
}

public enum RoomStatus
{
    Waiting = 0,
    Playing = 1,
    Finished = 2
}

[MessagePackObject]
public class RoomInfo
{
    [Key(0)]
    public Guid RoomId { get; set; }

    [Key(1)]
    public string RoomName { get; set; } = string.Empty;

    [Key(2)]
    public List<PlayerInfo> Players { get; set; } = new List<PlayerInfo>();

    [Key(3)]
    public RoomStatus Status { get; set; } = RoomStatus.Waiting;

    [Key(4)]
    public Guid HostId { get; set; }

    [Key(5)]
    public int MaxPlayers { get; set; } = 4;
}

public interface IMatchingHub : IStreamingHub<IMatchingHub, IMatchingHubReceiver>
{
    ValueTask<RoomInfo> CreateRoomAsync(string roomName, int maxPlayers = 4);
    ValueTask<RoomInfo[]> GetRoomListAsync();
    ValueTask<RoomInfo> JoinRoomAsync(Guid roomId, string playerName);
    ValueTask LeaveRoomAsync(Guid roomId);
    ValueTask<RoomInfo> StartGameAsync(Guid roomId);
    ValueTask<RoomInfo> GetRoomInfoAsync(Guid roomId);
    ValueTask<RoomInfo> GetRoomStatusAsync(Guid roomId);
    ValueTask SetReadyStatusAsync(Guid roomId, bool isReady);
}

public interface IMatchingHubReceiver
{
    void OnRoomCreated(RoomInfo roomInfo);
    void OnRoomUpdated(RoomInfo roomInfo);
    void OnRoomDeleted(Guid roomId);
    void OnPlayerJoinedRoom(PlayerInfo playerInfo, RoomInfo roomInfo);
    void OnPlayerLeftRoom(Guid playerId, RoomInfo roomInfo);
    void OnPlayerReadyChanged(Guid playerId, bool isReady);
    void OnGameStarted(Guid gameContextId, RoomInfo roomInfo);
    void OnGameEnded(Guid roomId);
}