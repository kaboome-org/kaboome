namespace KaboomeBackend.Models
{
    /// <summary>
    /// Contains both the Google Account Name and the Google Calendar Id
    /// </summary>
    public record GoogleCalendarPath
    {
        public string GoogleAccountId { get; set; }
        public string GoogleCalendarId { get; set; }
    }
}
