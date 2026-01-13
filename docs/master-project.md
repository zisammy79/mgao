# MGAO Master Project Document

## 0) Summary
Windows tool enabling two-way Google Calendar sync for multiple Google Workspace accounts in a single classic Outlook profile.

## 1) Repo Context
- **Status**: M1-Foundation + M2/M3 scaffolds implemented
- **Paths**:
  - `/src/MGAO.sln` - Solution file
  - `/src/MGAO.Core/` - Interfaces (ITokenStore, ICalendarProvider, ISyncEngine), Services (DpapiTokenStore, StateStore, SyncEngine)
  - `/src/MGAO.Google/` - GoogleAuthService, GoogleCalendarClient
  - `/src/MGAO.Outlook/` - OutlookCalendarBridge
  - `/src/MGAO.UI/` - MainForm, Program
- **Build**: `dotnet build src/MGAO.sln`

## 2) Assumptions
- A1: Target classic Outlook (COM-based)
- A2: Users have Google Workspace with Calendar API enabled
- A3: Windows 10/11, .NET 6+
- A4: Primary users manage 2-5 calendars
- A5: "Latest-modified-wins" conflict resolution accepted
- A6-A16: See individual agent notes for additional assumptions

## 3) Requirements
### MVP
- Multi-account Google OAuth sign-in (PKCE)
- Secure token storage (DPAPI)
- Calendar list + selection per account
- Create Outlook calendar folders per (account, calendar)
- Two-way sync: -30d to +180d window
- Incremental sync (Google syncToken)
- Loop prevention + conflict logging
- Minimal UI: accounts, calendars, sync status

### V1 (Post-MVP)
- Full recurrence exception support
- Attendee response management
- Background service mode

## 4) Architecture
- **GoogleAuthService**: OAuth flow, token refresh, DPAPI storage
- **GoogleCalendarClient**: Calendar/Event CRUD, batch requests, syncToken
- **OutlookCalendarBridge**: COM interop, folder creation, event mapping
- **SyncEngine**: Bidirectional sync, conflict detection, loop prevention
- **StateStore**: SQLite for sync state (tokens, hashes, timestamps)
- **UIShell**: WinForms/WPF main window
- **Interfaces**: `ICalendarProvider`, `ISyncEngine`, `ITokenStore`

## 5) Sync Semantics
- **Flow**: Pull Google → Diff → Push Outlook → Pull Outlook → Push Google
- **Conflict**: Latest-modified-wins + log conflicts
- **Loop Prevention**: Hash(subject+start+end+modified); skip unchanged
- **Recurrence**: RRULE → RecurrencePattern; unsupported → single instances + log
- **Time Zones**: Use Google event.start.timeZone; store in Outlook StartTimeZone

## 6) Security & Compliance
- **Token Storage**: DPAPI (CurrentUser scope); file-based in %LOCALAPPDATA%\MGAO
- **Credentials**: Via environment variables (MGAO_CLIENT_ID, MGAO_CLIENT_SECRET)
- **Logging**: Recommend email redaction in production logs
- **Compliance**: Google API ToS compliant; minimal scopes; local-only data

## 7) Test Plan
- **Unit**: `dotnet test src/MGAO.Core.Tests` (DpapiTokenStore, StateStore, SyncEngine)
- **Integration**: `dotnet test src/MGAO.Google.Tests` (requires OAuth credentials)
- **Smoke**: Manual checklist (launch, add account, sync, bi-directional verify, remove)

## 8) Milestones
1. **M1-Foundation**: Project setup, Google OAuth, token storage
2. **M2-GoogleClient**: Calendar list, Event CRUD, syncToken
3. **M3-OutlookBridge**: COM interop, folder creation, event mapping
4. **M4-SyncEngine**: Bidirectional sync, conflict resolution
5. **M5-UI**: Account mgmt, calendar selection, status, logs
6. **M6-Polish**: Error handling, logging, diagnostics, installer
- **Critical Path**: M1→M2→M4→M5→M6 (M3 parallelizable with M2)

## 9) Decisions Log
- D1: .NET + Outlook COM Interop selected
- D2: DPAPI for token storage
- D3: MVP excludes complex recurrence exceptions

## 10) Open Issues / Blockers
- ~~**BLOCKER**: Calendar selection UI missing post-OAuth~~ (FIXED by code-fixer)
- **MEDIUM**: Log filtering not implemented
- **LOW**: Performance optimization for FindAppointmentByGoogleId

---
**CHECKPOINT**: Phase 11 COMPLETE. All 22 agents executed. MVP scaffold ready for build and test.
