using Infrastructure.Mappers;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            services.AddSingleton<IMapper<PartOfSpeech?, Domain.Models.PartOfSpeech>, PartOfSpeechMapper>();
            services.AddSingleton<IMapper<Domain.Models.PartOfSpeech, PartOfSpeech?>, PartOfSpeechMapper>();
            services.AddSingleton<IMapper<Word, Domain.Models.Word>, WordMapper>();
            services.AddSingleton<IMapper<Domain.Models.Word, Word>, WordMapper>();

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
