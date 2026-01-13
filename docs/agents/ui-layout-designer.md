# UI Layout Designer Notes

## Goals
- Define window layout and component arrangement
- Specify responsive behavior for resizing

## Constraints
- Single window; 800x600 minimum; resizable

## Assumptions
- A13: Tab-based navigation sufficient for MVP scope

## Artifacts
- **Main Window**: 800x600 default, min 640x480
- **Layout**:
  - Top: Toolbar (Add Account, Sync Now, Settings)
  - Left: TabControl (Accounts | Status | Logs | Settings)
  - Center: Content area (ListView or details)
  - Bottom: StatusBar (sync status, last sync time)
- **Accounts Tab**: ListView with columns: Account Email, Calendars (count), Last Sync, Status
- **Status Tab**: Per-calendar sync status, progress bars during sync
- **Logs Tab**: DataGridView with Date, Account, Action, Result; filter controls

## Acceptance Criteria
- [ ] Layout accommodates all MVP features
- [ ] Resize behavior maintains usability

## Status
Done
