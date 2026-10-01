using Domain.Models.Backup;

namespace Domain.Interfaces
{
    public interface IDatabaseBackupStorage
    {
        BackupFile? GetLastBackup();

        Task<BackupFile> CreateBackupAsync();

        void DeleteOldBackups(int keepCount);
    }
}
