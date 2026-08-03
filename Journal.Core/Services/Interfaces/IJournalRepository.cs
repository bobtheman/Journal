using Journal.Models;

namespace Journal.Services.Interfaces
{
    public interface IJournalRepository
    {
        Task<JournalEntry?> GetByIdAsync(int id);

        Task<List<JournalEntrySummary>> GetAllSummariesAsync();

        Task<JournalEntry> UpsertAsync(int? id, DateTime entryDate, string title, string content, int? mood);

        Task DeleteAsync(int id);

        Task DeleteAllAsync();

        Task<List<JournalEntryImage>> GetImagesAsync(int entryId);

        Task<JournalEntryImage> AddImageAsync(int entryId, byte[] imageData, string imageMimeType);

        Task DeleteImageAsync(int imageId);
    }
}
