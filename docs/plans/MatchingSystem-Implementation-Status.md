# MatchingSystem実装状況ドキュメント

## 概要

MatchingHub中心の待機室システムの実装状況と、現在のマッチング処理の流れを説明します。

## 実装済み変更点

### 1. Shared層の拡張

#### PlayerInfo クラス
```csharp
[MessagePackObject]
public class PlayerInfo
{
    [Key(0)] public Guid PlayerId { get; set; }
    [Key(1)] public string PlayerName { get; set; } = string.Empty;
    [Key(2)] public bool IsHost { get; set; }
    [Key(3)] public bool IsReady { get; set; }
}
```

#### RoomStatus 列挙型
```csharp
public enum RoomStatus
{
    Waiting = 0,    // プレイヤー募集中
    Playing = 1,    // ゲーム進行中
    Finished = 2    // ゲーム終了
}
```

#### RoomInfo クラス拡張
```csharp
[MessagePackObject]
public class RoomInfo
{
    [Key(0)] public Guid RoomId { get; set; }
    [Key(1)] public string RoomName { get; set; } = string.Empty;
    [Key(2)] public List<PlayerInfo> Players { get; set; } = new List<PlayerInfo>();
    [Key(3)] public RoomStatus Status { get; set; } = RoomStatus.Waiting;
    [Key(4)] public Guid HostId { get; set; }
    [Key(5)] public int MaxPlayers { get; set; } = 4;
}
```

### 2. IMatchingHub インターフェース拡張

```csharp
public interface IMatchingHub : IStreamingHub<IMatchingHub, IMatchingHubReceiver>
{
    ValueTask<RoomInfo> CreateRoomAsync(string roomName, int maxPlayers = 4);
    ValueTask<RoomInfo[]> GetRoomListAsync();
    ValueTask<RoomInfo> JoinRoomAsync(Guid roomId, string playerName);
    ValueTask LeaveRoomAsync(Guid roomId);
    ValueTask<RoomInfo> StartGameAsync(Guid roomId);
    ValueTask<RoomInfo> GetRoomStatusAsync(Guid roomId);
    ValueTask SetReadyStatusAsync(Guid roomId, bool isReady);
}
```

### 3. IMatchingHubReceiver インターフェース拡張

```csharp
public interface IMatchingHubReceiver
{
    void OnRoomCreated(RoomInfo roomInfo);
    void OnRoomUpdated(RoomInfo roomInfo);
    void OnRoomDeleted(Guid roomId);
    void OnPlayerJoinedRoom(PlayerInfo playerInfo, RoomInfo roomInfo);
    void OnPlayerLeftRoom(Guid playerId, RoomInfo roomInfo);
    void OnPlayerReadyChanged(Guid playerId, bool isReady);
    void OnGameStarted(Guid gameContextId, RoomInfo roomInfo);
    void OnGameEnded(Guid roomId);
}
```

## システムアーキテクチャ

### クラス関係図

```mermaid
classDiagram
    class RoomInfo {
        +Guid RoomId
        +string RoomName
        +List~PlayerInfo~ Players
        +RoomStatus Status
        +Guid HostId
        +int MaxPlayers
    }
    
    class PlayerInfo {
        +Guid PlayerId
        +string PlayerName
        +bool IsHost
        +bool IsReady
    }
    
    class MatchingHub {
        -ConcurrentDictionary~Guid, RoomInfo~ _rooms
        -ConcurrentDictionary~Guid, Guid~ _playerToRoom
        +CreateRoomAsync()
        +JoinRoomAsync()
        +StartGameAsync()
        +LeaveRoomAsync()
    }
    
    class MatchingHubClient {
        -IMatchingHub hubClient
        +CreateRoom()
        +JoinRoom()
        +StartGame()
        +OnGameStarted()
    }
    
    class RoomPresenter {
        -RoomModel _roomModel
        +OnRoomJoinClicked()
        +JoinRoomViaMatchingHub()
    }
    
    class GameHubClient {
        +ConnectToGameHub()
        +JoinGame()
    }
    
    RoomInfo *-- PlayerInfo
    MatchingHub ..> RoomInfo
    MatchingHubClient ..> MatchingHub
    RoomPresenter ..> MatchingHubClient
    MatchingHubClient ..> GameHubClient : triggers
```

### データフロー図

```mermaid
flowchart TD
    A[マッチング画面] --> B{ユーザーアクション}
    B -->|ルーム作成| C[CreateRoomAsync]
    B -->|ルーム参加| D[JoinRoomAsync]
    
    C --> E[MatchingHub<br/>ルーム管理]
    D --> E
    
    E --> F[待機状態<br/>Players追加]
    F --> G{ホストが開始?}
    
    G -->|No| H[Ready状態変更<br/>プレイヤー待機]
    H --> G
    
    G -->|Yes| I[StartGameAsync]
    I --> J[OnGameStarted通知]
    J --> K[GameHubClient接続]
    K --> L[ゲーム開始]
    
    style E fill:#f9f,stroke:#333,stroke-width:2px
    style J fill:#bbf,stroke:#333,stroke-width:2px
```

## 処理の流れ

### 1. ルーム作成フロー

```mermaid
sequenceDiagram
    participant U as User
    participant RP as RoomPresenter
    participant MC as MatchingHubClient
    participant MH as MatchingHub
    participant GCR as GameContextRepository
    
    U->>RP: ルーム名入力 & 作成ボタン
    RP->>MC: CreateRoom(roomName)
    MC->>MH: CreateRoomAsync(roomName, maxPlayers)
    MH->>GCR: CreateAndRun()
    GCR-->>MH: GameContext
    MH->>MH: RoomInfo作成 & 保存
    MH-->>MC: RoomInfo
    MC-->>RP: RoomInfo
    RP->>RP: UI更新
```

### 2. ルーム参加フロー

```mermaid
sequenceDiagram
    participant U as User
    participant RP as RoomPresenter
    participant MC as MatchingHubClient
    participant MH as MatchingHub
    participant CI as CurrentRoomInfo
    participant SM as SceneManager
    
    U->>RP: 参加ボタンクリック
    RP->>MC: JoinRoom(roomId, playerName)
    MC->>MH: JoinRoomAsync(roomId, playerName)
    MH->>MH: プレイヤー追加 & 検証
    MH-->>MC: 更新されたRoomInfo
    MC-->>RP: RoomInfo
    RP->>CI: RoomInfo保存
    RP->>SM: ゲームシーンへ遷移
```

### 3. ゲーム開始フロー

```mermaid
sequenceDiagram
    participant H as Host
    participant MC as MatchingHubClient
    participant MH as MatchingHub
    participant GHC as GameHubClient
    participant GH as GameHub
    
    H->>MC: StartGame(roomId)
    MC->>MH: StartGameAsync(roomId)
    MH->>MH: Status→Playing
    MH->>MC: OnGameStarted(gameContextId, roomInfo)
    MC->>GHC: ConnectToGameHub() [TODO]
    GHC->>GH: JoinRoomAsync(roomId)
    GH-->>GHC: 既存タンク情報
    GHC->>GHC: ゲーム開始
```

## 主要コンポーネントの役割

### MatchingHub (Server)
- **責任**: ルーム管理、プレイヤー管理、ゲーム開始制御
- **機能**:
  - ルーム作成・削除
  - プレイヤー参加・離脱管理
  - ホスト権限管理
  - ゲーム開始タイミング制御
- **状態管理**: 
  - `_rooms`: 全ルーム情報
  - `_playerToRoom`: プレイヤー→ルームマッピング

### MatchingHubClient (Unity)
- **責任**: MatchingHubとの通信、イベントハンドリング
- **機能**:
  - サーバーAPI呼び出し
  - サーバーイベント受信
  - GameHubClientとの連携（TODO）
- **重要メソッド**:
  - `OnGameStarted()`: ゲーム開始トリガー

### RoomPresenter (Unity)
- **責任**: UI制御、ユーザーアクション処理
- **機能**:
  - ルーム一覧表示
  - ルーム作成UI
  - ルーム参加処理
- **変更点**: `JoinRoomViaMatchingHub()`メソッド追加

### GameHubClient (Unity)
- **現在の状態**: 自動接続モード
- **TODO**: 手動接続モードに変更
  - `Start()`での自動接続を無効化
  - `ConnectToGameHub()`メソッド追加
  - `OnGameStarted`イベントからの呼び出し

## 現在の制限事項

### 1. ブロードキャスト通知
- サーバー側のブロードキャスト機能は一時的にコメントアウト
- ルーム更新通知が動作しない状態
- **修正必要**: MagicOnionのGroup機能を正しく実装

### 2. GameHubClient統合
- 自動接続モードのまま
- MatchingHubからの制御が未実装
- **修正必要**: 手動接続メソッドの実装

### 3. UI統合
- 待機室専用UIが未実装
- プレイヤー名入力が仮実装
- **修正必要**: 待機室シーン・UI作成

## 次のステップ

### 優先度: 高
1. **ブロードキャスト機能修正**
   - MagicOnionのGroup機能を正しく実装
   - リアルタイム通知の復活

2. **GameHubClient統合**
   - 手動接続モード実装
   - MatchingHubからの制御

### 優先度: 中
3. **待機室UI実装**
   - 専用シーン作成
   - プレイヤーリスト表示
   - Ready/Start機能

4. **エラーハンドリング強化**
   - 接続エラー処理
   - タイムアウト処理
   - 再接続機能

## 技術的詳細

### データ永続化
- **サーバー**: インメモリ（ConcurrentDictionary）
- **クライアント**: CurrentRoomInfoシングルトン
- **制限**: サーバー再起動で全データ消失

### ネットワーク通信
- **プロトコル**: MagicOnion (gRPC)
- **シリアライゼーション**: MessagePack
- **接続**: WebSocket over HTTP/2

### 同期モデル
- **ルーム状態**: サーバーが真の状態を保持
- **プレイヤー状態**: サーバー側で管理
- **UI更新**: Reactive Extensions (UniRx)

---

このドキュメントは実装の進行に応じて更新されます。