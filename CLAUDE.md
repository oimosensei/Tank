# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

### Server (.NET 9 ASP.NET Core)
```bash
# Build the server
dotnet build src/Tank.Server/Tank.Server.csproj

# Run the server (development)
dotnet run --project src/Tank.Server/Tank.Server.csproj

# Publish for deployment
dotnet publish src/Tank.Server/Tank.Server.csproj -c Release
```

### Shared Library (.NET Standard 2.1)
```bash
# Build the shared library
dotnet build src/Tank.Shared/Tank.Shared.csproj
```

### Solution Build
```bash
# Build entire solution (Server + Shared)
dotnet build Tank.sln

# Clean solution
dotnet clean Tank.sln
```

### Unity Project
The Unity project is located in `src/Tank.Unity/` and should be opened with Unity Editor. Unity package dependencies are managed through the Package Manager using the manifest at `src/Tank.Unity/Packages/manifest.json`.

## Architecture Overview

This is a multiplayer tank game built with:
- **MagicOnion 7.0.4**: High-performance gRPC framework for real-time communication
- **Unity**: Game client with Universal Render Pipeline
- **ASP.NET Core**: Server hosting MagicOnion streaming hubs
- **MessagePack**: Serialization for network messages

### Project Structure

```
src/
├── Tank.Server/          # ASP.NET Core server with MagicOnion hubs
├── Tank.Shared/          # Shared interfaces and data models (.NET Standard 2.1)
└── Tank.Unity/           # Unity game client
```

### Core Components

#### Server (Tank.Server)
- **GameHub**: Main streaming hub handling player connections, tank movement, shooting, and shell physics
- **MatchingHub**: Room creation and matchmaking functionality
- **GameContext**: Manages game state, player data, and shell tracking per room
- **GameContextRepository**: Singleton managing multiple game rooms

#### Shared (Tank.Shared)
- **IGameHub/IGameHubReceiver**: Interfaces for game functionality (movement, shooting, shell physics)
- **IMatchingHub/IMatchingHubReceiver**: Interfaces for room management
- **TankInfo**: Player tank state (position, rotation, turret rotation)
- **ShellInfo**: Shell/projectile state (position, velocity, shooter, timestamp)
- **RoomInfo**: Room metadata

#### Unity Client (Tank.Unity)
- **GameHubClient**: MagicOnion client implementing IGameHubReceiver for server communication (located at root Assets/)
- **Main Game Scripts**: Located in `Assets/_Completed-Assets/Scripts/TankNew/` - this is the primary codebase being used
- **TankManager**: Manages tank GameObjects and their network state synchronization
- **ShellManager**: Handles shell/projectile lifecycle and physics
- **MagicOnionInitializer**: Sets up networking components

**Note**: The game scripts in `Assets/Scripts/Tank/` and `Assets/Scripts/` are legacy/unused. Active development should focus on `Assets/_Completed-Assets/Scripts/TankNew/`.

### Network Architecture

The game uses MagicOnion's StreamingHub pattern for real-time bidirectional communication:

1. **Client-to-Server RPCs**: Player actions (movement, shooting, shell updates)
2. **Server-to-Client Events**: Game state updates (other players' actions, shell physics)
3. **Room-based Groups**: Players are organized in rooms with isolated game state

### Key Networking Patterns

- **Tank Movement**: Client sends `TankTransformUpdateAsync`, server broadcasts to other players via `OnTankTransformUpdate`
- **Shell System**: Client fires shell with `ShootAsync`, server tracks physics and broadcasts updates via `OnShellUpdate`/`OnShellExplode`
- **Player Management**: Server handles join/leave events with automatic cleanup

### Development Notes

- Server runs on `http://localhost:5127` by default
- Unity project uses custom packages for MagicOnion, UniTask, and YetAnotherHttpHandler
- Shell physics are server-authoritative with client prediction
- Each player has a unique GUID for identification across the network
- Room system supports multiple concurrent games via GameContextRepository