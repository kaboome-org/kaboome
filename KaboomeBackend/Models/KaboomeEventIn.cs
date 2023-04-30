namespace KaboomeBackend.Models
{
    public class KaboomeEventIn
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        /// <summary>
        /// If this external event is changed, we sync the changes back into kaboome. If this KaboomeEvent is changed we also push the chagnes into the external calendar.
        /// The event is always synced in full detail.
        /// </summary>
        public ExternalEvent ReadWriteExternalEvent { get; set; } = new();
        /// <summary>
        /// Write only copies of this KaboomeEvent in external calendars. Mainly used to communicate, that you are not available during the time of this event.
        /// How much detail is written can be configured. We only push changes to these events.
        /// If they are moved, we don't do anything to the original kaboomeEvent and overwrite the changes in the external calendar if this KaboomeEvent is changed again.
        /// </summary>
        public List<ExternalEvent> WriteOnlyExternalEvents { get; set; } = new();
        public long StartTimestamp { get; set; }
        public long EndTimestamp { get; set; }
        public string? RRule { get; set; }
        public List<long> ExDates { get; set; } = new();
    }
}
