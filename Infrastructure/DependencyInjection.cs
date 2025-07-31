using Infrastructure.MappingConfigs;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            var config = TypeAdapterConfig.GlobalSettings;

            new PartOfSpeechMappingConfig().Register(config);
            new WordMappingConfig().Register(config);

            services.AddSingleton(config);

            services.AddSingleton<Domain.Interfaces.IWordRepository, Services.WordRepository>();

            string sqlConnectionString = Environment.GetEnvironmentVariable("ENGLISH_WORDS_STORAGE")!;
            services.AddDbContextFactory<MyDbContext>(options =>
            {
                options.UseNpgsql(sqlConnectionString);

                options.EnableDetailedErrors();
            });

            return services;
        }
    }
}
