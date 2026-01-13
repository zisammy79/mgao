using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using MGAO.Core.Interfaces;

namespace MGAO.GoogleCalendar;

public class GoogleCalendarClient : ICalendarProvider
{
    private readonly GoogleAuthService _authService;
    private readonly Dictionary<string, CalendarService> _services = new();

    public GoogleCalendarClient(GoogleAuthService authService)
    {
        _authService = authService;
    }

    private async Task<CalendarService> GetServiceAsync(string accountId)
    {
        if (!_services.TryGetValue(accountId, out var service))
        {
            var credential = await _authService.AuthorizeAsync(accountId);
            service = new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "MGAO"
            });
            _services[accountId] = service;
        }
        return service;
    }

    public async Task<IEnumerable<CalendarInfo>> GetCalendarsAsync(string accountId)
    {
        var service = await GetServiceAsync(accountId);
        var list = await service.CalendarList.List().ExecuteAsync();
        return list.Items.Select(c => new CalendarInfo(c.Id, c.Summary, accountId, c.TimeZone));
    }

    public async Task<IEnumerable<CalendarEvent>> GetEventsAsync(string accountId, string calendarId,
        DateTime start, DateTime end, string? syncToken = null)
    {
        var service = await GetServiceAsync(accountId);
        var request = service.Events.List(calendarId);

        if (!string.IsNullOrEmpty(syncToken))
        {
            request.SyncToken = syncToken;
        }
        else
        {
            request.TimeMin = start;
            request.TimeMax = end;
        }

        request.SingleEvents = false;
        request.MaxResults = 2500;

        var events = new List<CalendarEvent>();
        string? pageToken = null;

        do
        {
            request.PageToken = pageToken;
            var response = await request.ExecuteAsync();

            foreach (var evt in response.Items ?? Enumerable.Empty<Event>())
            {
                if (evt.Status == "cancelled") continue;
                events.Add(MapToCalendarEvent(evt, accountId));
            }

            pageToken = response.NextPageToken;
        } while (!string.IsNullOrEmpty(pageToken));

        return events;
    }

    public async Task<CalendarEvent> CreateEventAsync(string accountId, string calendarId, CalendarEvent evt)
    {
        var service = await GetServiceAsync(accountId);
        var googleEvent = MapToGoogleEvent(evt);
        var created = await service.Events.Insert(googleEvent, calendarId).ExecuteAsync();
        return MapToCalendarEvent(created, accountId);
    }

    public async Task<CalendarEvent> UpdateEventAsync(string accountId, string calendarId, CalendarEvent evt)
    {
        var service = await GetServiceAsync(accountId);
        var googleEvent = MapToGoogleEvent(evt);
        var updated = await service.Events.Update(googleEvent, calendarId, evt.Id).ExecuteAsync();
        return MapToCalendarEvent(updated, accountId);
    }

    public async Task DeleteEventAsync(string accountId, string calendarId, string eventId)
    {
        var service = await GetServiceAsync(accountId);
        await service.Events.Delete(calendarId, eventId).ExecuteAsync();
    }

    public async Task<string?> GetSyncTokenAsync(string accountId, string calendarId)
    {
        var service = await GetServiceAsync(accountId);
        var request = service.Events.List(calendarId);
        request.MaxResults = 1;

        string? nextSyncToken = null;
        string? pageToken = null;
        do
        {
            request.PageToken = pageToken;
            var response = await request.ExecuteAsync();
            nextSyncToken = response.NextSyncToken;
            pageToken = response.NextPageToken;
        } while (!string.IsNullOrEmpty(pageToken));

        return nextSyncToken;
    }

    private static CalendarEvent MapToCalendarEvent(Event evt, string accountId)
    {
        var start = evt.Start?.DateTime ?? DateTime.Parse(evt.Start?.Date ?? DateTime.Now.ToString("yyyy-MM-dd"));
        var end = evt.End?.DateTime ?? DateTime.Parse(evt.End?.Date ?? DateTime.Now.ToString("yyyy-MM-dd"));
        var isAllDay = evt.Start?.DateTime == null;
        var lastMod = evt.Updated ?? DateTime.UtcNow;

        RecurrenceRule? recurrence = null;
        if (evt.Recurrence?.Count > 0)
        {
            recurrence = ParseRRule(evt.Recurrence.FirstOrDefault());
        }

        return new CalendarEvent(
            evt.Id,
            evt.Summary ?? "(No Title)",
            start,
            end,
            evt.Start?.TimeZone,
            evt.Description,
            evt.Location,
            isAllDay,
            recurrence,
            lastMod);
    }

    private static Event MapToGoogleEvent(CalendarEvent evt)
    {
        var googleEvent = new Event
        {
            Id = evt.Id,
            Summary = evt.Subject,
            Description = evt.Description,
            Location = evt.Location
        };

        if (evt.IsAllDay)
        {
            googleEvent.Start = new EventDateTime { Date = evt.Start.ToString("yyyy-MM-dd") };
            googleEvent.End = new EventDateTime { Date = evt.End.ToString("yyyy-MM-dd") };
        }
        else
        {
            googleEvent.Start = new EventDateTime { DateTime = evt.Start, TimeZone = evt.TimeZone };
            googleEvent.End = new EventDateTime { DateTime = evt.End, TimeZone = evt.TimeZone };
        }

        return googleEvent;
    }

    private static RecurrenceRule? ParseRRule(string? rrule)
    {
        if (string.IsNullOrEmpty(rrule) || !rrule.StartsWith("RRULE:")) return null;

        var parts = rrule[6..].Split(';').ToDictionary(
            p => p.Split('=')[0],
            p => p.Split('=').Length > 1 ? p.Split('=')[1] : "");

        var freq = parts.GetValueOrDefault("FREQ", "DAILY");
        var interval = int.TryParse(parts.GetValueOrDefault("INTERVAL", "1"), out var i) ? i : 1;
        DateTime? until = parts.TryGetValue("UNTIL", out var u) && DateTime.TryParse(u, out var d) ? d : null;
        int? count = parts.TryGetValue("COUNT", out var c) && int.TryParse(c, out var cnt) ? cnt : null;
        var byDay = parts.GetValueOrDefault("BYDAY");

        return new RecurrenceRule(freq, interval, until, count, byDay);
    }
}
