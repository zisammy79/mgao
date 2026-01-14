# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

MGAO (Multiple Google Accounts on Outlook) is a Windows desktop application enabling two-way synchronization of Google Calendar events for multiple Google Workspace accounts into a single classic Outlook profile.

- **Language**: C# 11 (.NET 6.0)
- **UI Framework**: WinForms
- **Platform**: Windows 10/11 only (requires COM interop + DPAPI)

## Build Commands

```bash
# Restore and build
dotnet restore src/MGAO.sln
dotnet build src/MGAO.sln --configuration Release --no-restore

# Run the application
dotnet run --project src/MGAO.UI/MGAO.UI.csproj

# Publish self-contained executable
dotnet publish src/MGAO.UI/MGAO.UI.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true
```

Alternative: Use `./build.ps1` or `./build.cmd` scripts.

## Architecture

```
┌─────────────────────────────────────┐
│      MGAO.UI (WinForms)             │  Entry point, MainForm, CalendarSelectionForm
├─────────────────────────────────────┤
│      MGAO.Core                      │  SyncEngine, StateStore, DpapiTokenStore
├─────────────────────────────────────┤
│      Provider Abstraction           │  ICalendarProvider, ISyncEngine, ITokenStore
├──────────────────┬──────────────────┤
│   MGAO.Google    │  MGAO.Outlook    │  GoogleCalendarClient, OutlookCalendarBridge
└──────────────────┴──────────────────┘
```

**Dependency Flow**: `MGAO.UI` → `MGAO.Core` + `MGAO.Google` + `MGAO.Outlook`; both `MGAO.Google` and `MGAO.Outlook` → `MGAO.Core`

## Key Source Locations

- **Sync logic**: `src/MGAO.Core/Services/SyncEngine.cs` - Bidirectional sync (-30d to +180d window)
- **State persistence**: `src/MGAO.Core/Services/StateStore.cs` - SQLite-based sync tokens and event mappings
- **Google integration**: `src/MGAO.Google/GoogleCalendarClient.cs` - Calendar API CRUD and sync tokens
- **Outlook integration**: `src/MGAO.Outlook/OutlookCalendarBridge.cs` - COM interop for Outlook folders/events
- **Token security**: `src/MGAO.Core/Services/DpapiTokenStore.cs` - DPAPI-encrypted OAuth storage

## Configuration

OAuth credentials must be set via environment variables:
- `MGAO_CLIENT_ID` - Google OAuth client ID
- `MGAO_CLIENT_SECRET` - Google OAuth client secret

Runtime data stored in `%LOCALAPPDATA%\MGAO\`:
- `state.db` - SQLite database for sync state
- `tokens.dat` - DPAPI-encrypted OAuth tokens

## Key Dependencies

- `Google.Apis.Calendar.v3` / `Google.Apis.Auth` - Google Calendar API
- `Microsoft.Office.Interop.Outlook` - Outlook COM automation
- `Microsoft.Data.Sqlite` - State persistence
- `System.Security.Cryptography.ProtectedData` - DPAPI encryption

## Code Conventions

- Nullable reference types enabled throughout
- Async/await for all I/O operations
- Immutable record types for data transfer objects (CalendarEvent, SyncResult)
- Interface-based abstractions for testability (ICalendarProvider, ISyncEngine, ITokenStore)

## Reference Documentation

- `/docs/master-project.md` - Full project specification with architecture decisions
- `/docs/agents/` - 22 specialized design documents covering all aspects of the system
