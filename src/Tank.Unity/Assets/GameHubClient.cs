using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Collections.Generic; // For Queue
using Grpc.Core; // For ChannelOption, ChannelCredentials if needed
using MagicOnion.Client;
using Grpc.Net.Client;
using MagicOnion;

// Receiver class: Implements methods called by the server
public class GameHubReceiver : IGameHubReceiver
{
    // Action to enqueue UI updates to be run on the main thread
    private readonly Action<Action> _enqueueMainThreadAction;

    public GameHubReceiver(Action<Action> enqueueMainThreadAction)
    {
        _enqueueMainThreadAction = enqueueMainThreadAction;
    }

    public void OnAttack(Guid playerId, Guid targetId)
    {
        _enqueueMainThreadAction(() =>
        {
            Debug.Log($"[Receiver] Player {playerId} attacked target {targetId}");
            // TODO: Update game state or UI based on this attack
        });
    }

    public void OnMove(Guid playerId, int x, int y)
    {
        _enqueueMainThreadAction(() =>
        {
            Debug.Log($"[Receiver] Player {playerId} moved to ({x}, {y})");
            // TODO: Update character position or UI
        });
    }

    public void OnPlayerJoined(string playerName, Guid playerId)
    {
        _enqueueMainThreadAction(() =>
        {
            Debug.Log($"[Receiver] Player {playerName} (ID: {playerId}) joined the game.");
            // TODO: Add player to scene, update player list UI
        });
    }

    public void OnPlayerLeft(string playerName, Guid playerId)
    {
        _enqueueMainThreadAction(() =>
        {
            Debug.Log($"[Receiver] Player {playerName} (ID: {playerId}) left the game.");
            // TODO: Remove player from scene, update player list UI
        });
    }
    public void OnNotification(string message)
    {
        _enqueueMainThreadAction(() =>
        {
            Debug.Log($"[Receiver] Notification: {message}");
            // TODO: Display notification
        });
    }
}

public class GameHubClient : MonoBehaviour
{
    [Header("Server Connection")]
    [SerializeField] private string serverAddress = "localhost:5217"; // For gRPC, usually just host:port

    [Header("Player Info")]
    [SerializeField] private string playerName = "UnityPlayer";

    private GrpcChannelx _channel; // Grpc.Core.Channel or Grpc.Net.Client.GrpcChannel
    private IGameHub _hubClient;
    private GameHubReceiver _receiver;

    // Queue for actions to be executed on the main thread
    private readonly Queue<Action> _mainThreadActions = new Queue<Action>();

    void Awake()
    {
        // Initialize receiver, passing a lambda to enqueue actions
        _receiver = new GameHubReceiver(action =>
        {
            lock (_mainThreadActions)
            {
                _mainThreadActions.Enqueue(action);
            }
        });
    }

    async void Start()
    {
        await ConnectToServerAsync();
    }

    void Update()
    {
        // Process any actions queued from background threads (like receiver methods)
        lock (_mainThreadActions)
        {
            while (_mainThreadActions.Count > 0)
            {
                _mainThreadActions.Dequeue()?.Invoke();
            }
        }

        // --- Example Input for testing ---
        if (_hubClient != null)
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                Debug.Log("Sending Attack command...");
                // Create a dummy target ID for testing
                _ = SendAttackAsync(Guid.NewGuid()); // Fire and forget for this example
            }
            if (Input.GetKeyDown(KeyCode.M))
            {
                int randX = UnityEngine.Random.Range(-10, 11);
                int randY = UnityEngine.Random.Range(-10, 11);
                Debug.Log($"Sending Move command to ({randX}, {randY})...");
                _ = SendMoveAsync(randX, randY); // Fire and forget
            }
        }
    }

    public async Task ConnectToServerAsync()
    {
        if (_hubClient != null)
        {
            Debug.Log("Already connected or connecting.");
            return;
        }

        Debug.Log($"Attempting to connect to {serverAddress}...");
        try
        {
            // For Grpc.Core (typically used in Unity)
            // For HTTP (non-TLS):
            _channel = GrpcChannelx.ForAddress("http://" + serverAddress); // Or "https://";
            // For HTTPS (TLS):
            // var credentials = new SslCredentials(); // Or load from file if you have custom certs
            // _channel = new Channel(serverAddress, credentials);

            // For Grpc.Net.Client (if you are sure your target platform supports it well with MagicOnion)
            // _channel = GrpcChannelx.ForAddress("http://" + serverAddress); // Or "https://"

            // Connect to the StreamingHub
            // The _receiver instance is passed to handle messages from the server
            _hubClient = await StreamingHubClient.ConnectAsync<IGameHub, IGameHubReceiver>(_channel, _receiver);

            Debug.Log("Successfully connected to GameHub!");

            // Join the game after connecting
            //TODO
            // await _hubClient.JoinAsync(playerName);
            Debug.Log($"Sent Join request as {playerName}.");

        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to connect to GameHub: {e.GetType().Name} - {e.Message}");
            Debug.LogException(e);
            _hubClient = null;
            if (_channel != null)
            {
                await _channel.ShutdownAsync();
                _channel = null;
            }
        }
    }

    public async Task SendAttackAsync(Guid targetId)
    {
        if (_hubClient == null)
        {
            Debug.LogWarning("Not connected to server. Cannot send Attack.");
            return;
        }
        try
        {
            await _hubClient.AttackAsync(targetId);
            Debug.Log($"AttackAsync called for target {targetId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error sending Attack: {e.Message}");
            // Handle potential disconnection or other errors
        }
    }

    public async Task SendMoveAsync(int x, int y)
    {
        if (_hubClient == null)
        {
            Debug.LogWarning("Not connected to server. Cannot send Move.");
            return;
        }
        try
        {
            await _hubClient.MoveAsync(x, y);
            Debug.Log($"MoveAsync called to ({x}, {y})");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error sending Move: {e.Message}");
        }
    }

    async void OnDestroy()
    {
        await DisconnectAsync();
    }

    async void OnApplicationQuit()
    {
        await DisconnectAsync();
    }

    public async Task DisconnectAsync()
    {
        if (_hubClient != null)
        {
            Debug.Log("Disconnecting from GameHub...");
            try
            {
                //TODO
                // await _hubClient.LeaveAsync(); // Gracefully leave
                await _hubClient.DisposeAsync(); // Dispose the client-side hub
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during hub disposal: {e.Message}");
            }
            _hubClient = null;
        }

        if (_channel != null)
        {
            Debug.Log("Shutting down gRPC channel...");
            try
            {
                await _channel.ShutdownAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during channel shutdown: {e.Message}");
            }
            _channel = null;
        }
        Debug.Log("Disconnected.");
    }
}