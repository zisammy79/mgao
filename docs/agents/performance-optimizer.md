# Performance Optimizer Notes

## Goals
- Identify performance bottlenecks in sync operations
- Recommend optimizations for API and COM interactions

## Constraints
- Target <5s incremental sync for typical calendar (50-100 events)

## Assumptions
- A20: Network latency ~100-300ms per API call; batch where possible

## Artifacts
### Bottlenecks Identified
1. **OutlookCalendarBridge.FindAppointmentByGoogleId** - O(n) scan; use filter or index
2. **GoogleCalendarClient.GetEventsAsync** - No batch support for multiple calendars
3. **SyncEngine** - Sequential sync; could parallelize across accounts

### Recommendations
1. Add Outlook item filter: `[UserProperties('MGAOGoogleEventId')] = 'xyz'`
2. Use Google Batch API for multiple calendar fetches
3. Parallelize account syncs with SemaphoreSlim throttling
4. Add incremental sync skip: if syncToken valid and no changes, skip full diff

### Metrics Target
- Initial sync (500 events): <30s
- Incremental sync (10 changes): <5s

## Acceptance Criteria
- [ ] Critical bottleneck (O(n) search) has documented fix
- [ ] Batch API usage recommended

## Edge Cases
- Large calendars (1000+ events): paginate, stream results
- Slow COM: minimize Interop calls, cache folder references

## Status
Done (optimizations documented for builder)
