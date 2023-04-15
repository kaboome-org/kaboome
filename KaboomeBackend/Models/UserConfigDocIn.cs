namespace KaboomeBackend.Models
{
    using Newtonsoft.Json;

    /// <inheritdoc cref="UserConfigDoc"/>
    public class UserConfigDocIn
    {
        /// <summary>
        /// Google Accounts can have multiple calendars. They can be individually configured in kaboome
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<GoogleCalendarConfig>? GoogleCalendarConfigs { get; set; }

    }
}
