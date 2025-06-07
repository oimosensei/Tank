using MagicOnion;
using MagicOnion.Server.Hubs;
using UnityEngine;
using System.Linq;

public class GameHub(GameContextRepository gameContextRepository) : StreamingHubBase<IGameHub, IGameHubReceiver>, IGameHub
{
    private GameContext? gameContext;
    protected override ValueTask OnConnected()
    {
        if (gameContextRepository.TryGet(Guid.Empty, out var context))
        {
            gameContext = context;
            context.Group.Add(this.ConnectionId, Client);
            //ログ
            Console.WriteLine($"Connected: {this.ConnectionId}");
        }
        return default;
    }


    protected override ValueTask OnDisconnected()
    {
        gameContext?.Group.Remove(this.ConnectionId);
        gameContext?.TankInfos.TryRemove(this.ConnectionId, out _);
        gameContext?.Group.All.OnPlayerLeft(this.ConnectionId);
        //ログを出す
        Console.WriteLine($"Disconnected: {this.ConnectionId}");
        return default;
    }

    public ValueTask<(TankInfo[] existingTanks, Guid connectionId)> JoinAndSpawnAsync(Vector3 spawnPosition)
    {
        var tankInfo = new TankInfo { Id = this.ConnectionId, Position = spawnPosition, Rotation = Quaternion.identity };
        gameContext?.TankInfos.TryAdd(this.ConnectionId, tankInfo);

        var existingTanks = gameContext?.TankInfos.Values.Where(t => t.Id != this.ConnectionId).ToArray() ?? [];

        gameContext?.Group.Except([this.ConnectionId]).OnPlayerJoined(this.ConnectionId, spawnPosition, false);
        gameContext?.Group.Single(this.ConnectionId).OnPlayerJoined(this.ConnectionId, spawnPosition, true);

        return ValueTask.FromResult((existingTanks, this.ConnectionId));
    }

    public ValueTask AttackAsync(Guid targetId)
    {
        // if (gameContextRepository.TryGet(Context.ContextId, out var context))
        // {
        //     context.CommandQueue.Enqueue(new AttackCommand(Context.ContextId, targetId));
        // }
        gameContext?.Group.All.OnAttack(this.ConnectionId, targetId);
        return default;
    }
    public ValueTask MoveAsync(Guid playerId, Vector3 position, Quaternion rotation)
    {
        // if (gameContextRepository.TryGet(Context.ContextId, out var context))
        // {
        //     context.CommandQueue.Enqueue(new MoveCommand(Context.ContextId, position));
        // }
        if (gameContext?.TankInfos.TryGetValue(playerId, out var tankInfo) == true)
        {
            tankInfo.Position = position;
            tankInfo.Rotation = rotation;
        }
        gameContext?.Group.Except([ConnectionId]).OnMove(playerId, position, rotation);
        return default;
    }
}