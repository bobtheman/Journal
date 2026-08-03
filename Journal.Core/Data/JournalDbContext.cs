using Journal.Models;
using SQLite;
using System.Linq;

namespace Journal.Data
{
    public class JournalDbContext
    {
        private readonly string _dbPath;
        private SQLiteAsyncConnection? _connection;

        public JournalDbContext(string dbPath)
        {
            _dbPath = dbPath;
        }

        public string DbPath => _dbPath;

        public bool DatabaseFileExists => File.Exists(_dbPath);

        public SQLiteAsyncConnection Connection =>
            _connection ?? throw new InvalidOperationException("Database is not open. Call OpenAsync first.");

        public async Task OpenAsync(string key)
        {
            var options = new SQLiteConnectionString(_dbPath, storeDateTimeAsTicks: true, key: key);
            var connection = new SQLiteAsyncConnection(options);

            // Throws SQLiteException if the key is wrong for an existing encrypted file.
            await connection.ExecuteScalarAsync<int>("SELECT count(*) FROM sqlite_master");
            await connection.CreateTableAsync<JournalEntry>();
            await connection.CreateTableAsync<JournalEntryImage>();
            await DropObsoleteEntryDateUniqueIndexAsync(connection);

            _connection = connection;
        }

        // Multiple entries per day used to be blocked by a unique index on EntryDate.
        // CreateTableAsync only adds missing indexes/columns, it never drops one that's no
        // longer declared on the model, so existing installs still carry it - drop it here.
        private static async Task DropObsoleteEntryDateUniqueIndexAsync(SQLiteAsyncConnection connection)
        {
            try
            {
                var indexes = await connection.QueryAsync<IndexListRow>("PRAGMA index_list('JournalEntry')");
                foreach (var index in indexes.Where(i => i.unique))
                {
                    var columns = await connection.QueryAsync<IndexInfoRow>($"PRAGMA index_info('{index.name}')");
                    if (columns.Any(c => c.name == nameof(JournalEntry.EntryDate)))
                    {
                        await connection.ExecuteAsync($"DROP INDEX IF EXISTS \"{index.name}\"");
                    }
                }
            }
            catch (Exception)
            {
                // Best-effort migration; a fresh install never had the index to begin with.
            }
        }

        private class IndexListRow
        {
            public string name { get; set; } = string.Empty;
            public bool unique { get; set; }
        }

        private class IndexInfoRow
        {
            public string name { get; set; } = string.Empty;
        }

        public async Task RekeyAsync(string newKey)
        {
            await Connection.ExecuteAsync($"PRAGMA rekey = '{newKey}'");
        }

        public async Task CloseAsync()
        {
            if (_connection is not null)
            {
                await _connection.CloseAsync();
                _connection = null;
            }
        }
    }
}
