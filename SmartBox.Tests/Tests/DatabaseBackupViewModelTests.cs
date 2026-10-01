using Application.Interfaces;
using Domain.Models.Backup;
using Microsoft.Extensions.Logging.Abstractions;
using SmartBox.Interfaces;
using SmartBox.ViewModels;

namespace SmartBox.Tests.Tests
{
    public class DatabaseBackupViewModelTests
    {
        private readonly FakeBackupService _backupService = new();
        private readonly FakeAlertService _alertService = new();
        private readonly DatabaseBackupViewModel _viewModel;

        public DatabaseBackupViewModelTests()
        {
            _viewModel = new DatabaseBackupViewModel(NullLogger<DatabaseBackupViewModel>.Instance, _backupService, _alertService);
        }

        [Fact]
        public async Task RunIfDueAsync_NotDue_ShowsLastBackup()
        {
            _backupService.IsDue = false;
            _backupService.LastBackup = new BackupFile(@"C:\Backups\20261001_101200.dump", DateTime.Today.AddHours(10).AddMinutes(12));

            await _viewModel.RunIfDueAsync();

            Assert.Equal(0, _backupService.BackupCount);
            Assert.Equal("Last database backup: today at 10:12", _viewModel.Status);
            Assert.Equal(@"C:\Backups\20261001_101200.dump", _viewModel.Details);
            Assert.False(_viewModel.IsRunning);
        }

        [Fact]
        public async Task RunIfDueAsync_NotDueSinceYesterday_ShowsYesterday()
        {
            _backupService.IsDue = false;
            _backupService.LastBackup = new BackupFile("backup.dump", DateTime.Today.AddDays(-1).AddHours(18).AddMinutes(5));

            await _viewModel.RunIfDueAsync();

            Assert.Equal("Last database backup: yesterday at 18:05", _viewModel.Status);
        }

        [Fact]
        public async Task RunIfDueAsync_BackupRunning_ShowsProgress()
        {
            TaskCompletionSource<BackupFile> backup = new();
            _backupService.Backup = () => backup.Task;

            Task run = _viewModel.RunIfDueAsync();

            Assert.True(_viewModel.IsRunning);
            Assert.Contains("Backing up", _viewModel.Status);

            backup.SetResult(new BackupFile("backup.dump", DateTime.Now));
            await run;

            Assert.False(_viewModel.IsRunning);
        }

        [Fact]
        public async Task RunIfDueAsync_BackupSucceeded_ShowsCompletionAndPath()
        {
            _backupService.Backup = () => Task.FromResult(new BackupFile(@"C:\Backups\20261001_120000.dump", DateTime.Today.AddHours(12)));

            await _viewModel.RunIfDueAsync();

            Assert.Equal("Database backup completed at 12:00", _viewModel.Status);
            Assert.Equal(@"C:\Backups\20261001_120000.dump", _viewModel.Details);
            Assert.False(_viewModel.IsFailed);
        }

        [Fact]
        public async Task RunIfDueAsync_BackupFailed_ShowsErrorAndAlertOnRequest()
        {
            _backupService.Backup = () => throw new InvalidOperationException("pg_dump exited with code 1");

            await _viewModel.RunIfDueAsync();

            Assert.True(_viewModel.IsFailed);
            Assert.False(_viewModel.IsRunning);
            Assert.Contains("pg_dump exited with code 1", _viewModel.Status);

            await _viewModel.ShowErrorCommand.ExecuteAsync(null);

            Assert.Contains("pg_dump exited with code 1", Assert.Single(_alertService.Messages));
        }

        [Fact]
        public async Task RunIfDueAsync_BackupFailedWithCause_ShowsCauseInDetailsOnly()
        {
            _backupService.Backup = () => throw new InvalidOperationException("pg_dump.exe was not found", new FileNotFoundException("The system cannot find the file specified"));

            await _viewModel.RunIfDueAsync();

            Assert.DoesNotContain("cannot find", _viewModel.Status);
            Assert.Equal($"pg_dump.exe was not found{Environment.NewLine}The system cannot find the file specified", _viewModel.Details);
        }

        [Fact]
        public async Task ShowError_BackupSucceeded_DoesNotAlert()
        {
            await _viewModel.RunIfDueAsync();
            await _viewModel.ShowErrorCommand.ExecuteAsync(null);

            Assert.Empty(_alertService.Messages);
        }

        private class FakeBackupService : IDatabaseBackupService
        {
            public bool IsDue { get; set; } = true;
            public BackupFile? LastBackup { get; set; }
            public Func<Task<BackupFile>> Backup { get; set; } = () => Task.FromResult(new BackupFile("backup.dump", DateTime.Now));
            public int BackupCount { get; private set; }

            public bool IsBackupDue()
            {
                return IsDue;
            }

            public BackupFile? GetLastBackup()
            {
                return LastBackup;
            }

            public Task<BackupFile> BackupAsync()
            {
                BackupCount++;
                return Backup();
            }
        }

        private class FakeAlertService : IAlertService
        {
            public List<string> Messages { get; } = [];

            public Task DisplayAlertAsync(string message, string title = "Error", string cancel = "Ok")
            {
                Messages.Add(message);
                return Task.CompletedTask;
            }
        }
    }
}
