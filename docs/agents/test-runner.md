# Test Runner Notes

## Goals
- Define test plan and commands for validating MVP
- Specify unit tests, integration tests, and manual smoke tests

## Constraints
- Must run on Windows with Outlook installed for full integration tests

## Assumptions
- A24: xUnit for unit tests; manual verification for COM/Outlook integration

## Artifacts
### Test Plan
#### Unit Tests (Automated)
```bash
dotnet test src/MGAO.Core.Tests
```
- `DpapiTokenStoreTests`: Save/Get/Delete token round-trip
- `StateStoreTests`: SaveSyncToken, GetSyncToken, EventMapping CRUD
- `SyncEngineTests`: Mock providers, verify sync logic

#### Integration Tests (Requires Google OAuth)
```bash
# Set credentials first
export MGAO_CLIENT_ID=xxx
export MGAO_CLIENT_SECRET=xxx
dotnet test src/MGAO.Google.Tests
```
- `GoogleCalendarClientTests`: List calendars, CRUD events (sandbox account)

#### Manual Smoke Tests
1. Launch app → Verify UI renders
2. Add Account → Complete OAuth → Verify account appears
3. Sync Now → Verify events appear in Outlook
4. Create event in Outlook → Sync → Verify appears in Google
5. Remove Account → Verify tokens deleted

### Expected Outputs
- Unit tests: All pass
- Integration: Requires sandbox Google account
- Smoke: Manual verification checklist

## Acceptance Criteria
- [ ] Test plan covers all MVP features
- [ ] Commands documented for CI/CD

## Status
Done (test plan defined; test files to be created by code-fixer)
