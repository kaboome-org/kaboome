namespace KaboomeBackend.Models
{
    internal interface IMyCouchDocument
    {
        /// <summary>
        /// CouchDB internal id
        /// </summary>
        public string _id { get; set; }
        /// <summary>
        /// CouchDB internal revision number
        /// </summary>
        public string _rev { get; set; }
    }
}
