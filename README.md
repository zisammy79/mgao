# MGAO - Multiple Google Accounts on Outlook

A Windows tool enabling **two-way Google Calendar sync** for multiple Google Workspace accounts inside a single classic Outlook profile.

![Build Status](https://github.com/YOUR_USERNAME/mgao/actions/workflows/build.yml/badge.svg)

## Features

- **Multi-account support**: Connect multiple Google Workspace accounts
- **Two-way sync**: Changes sync bidirectionally between Google Calendar and Outlook
- **Calendar selection**: Choose which calendars to sync per account
- **Incremental sync**: Uses Google sync tokens for efficient updates
- **Secure storage**: Tokens encrypted with Windows DPAPI
- **Conflict resolution**: Latest-modified-wins with conflict logging
- **Minimal UI**: Simple interface for account management and sync status

## Requirements

- Windows 10/11
- [.NET 6.0 Runtime](https://dotnet.microsoft.com/download/dotnet/6.0) (or SDK for building)
- Microsoft Outlook (classic desktop version)
- Google Workspace account(s) with Calendar API access

## Installation

### From Release

1. Download the latest release from [Releases](https://github.com/YOUR_USERNAME/mgao/releases)
2. Extract `MGAO-vX.X.X-win-x64.zip`
3. Run `MGAO.UI.exe`

### From Source

```cmd
git clone https://github.com/YOUR_USERNAME/mgao.git
cd mgao
dotnet build src\MGAO.sln --configuration Release
```

## Configuration

### Google Cloud Setup

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select existing
3. Enable the **Google Calendar API**
4. Go to **APIs & Services → Credentials**
5. Create **OAuth 2.0 Client ID** (Desktop application)
6. Download or copy the Client ID and Client Secret

### Environment Variables

Set your Google OAuth credentials before running:

```cmd
set MGAO_CLIENT_ID=your-client-id.apps.googleusercontent.com
set MGAO_CLIENT_SECRET=your-client-secret
```

Or in PowerShell:

```powershell
$env:MGAO_CLIENT_ID = "your-client-id.apps.googleusercontent.com"
$env:MGAO_CLIENT_SECRET = "your-client-secret"
```

## Usage

1. **Launch MGAO** with environment variables set
2. **Add Account**: Click "Add Account" → Complete Google OAuth → Select calendars to sync
3. **Sync**: Click "Sync Now" or wait for automatic sync
4. **View Logs**: Check the Logs tab for sync history and any conflicts

### Sync Behavior

| Scenario | Behavior |
|----------|----------|
| New event in Google | Created in Outlook |
| New event in Outlook | Created in Google |
| Event modified in both | Latest modification wins |
| Event deleted | Deleted on both sides |
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

### "Configuration Required" error
Ensure `MGAO_CLIENT_ID` and `MGAO_CLIENT_SECRET` environment variables are set.

### OAuth fails
1. Verify Google Calendar API is enabled in Cloud Console
2. Check OAuth consent screen is configured
3. Ensure redirect URI includes `http://localhost`

### Outlook not detected
1. Ensure classic Outlook is installed (not new Outlook)
2. Run MGAO as the same user that owns the Outlook profile

### Sync conflicts
Check the Logs tab for conflict details. The latest-modified version is kept; the other is logged.

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
- [Microsoft.Office.Interop.Outlook](https://docs.microsoft.com/en-us/visualstudio/vsto/office-primary-interop-assemblies) - Outlook COM interop
