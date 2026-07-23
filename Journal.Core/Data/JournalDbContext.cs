using Journal.Models;
using SQLite;

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

            _connection = connection;
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
