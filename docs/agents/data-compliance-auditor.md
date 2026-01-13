# Data Compliance Auditor Notes

## Goals
- Verify compliance with Google API ToS and data handling requirements
- Ensure proper OAuth scope usage and data retention

## Constraints
- Google Calendar API ToS; no excessive data caching

## Assumptions
- A19: Users consent to calendar data sync via OAuth prompt

## Artifacts
### Compliance Review
1. **PASS**: OAuth scopes limited to calendar.v3 (minimal required)
2. **PASS**: No persistent caching of event content beyond sync state
3. **PASS**: User can revoke access (Remove Account clears tokens)
4. **INFO**: Google API ToS requires attribution - add "Powered by Google" if displaying Google data
5. **PASS**: No sharing of user data with third parties

### Data Flow
- Google → Local (StateStore: IDs, hashes, timestamps only)
- Google → Outlook (event data via COM)
- No cloud storage; all data local

## Acceptance Criteria
- [x] Google API ToS compliance verified
- [x] Data retention policy defined (local only, user-controlled)

## Status
Done (PASS)
