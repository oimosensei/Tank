using Cysharp.Runtime.Multicast;
using System.Collections.Concurrent;
using UnityEngine;

public class GameContext : IDisposable
{
    public Guid Id { get; }
    public bool IsCompleted { get; set; }
    // public ConcurrentQueue<ICommand> CommandQueue { get; } = new();
    public IMulticastSyncGroup<Guid, IGameHubReceiver> Group { get; }
    public ConcurrentDictionary<Guid, TankInfo> TankInfos { get; } = new();
    public ConcurrentDictionary<Guid, ShellInfo> ShellInfos { get; } = new();

    public GameContext(IMulticastGroupProvider groupProvider)
    {
        Id = Guid.NewGuid();
        Group = groupProvider.GetOrAddSynchronousGroup<Guid, IGameHubReceiver>($"Game/{Id}");
    }

    public void Dispose()
    {
        Group.Dispose();
    }
}

public class GameContextRepository
{
    private readonly ConcurrentDictionary<Guid, GameContext> _contexts = new();

    private readonly IMulticastGroupProvider _groupProvider;

    public GameContextRepository(IMulticastGroupProvider groupProvider)
    {
        _groupProvider = groupProvider;
        var context = new GameContext(groupProvider);
        // var loopTask = GameLoop.RunLoopAsync(context);
        _contexts[Guid.Empty] = context;
    }

    public GameContext CreateAndRun()
    {
        var context = new GameContext(_groupProvider);
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
}