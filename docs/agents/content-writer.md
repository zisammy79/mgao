# Content Writer Notes

## Goals
- Define UI text, labels, and error messages
- Ensure clarity and consistency in user-facing copy

## Constraints
- Technical audience; concise language

## Assumptions
- A14: English-only for MVP

## Artifacts
- **Labels**: "Add Account", "Remove", "Sync Now", "Calendars", "Last Sync", "Status", "Logs", "Export"
- **Status Messages**: "Syncing...", "Sync complete", "Last sync: [timestamp]", "Error: [details]"
- **Error Messages**:
  - "Authentication failed. Please re-authorize."
  - "Calendar not found. It may have been deleted."
  - "Sync conflict: [event] modified in both locations. Latest version kept."
  - "Rate limit exceeded. Retry in [X] seconds."
- **Tooltips**: "Click to add a Google account", "Manually trigger sync for all calendars"

## Acceptance Criteria
- [ ] All UI elements have clear, actionable labels
- [ ] Error messages guide user to resolution

## Status
Done
