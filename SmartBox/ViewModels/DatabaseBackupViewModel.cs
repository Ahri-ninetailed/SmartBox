using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SmartBox.Interfaces;

namespace SmartBox.ViewModels
{
    /// <summary>
    /// Статус бэкапа БД. Показывается в шапке приложения, поэтому виден на всех вкладках
    /// </summary>
    public partial class DatabaseBackupViewModel : ObservableObject
    {
        private const string BackupInProgressMessage = "Backing up the database...";
        private const string BackupCompletedMessage = "Database backup completed at {0:HH:mm}";
        private const string LastBackupMessage = "Last database backup: {0}";
        private const string BackupErrorMessage = "Failed to back up the database";

        private readonly ILogger<DatabaseBackupViewModel> _logger;
        private readonly IDatabaseBackupService _backupService;
        private readonly IAlertService _alertService;

        [ObservableProperty]
        private bool isRunning;

        [ObservableProperty]
        private bool isFailed;

        [ObservableProperty]
        private string status = string.Empty;

        // Путь к дампу или полный текст ошибки, показывается во всплывающей подсказке
        [ObservableProperty]
        private string? details;

        public DatabaseBackupViewModel(ILogger<DatabaseBackupViewModel> logger,
                                       IDatabaseBackupService backupService,
                                       IAlertService alertService)
        {
            _logger = logger;
            _backupService = backupService;
            _alertService = alertService;
        }

        public async Task RunIfDueAsync()
        {
            try
            {
                if (!_backupService.IsBackupDue() && _backupService.GetLastBackup() is { } lastBackup)
                {
                    Status = string.Format(LastBackupMessage, FormatBackupTime(lastBackup.CreatedAt));
                    Details = lastBackup.Path;
                    return;
                }

                IsRunning = true;
                Status = BackupInProgressMessage;

                Domain.Models.Backup.BackupFile backup = await Task.Run(_backupService.BackupAsync);

                Status = string.Format(BackupCompletedMessage, backup.CreatedAt);
                Details = backup.Path;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, BackupErrorMessage);

                IsFailed = true;
                Status = $"{BackupErrorMessage}: {ex.Message}";
                Details = GetFullMessage(ex);
            }
            finally
            {
                IsRunning = false;
            }
        }

        [RelayCommand]
        private async Task ShowError()
        {
            if (IsFailed)
            {
                await _alertService.DisplayAlertAsync($"{BackupErrorMessage}: {Details}");
            }
        }

        private static string FormatBackupTime(DateTime time)
        {
            string day = time.Date == DateTime.Today ? "today"
                : time.Date == DateTime.Today.AddDays(-1) ? "yesterday"
                : time.ToString("dd.MM.yyyy");

            return $"{day} at {time:HH:mm}";
        }

        /// <summary>
        /// Сообщение вместе с причинами, например с системной ошибкой, из-за которой не запустился pg_dump
        /// </summary>
        private static string GetFullMessage(Exception exception)
        {
            List<string> messages = [];
            for (Exception? current = exception; current != null; current = current.InnerException)
            {
                messages.Add(current.Message);
            }

            return string.Join(Environment.NewLine, messages.Distinct());
        }
    }
}
