# MatchingHub待機室システム実装計画

## 概要

現在のシステムを拡張し、MatchingHub中心の待機室機能を実装します。プレイヤーがルームに参加後、待機状態となり、ホストが開始ボタンを押すことで一斉にGameHubに移動する仕組みを構築します。

## 現在のシステム分析

### データ構造

#### RoomInfo（現在）
```csharp
[MessagePackObject]
public class RoomInfo
{
    [Key(0)] public Guid RoomId { get; set; }
    [Key(1)] public string RoomName { get; set; }
}
```

#### IMatchingHub（現在）
```csharp
public interface IMatchingHub : IStreamingHub<IMatchingHub, IMatchingHubReceiver>
{
    ValueTask<RoomInfo> CreateRoomAsync(string roomName);
    ValueTask<RoomInfo[]> GetRoomListAsync();
}
```

### 現在のフロー
```
ルーム選択 → 直接GameHub接続 → 即座にゲーム開始
```

## 新しい設計

### 新しいフロー
```
ルーム選択 → MatchingHub参加 → 待機状態 → Start指示 → 一斉GameHub移動
```

### 拡張データ構造

#### PlayerInfo（新規）
```csharp
[MessagePackObject]
public class PlayerInfo
{
    [Key(0)] public Guid PlayerId { get; set; }
    [Key(1)] public string PlayerName { get; set; }
    [Key(2)] public bool IsHost { get; set; }
    [Key(3)] public bool IsReady { get; set; }
}
```

#### RoomStatus（新規）
```csharp
[MessagePackObject]
public enum RoomStatus
{
    Waiting = 0,
    Playing = 1,
    Finished = 2
}
```

#### RoomInfo（拡張）
```csharp
[MessagePackObject]
public class RoomInfo
{
    [Key(0)] public Guid RoomId { get; set; }
    [Key(1)] public string RoomName { get; set; }
    [Key(2)] public List<PlayerInfo> Players { get; set; } = new();
    [Key(3)] public RoomStatus Status { get; set; }
    [Key(4)] public Guid HostId { get; set; }
    [Key(5)] public int MaxPlayers { get; set; } = 4;
}
```

### 拡張インターフェース

#### IMatchingHub（拡張）
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

#### IMatchingHubReceiver（拡張）
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

## 実装ステップ

### Step 1: Shared層拡張
- [ ] `PlayerInfo`クラス追加（Tank.Shared）
- [ ] `RoomStatus`enum追加（Tank.Shared）
- [ ] `RoomInfo`クラス拡張（Tank.Shared）
- [ ] `IMatchingHub`インターフェース拡張（Tank.Shared）
- [ ] `IMatchingHubReceiver`インターフェース拡張（Tank.Shared）

### Step 2: Server側実装
- [ ] `MatchingHub`クラスの機能拡張
  - [ ] プレイヤー管理機能
  - [ ] ルーム状態管理機能
  - [ ] ホスト権限管理
  - [ ] ゲーム開始制御
- [ ] `GameContextRepository`の待機室連携

### Step 3: Client側実装
- [ ] `MatchingHubClient`機能拡張
  - [ ] ルーム参加/離脱機能
  - [ ] プレイヤー状態管理
  - [ ] ゲーム開始イベント処理
- [ ] `GameHubClient`初期化タイミング変更
  - [ ] 自動接続の無効化
  - [ ] 手動接続メソッド追加

### Step 4: UI実装
- [ ] 待機室シーン/UI作成
  - [ ] 参加者リスト表示
  - [ ] スタートボタン（ホスト権限）
  - [ ] Ready状態切り替え
  - [ ] ルーム情報表示
- [ ] `RoomPresenter`の参加機能追加

### Step 5: フロー統合
- [ ] 全体の流れ統合
- [ ] エラーハンドリング
- [ ] 切断処理
- [ ] テスト・デバッグ

## 詳細実装仕様

### MatchingHub新機能

#### プレイヤー管理
- ルーム参加時にPlayerInfoを作成
- 最初の参加者がホストに設定
- プレイヤー離脱時の自動クリーンアップ
- ホスト離脱時の権限移譲

#### ルーム状態管理
- Waiting: 参加者募集中
- Playing: ゲーム進行中
- Finished: ゲーム終了

#### ゲーム開始制御
- ホストのみ開始可能
- 全プレイヤーにOnGameStarted通知
- GameContextと連携

### GameHubClient変更

#### 現在の自動接続処理
```csharp
async UniTaskVoid Start()
{
    channel = GrpcChannelx.ForAddress("http://localhost:5127");
    hubClient = await StreamingHubClient.ConnectAsync<IGameHub, IGameHubReceiver>(
        channel, this);
    // 即座にゲーム参加
    JoinGame(currentRoom.RoomId, spawnPosition, isSpectating);
}
```

#### 新しい手動接続処理
```csharp
async UniTaskVoid Start()
{
    // 待機状態
}

public async UniTask ConnectToGameHub()
{
    channel = GrpcChannelx.ForAddress("http://localhost:5127");
    hubClient = await StreamingHubClient.ConnectAsync<IGameHub, IGameHubReceiver>(
        channel, this);
    // ゲーム参加処理
    JoinGame(currentRoom.RoomId, spawnPosition, isSpectating);
}
```

### UI設計

#### 待機室UI要素
- ルーム名表示
- 参加者リスト（Name, Ready状態, Host表示）
- Ready切り替えボタン
- Startボタン（ホストのみ有効）
- 離脱ボタン
- ルーム設定表示

#### 画面遷移
```
マッチングシーン → 待機室シーン → ゲームシーン
```

## セキュリティ・エラーハンドリング

### 権限制御
- ゲーム開始はホストのみ
- ルーム設定変更はホストのみ
- 無効な操作の検証

### 切断処理
- プレイヤー切断時の自動クリーンアップ
- ホスト切断時の権限移譲
- ネットワークエラー時の復旧処理

### 状態同期
- ルーム状態の一貫性保証
- プレイヤー状態の同期
- 競合状態の解決

## テスト計画

### 単体テスト
- MatchingHub各機能のテスト
- データ構造のシリアライゼーション
- エラーケースの検証

### 統合テスト
- クライアント・サーバー間通信
- 複数プレイヤーの同時操作
- ネットワーク切断・復旧

### シナリオテスト
- 正常フロー（作成→参加→開始→ゲーム）
- ホスト権限移譲シナリオ
- エラー・切断シナリオ

## 今後の拡張可能性

### 機能拡張
- プライベートルーム（パスワード）
- 観戦者システム
- チーム分け機能
- マッチメイキング

### パフォーマンス最適化
- ルーム数制限
- プレイヤー数制限
- メモリ使用量最適化

---

この計画に基づいて実装を進めることで、より制御されたマルチプレイヤー体験を提供できます。