namespace KaboomeBackend.Models
{
    using Newtonsoft.Json;

    /// <inheritdoc cref="UserSecretDoc"/>
    public class UserSecretDocIn
    {
        /// <summary>
        /// Googles refresh token. Used to get temporary auth tokens
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? GoogleRefreshToken { get; set; }
    }
}
