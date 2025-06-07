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

public interface IGameHub : IStreamingHub<IGameHub, IGameHubReceiver>
{
    ValueTask<(TankInfo[] existingTanks, Guid connectionId)> JoinAndSpawnAsync(Vector3 spawnPosition);
    ValueTask AttackAsync(Guid targetId);
    ValueTask MoveAsync(Guid playerId, Vector3 position, Quaternion rotation);
}

public interface IGameHubReceiver
{
    void OnAttack(Guid playerId, Guid targetId);
    void OnMove(Guid playerId, Vector3 position, Quaternion rotation);
    void OnPlayerJoined(Guid playerId, Vector3 position, bool isSelf);
    void OnPlayerLeft(Guid playerId);
}