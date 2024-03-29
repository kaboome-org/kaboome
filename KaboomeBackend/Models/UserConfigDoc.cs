namespace KaboomeBackend.Models
{
    /// <summary>
    /// For every external account there is a UserConfigDoc
    /// It saves data needed for syncing
    /// The superclass UserConfigDocIn should be used when writing new docs into CouchDB
    /// </summary>
    public class UserConfigDoc : UserConfigDocIn, IMyCouchDocument
    {
        /// <summary>
        /// In the form of <code>$"{externalService}-{accountName}"</code>
        /// <example>For example: "google-user@gmail.com"</example> 
        /// <inheritdoc cref="IMyCouchDocument._id"/>
        /// </summary>
        public string _id { get; set; }
        /// <inheritdoc cref="IMyCouchDocument._rev"/>
        public string _rev { get; set; }
    }
}
