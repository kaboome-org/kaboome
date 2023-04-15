namespace KaboomeBackend.Models
{
    /// <summary>
    /// For every external account there is a UserSecretDoc
    /// It saves data needed for authentication and authorization with the third party
    /// The superclass UserSecretDocIn should be used when writing new docs into CouchDB
    /// </summary>
    public class UserSecretDoc : UserSecretDocIn, IMyCouchDocument
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
