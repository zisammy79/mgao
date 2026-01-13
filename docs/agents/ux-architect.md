# UX Architect Notes

## Goals
- Define user flows for account management and sync operations
- Ensure minimal friction for primary tasks

## Constraints
- Max 3 clicks for any primary action

## Assumptions
- A12: Users configure once, monitor occasionally

## Artifacts
- **Flow 1 - Add Account**: Main → "Add Account" button → OAuth popup → Select calendars → Done
- **Flow 2 - Sync**: Automatic on interval OR manual "Sync Now" button → Progress indicator → Status update
- **Flow 3 - View Logs**: Main → Logs tab → Filter by account/date → Export button
- **States**: Idle, Syncing, Error, Success (with timestamps)
- **Feedback**: StatusBar shows last sync time; errors highlighted in red

## Acceptance Criteria
- [ ] All primary flows achievable in ≤3 clicks
- [ ] Clear feedback for sync state and errors

## Status
Done
