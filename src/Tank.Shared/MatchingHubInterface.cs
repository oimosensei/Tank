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

}

public interface IMatchingHub : IStreamingHub<IMatchingHub, IMatchingHubReceiver>
{
    ValueTask<RoomInfo> CreateRoomAsync(string roomName);
    ValueTask<RoomInfo[]> GetRoomListAsync();
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