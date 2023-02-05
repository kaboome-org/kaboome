namespace KaboomeBackend.Couch
{
    using Newtonsoft.Json;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public partial class CouchSessionResponse
    {
        [JsonProperty("ok")]
        public bool Ok { get; set; }

        [JsonProperty("userCtx")]
        public UserCtx UserCtx { get; set; }

        [JsonProperty("info")]
        public Info Info { get; set; }
    }

    public partial class Info
    {
        [JsonProperty("authentication_handlers")]
        public string[] AuthenticationHandlers { get; set; }
    }

    public partial class UserCtx
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("roles")]
        public string[] Roles { get; set; }
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
}
