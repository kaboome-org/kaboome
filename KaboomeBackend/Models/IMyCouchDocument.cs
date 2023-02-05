namespace KaboomeBackend.Models
{
    internal interface IMyCouchDocument
    {
        public string _id { get; set; }
        public string _rev { get; set; }
    }
}
