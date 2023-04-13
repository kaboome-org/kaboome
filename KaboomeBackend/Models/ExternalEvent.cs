namespace KaboomeBackend.Models
{
    using Google.Apis.Calendar.v3.Data;
    using Newtonsoft.Json;

    public class ExternalEvent
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Event? Google { get; set; }
    }
}
