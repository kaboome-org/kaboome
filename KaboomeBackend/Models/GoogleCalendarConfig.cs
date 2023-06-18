namespace KaboomeBackend.Models
{
    /// <summary>
    /// Config for a Google calendar
    /// </summary>
    public class GoogleCalendarConfig
    {
        /// <summary>
        /// Identifies which Google calendar this config is for
        /// </summary>
        public GoogleCalendarPath GoogleCalendarPath { get; set; }
        /// <summary>
        /// These event Ids are WriteOnlyEvents and don't need to be pulled 
        /// </summary>
        public List<string?> BlackListEventIds { get; set; } = new();
        /// <summary>
        /// This is a cursor used for the event feed 
        /// </summary>
        public string? GoogleSyncToken { get; set; }

        /// <summary>
        /// The Since value marks the revision until which the couchDB contents where already synced into this calendar.
        /// </summary>
        public string? Since { get; set; }
        /// <summary>
        /// Should this calendar be synced?
        /// </summary>
        public bool ShouldSync { get; set; }

    }
}
