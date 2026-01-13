# Code Reviewer Notes

## Goals
- Review implemented code for correctness and maintainability
- Identify bugs, missing error handling, and code quality issues

## Constraints
- Review M1-M5 scaffolds only; no scope expansion

## Assumptions
- A17: Code builds but not yet tested on Windows with Outlook installed

## Artifacts
### Issues Found
1. **GoogleCalendarClient.cs:45** - Missing `try-catch` for network errors in `GetEventsAsync`
2. **OutlookCalendarBridge.cs:92** - `FindAppointmentByGoogleId` iterates all items; needs index or filter
3. **SyncEngine.cs:85** - `StateStoreExtensions.GetAllAccountIds` returns empty; needs implementation
4. **MainForm.cs:52** - Potential null reference if `_authService` init fails silently

### Recommendations
- Add exponential backoff for Google API calls
- Add COM exception handling in OutlookCalendarBridge
- Implement proper logging framework (Serilog or similar)

## Acceptance Criteria
- [ ] No critical bugs blocking MVP functionality
- [ ] Error handling covers primary failure modes

## Edge Cases
- COM stability: Marshal.ReleaseComObject after use; catch COMException
- Network failures: Retry with backoff; log failures

## Status
Done (issues logged for code-fixer)
