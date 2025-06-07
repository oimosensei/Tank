using UnityEngine;
using System; // Guid のために必要
using System.Collections.Generic; // Dictionary のために必要
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using MagicOnion.Client;
using Nakatani;
using MagicOnion;
using Cysharp.Threading.Tasks;


// --- Unity で動作する実装クラス ---
public class GameHubClient : MonoBehaviour, IGameHubReceiver
{
    private GrpcChannelx channel;
    private IGameHub hubClient;
    private Guid myConnectionId;

    async UniTaskVoid Start()
    {
        channel = GrpcChannelx.ForAddress("http://localhost:5127");
        hubClient = await StreamingHubClient.ConnectAsync<IGameHub, IGameHubReceiver>(
            channel, this);

        Debug.Log("Connected to GameHub server");
        float randomX = UnityEngine.Random.Range(-10f, 10f);
        float randomZ = UnityEngine.Random.Range(-10f, 10f);

        var (existingTanks, connectionId) = await hubClient.JoinAndSpawnAsync(new Vector3(randomX, 0, randomZ));
        myConnectionId = connectionId;

        // Spawn all existing tanks
        if (Nakatani.TankManager.Instance != null)
        {
            foreach (var tankInfo in existingTanks)
            {
                if (tankInfo.Id != System.Guid.Empty) // Skip empty guid
                {
                    Debug.Log($"Spawning existing tank: {tankInfo.Id} at {tankInfo.Position} with rotation {tankInfo.Rotation}");
                    Nakatani.TankManager.Instance.SpawnTank(tankInfo.Id, tankInfo.Position, tankInfo.Rotation, false, 1);
                }
            }
        }
    }

    public void MoveTank(Vector3 position, Quaternion rotation)
    {
        hubClient.MoveAsync(myConnectionId, position, rotation);
    }

    private async void OnDestroy()
    {
        if (hubClient != null)
        {
            await hubClient.DisposeAsync();
        }
        if (channel != null)
        {
            await channel.ShutdownAsync();
        }
    }

    // シングルトンインスタンス (必要に応じて)
    public static GameHubClient Instance { get; private set; }
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // シーンをまたいで存在させたい場合
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // MagicOnionのクライアントセットアップ時に、このインスタンスをレシーバーとして登録する必要があります。
    // 例: hubClient.Register(this); のような形

    public void OnAttack(Guid playerId, Guid targetId)
    {
        Debug.Log($"[GameHubClient] OnAttack: Player {playerId} attacked Target {targetId}");

        GameObject attackerTank = Nakatani.TankManager.Instance?.GetTank(playerId);
        if (attackerTank != null)
        {
            // 攻撃者のアニメーションやエフェクトを再生
            // 例: attackerTank.GetComponent<TankShootingController>()?.PlayAttackAnimation();
            Debug.Log($"Attacker tank {playerId} found. Playing attack animation/effect.");
        }
        else
        {
            Debug.LogWarning($"Attacker tank {playerId} not found for OnAttack.");
        }

        GameObject targetTank = Nakatani.TankManager.Instance?.GetTank(targetId);
        if (targetTank != null)
        {
            // 被弾者のアニメーションやエフェクト、ダメージ処理など
            // 例: targetTank.GetComponent<TankHealth>()?.TakeDamage(10);
            Debug.Log($"Target tank {targetId} found. Applying damage/effect.");
        }
        else
        {
            Debug.LogWarning($"Target tank {targetId} not found for OnAttack.");
        }
        // TODO: 攻撃エフェクトの生成やサウンド再生などをここに追加
    }

    public void OnMove(Guid playerId, Vector3 position, Quaternion rotation)
    {
        Debug.Log($"[GameHubClient] OnMove: Player {playerId} moved to {position} with rotation {rotation}");

        if (Nakatani.TankManager.Instance != null)
        {
            Nakatani.TankManager.Instance.OnTankMove(playerId, position, rotation);
        }
        else
        {
            Debug.LogError("Nakatani.TankManager.Instance is null in OnMove");
        }
    }

    public void OnPlayerJoined(Guid playerId, Vector3 position, bool isSelf)
    {
        Debug.Log($"[GameHubClient] OnPlayerJoined: Player {playerId} at {position}, IsSelf: {isSelf}");

        if (Nakatani.TankManager.Instance != null)
        {
            // 1 is a default player number, can be modified as needed
            Nakatani.TankManager.Instance.SpawnTank(playerId, position, isSelf, 1);
        }
        else
        {
            Debug.LogError("Nakatani.TankManager.Instance is null in OnPlayerJoined");
        }
    }

    public void OnPlayerLeft(Guid playerId)
    {
        Debug.Log($"[GameHubClient] OnPlayerLeft: Player {playerId}");
        if (Nakatani.TankManager.Instance != null)
        {
            Nakatani.TankManager.Instance.DestroyTank(playerId);
        }
        else
        {
            Debug.LogError("Nakatani.TankManager.Instance is null in OnPlayerLeft");
        }
    }

    // Helper method to test movement
    public async void TestMove(Vector3 position, Quaternion rotation)
    {
        if (hubClient != null)
        {
            await hubClient.MoveAsync(myConnectionId, position, rotation);
            Debug.Log($"Sent move command to position: {position} with rotation: {rotation}");
        }
        else
        {
            Debug.LogError("Hub client is not connected");
        }
    }

    // Helper method to join the game
    public async void JoinGame(Vector3 spawnPosition)
    {
        if (hubClient != null)
        {
            var (existingTanks, connectionId) = await hubClient.JoinAndSpawnAsync(spawnPosition);
            myConnectionId = connectionId;
            Debug.Log($"Joined game at position: {spawnPosition}");

            // Spawn all existing tanks
            if (Nakatani.TankManager.Instance != null)
            {
                foreach (var tankInfo in existingTanks)
                {
                    if (tankInfo.Id != System.Guid.Empty) // Skip empty guid
                    {
                        Debug.Log($"Spawning existing tank: {tankInfo.Id} at {tankInfo.Position} with rotation {tankInfo.Rotation}");
                        Nakatani.TankManager.Instance.SpawnTank(tankInfo.Id, tankInfo.Position, tankInfo.Rotation, false, 1);
                    }
                }
            }
        }
        else
        {
            Debug.LogError("Hub client is not connected");
        }
    }

}