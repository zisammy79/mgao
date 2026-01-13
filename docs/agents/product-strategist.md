# Product Strategist Notes

## Goals
- Define MVP scope boundaries for multi-account Google Calendar sync
- Establish success metrics and user value proposition

## Constraints
- MVP-first; no mail sync, no multi-profile

## Assumptions
- A4: Primary users are power users managing 2-5 Google Workspace calendars
- A5: Users accept "latest-modified-wins" conflict resolution

## Artifacts
- **MVP Features**: Multi-account OAuth, calendar selection UI, two-way sync (-30d/+180d), incremental sync, conflict logging
- **User Personas**: IT admins, consultants with multiple client accounts, small business owners
- **Success Metrics**: <5s per incremental sync, zero data loss, <3 clicks to add account
- **Out-of-scope**: Complex recurrence exceptions (MVP supports daily/weekly/monthly; exceptions logged but not fully round-tripped)

## Acceptance Criteria
- [ ] MVP scope is implementable in single milestone
- [ ] Clear boundary for recurrence/attendee handling documented

## Status
Done
