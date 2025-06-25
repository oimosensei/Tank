
using System;
using System.Linq;
using System.Threading.Tasks;
using MagicOnion;
using MagicOnion.Server.Hubs;
// using Tank.Shared;

public class MatchingHub : StreamingHubBase<IMatchingHub, IMatchingHubReceiver>, IMatchingHub
{
    private readonly GameContextRepository _gameContextRepository;

    public MatchingHub(GameContextRepository gameContextRepository)
    {
        _gameContextRepository = gameContextRepository;
    }

    public ValueTask<RoomInfo> CreateRoomAsync(string roomName)
    {
        var context = _gameContextRepository.CreateAndRun();
        context.RoomName = roomName;
        var roomInfo = new RoomInfo
        {
            RoomId = context.Id,
            RoomName = roomName,
        };
        return new ValueTask<RoomInfo>(roomInfo);
    }

    public ValueTask<RoomInfo[]> GetRoomListAsync()
    {
        var rooms = _gameContextRepository.GetAll().Select(context => new RoomInfo
        {
            RoomId = context.Id,
            RoomName = context.RoomName,
        }).ToArray();
        return new ValueTask<RoomInfo[]>(rooms);
    }
}
