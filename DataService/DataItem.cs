namespace DataService
{
    public class DataItem
    {
        public string Id { get; set; } = string.Empty;
        public string Info { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
