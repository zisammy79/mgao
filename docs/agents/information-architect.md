# Information Architect Notes

## Goals
- Define UI navigation structure and data hierarchy
- Map user flows for account management and sync

## Constraints
- Minimal UI; single-window app with tabs/panels

## Assumptions
- A7: Users interact infrequently (setup + occasional status check)

## Artifacts
- **Primary Views**: Accounts List, Calendar Selection, Sync Status, Logs
- **Navigation**: Tab-based or sidebar (Accounts | Sync | Logs | Settings)
- **Data Hierarchy**: Account → Calendars → Events (not shown in UI, only status)
- **User Flows**: Add Account → Select Calendars → Enable Sync → View Status

## Acceptance Criteria
- [ ] All MVP features accessible within 2 clicks from main view
- [ ] Clear account-calendar relationship visible

## Status
Done
