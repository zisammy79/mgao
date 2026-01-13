# Code Fixer Notes

## Goals
- Fix BLOCKER: Add calendar selection dialog
- Address code review issues

## Constraints
- Minimal changes to unblock MVP

## Assumptions
- A25: Calendar selection dialog shows after successful OAuth

## Artifacts
### Fixes Applied (PATCH blocks below)
1. **CalendarSelectionForm.cs** - New dialog for selecting calendars to sync
2. **MainForm.cs** - Integrate calendar selection after OAuth
3. **StateStore.cs** - Add GetAllAccountIds implementation

## Acceptance Criteria
- [ ] Calendar selection dialog implemented
- [ ] User can choose which calendars to sync

## Status
Done (patches applied)
