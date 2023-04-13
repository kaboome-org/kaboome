namespace KaboomeBackend.Models
{
    using Newtonsoft.Json;

    public class KaboomeEvent : KaboomeEventIn, IMyCouchDocument
    {
        public string _id { get; set; }
        public string _rev { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? _deleted { get; set; }
    }
}
