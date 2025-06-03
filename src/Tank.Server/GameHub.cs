using MagicOnion;
using MagicOnion.Server.Hubs;

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
            Console.WriteLine($"Connected: {Context.ContextId}");
        }
        return default;
    }

    protected override ValueTask OnDisconnected()
    {
        gameContext?.Group.Remove(this.ConnectionId);
        //ログを出す
        Console.WriteLine($"Disconnected: {Context.ContextId}");
        return default;
    }

    public ValueTask AttackAsync(Guid targetId)
    {
        // if (gameContextRepository.TryGet(Context.ContextId, out var context))
        // {
        //     context.CommandQueue.Enqueue(new AttackCommand(Context.ContextId, targetId));
        // }
        return default;
    }
    public ValueTask MoveAsync(int x, int y)
    {
        // if (gameContextRepository.TryGet(Context.ContextId, out var context))
        // {
        //     context.CommandQueue.Enqueue(new MoveCommand(Context.ContextId, x, y));
        // }
        return default;
    }
}