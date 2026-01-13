namespace MGAO.Core.Interfaces;

public interface ISyncEngine
{
    Task<SyncResult> SyncAsync(string accountId, string calendarId, CancellationToken ct = default);
    Task<SyncResult> SyncAllAsync(CancellationToken ct = default);
    event EventHandler<SyncProgressEventArgs>? ProgressChanged;
}

public record SyncResult(
    bool Success,
    int Created,
    int Updated,
    int Deleted,
    int Conflicts,
    string? Error = null);

public class SyncProgressEventArgs : EventArgs
{
    public string AccountId { get; init; } = "";
    public string CalendarId { get; init; } = "";
    public string Status { get; init; } = "";
    public int PercentComplete { get; init; }
}
