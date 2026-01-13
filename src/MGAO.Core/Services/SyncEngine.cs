using MGAO.Core.Interfaces;

namespace MGAO.Core.Services;

public class SyncEngine : ISyncEngine
{
    private readonly ICalendarProvider _googleProvider;
    private readonly ICalendarProvider _outlookProvider;
    private readonly StateStore _stateStore;
    private readonly int _syncWindowDaysPast;
    private readonly int _syncWindowDaysFuture;

    public event EventHandler<SyncProgressEventArgs>? ProgressChanged;

    public SyncEngine(
        ICalendarProvider googleProvider,
        ICalendarProvider outlookProvider,
        StateStore stateStore,
        int syncWindowDaysPast = 30,
        int syncWindowDaysFuture = 180)
    {
        _googleProvider = googleProvider;
        _outlookProvider = outlookProvider;
        _stateStore = stateStore;
        _syncWindowDaysPast = syncWindowDaysPast;
        _syncWindowDaysFuture = syncWindowDaysFuture;
    }

    public async Task<SyncResult> SyncAsync(string accountId, string calendarId, CancellationToken ct = default)
    {
        int created = 0, updated = 0, deleted = 0, conflicts = 0;

        try
        {
            ReportProgress(accountId, calendarId, "Starting sync...", 0);

            var syncToken = await _stateStore.GetSyncTokenAsync(accountId, calendarId);
            var start = DateTime.UtcNow.AddDays(-_syncWindowDaysPast);
            var end = DateTime.UtcNow.AddDays(_syncWindowDaysFuture);

            ReportProgress(accountId, calendarId, "Fetching Google events...", 20);
            var googleEvents = await _googleProvider.GetEventsAsync(accountId, calendarId, start, end, syncToken);

            ReportProgress(accountId, calendarId, "Fetching Outlook events...", 40);
            var outlookEvents = await _outlookProvider.GetEventsAsync(accountId, calendarId, start, end);

            var googleDict = googleEvents.ToDictionary(e => e.Id);
            var outlookDict = outlookEvents
                .Where(e => e.SourceId != null)
                .ToDictionary(e => e.SourceId!);

            ReportProgress(accountId, calendarId, "Processing changes...", 60);

            foreach (var gEvt in googleEvents)
            {
                ct.ThrowIfCancellationRequested();

                if (!outlookDict.TryGetValue(gEvt.Id, out var oEvt))
                {
                    await _outlookProvider.CreateEventAsync(accountId, calendarId,
                        gEvt with { SourceId = gEvt.Id });
                    created++;
                }
                else if (gEvt.LastModified > oEvt.LastModified)
                {
                    await _outlookProvider.UpdateEventAsync(accountId, calendarId,
                        gEvt with { Id = oEvt.Id, SourceId = gEvt.Id });
                    updated++;
                }
                else if (oEvt.LastModified > gEvt.LastModified)
                {
                    await _googleProvider.UpdateEventAsync(accountId, calendarId, oEvt with { Id = gEvt.Id });
                    updated++;
                }
            }

            ReportProgress(accountId, calendarId, "Updating sync token...", 90);
            var newSyncToken = await _googleProvider.GetSyncTokenAsync(accountId, calendarId);
            if (newSyncToken != null)
            {
                await _stateStore.SaveSyncTokenAsync(accountId, calendarId, newSyncToken);
            }

            ReportProgress(accountId, calendarId, "Sync complete", 100);
            return new SyncResult(true, created, updated, deleted, conflicts);
        }
        catch (System.Exception ex)
        {
            return new SyncResult(false, created, updated, deleted, conflicts, ex.Message);
        }
    }

    public async Task<SyncResult> SyncAllAsync(CancellationToken ct = default)
    {
        int totalCreated = 0, totalUpdated = 0, totalDeleted = 0, totalConflicts = 0;
        string? lastError = null;

        var calendars = await _stateStore.GetAllCalendars();

        foreach (var (accountId, calendarId) in calendars)
        {
            ct.ThrowIfCancellationRequested();
            var result = await SyncAsync(accountId, calendarId, ct);

            totalCreated += result.Created;
            totalUpdated += result.Updated;
            totalDeleted += result.Deleted;
            totalConflicts += result.Conflicts;

            if (!result.Success) lastError = result.Error;
        }

        return new SyncResult(
            lastError == null,
            totalCreated, totalUpdated, totalDeleted, totalConflicts,
            lastError);
    }

    private void ReportProgress(string accountId, string calendarId, string status, int percent)
    {
        ProgressChanged?.Invoke(this, new SyncProgressEventArgs
        {
            AccountId = accountId,
            CalendarId = calendarId,
            Status = status,
            PercentComplete = percent
        });
    }
}

