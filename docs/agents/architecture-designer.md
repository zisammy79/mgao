# Architecture Designer Notes

## Goals
- Define component architecture and interfaces for two-way sync
- Establish sync semantics, conflict resolution, and loop prevention

## Constraints
- Minimize COM calls; batch Google API calls with backoff

## Assumptions
- A8: Outlook ItemAdd/ItemChange events reliable for change detection
- A9: Google syncToken valid for incremental sync

## Artifacts
- **Components**:
  - `GoogleAuthService`: OAuth flow, token refresh, DPAPI storage
  - `GoogleCalendarClient`: Calendar/Event CRUD, batch requests, syncToken management
  - `OutlookCalendarBridge`: COM interop, folder creation, event mapping, UserProperties metadata
  - `SyncEngine`: Bidirectional sync orchestration, conflict detection, loop prevention
  - `StateStore`: SQLite for sync state (lastSyncToken, eventHashes, timestamps)
  - `UIShell`: WinForms/WPF main window
- **Interfaces**: `ICalendarProvider`, `ISyncEngine`, `ITokenStore`
- **Sync Flow**: Pull Google → Diff → Push to Outlook → Pull Outlook changes → Push to Google

## Acceptance Criteria
- [ ] All components have defined responsibilities
- [ ] Sync loop prevention mechanism specified

## Edge Cases
- **Recurrence**: Map RRULE to Outlook RecurrencePattern; unsupported rules → single instances + log
- **Time Zones**: Use Google event.start.timeZone; store in Outlook appointment.StartTimeZone
- **Loop Prevention**: Hash(subject+start+end+lastModified) stored; skip if hash unchanged after own write

## Status
Done
