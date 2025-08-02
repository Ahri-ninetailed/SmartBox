using Infrastructure.Mappers;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using Shared;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        private const string NotAvailable = "N/A";
        private const string WordPopularityServiceRetryMessage = "WordPopularityService retry attempt {RetryCount} after {Delay}s. Status: {StatusCode}, Exception: {ExceptionType}, Message: {ErrorMessage}";

        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            services.AddSingleton<IMapper<PartOfSpeech?, Domain.Models.PartOfSpeech>, PartOfSpeechMapper>();
            services.AddSingleton<IMapper<Domain.Models.PartOfSpeech, PartOfSpeech?>, PartOfSpeechMapper>();
            services.AddSingleton<IMapper<Word, Domain.Models.Word>, WordMapper>();
            services.AddSingleton<IMapper<Domain.Models.Word, Word>, WordMapper>();

            services.AddSingleton<Domain.Interfaces.IWordRepository, Services.WordRepository>();

            services.AddHttpClient<Domain.Interfaces.IWordPopularityService, Services.WordPopularityService>(client =>
            {
                client.BaseAddress = new Uri("https://books.google.com/ngrams/json");
                client.Timeout = TimeSpan.FromSeconds(10);
            })
            .AddPolicyHandler((serviceProvider, request) =>
            {
                ILogger<Services.WordPopularityService> logger = serviceProvider.GetService<ILogger<Services.WordPopularityService>>()!;
                return GetRetryPolicy(logger);
            });

            string sqlConnectionString = Environment.GetEnvironmentVariable("ENGLISH_WORDS_STORAGE")!;
            services.AddDbContextFactory<MyDbContext>(options =>
            {
                options.UseNpgsql(sqlConnectionString);

                options.EnableDetailedErrors();
            });

            return services;
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ILogger logger)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: _ => TimeSpan.FromSeconds(1),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        string statusCode = outcome.Result?.StatusCode.ToString() ?? NotAvailable;
                        string exceptionType = outcome.Exception?.GetType().Name ?? NotAvailable;
                        string errorMessage = outcome.Exception?.Message ?? outcome.Result?.ReasonPhrase ?? NotAvailable;

                        logger.LogWarning(WordPopularityServiceRetryMessage,
                            retryCount,
                            timespan.TotalSeconds,
                            statusCode,
                            exceptionType,
                            errorMessage);
                    });
        }
    }
}
