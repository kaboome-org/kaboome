namespace KaboomeBackend.Models
{
    internal class UserConfigDoc : UserConfigDocIn, IMyCouchDocument
    {
        public string _id { get; set; }
        public string _rev { get; set; }
    }
}
