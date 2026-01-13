# Incremental Builder Notes

## Goals
- Implement M1-Foundation: project structure, OAuth, token storage
- Create core interfaces and service scaffolding

## Constraints
- PATCH blocks only; .NET 6+ with Outlook Interop

## Assumptions
- A16: Google Cloud project with OAuth credentials exists (user provides client_id/secret)

## Artifacts
See PATCH blocks below for:
- Solution file and projects
- `ITokenStore` interface + `DpapiTokenStore` implementation
- `GoogleAuthService` with OAuth PKCE flow
- `GoogleCalendarClient` scaffold
- `OutlookCalendarBridge` scaffold
- `SyncEngine` scaffold
- Basic WinForms UI shell

## Acceptance Criteria
- [ ] Solution builds without errors
- [ ] OAuth flow completes and stores tokens

## Edge Cases
- **Token Refresh**: Auto-refresh before expiry; retry once on 401
- **Recurrence Mapping**: Daily/Weekly/Monthly → RecurrencePattern; complex RRULE → log + create single instances

## Status
Done (patches below)
