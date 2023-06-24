namespace KaboomeBackend.Models
{
    using Google.Apis.Calendar.v3.Data;
    using Newtonsoft.Json;
    /// <summary>
    /// Container class that can contain any third party event and information needed to change it
    /// </summary>
    public class ExternalEvent
    {
        /// <summary>
        /// Google Event
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Event? Google { get; set; }
        /// <inheritdoc cref="Models.GoogleCalendarPath"/>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public GoogleCalendarPath? GoogleCalendarPath { get; set; }
        /// <inheritdoc cref="Models.SyncType"/>
        public SyncType SyncType { get; set; } = SyncType.ReadWrite;
    }
}
