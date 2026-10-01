using Domain.Models.Backup;
using Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Tests.Tests
{
    public class DatabaseBackupStorageTests : IDisposable
    {
        private static readonly DateTime Now = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

        private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        public DatabaseBackupStorageTests()
        {
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }

        [Fact]
        public void GetLastBackup_NoBackupsDirectory_ReturnsNull()
        {
            DatabaseBackupStorage storage = new(NullLogger<DatabaseBackupStorage>.Instance, "", Path.Combine(_tempDir, "Missing"));

            Assert.Null(storage.GetLastBackup());
        }

        [Fact]
        public void GetLastBackup_OnlyUnfinishedDump_ReturnsNull()
        {
            CreateFile("20261001_110000.dump.tmp", Now.AddHours(-1));

            Assert.Null(CreateStorage().GetLastBackup());
        }

        [Fact]
        public void GetLastBackup_SeveralDumps_ReturnsNewestIgnoringUnfinished()
        {
            CreateFile("20260930_000000.dump", Now.AddDays(-1));
            CreateFile("20261001_000000.dump", Now.AddHours(-12));
            CreateFile("20261001_110000.dump.tmp", Now.AddHours(-1));

            BackupFile? lastBackup = CreateStorage().GetLastBackup();

            Assert.NotNull(lastBackup);
            Assert.Equal(Path.Combine(_tempDir, "20261001_000000.dump"), lastBackup.Path);
            Assert.Equal(Now.AddHours(-12).ToLocalTime(), lastBackup.CreatedAt);
        }

        [Fact]
        public void DeleteOldBackups_MoreThanKeepCount_KeepsNewest()
        {
            CreateFile("1.dump", Now.AddDays(-4));
            CreateFile("2.dump", Now.AddDays(-3));
            CreateFile("3.dump", Now.AddDays(-2));
            CreateFile("4.dump", Now.AddDays(-1));

            CreateStorage().DeleteOldBackups(keepCount: 2);

            Assert.Equal(["3.dump", "4.dump"], GetFileNames());
        }

        [Fact]
        public void DeleteUnfinishedBackups_RemovesTempFilesOnly()
        {
            CreateFile("20261001_000000.dump", Now.AddHours(-1));
            CreateFile("20261001_010000.dump.tmp", Now.AddHours(-1));

            CreateStorage().DeleteUnfinishedBackups();

            Assert.Equal(["20261001_000000.dump"], GetFileNames());
        }

        [Fact]
        public void FindPgDump_SeveralVersions_ReturnsNewestWithPgDump()
        {
            CreatePostgresVersion("9.6", withPgDump: true);
            CreatePostgresVersion("10", withPgDump: true);
            CreatePostgresVersion("18", withPgDump: true);
            CreatePostgresVersion("19", withPgDump: false);

            string pgDump = DatabaseBackupStorage.FindPgDump(_tempDir);

            Assert.Equal(Path.Combine(_tempDir, "PostgreSQL", "18", "bin", "pg_dump.exe"), pgDump);
        }

        [Fact]
        public void FindPgDump_NoPostgres_ReturnsFileNameForPathLookup()
        {
            Assert.Equal("pg_dump.exe", DatabaseBackupStorage.FindPgDump(_tempDir));
        }

        private DatabaseBackupStorage CreateStorage()
        {
            return new DatabaseBackupStorage(NullLogger<DatabaseBackupStorage>.Instance, "", _tempDir);
        }

        private void CreateFile(string name, DateTime lastWriteUtc)
        {
            string path = Path.Combine(_tempDir, name);
            File.WriteAllText(path, "");
            File.SetLastWriteTimeUtc(path, lastWriteUtc);
        }

        private void CreatePostgresVersion(string version, bool withPgDump)
        {
            string binDir = Path.Combine(_tempDir, "PostgreSQL", version, "bin");
            Directory.CreateDirectory(binDir);

            if (withPgDump)
            {
                File.WriteAllText(Path.Combine(binDir, "pg_dump.exe"), "");
            }
        }

        private string[] GetFileNames()
        {
            return Directory.GetFiles(_tempDir).Select(Path.GetFileName).Order().ToArray()!;
        }
    }
}
