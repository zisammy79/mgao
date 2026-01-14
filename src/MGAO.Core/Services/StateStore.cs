using Microsoft.Data.Sqlite;

namespace MGAO.Core.Services;

public class StateStore : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _disposed;

    public StateStore(string? dbPath = null)
    {
        var path = dbPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MGAO", "state.db");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        _connection = new SqliteConnection($"Data Source={path}");
        _connection.Open();
        InitializeSchema();
    }

    private void InitializeSchema()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS SyncState (
                AccountId TEXT NOT NULL,
                CalendarId TEXT NOT NULL,
                GoogleSyncToken TEXT,
                LastSync TEXT,
                PRIMARY KEY (AccountId, CalendarId)
            );
            CREATE TABLE IF NOT EXISTS EventMapping (
                AccountId TEXT NOT NULL,
                CalendarId TEXT NOT NULL,
                GoogleEventId TEXT NOT NULL,
                OutlookEntryId TEXT NOT NULL,
                ContentHash TEXT,
                LastModified TEXT,
                PRIMARY KEY (AccountId, CalendarId, GoogleEventId)
            );
            """;
        cmd.ExecuteNonQuery();
    }

    public async Task SaveSyncTokenAsync(string accountId, string calendarId, string? syncToken)
    {
        await _lock.WaitAsync();
        try
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = """
                INSERT OR REPLACE INTO SyncState (AccountId, CalendarId, GoogleSyncToken, LastSync)
                VALUES (@aid, @cid, @token, @now)
                """;
            cmd.Parameters.AddWithValue("@aid", accountId);
            cmd.Parameters.AddWithValue("@cid", calendarId);
            cmd.Parameters.AddWithValue("@token", syncToken ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("O"));
            await cmd.ExecuteNonQueryAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<string?> GetSyncTokenAsync(string accountId, string calendarId)
    {
        await _lock.WaitAsync();
        try
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT GoogleSyncToken FROM SyncState WHERE AccountId = @aid AND CalendarId = @cid";
            cmd.Parameters.AddWithValue("@aid", accountId);
            cmd.Parameters.AddWithValue("@cid", calendarId);
            var result = await cmd.ExecuteScalarAsync();
            return result as string;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveEventMappingAsync(string accountId, string calendarId, string googleEventId,
        string outlookEntryId, string contentHash)
    {
        await _lock.WaitAsync();
        try
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = """
                INSERT OR REPLACE INTO EventMapping (AccountId, CalendarId, GoogleEventId, OutlookEntryId, ContentHash, LastModified)
                VALUES (@aid, @cid, @gid, @oid, @hash, @now)
                """;
            cmd.Parameters.AddWithValue("@aid", accountId);
            cmd.Parameters.AddWithValue("@cid", calendarId);
            cmd.Parameters.AddWithValue("@gid", googleEventId);
            cmd.Parameters.AddWithValue("@oid", outlookEntryId);
            cmd.Parameters.AddWithValue("@hash", contentHash);
            cmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("O"));
            await cmd.ExecuteNonQueryAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<string?> GetOutlookEntryIdAsync(string accountId, string calendarId, string googleEventId)
    {
        await _lock.WaitAsync();
        try
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT OutlookEntryId FROM EventMapping WHERE AccountId = @aid AND CalendarId = @cid AND GoogleEventId = @gid";
            cmd.Parameters.AddWithValue("@aid", accountId);
            cmd.Parameters.AddWithValue("@cid", calendarId);
            cmd.Parameters.AddWithValue("@gid", googleEventId);
            var result = await cmd.ExecuteScalarAsync();
            return result as string;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IEnumerable<string>> GetAllAccountIds()
    {
        await _lock.WaitAsync();
        try
        {
            var accounts = new List<string>();
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT DISTINCT AccountId FROM SyncState";
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                accounts.Add(reader.GetString(0));
            }
            return accounts;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IEnumerable<(string AccountId, string CalendarId)>> GetAllCalendars()
    {
        await _lock.WaitAsync();
        try
        {
            var calendars = new List<(string, string)>();
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT AccountId, CalendarId FROM SyncState";
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                calendars.Add((reader.GetString(0), reader.GetString(1)));
            }
            return calendars;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _connection.Dispose();
        _lock.Dispose();
        GC.SuppressFinalize(this);
    }
}
