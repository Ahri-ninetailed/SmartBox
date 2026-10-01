using Domain.Models.Backup;

namespace Application.Interfaces
{
    public interface IDatabaseBackupService
    {
        bool IsBackupDue();

        BackupFile? GetLastBackup();

        Task<BackupFile> BackupAsync();
    }
}
