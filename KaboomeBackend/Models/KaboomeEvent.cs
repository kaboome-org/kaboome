namespace KaboomeBackend.Models
{
    using Newtonsoft.Json;

    public class KaboomeEvent : KaboomeEventIn, IMyCouchDocument
    {
        /// <summary>
        /// In the form of <code>$"{externalService}-{externalEventId}"</code> if there is an <see cref="KaboomeEventIn.ReadWriteExternalEvent"/>.
        /// In the form of <code>$"kaboome-{creationTimestamp}"</code> for internal events.
        /// <inheritdoc cref="IMyCouchDocument._id"/>
        /// </summary>
        public string _id { get; set; }
        /// <inheritdoc cref="IMyCouchDocument._rev"/>
        public string _rev { get; set; }
        /// <summary>
        /// This flag is set if this event is deleted. CouchDb handles it this way, to sync the deletion.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? _deleted { get; set; }
    }
}
