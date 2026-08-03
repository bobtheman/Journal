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

        public Task<JournalEntry?> GetByIdAsync(int id)
        {
            return _dbContext.Connection.Table<JournalEntry>()
                .Where(e => e.Id == id)
                .FirstOrDefaultAsync()!;
        }

        public async Task<List<JournalEntrySummary>> GetAllSummariesAsync()
        {
            var entries = await _dbContext.Connection.Table<JournalEntry>()
                .OrderByDescending(e => e.EntryDate)
                .ToListAsync();

            var entryIdsWithImages = (await _dbContext.Connection.Table<JournalEntryImage>().ToListAsync())
                .Select(i => i.JournalEntryId)
                .ToHashSet();

            return entries.Select(e => new JournalEntrySummary
            {
                Id = e.Id,
                EntryDate = e.EntryDate,
                Title = e.Title,
                Mood = e.Mood,
                HasImage = entryIdsWithImages.Contains(e.Id),
                ModifiedUtc = e.ModifiedUtc
            }).ToList();
        }

        public async Task<JournalEntry> UpsertAsync(int? id, DateTime entryDate, string title, string content, int? mood)
        {
            var existing = id.HasValue ? await GetByIdAsync(id.Value) : null;
            var now = DateTime.UtcNow;

            if (existing is null)
            {
                var entry = new JournalEntry
                {
                    EntryDate = entryDate,
                    Title = title,
                    Content = content,
                    Mood = mood,
                    CreatedUtc = now,
                    ModifiedUtc = now
                };
                await _dbContext.Connection.InsertAsync(entry);
                return entry;
            }

            existing.EntryDate = entryDate;
            existing.Title = title;
            existing.Content = content;
            existing.Mood = mood;
            existing.ModifiedUtc = now;
            await _dbContext.Connection.UpdateAsync(existing);
            return existing;
        }

        public async Task DeleteAsync(int id)
        {
            var existing = await GetByIdAsync(id);
            if (existing is not null)
            {
                await _dbContext.Connection.DeleteAsync(existing);
            }

            await _dbContext.Connection.Table<JournalEntryImage>()
                .Where(i => i.JournalEntryId == id)
                .DeleteAsync();
        }

        public async Task DeleteAllAsync()
        {
            await _dbContext.Connection.DeleteAllAsync<JournalEntry>();
            await _dbContext.Connection.DeleteAllAsync<JournalEntryImage>();
        }

        public Task<List<JournalEntryImage>> GetImagesAsync(int entryId)
        {
            return _dbContext.Connection.Table<JournalEntryImage>()
                .Where(i => i.JournalEntryId == entryId)
                .OrderBy(i => i.CreatedUtc)
                .ToListAsync();
        }

        public async Task<JournalEntryImage> AddImageAsync(int entryId, byte[] imageData, string imageMimeType)
        {
            var image = new JournalEntryImage
            {
                JournalEntryId = entryId,
                ImageData = imageData,
                ImageMimeType = imageMimeType,
                CreatedUtc = DateTime.UtcNow
            };
            await _dbContext.Connection.InsertAsync(image);
            return image;
        }

        public async Task DeleteImageAsync(int imageId)
        {
            await _dbContext.Connection.DeleteAsync<JournalEntryImage>(imageId);
        }
    }
}
