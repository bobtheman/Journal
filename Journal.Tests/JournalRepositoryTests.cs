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
        public async Task UpsertAsync_NewEntry_CreatesEntry()
        {
            var date = new DateTime(2026, 1, 15, 10, 0, 0);

            var entry = await _repository.UpsertAsync(null, date, "First day", "Started the journal.", null);

            Assert.NotEqual(0, entry.Id);
            Assert.Equal("First day", entry.Title);
        }

        [Fact]
        public async Task UpsertAsync_SameIdTwice_UpdatesInPlace()
        {
            var date = new DateTime(2026, 1, 16, 9, 0, 0);

            var created = await _repository.UpsertAsync(null, date, "Original", "Original content", null);
            await _repository.UpsertAsync(created.Id, date, "Updated", "Updated content", null);

            var stored = await _repository.GetByIdAsync(created.Id);
            var all = await _repository.GetAllSummariesAsync();

            Assert.Equal("Updated", stored!.Title);
            Assert.Single(all);
        }

        [Fact]
        public async Task GetByIdAsync_NoEntry_ReturnsNull()
        {
            var entry = await _repository.GetByIdAsync(999);

            Assert.Null(entry);
        }

        [Fact]
        public async Task DeleteAsync_ExistingEntry_RemovesIt()
        {
            var date = new DateTime(2026, 1, 20, 12, 0, 0);
            var created = await _repository.UpsertAsync(null, date, "To delete", "content", null);

            await _repository.DeleteAsync(created.Id);

            var stored = await _repository.GetByIdAsync(created.Id);
            Assert.Null(stored);
        }

        [Fact]
        public async Task GetAllSummariesAsync_MultipleEntries_OrderedByDateDescending()
        {
            await _repository.UpsertAsync(null, new DateTime(2026, 1, 1), "Old", "content", null);
            await _repository.UpsertAsync(null, new DateTime(2026, 1, 5), "New", "content", null);

            var summaries = await _repository.GetAllSummariesAsync();

            Assert.Equal(2, summaries.Count);
            Assert.Equal("New", summaries[0].Title);
        }

        [Fact]
        public async Task UpsertAsync_SameDayDifferentTimes_PersistsBothEntries()
        {
            var morning = new DateTime(2026, 1, 10, 10, 0, 0);
            var afternoon = new DateTime(2026, 1, 10, 15, 30, 0);

            await _repository.UpsertAsync(null, morning, "Morning", "content", 1);
            await _repository.UpsertAsync(null, afternoon, "Afternoon", "content", 4);

            var summaries = await _repository.GetAllSummariesAsync();

            Assert.Equal(2, summaries.Count);
        }

        [Fact]
        public async Task AddImageAsync_MultipleImages_AllPersistForEntry()
        {
            var entry = await _repository.UpsertAsync(null, new DateTime(2026, 1, 12), "With photos", "content", null);

            await _repository.AddImageAsync(entry.Id, [1, 2, 3], "image/jpeg");
            await _repository.AddImageAsync(entry.Id, [4, 5, 6], "image/png");

            var images = await _repository.GetImagesAsync(entry.Id);
            var summaries = await _repository.GetAllSummariesAsync();

            Assert.Equal(2, images.Count);
            Assert.True(summaries.Single().HasImage);
        }

        [Fact]
        public async Task DeleteImageAsync_ExistingImage_RemovesOnlyThatImage()
        {
            var entry = await _repository.UpsertAsync(null, new DateTime(2026, 1, 13), "With photos", "content", null);
            var image1 = await _repository.AddImageAsync(entry.Id, [1, 2, 3], "image/jpeg");
            await _repository.AddImageAsync(entry.Id, [4, 5, 6], "image/png");

            await _repository.DeleteImageAsync(image1.Id);

            var images = await _repository.GetImagesAsync(entry.Id);
            Assert.Single(images);
        }

        [Fact]
        public async Task DeleteAsync_EntryWithImages_RemovesImagesToo()
        {
            var entry = await _repository.UpsertAsync(null, new DateTime(2026, 1, 14), "With photos", "content", null);
            await _repository.AddImageAsync(entry.Id, [1, 2, 3], "image/jpeg");

            await _repository.DeleteAsync(entry.Id);

            var images = await _repository.GetImagesAsync(entry.Id);
            Assert.Empty(images);
        }
    }
}
