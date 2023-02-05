namespace KaboomeBackend.Options
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public class KaboomeOptions
    {
        public Uri CouchDbUri { get; set; }
        public string CouchDbAdminUsername { get; set; }
        public string CouchDbAdminPassword { get; set; }
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
}
