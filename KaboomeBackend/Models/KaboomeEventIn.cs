namespace KaboomeBackend.Models
{
    public class KaboomeEventIn
    {
        public string? Description { get; set; }
        public long EndTimestamp { get; set; }
        public ExternalEvent ReadWriteExternalEvent { get; set; } = new();
        public List<ExternalEvent> WriteOnlyExternalEvents { get; set; } = new();
        public long StartTimestamp { get; set; }
        public string? Title { get; set; }
    }
}
