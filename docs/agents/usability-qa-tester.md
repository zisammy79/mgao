# Usability QA Tester Notes

## Goals
- Validate user flows against UX specifications
- Identify friction points in primary tasks

## Constraints
- Test against defined flows (Add Account, Sync, View Logs)

## Assumptions
- A22: Testing performed via code review; manual testing deferred to M6

## Artifacts
### Flow Validation
1. **Add Account**: Button → OAuth popup → Select calendars (NOT IMPLEMENTED - missing calendar selection UI)
2. **Sync Now**: Button → Progress → Status update → ✓ Working as specified
3. **View Logs**: Tab → Grid with filter → Export → ✓ Working (filter not implemented)
4. **Remove Account**: Select → Confirm → Delete → ✓ Working

### Issues Found
- **BLOCKER**: Calendar selection UI missing after OAuth; all calendars sync by default
- **MEDIUM**: Log filtering not implemented (filter controls missing)

### Recommendations
- Add calendar selection dialog post-OAuth
- Add filter dropdowns to logs tab

## Acceptance Criteria
- [ ] All primary flows complete without errors
- [ ] BLOCKER: Calendar selection must be added

## Status
Done (BLOCKER found - calendar selection missing)
