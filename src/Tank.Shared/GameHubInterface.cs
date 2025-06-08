using System;
using System.Threading.Tasks;
using MagicOnion;
using UnityEngine;
using MessagePack;

[MessagePackObject]
public class TankInfo
{
    [Key(0)]
    public Guid Id { get; set; }
    
    [Key(1)]
    public Vector3 Position { get; set; }
    
    [Key(2)]
    public Quaternion Rotation { get; set; }
}

[MessagePackObject]
public class ShellInfo
{
    [Key(0)]
    public Guid Id { get; set; }
    
    [Key(1)]
    public Guid ShooterId { get; set; }
    
    [Key(2)]
    public Vector3 Position { get; set; }
    
    [Key(3)]
    public Vector3 Velocity { get; set; }
    
    [Key(4)]
    public Quaternion Rotation { get; set; }
    
    [Key(5)]
    public float LaunchForce { get; set; }
    
    [Key(6)]
    public float Timestamp { get; set; }
}

public interface IGameHub : IStreamingHub<IGameHub, IGameHubReceiver>
{
    ValueTask<(TankInfo[] existingTanks, Guid connectionId)> JoinAndSpawnAsync(Vector3 spawnPosition);
    ValueTask AttackAsync(Guid targetId);
    ValueTask MoveAsync(Guid playerId, Vector3 position, Quaternion rotation);
    ValueTask ShootAsync(Vector3 firePosition, Vector3 velocity, Quaternion rotation, float launchForce);
    ValueTask ShellUpdateAsync(Guid shellId, Vector3 position, Vector3 velocity);
    ValueTask ShellExplodeAsync(Guid shellId, Vector3 explosionPosition);
}

public interface IGameHubReceiver
{
    void OnAttack(Guid playerId, Guid targetId);
    void OnMove(Guid playerId, Vector3 position, Quaternion rotation);
    void OnPlayerJoined(Guid playerId, Vector3 position, bool isSelf);
    void OnPlayerLeft(Guid playerId);
    void OnShellFired(ShellInfo shellInfo);
    void OnShellUpdate(Guid shellId, Vector3 position, Vector3 velocity);
    void OnShellExplode(Guid shellId, Vector3 explosionPosition, Guid shooterId);
}