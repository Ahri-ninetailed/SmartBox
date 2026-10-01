using Application.Interfaces;
using Application.Services;
using Application.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
        {
            int backupIntervalHours = config.GetValue<int?>("Backup:IntervalHours") ?? throw new InvalidOperationException("Backup:IntervalHours is not set in config");
            int backupKeepCount = config.GetValue<int?>("Backup:KeepCount") ?? throw new InvalidOperationException("Backup:KeepCount is not set in config");
            services.AddSingleton(new BackupSettings(TimeSpan.FromHours(backupIntervalHours), backupKeepCount));

            services.AddSingleton<IDictionaryService, DictionaryService>();
            services.AddSingleton<IParserService, ParserService>();
            services.AddSingleton<IDatabaseBackupService, DatabaseBackupService>();

            return services;
        }
    }
}
