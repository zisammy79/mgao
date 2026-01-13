# Design System Architect Notes

## Goals
- Define minimal design tokens for consistent Windows desktop UI
- Establish component patterns for utility-focused app

## Constraints
- Use native Windows controls; minimal custom styling

## Assumptions
- A11: WinForms or WPF with system theme; no custom theming MVP

## Artifacts
- **Colors**: System colors + Google blue (#4285F4), Outlook blue (#0078D4), Error red, Success green
- **Typography**: Segoe UI; sizes: 9pt (small), 11pt (body), 14pt (header)
- **Spacing**: 4px base unit; margins 8/16/24px
- **Components**: ListView (accounts/calendars), StatusBar, TabControl, Button, TextBox, ProgressBar

## Acceptance Criteria
- [ ] Design tokens enable consistent UI without custom framework
- [ ] Native look and feel preserved

## Status
Done
