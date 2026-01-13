namespace MGAO.Core.Interfaces;

public interface ICalendarProvider
{
    Task<IEnumerable<CalendarInfo>> GetCalendarsAsync(string accountId);
    Task<IEnumerable<CalendarEvent>> GetEventsAsync(string accountId, string calendarId, DateTime start, DateTime end, string? syncToken = null);
    Task<CalendarEvent> CreateEventAsync(string accountId, string calendarId, CalendarEvent evt);
    Task<CalendarEvent> UpdateEventAsync(string accountId, string calendarId, CalendarEvent evt);
    Task DeleteEventAsync(string accountId, string calendarId, string eventId);
    Task<string?> GetSyncTokenAsync(string accountId, string calendarId);
}

public record CalendarInfo(string Id, string Name, string AccountId, string? TimeZone = null);

public record CalendarEvent(
    string Id,
    string Subject,
    DateTime Start,
    DateTime End,
    string? TimeZone,
    string? Description,
    string? Location,
    bool IsAllDay,
    RecurrenceRule? Recurrence,
    DateTime LastModified,
    string? SourceId = null);

public record RecurrenceRule(string Type, int Interval, DateTime? Until, int? Count, string? DaysOfWeek);
