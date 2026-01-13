using System.Security.Cryptography;
using System.Text;
using Microsoft.Office.Interop.Outlook;
using MGAO.Core.Interfaces;
using OlItemType = Microsoft.Office.Interop.Outlook.OlItemType;

namespace MGAO.Outlook;

public class OutlookCalendarBridge : ICalendarProvider, IDisposable
{
    private Application? _outlook;
    private NameSpace? _namespace;
    private readonly Dictionary<string, MAPIFolder> _folderCache = new();

    private const string PropGoogleEventId = "MGAOGoogleEventId";
    private const string PropGoogleCalendarId = "MGAOGoogleCalendarId";
    private const string PropAccountId = "MGAOAccountId";
    private const string PropContentHash = "MGAOContentHash";

    public void Initialize()
    {
        _outlook = new Application();
        _namespace = _outlook.GetNamespace("MAPI");
    }

    public MAPIFolder GetOrCreateFolder(string accountId, string calendarId, string calendarName)
    {
        var key = $"{accountId}|{calendarId}";
        if (_folderCache.TryGetValue(key, out var cached)) return cached;

        var calendarRoot = _namespace!.GetDefaultFolder(OlDefaultFolders.olFolderCalendar);
        var folderName = $"G:{calendarName}";

        MAPIFolder? folder = null;
        foreach (MAPIFolder f in calendarRoot.Folders)
        {
            if (f.Name == folderName)
            {
                folder = f;
                break;
            }
        }

        folder ??= calendarRoot.Folders.Add(folderName, OlDefaultFolders.olFolderCalendar);
        _folderCache[key] = folder;
        return folder;
    }

    public Task<IEnumerable<CalendarInfo>> GetCalendarsAsync(string accountId)
    {
        var calendars = new List<CalendarInfo>();
        var root = _namespace!.GetDefaultFolder(OlDefaultFolders.olFolderCalendar);

        foreach (MAPIFolder folder in root.Folders)
        {
            if (folder.Name.StartsWith("G:"))
            {
                calendars.Add(new CalendarInfo(folder.EntryID, folder.Name, accountId));
            }
        }

        return Task.FromResult<IEnumerable<CalendarInfo>>(calendars);
    }

    public Task<IEnumerable<CalendarEvent>> GetEventsAsync(string accountId, string calendarId,
        DateTime start, DateTime end, string? syncToken = null)
    {
        var folder = GetFolderByEntryId(calendarId);
        if (folder == null) return Task.FromResult(Enumerable.Empty<CalendarEvent>());

        var events = new List<CalendarEvent>();
        var filter = $"[Start] >= '{start:g}' AND [End] <= '{end:g}'";

        var items = folder.Items;
        items.Sort("[Start]");
        items.IncludeRecurrences = true;

        var restricted = items.Restrict(filter);
        foreach (object item in restricted)
        {
            if (item is AppointmentItem appt)
            {
                events.Add(MapToCalendarEvent(appt));
            }
        }

        return Task.FromResult<IEnumerable<CalendarEvent>>(events);
    }

    public Task<CalendarEvent> CreateEventAsync(string accountId, string calendarId, CalendarEvent evt)
    {
        var folder = GetFolderByEntryId(calendarId);
        var appt = (AppointmentItem)folder!.Items.Add(OlItemType.olAppointmentItem);

        MapToAppointment(evt, appt);
        SetUserProperty(appt, PropGoogleEventId, evt.SourceId ?? evt.Id);
        SetUserProperty(appt, PropAccountId, accountId);
        SetUserProperty(appt, PropContentHash, ComputeHash(evt));

        appt.Save();

        return Task.FromResult(evt with { Id = appt.EntryID });
    }

    public Task<CalendarEvent> UpdateEventAsync(string accountId, string calendarId, CalendarEvent evt)
    {
        var folder = GetFolderByEntryId(calendarId);
        var appt = FindAppointmentByGoogleId(folder!, evt.SourceId ?? evt.Id);

        if (appt != null)
        {
            MapToAppointment(evt, appt);
            SetUserProperty(appt, PropContentHash, ComputeHash(evt));
            appt.Save();
        }

        return Task.FromResult(evt);
    }

    public Task DeleteEventAsync(string accountId, string calendarId, string eventId)
    {
        var folder = GetFolderByEntryId(calendarId);
        var appt = FindAppointmentByGoogleId(folder!, eventId);
        appt?.Delete();
        return Task.CompletedTask;
    }

    public Task<string?> GetSyncTokenAsync(string accountId, string calendarId) =>
        Task.FromResult<string?>(null);

    private MAPIFolder? GetFolderByEntryId(string entryId)
    {
        try { return _namespace!.GetFolderFromID(entryId); }
        catch { return null; }
    }

    private AppointmentItem? FindAppointmentByGoogleId(MAPIFolder folder, string googleEventId)
    {
        foreach (object item in folder.Items)
        {
            if (item is AppointmentItem appt)
            {
                var stored = GetUserProperty(appt, PropGoogleEventId);
                if (stored == googleEventId) return appt;
            }
        }
        return null;
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
        _folderCache.Clear();
        _namespace = null;
        _outlook?.Quit();
        _outlook = null;
    }
}
