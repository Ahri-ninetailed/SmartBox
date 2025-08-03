using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using Shared;
using Infrastructure.Services;
using Infrastructure.Interfaces;
using Infrastructure.Services.Internal;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        private const string NotAvailable = "N/A";
        private const string WordPopularityServiceRetryMessage = "WordPopularityService retry attempt {RetryCount} after {Delay}s. Status: {StatusCode}, Exception: {ExceptionType}, Message: {ErrorMessage}";
        private const string ParserRetryMessage = "{Parser} retry attempt {RetryCount} after {Delay}s. Status: {StatusCode}, Exception: {ExceptionType}, Message: {ErrorMessage}";


        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            services.AddSingleton<IMapper<Models.DataBase.PartOfSpeech?, Domain.Models.Dictionary.PartOfSpeech>, PartOfSpeechMapper>();
            services.AddSingleton<IMapper<Domain.Models.Dictionary.PartOfSpeech, Models.DataBase.PartOfSpeech?>, PartOfSpeechMapper>();
            services.AddSingleton<IMapper<Models.Parser.PartsOfSpeech, Domain.Models.Parser.PartsOfSpeech>, PartOfSpeechMapper>();

            services.AddSingleton<IMapper<Models.DataBase.Word, Domain.Models.Dictionary.Word>, WordMapper>();
            services.AddSingleton<IMapper<Domain.Models.Dictionary.Word, Models.DataBase.Word>, WordMapper>();
            services.AddSingleton<IMapper<Models.Parser.Word, Domain.Models.Parser.Word>, WordMapper>();

            services.AddSingleton<Domain.Interfaces.IWordRepository, WordRepository>();
            services.AddSingleton<Domain.Interfaces.IParserService, ParserService>();

            services.AddSingleton<IParser, CambridgeDictionaryParser>();

            services.AddHttpClient<Domain.Interfaces.IWordPopularityService, WordPopularityService>(client =>
            {
                client.BaseAddress = new Uri("https://books.google.com/ngrams/json");
                client.Timeout = TimeSpan.FromSeconds(10);
            })
            .AddPolicyHandler((serviceProvider, request) =>
            {
                ILogger<WordPopularityService> logger = serviceProvider.GetService<ILogger<WordPopularityService>>()!;
                return GetRetryPolicy(logger);
            });

            services.AddHttpClient(Models.Parser.Parser.CambridgeDictionary.ToString(), client =>
            {
                client.BaseAddress = new Uri("https://dictionary.cambridge.org/dictionary/english/");
                client.Timeout = TimeSpan.FromSeconds(10);
            })
            // TODO: Вынести в отдельный метод, когда появится другой парсер
            .AddPolicyHandler((serviceProvider, request) =>
            {
                ILogger logger = serviceProvider.GetService<ILoggerFactory>()!
                    .CreateLogger(Models.Parser.Parser.CambridgeDictionary.ToString());
            
                return Policy
                    .Handle<HttpRequestException>()
                    .OrResult<HttpResponseMessage>(r => r.StatusCode != System.Net.HttpStatusCode.OK)
                    .WaitAndRetryForeverAsync(
                        retryAttempt => TimeSpan.FromSeconds(2),
                        (result, retryCount, timespan) =>
                        {
                            string statusCode = result.Result?.StatusCode.ToString() ?? NotAvailable;
                            string exceptionType = result.Exception?.GetType().Name ?? NotAvailable;
                            string errorMessage = result.Exception?.Message ?? result.Result?.ReasonPhrase ?? NotAvailable;
            
                            logger.LogWarning(ParserRetryMessage,
                                Models.Parser.Parser.CambridgeDictionary,
                                retryCount,
                                timespan.TotalSeconds,
                                statusCode,
                                exceptionType,
                                errorMessage);
                        });
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
