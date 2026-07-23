using Journal.Models;

namespace Journal.Services.Interfaces
{
    public interface IJournalRepository
    {
        Task<JournalEntry?> GetByDateAsync(DateTime date);

        Task<List<JournalEntrySummary>> GetAllSummariesAsync();

        Task<JournalEntry> UpsertAsync(DateTime date, string title, string content, int? mood);

        Task DeleteAsync(DateTime date);

        Task DeleteAllAsync();
    }
}
