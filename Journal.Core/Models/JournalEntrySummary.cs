namespace Journal.Models
{
    public class JournalEntrySummary
    {
        public int Id { get; set; }

        public DateTime EntryDate { get; set; }

        public string Title { get; set; } = string.Empty;

        public int? Mood { get; set; }

        public bool HasImage { get; set; }

        public DateTime ModifiedUtc { get; set; }
    }
}
