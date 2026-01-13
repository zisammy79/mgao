# Motion Designer Notes

## Goals
- Define minimal animations/transitions for feedback
- Keep motion functional, not decorative

## Constraints
- Windows desktop; native controls; minimal custom animation

## Assumptions
- A15: Standard Windows animations sufficient (no custom motion)

## Artifacts
- **Sync Progress**: Native ProgressBar animation (marquee during unknown duration)
- **Status Updates**: Fade-in for status messages (if WPF); instant update if WinForms
- **Button Feedback**: Standard Windows pressed state
- **No Custom Motion**: MVP uses native control animations only

## Acceptance Criteria
- [ ] User receives visual feedback during operations
- [ ] No jarring transitions or missing feedback

## Status
Done
