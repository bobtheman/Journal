using SQLite;

namespace Journal.Models
{
    public class JournalEntryImage
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int JournalEntryId { get; set; }

        public byte[] ImageData { get; set; } = [];

        public string ImageMimeType { get; set; } = string.Empty;

        public DateTime CreatedUtc { get; set; }
    }
}
