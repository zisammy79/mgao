# MGAO - Multiple Google Accounts on Outlook

A Windows tool enabling **two-way Google Calendar sync** for multiple Google Workspace accounts inside a single classic Outlook profile.

[![Build Status](https://github.com/zisammy79/mgao/actions/workflows/build.yml/badge.svg)](https://github.com/zisammy79/mgao/actions/workflows/build.yml)

## Features

- **Multi-account support**: Connect multiple Google Workspace accounts
- **Two-way sync**: Bidirectional updates for paired events; Google → Outlook for new events
- **Calendar selection**: Choose which calendars to sync per account
- **Incremental sync**: Uses Google sync tokens for efficient updates
- **Secure storage**: Tokens encrypted with Windows DPAPI
- **Conflict resolution**: Latest-modified-wins with conflict logging
- **Minimal UI**: Simple interface for account management and sync status

## Requirements

- Windows 10/11
- [.NET 6.0 Runtime](https://dotnet.microsoft.com/download/dotnet/6.0) (or SDK for building)
- Microsoft Outlook (classic desktop version)
- Google Workspace account(s)

## Installation

### From Release (Recommended)

1. Go to the [Releases](https://github.com/zisammy79/mgao/releases) page
2. Download the latest `MGAO-vX.Y.Z-win-x64.zip`
3. Extract the ZIP to a folder of your choice
4. Run `MGAO.UI.exe`

**No Google Cloud setup required** - MGAO works out of the box like GWSMO.

### From Source

```cmd
git clone https://github.com/zisammy79/mgao.git
cd mgao
dotnet build src\MGAO.sln --configuration Release
```

The built executable will be at `src\MGAO.UI\bin\Release\net6.0-windows\MGAO.UI.exe`

## Usage

1. **Launch MGAO**
2. **Add Account**: Click "Add Account" → Sign in with Google → Select calendars to sync
3. **Sync**: Click "Sync Now" to synchronize
4. **View Logs**: Check the Logs tab for sync history and any conflicts

### First-Time Sign-In

When signing in, you may see an "unverified app" warning. This is normal for apps in testing mode:
1. Click "Advanced"
2. Click "Go to MGAO (unsafe)"
3. Grant calendar permissions

This warning appears because the app is in Google's testing phase (limited to 100 users).

### Sync Behavior

| Scenario | Behavior |
|----------|----------|
| New event in Google | Created in Outlook |
| New event in Outlook | Not synced (Outlook → Google creation not yet implemented) |
| Event modified in both | Latest modification wins |
| Event deleted | Not synced (deletion sync not yet implemented) |
| Sync window | -30 days to +180 days |

## Project Structure

```
mgao/
├── src/
│   ├── MGAO.sln                 # Solution file
│   ├── MGAO.Core/               # Interfaces, sync engine, state store
│   ├── MGAO.Google/             # Google Calendar API integration
│   ├── MGAO.Outlook/            # Outlook COM interop
│   └── MGAO.UI/                 # WinForms user interface
├── docs/
│   ├── master-project.md        # Project specifications
│   └── agents/                  # Design documents
├── .github/workflows/           # CI/CD pipelines
├── build.cmd                    # Windows build script
└── build.ps1                    # PowerShell build script
```

## Architecture

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   Google    │     │    Sync     │     │   Outlook   │
│  Calendar   │◄───►│   Engine    │◄───►│    COM      │
│    API      │     │             │     │  Interop    │
└─────────────┘     └─────────────┘     └─────────────┘
                           │
                    ┌──────┴──────┐
                    │ State Store │
                    │  (SQLite)   │
                    └─────────────┘
```

## Data Storage

| Data | Location | Encryption |
|------|----------|------------|
| OAuth tokens | `%LOCALAPPDATA%\MGAO\tokens\` | DPAPI |
| Sync state | `%LOCALAPPDATA%\MGAO\state.db` | None (IDs only) |
| Event data | Outlook folders | Outlook's storage |

## Limitations

- **Classic Outlook only**: New Outlook (web-based) not supported
- **Windows only**: Requires Windows for COM interop and DPAPI
- **Recurrence**: Complex recurrence rules may be simplified
- **Attendees**: Synced as data; response management not supported in MVP

## Troubleshooting

### "Unverified app" warning
This is expected - click "Advanced" → "Go to MGAO (unsafe)" to continue. The app is in testing mode.

### "Access blocked" error
Your Google Workspace admin may have restricted third-party app access. Contact your admin to allow MGAO.

### Account shows "Needs Reauth"
Click the account and press "Reauth" to re-authenticate with Google.

### Outlook not detected
1. Ensure classic Outlook is installed (not new Outlook)
2. Run MGAO as the same user that owns the Outlook profile

### Sync conflicts
Check the Logs tab for conflict details. The latest-modified version is kept; the other is logged.

## Advanced Configuration

For custom deployments, you can override the embedded OAuth credentials:

```cmd
set MGAO_CLIENT_ID=your-custom-client-id
set MGAO_CLIENT_SECRET=your-custom-client-secret
```

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit changes (`git commit -m 'Add amazing feature'`)
4. Push to branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [Google.Apis.Calendar.v3](https://www.nuget.org/packages/Google.Apis.Calendar.v3/) - Google Calendar API client
- [Microsoft.Office.Interop.Outlook](https://learn.microsoft.com/en-us/visualstudio/vsto/office-primary-interop-assemblies) - Outlook COM interop
