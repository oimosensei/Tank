using Cysharp.Runtime.Multicast;
using System.Collections.Concurrent;
using UnityEngine;
using System.Linq;

public class GameContext : IDisposable
{
    public Guid Id { get; }
    public string RoomName { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    // public ConcurrentQueue<ICommand> CommandQueue { get; } = new();
    public IMulticastSyncGroup<Guid, IGameHubReceiver> Group { get; }
    public ConcurrentDictionary<Guid, TankInfo> TankInfos { get; } = new();
    public ConcurrentDictionary<Guid, ShellInfo> ShellInfos { get; } = new();
    public ConcurrentDictionary<Guid, string> Players { get; } = new();

    // MatchingHub用のルーム情報
    public RoomInfo RoomInfo { get; private set; }

    public void AddPlayer(Guid playerId, string connectionId)
    {
        Players[playerId] = connectionId;
    }

    public void RemovePlayer(Guid playerId)
    {
        Players.TryRemove(playerId, out _);
    }

    public Guid[] GetAllPlayerIds() => Players.Keys.ToArray();

    // InitializeRoomInfoメソッドは不要（コンストラクタで必ず作成）

    public bool TryJoinRoom(Guid playerId, string playerName, out PlayerInfo? playerInfo)
    {
        playerInfo = null;

        // 待機中またはプレイ中のルームに参加可能（終了したルームには参加不可）
        if (RoomInfo.Status == RoomStatus.Finished)
            return false;

        if (RoomInfo.Players.Count >= RoomInfo.MaxPlayers)
            return false;

        // プレイヤーが既に参加していないかチェック
        if (RoomInfo.Players.Any(p => p.PlayerId == playerId))
            return false;

        playerInfo = new PlayerInfo
        {
            PlayerId = playerId,
            PlayerName = playerName,
            IsHost = RoomInfo.Players.Count == 0, // 最初のプレイヤーがホスト
            IsReady = RoomInfo.Status == RoomStatus.Playing // プレイ中なら自動でReady状態
        };

        // ホストの場合、HostIdも設定
        if (playerInfo.IsHost)
        {
            RoomInfo.HostId = playerId;
        }

        RoomInfo.Players.Add(playerInfo);
        return true;
    }

    public bool TryLeaveRoom(Guid playerId)
    {

        var player = RoomInfo.Players.FirstOrDefault(p => p.PlayerId == playerId);
        if (player == null)
            return false;

        RoomInfo.Players.RemoveAll(p => p.PlayerId == playerId);

        // ホストが離脱した場合、次のプレイヤーをホストにする
        if (player.IsHost && RoomInfo.Players.Count > 0)
        {
            var newHost = RoomInfo.Players.First();
            newHost.IsHost = true;
            RoomInfo.HostId = newHost.PlayerId;
        }

        return true;
    }

    public bool TryStartGame(Guid hostId)
    {
        if (RoomInfo.HostId != hostId || RoomInfo.Status != RoomStatus.Waiting)
            return false;

        RoomInfo.Status = RoomStatus.Playing;
        return true;
    }

    public bool TrySetReady(Guid playerId, bool isReady)
    {

        var player = RoomInfo.Players.FirstOrDefault(p => p.PlayerId == playerId);
        if (player == null)
            return false;

        player.IsReady = isReady;
        return true;
    }

    public bool IsEmpty => RoomInfo.Players.Count == 0;

    // 通常のゲーム用コンストラクタ（ルーム名必須）
    public GameContext(IMulticastGroupProvider groupProvider, string roomName, int maxPlayers = 4)
    {
        Id = Guid.NewGuid();
        RoomName = roomName;
        Group = groupProvider.GetOrAddSynchronousGroup<Guid, IGameHubReceiver>($"Game/{Id}");

        // RoomInfoを必ず作成
        RoomInfo = new RoomInfo
        {
            RoomId = Id,
            RoomName = roomName,
            MaxPlayers = maxPlayers,
            Status = RoomStatus.Waiting,
            Players = new List<PlayerInfo>()
        };
    }

    // 既存IDを指定するコンストラクタ（特別な用途・デフォルトルーム用）
    public GameContext(IMulticastGroupProvider groupProvider, Guid id, string roomName, int maxPlayers = 4)
    {
        Id = id;
        RoomName = roomName;
        Group = groupProvider.GetOrAddSynchronousGroup<Guid, IGameHubReceiver>($"Game/{Id}");

        // RoomInfoを必ず作成
        RoomInfo = new RoomInfo
        {
            RoomId = id,
            RoomName = roomName,
            MaxPlayers = maxPlayers,
            Status = RoomStatus.Waiting,
            Players = new List<PlayerInfo>()
        };
    }

    public void Dispose()
    {
        Group.Dispose();
    }
}

//GameContextをGuidで複数個管理するクラス
//Singletonで運用
public class GameContextRepository
{
    private readonly ConcurrentDictionary<Guid, GameContext> _contexts = new();

    private readonly IMulticastGroupProvider _groupProvider;

    public GameContextRepository(IMulticastGroupProvider groupProvider)
    {
        _groupProvider = groupProvider;

        // デフォルトのルームをGuid.Emptyで作成
        var context = new GameContext(groupProvider, Guid.Empty, "Default Room");
        // var loopTask = GameLoop.RunLoopAsync(context);
        _contexts[Guid.Empty] = context;
    }

    // MatchingHub用のルーム作成メソッド
    public GameContext CreateRoomWithMatchingInfo(string roomName, int maxPlayers = 4)
    {
        var context = new GameContext(_groupProvider, roomName, maxPlayers);
        // var loopTask = GameLoop.RunLoopAsync(context);
        _contexts[context.Id] = context;
        return context;
    }

    public bool TryGet(Guid id, out GameContext? context)
    {
        if (_contexts.TryGetValue(id, out var context1))
        {
            context = context1;
            return true;
        }

        context = null;
        return false;
    }

    public void Remove(Guid id)
    {
        if (_contexts.Remove(id, out var context))
        {
            context.Dispose();
        }
    }

    public System.Collections.Generic.IEnumerable<GameContext> GetAll()
    {
        return _contexts.Values;
    }

    public IEnumerable<RoomInfo> GetWaitingRooms()
    {
        return _contexts.Values
            .Where(c => c.RoomInfo.Status == RoomStatus.Waiting)
            .Select(c => c.RoomInfo);
    }

    public bool TryGetRoomInfo(Guid roomId, out RoomInfo? roomInfo)
    {
        roomInfo = null;
        if (TryGet(roomId, out var context) && context != null)
        {
            roomInfo = context.RoomInfo;
            return true;
        }
        return false;
    }
}