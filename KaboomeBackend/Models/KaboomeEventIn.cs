namespace KaboomeBackend.Models
{
    public class KaboomeEventIn<T>
    {
        public string? Description { get; set; }
        public long EndTimestamp { get; set; }
        public T innerData { get; set; }
        public long StartTimestamp { get; set; }
        public string? Title { get; set; }
    }
}
