using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Office.Interop.Outlook;
using MGAO.Core.Interfaces;
using OlItemType = Microsoft.Office.Interop.Outlook.OlItemType;
using OutlookApp = Microsoft.Office.Interop.Outlook.Application;

namespace MGAO.Outlook;

public class OutlookCalendarBridge : ICalendarProvider, IDisposable
{
    private OutlookApp? _outlook;
    private NameSpace? _namespace;
    private readonly Dictionary<string, MAPIFolder> _folderCache = new();

    private const string PropGoogleEventId = "MGAOGoogleEventId";
    private const string PropGoogleCalendarId = "MGAOGoogleCalendarId";
    private const string PropAccountId = "MGAOAccountId";
    private const string PropContentHash = "MGAOContentHash";

    public void Initialize()
    {
        _outlook = new OutlookApp();
        _namespace = _outlook.GetNamespace("MAPI");
    }

    public MAPIFolder GetOrCreateFolder(string accountId, string calendarId, string calendarName)
    {
        var key = $"{accountId}|{calendarId}";
        if (_folderCache.TryGetValue(key, out var cached)) return cached;

        var calendarRoot = _namespace!.GetDefaultFolder(OlDefaultFolders.olFolderCalendar);
        var folderName = $"G:{calendarName}";

        MAPIFolder? folder = null;
        var folders = calendarRoot.Folders;
        try
        {
            foreach (MAPIFolder f in folders)
            {
                if (f.Name == folderName)
                {
                    folder = f;
                }
                else
                {
                    ReleaseCom(f);
                }
            }
        }
        finally
        {
            ReleaseCom(folders);
            ReleaseCom(calendarRoot);
        }

        folder ??= _namespace!.GetDefaultFolder(OlDefaultFolders.olFolderCalendar)
            .Folders.Add(folderName, OlDefaultFolders.olFolderCalendar);
        _folderCache[key] = folder;
        return folder;
    }

    public Task<IEnumerable<CalendarInfo>> GetCalendarsAsync(string accountId)
    {
        var calendars = new List<CalendarInfo>();
        var root = _namespace!.GetDefaultFolder(OlDefaultFolders.olFolderCalendar);
        var folders = root.Folders;
        try
        {
            foreach (MAPIFolder folder in folders)
            {
                try
                {
                    if (folder.Name.StartsWith("G:"))
                    {
                        calendars.Add(new CalendarInfo(folder.EntryID, folder.Name, accountId));
                    }
                }
                finally
                {
                    ReleaseCom(folder);
                }
            }
        }
        finally
        {
            ReleaseCom(folders);
            ReleaseCom(root);
        }

        return Task.FromResult<IEnumerable<CalendarInfo>>(calendars);
    }

    public Task<IEnumerable<CalendarEvent>> GetEventsAsync(string accountId, string calendarId,
        DateTime start, DateTime end, string? syncToken = null)
    {
        var folder = GetFolderByEntryId(calendarId);
        if (folder == null) return Task.FromResult(Enumerable.Empty<CalendarEvent>());

        var events = new List<CalendarEvent>();
        var filter = $"[Start] >= '{start:yyyy-MM-dd}' AND [End] <= '{end:yyyy-MM-dd}'";

        Items? items = null;
        Items? restricted = null;
        try
        {
            items = folder.Items;
            items.Sort("[Start]");
            items.IncludeRecurrences = true;
            restricted = items.Restrict(filter);

            foreach (object item in restricted)
            {
                if (item is AppointmentItem appt)
                {
                    try
                    {
                        events.Add(MapToCalendarEvent(appt));
                    }
                    finally
                    {
                        ReleaseCom(appt);
                    }
                }
                else
                {
                    ReleaseCom(item);
                }
            }
        }
        finally
        {
            ReleaseCom(restricted);
            ReleaseCom(items);
            ReleaseCom(folder);
        }

        return Task.FromResult<IEnumerable<CalendarEvent>>(events);
    }

    public Task<CalendarEvent> CreateEventAsync(string accountId, string calendarId, CalendarEvent evt)
    {
        var folder = GetFolderByEntryId(calendarId);
        if (folder == null) throw new InvalidOperationException($"Calendar folder not found: {calendarId}");

        Items? items = null;
        AppointmentItem? appt = null;
        try
        {
            items = folder.Items;
            appt = (AppointmentItem)items.Add(OlItemType.olAppointmentItem);

            MapToAppointment(evt, appt);
            SetUserProperty(appt, PropGoogleEventId, evt.SourceId ?? evt.Id);
            SetUserProperty(appt, PropAccountId, accountId);
            SetUserProperty(appt, PropContentHash, ComputeHash(evt));

            appt.Save();
            var entryId = appt.EntryID;
            return Task.FromResult(evt with { Id = entryId });
        }
        finally
        {
            ReleaseCom(appt);
            ReleaseCom(items);
            ReleaseCom(folder);
        }
    }

    public Task<CalendarEvent> UpdateEventAsync(string accountId, string calendarId, CalendarEvent evt)
    {
        var folder = GetFolderByEntryId(calendarId);
        if (folder == null) return Task.FromResult(evt);

        AppointmentItem? appt = null;
        try
        {
            appt = FindAppointmentByGoogleId(folder, evt.SourceId ?? evt.Id);
            if (appt != null)
            {
                MapToAppointment(evt, appt);
                SetUserProperty(appt, PropContentHash, ComputeHash(evt));
                appt.Save();
            }
            return Task.FromResult(evt);
        }
        finally
        {
            ReleaseCom(appt);
            ReleaseCom(folder);
        }
    }

    public Task DeleteEventAsync(string accountId, string calendarId, string eventId)
    {
        var folder = GetFolderByEntryId(calendarId);
        if (folder == null) return Task.CompletedTask;

        AppointmentItem? appt = null;
        try
        {
            appt = FindAppointmentByGoogleId(folder, eventId);
            appt?.Delete();
            return Task.CompletedTask;
        }
        finally
        {
            ReleaseCom(appt);
            ReleaseCom(folder);
        }
    }

    public Task<string?> GetSyncTokenAsync(string accountId, string calendarId) =>
        Task.FromResult<string?>(null);

    private MAPIFolder? GetFolderByEntryId(string entryId)
    {
        try
        {
            return _namespace!.GetFolderFromID(entryId);
        }
        catch (COMException ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetFolderByEntryId failed for {entryId}: {ex.Message}");
            return null;
        }
    }

    private AppointmentItem? FindAppointmentByGoogleId(MAPIFolder folder, string googleEventId)
    {
        Items? items = null;
        try
        {
            items = folder.Items;
            foreach (object item in items)
            {
                if (item is AppointmentItem appt)
                {
                    var stored = GetUserProperty(appt, PropGoogleEventId);
                    if (stored == googleEventId)
                    {
                        return appt; // Caller is responsible for releasing
                    }
                    ReleaseCom(appt);
                }
                else
                {
                    ReleaseCom(item);
                }
            }
            return null;
        }
        finally
        {
            ReleaseCom(items);
        }
    }

    private static CalendarEvent MapToCalendarEvent(AppointmentItem appt)
    {
        return new CalendarEvent(
            appt.EntryID,
            appt.Subject,
            appt.Start,
            appt.End,
            null,
            appt.Body,
            appt.Location,
            appt.AllDayEvent,
            null,
            appt.LastModificationTime);
    }

    private static void MapToAppointment(CalendarEvent evt, AppointmentItem appt)
    {
        appt.Subject = evt.Subject;
        appt.Start = evt.Start;
        appt.End = evt.End;
        appt.Body = evt.Description ?? "";
        appt.Location = evt.Location ?? "";
        appt.AllDayEvent = evt.IsAllDay;
    }

    private static void SetUserProperty(AppointmentItem appt, string name, string value)
    {
        var prop = appt.UserProperties.Find(name) ??
                   appt.UserProperties.Add(name, OlUserPropertyType.olText);
        prop.Value = value;
    }

    private static string? GetUserProperty(AppointmentItem appt, string name)
    {
        var prop = appt.UserProperties.Find(name);
        return prop?.Value as string;
    }

    private static string ComputeHash(CalendarEvent evt)
    {
        var data = $"{evt.Subject}|{evt.Start:O}|{evt.End:O}|{evt.LastModified:O}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }

    public void Dispose()
    {
        foreach (var folder in _folderCache.Values)
        {
            ReleaseCom(folder);
        }
        _folderCache.Clear();

        if (_namespace != null)
        {
            ReleaseCom(_namespace);
            _namespace = null;
        }

        if (_outlook != null)
        {
            try { _outlook.Quit(); } catch { /* Ignore quit errors */ }
            ReleaseCom(_outlook);
            _outlook = null;
        }

        GC.SuppressFinalize(this);
    }

    private static void ReleaseCom(object? obj)
    {
        if (obj == null) return;
        try
        {
            Marshal.ReleaseComObject(obj);
        }
        catch
        {
            // Ignore release errors
        }
    }
}
