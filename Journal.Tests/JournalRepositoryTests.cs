using Journal.Data;
using Journal.Services;
using Xunit;

namespace Journal.Tests
{
    public class JournalRepositoryTests : IAsyncLifetime
    {
        private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"journal-test-{Guid.NewGuid():N}.db3");
        private JournalDbContext _dbContext = null!;
        private JournalRepository _repository = null!;

        static JournalRepositoryTests()
        {
            SQLitePCL.Batteries_V2.Init();
        }

        public async Task InitializeAsync()
        {
            _dbContext = new JournalDbContext(_dbPath);
            await _dbContext.OpenAsync("test-key-0123456789abcdef");
            _repository = new JournalRepository(_dbContext);
        }

        public async Task DisposeAsync()
        {
            await _dbContext.CloseAsync();
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
        }

        [Fact]
        public async Task UpsertAsync_NewDate_CreatesEntry()
        {
            var date = new DateTime(2026, 1, 15);

            var entry = await _repository.UpsertAsync(date, "First day", "Started the journal.");

            Assert.NotEqual(0, entry.Id);
            Assert.Equal("First day", entry.Title);
        }

        [Fact]
        public async Task UpsertAsync_SameDateTwice_UpdatesInPlace()
        {
            var date = new DateTime(2026, 1, 16);

            await _repository.UpsertAsync(date, "Original", "Original content");
            await _repository.UpsertAsync(date, "Updated", "Updated content");

            var stored = await _repository.GetByDateAsync(date);
            var all = await _repository.GetAllSummariesAsync();

            Assert.Equal("Updated", stored!.Title);
            Assert.Single(all);
        }

        [Fact]
        public async Task GetByDateAsync_NoEntry_ReturnsNull()
        {
            var entry = await _repository.GetByDateAsync(new DateTime(2026, 2, 1));

            Assert.Null(entry);
        }

        [Fact]
        public async Task DeleteAsync_ExistingEntry_RemovesIt()
        {
            var date = new DateTime(2026, 1, 20);
            await _repository.UpsertAsync(date, "To delete", "content");

            await _repository.DeleteAsync(date);

            var stored = await _repository.GetByDateAsync(date);
            Assert.Null(stored);
        }

        [Fact]
        public async Task GetAllSummariesAsync_MultipleEntries_OrderedByDateDescending()
        {
            await _repository.UpsertAsync(new DateTime(2026, 1, 1), "Old", "content");
            await _repository.UpsertAsync(new DateTime(2026, 1, 5), "New", "content");

            var summaries = await _repository.GetAllSummariesAsync();

            Assert.Equal(2, summaries.Count);
            Assert.Equal("New", summaries[0].Title);
        }
    }
}
