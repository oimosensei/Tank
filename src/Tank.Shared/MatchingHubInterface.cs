using System;
using System.Threading.Tasks;
using MagicOnion;
using MessagePack;

[MessagePackObject]
public class RoomInfo
{
    [Key(0)]
    public Guid RoomId { get; set; }
    
    [Key(1)]
    public string RoomName { get; set; } = string.Empty;
    
    [Key(2)]
    public Guid CreatorId { get; set; }
    
    [Key(3)]
    public int MaxPlayers { get; set; }
    
    [Key(4)]
    public int CurrentPlayers { get; set; }
    
    [Key(5)]
    public bool IsGameStarted { get; set; }
    
    [Key(6)]
    public DateTime CreatedAt { get; set; }
}

public interface IMatchingHub : IStreamingHub<IMatchingHub, IMatchingHubReceiver>
{
    ValueTask<RoomInfo> CreateRoomAsync(string roomName, int maxPlayers);
    ValueTask<RoomInfo[]> GetRoomListAsync();
    ValueTask<RoomInfo> JoinRoomAsync(Guid roomId);
    ValueTask LeaveRoomAsync();
    ValueTask<Guid> StartGameAsync();
}

public interface IMatchingHubReceiver
{
    void OnRoomCreated(RoomInfo roomInfo);
    void OnRoomUpdated(RoomInfo roomInfo);
    void OnRoomDeleted(Guid roomId);
    void OnPlayerJoinedRoom(Guid playerId, RoomInfo roomInfo);
    void OnPlayerLeftRoom(Guid playerId, RoomInfo roomInfo);
    void OnGameStarted(Guid gameContextId);
}