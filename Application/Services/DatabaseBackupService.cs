using Application.Interfaces;
using Application.Settings;
using Domain.Interfaces;
using Domain.Models.Backup;

namespace Application.Services
{
    /// <summary>
    /// Решает, когда делать бэкап и сколько бэкапов хранить. Как именно делается бэкап, знает IDatabaseBackupStorage
    /// </summary>
    public class DatabaseBackupService : IDatabaseBackupService
    {
        private readonly IDatabaseBackupStorage _storage;
        private readonly BackupSettings _settings;

        public DatabaseBackupService(IDatabaseBackupStorage storage, BackupSettings settings)
        {
            _storage = storage;
            _settings = settings;
        }

        public bool IsBackupDue()
        {
            BackupFile? lastBackup = _storage.GetLastBackup();

            return lastBackup == null || lastBackup.IsOlderThan(_settings.Interval, DateTime.Now);
        }

        public BackupFile? GetLastBackup()
        {
            return _storage.GetLastBackup();
        }

        public async Task<BackupFile> BackupAsync()
        {
            BackupFile backup = await _storage.CreateBackupAsync();

            _storage.DeleteOldBackups(_settings.KeepCount);

            return backup;
        }
    }
}
