using Journal.Data;
using Journal.Models;
using Journal.Services.Interfaces;

namespace Journal.Services
{
    public class JournalRepository : IJournalRepository
    {
        private readonly JournalDbContext _dbContext;

        public JournalRepository(JournalDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<JournalEntry?> GetByDateAsync(DateTime date)
        {
            var day = date.Date;
            return _dbContext.Connection.Table<JournalEntry>()
                .Where(e => e.EntryDate == day)
                .FirstOrDefaultAsync()!;
        }

        public async Task<List<JournalEntrySummary>> GetAllSummariesAsync()
        {
            var entries = await _dbContext.Connection.Table<JournalEntry>()
                .OrderByDescending(e => e.EntryDate)
                .ToListAsync();

            return entries.Select(e => new JournalEntrySummary
            {
                EntryDate = e.EntryDate,
                Title = e.Title,
                Mood = e.Mood,
                ModifiedUtc = e.ModifiedUtc
            }).ToList();
        }

        public async Task<JournalEntry> UpsertAsync(DateTime date, string title, string content, int? mood)
        {
            var day = date.Date;
            var existing = await GetByDateAsync(day);
            var now = DateTime.UtcNow;

            if (existing is null)
            {
                var entry = new JournalEntry
                {
                    EntryDate = day,
                    Title = title,
                    Content = content,
                    Mood = mood,
                    CreatedUtc = now,
                    ModifiedUtc = now
                };
                await _dbContext.Connection.InsertAsync(entry);
                return entry;
            }

            existing.Title = title;
            existing.Content = content;
            existing.Mood = mood;
            existing.ModifiedUtc = now;
            await _dbContext.Connection.UpdateAsync(existing);
            return existing;
        }

        public async Task DeleteAsync(DateTime date)
        {
            var existing = await GetByDateAsync(date);
            if (existing is not null)
            {
                await _dbContext.Connection.DeleteAsync(existing);
            }
        }

        public async Task DeleteAllAsync()
        {
            await _dbContext.Connection.DeleteAllAsync<JournalEntry>();
        }
    }
}
