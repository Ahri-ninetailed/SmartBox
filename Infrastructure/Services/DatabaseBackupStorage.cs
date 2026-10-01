using Domain.Interfaces;
using Domain.Models.Backup;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;

namespace Infrastructure.Services
{
    /// <summary>
    /// Хранит дампы схемы public в папке и делает новые через pg_dump
    /// </summary>
    public class DatabaseBackupStorage : IDatabaseBackupStorage
    {
        private const string DumpExtension = ".dump";
        private const string TempExtension = ".tmp";
        private const string PgDumpFileName = "pg_dump.exe";
        private const int FileNotFoundErrorCode = 2;
        private static readonly TimeSpan PgDumpTimeout = TimeSpan.FromMinutes(2);

        private readonly ILogger<DatabaseBackupStorage> _logger;
        private readonly string _connectionString;
        private readonly string _backupsDirectory;
        private readonly string _pgDumpPathEnvVar;

        public DatabaseBackupStorage(ILogger<DatabaseBackupStorage> logger, string connectionString, string backupsDirectory, string pgDumpPathEnvVar)
        {
            _logger = logger;
            _connectionString = connectionString;
            _backupsDirectory = backupsDirectory;
            _pgDumpPathEnvVar = pgDumpPathEnvVar;
        }

        public BackupFile? GetLastBackup()
        {
            if (!Directory.Exists(_backupsDirectory))
            {
                return null;
            }

            FileInfo? lastBackup = GetFiles(DumpExtension).MaxBy(file => file.LastWriteTimeUtc);

            return lastBackup == null ? null : new BackupFile(lastBackup.FullName, lastBackup.LastWriteTime);
        }

        public async Task<BackupFile> CreateBackupAsync()
        {
            Directory.CreateDirectory(_backupsDirectory);
            DeleteUnfinishedBackups();

            string dumpPath = Path.Combine(_backupsDirectory, $"{DateTime.Now:yyyyMMdd_HHmmss}{DumpExtension}");
            string tempPath = dumpPath + TempExtension;

            // Пишем во временный файл, чтобы оборванный дамп никогда не выглядел готовым
            try
            {
                await RunPgDumpAsync(tempPath);
            }
            catch
            {
                TryDelete(new FileInfo(tempPath));
                throw;
            }

            File.Move(tempPath, dumpPath);
            _logger.LogInformation("Дамп БД сохранен: {Path}", dumpPath);

            return new BackupFile(dumpPath, File.GetLastWriteTime(dumpPath));
        }

        public void DeleteOldBackups(int keepCount)
        {
            IEnumerable<FileInfo> oldBackups = GetFiles(DumpExtension)
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .Skip(keepCount);

            foreach (FileInfo backup in oldBackups)
            {
                TryDelete(backup);
            }
        }

        /// <summary>
        /// Удаляет остатки дампов, которые не успели доделаться, например если приложение закрыли во время дампа
        /// </summary>
        internal void DeleteUnfinishedBackups()
        {
            foreach (FileInfo tempFile in GetFiles(TempExtension))
            {
                TryDelete(tempFile);
            }
        }

        /// <summary>
        /// Путь из переменной окружения, если он задан, иначе автопоиск
        /// </summary>
        internal static string ResolvePgDump(string? configuredPath, string envVarName, string programFilesDirectory)
        {
            // Кавычки появляются, если путь скопирован в проводнике через "Копировать как путь"
            string? path = configuredPath?.Trim().Trim('"').Trim();

            if (string.IsNullOrEmpty(path))
            {
                return FindPgDump(programFilesDirectory);
            }

            if (Directory.Exists(path))
            {
                throw new InvalidOperationException($"{envVarName} environment variable must be the full path to {PgDumpFileName}, but '{path}' is a folder");
            }

            // Явно заданный путь не подменяем автопоиском, иначе опечатка в нем останется незамеченной
            if (!File.Exists(path))
            {
                throw new InvalidOperationException($"{envVarName} environment variable points to '{path}', but this file does not exist");
            }

            return path;
        }

        /// <summary>
        /// Ищет pg_dump самой свежей установленной версии PostgreSQL, иначе рассчитывает на PATH
        /// </summary>
        internal static string FindPgDump(string programFilesDirectory)
        {
            string postgresDirectory = Path.Combine(programFilesDirectory, "PostgreSQL");

            if (!Directory.Exists(postgresDirectory))
            {
                return PgDumpFileName;
            }

            // Папки называются по версии: 18, 9.6
            return Directory.EnumerateDirectories(postgresDirectory)
                .Select(dir => (Path: Path.Combine(dir, "bin", PgDumpFileName),
                                Version: decimal.TryParse(Path.GetFileName(dir), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal version) ? version : 0))
                .Where(candidate => File.Exists(candidate.Path))
                .OrderByDescending(candidate => candidate.Version)
                .Select(candidate => candidate.Path)
                .FirstOrDefault() ?? PgDumpFileName;
        }

        private async Task RunPgDumpAsync(string outputPath)
        {
            NpgsqlConnectionStringBuilder connection = new(_connectionString);

            ProcessStartInfo startInfo = new()
            {
                FileName = ResolvePgDump(Environment.GetEnvironmentVariable(_pgDumpPathEnvVar),
                                         _pgDumpPathEnvVar,
                                         Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
            };

            // Только свои таблицы без служебных схем Supabase, а без владельцев и прав дамп разворачивается в любую БД
            startInfo.ArgumentList.Add("--schema=public");
            startInfo.ArgumentList.Add("--format=custom");
            startInfo.ArgumentList.Add("--no-owner");
            startInfo.ArgumentList.Add("--no-privileges");
            startInfo.ArgumentList.Add("--no-password");
            startInfo.ArgumentList.Add($"--file={outputPath}");

            // Подключение передаем через переменные окружения, чтобы пароль не попал в командную строку
            SetEnvironment(startInfo, "PGHOST", connection.Host);
            SetEnvironment(startInfo, "PGPORT", connection.Port.ToString(CultureInfo.InvariantCulture));
            SetEnvironment(startInfo, "PGDATABASE", connection.Database);
            SetEnvironment(startInfo, "PGUSER", connection.Username);
            SetEnvironment(startInfo, "PGPASSWORD", connection.Password);

            using Process process = StartProcess(startInfo);
            Task<string> errorOutput = process.StandardError.ReadToEndAsync();

            using CancellationTokenSource timeout = new(PgDumpTimeout);
            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
                throw new TimeoutException($"pg_dump did not finish in {PgDumpTimeout.TotalMinutes} minutes and was stopped");
            }

            string error = await errorOutput;
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"pg_dump exited with code {process.ExitCode}: {error.Trim()}");
            }
        }

        private Process StartProcess(ProcessStartInfo startInfo)
        {
            try
            {
                return Process.Start(startInfo) ?? throw new InvalidOperationException($"Failed to start {startInfo.FileName}");
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == FileNotFoundErrorCode)
            {
                throw new InvalidOperationException($"{PgDumpFileName} was not found in Program Files\\PostgreSQL\\<version>\\bin or in PATH. Install PostgreSQL or set the {_pgDumpPathEnvVar} environment variable to the full path of {PgDumpFileName}", ex);
            }
            catch (Win32Exception ex)
            {
                throw new InvalidOperationException($"Failed to start {startInfo.FileName}", ex);
            }
        }

        private static void SetEnvironment(ProcessStartInfo startInfo, string name, string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                startInfo.Environment[name] = value;
            }
        }

        private IEnumerable<FileInfo> GetFiles(string extension)
        {
            return new DirectoryInfo(_backupsDirectory).EnumerateFiles($"*{extension}");
        }

        private void TryDelete(FileInfo file)
        {
            try
            {
                file.Delete();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(ex, "Не удалось удалить {Path}", file.FullName);
            }
        }
    }
}
