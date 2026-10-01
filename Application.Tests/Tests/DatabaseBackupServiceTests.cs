using Application.Services;
using Application.Settings;
using Domain.Interfaces;
using Domain.Models.Backup;

namespace Application.Tests.Tests
{
    public class DatabaseBackupServiceTests
    {
        private readonly FakeBackupStorage _storage = new();
        private readonly DatabaseBackupService _service;

        public DatabaseBackupServiceTests()
        {
            _service = new DatabaseBackupService(_storage, new BackupSettings(TimeSpan.FromHours(24), keepCount: 30));
        }

        [Fact]
        public void IsBackupDue_NoBackups_ReturnsTrue()
        {
            Assert.True(_service.IsBackupDue());
        }

        [Fact]
        public void IsBackupDue_BackupYoungerThanInterval_ReturnsFalse()
        {
            _storage.LastBackup = new BackupFile("last.dump", DateTime.Now.AddHours(-12));

            Assert.False(_service.IsBackupDue());
        }

        [Fact]
        public void IsBackupDue_BackupOlderThanInterval_ReturnsTrue()
        {
            _storage.LastBackup = new BackupFile("last.dump", DateTime.Now.AddHours(-25));

            Assert.True(_service.IsBackupDue());
        }

        [Fact]
        public async Task BackupAsync_CreatesBackupThenDeletesOldOnes()
        {
            BackupFile backup = await _service.BackupAsync();

            Assert.Equal("new.dump", backup.Path);
            Assert.Equal(["create", "delete old, keep 30"], _storage.Calls);
        }

        [Fact]
        public async Task BackupAsync_CreateFailed_KeepsOldBackups()
        {
            _storage.CreateError = new InvalidOperationException("pg_dump exited with code 1");

            await Assert.ThrowsAsync<InvalidOperationException>(_service.BackupAsync);

            Assert.Equal(["create"], _storage.Calls);
        }

        private class FakeBackupStorage : IDatabaseBackupStorage
        {
            public BackupFile? LastBackup { get; set; }
            public Exception? CreateError { get; set; }
            public List<string> Calls { get; } = [];

            public BackupFile? GetLastBackup()
            {
                return LastBackup;
            }

            public Task<BackupFile> CreateBackupAsync()
            {
                Calls.Add("create");

                return CreateError == null
                    ? Task.FromResult(new BackupFile("new.dump", DateTime.Now))
                    : Task.FromException<BackupFile>(CreateError);
            }

            public void DeleteOldBackups(int keepCount)
            {
                Calls.Add($"delete old, keep {keepCount}");
            }
        }
    }
}
