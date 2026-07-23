namespace Journal.Models
{
    public class JournalEntrySummary
    {
        public DateTime EntryDate { get; set; }

        public string Title { get; set; } = string.Empty;

        public int? Mood { get; set; }

        public DateTime ModifiedUtc { get; set; }
    }
}
