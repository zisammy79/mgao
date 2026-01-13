# Discovery Agent Notes

## Goals
- Map technical landscape: Outlook COM API + Google Calendar API integration feasibility
- Identify existing patterns for multi-account OAuth in desktop Windows apps

## Constraints
- No repo scanning; proposing initial structure

## Assumptions
- A1: Target is classic Outlook (COM-based, not new Outlook)
- A2: Users have Google Workspace accounts with Calendar API enabled
- A3: Windows 10/11 with .NET Framework 4.7.2+ or .NET 6+

## Artifacts
- **Tech Stack**: C# / .NET, Outlook Interop (Microsoft.Office.Interop.Outlook), Google.Apis.Calendar.v3
- **Auth**: Google OAuth 2.0 desktop flow (installed app), PKCE recommended
- **Storage**: DPAPI for token encryption, UserProperties for event metadata
- **Proposed Structure**: `/src/MGAO.Core`, `/src/MGAO.Outlook`, `/src/MGAO.Google`, `/src/MGAO.UI`

## Acceptance Criteria
- [ ] Tech stack confirmed viable for two-way sync
- [ ] No blocking incompatibilities identified

## Status
Done
