namespace KaboomeBackend.Models
{
    /// <summary>
    /// Config for a calendar sync. Only used by the frontend
    /// </summary>
    public class CalendarSyncConfig
    {
        /// <summary>
        /// The vendor (for example "google" or "kaboome")
        /// </summary>
        public string Vendor { get; set; } = "";
        /// <summary>
        /// The calendar path (for example a GoogleCalendarPath) serialized as a JSON
        /// </summary>
        public string VendorCalendarPathJson { get; set; } = "";
        /// <inheritdoc cref="Models.SyncType"/>
        public SyncType SyncType { get; set; }
    }
}
