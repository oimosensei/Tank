using System;
using System.Threading.Tasks;
using MagicOnion;

public interface IGameHub : IStreamingHub<IGameHub, IGameHubReceiver>
{
    ValueTask AttackAsync(Guid targetId);
    ValueTask MoveAsync(int x, int y);
}

public interface IGameHubReceiver
{
    void OnAttack(Guid playerId, Guid targetId);
    void OnMove(Guid playerId, int x, int y);
}