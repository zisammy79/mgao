# Architecture Planner Notes

## Goals
- Define implementation milestones for MVP delivery
- Sequence work to enable incremental testing

## Constraints
- No time estimates; focus on dependencies and order

## Assumptions
- A10: Single developer workflow; milestones are sequential

## Artifacts
### Milestones
1. **M1-Foundation**: Project setup, Google OAuth flow, token storage (DPAPI)
2. **M2-GoogleClient**: Calendar list API, Event CRUD, syncToken handling
3. **M3-OutlookBridge**: COM interop, folder creation, event mapping, UserProperties
4. **M4-SyncEngine**: Bidirectional sync logic, conflict resolution, loop prevention
5. **M5-UI**: Account management, calendar selection, sync status, logs viewer
6. **M6-Polish**: Error handling, logging, diagnostics export, installer

### Dependencies
- M2 depends on M1 (auth required)
- M3 can parallel M2 (mock data)
- M4 depends on M2+M3
- M5 depends on M4
- M6 depends on M5

## Acceptance Criteria
- [ ] Milestones are atomic and testable
- [ ] Critical path identified: M1→M2→M4→M5→M6

## Status
Done
