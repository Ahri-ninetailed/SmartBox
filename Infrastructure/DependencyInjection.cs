using Infrastructure.Interfaces;
using Infrastructure.Mappers;
using Infrastructure.Services;
using Infrastructure.Services.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using Polly;
using Polly.Extensions.Http;
using Shared;
using System.Net;

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
            services.AddSingleton<IMapper<Models.Parser.PartOfSpeech, Domain.Models.Parser.PartOfSpeech>, PartOfSpeechMapper>();

            services.AddSingleton<IMapper<Models.DataBase.Word, Domain.Models.Dictionary.Word>, WordMapper>();
            services.AddSingleton<IMapper<Domain.Models.Dictionary.Word, Models.DataBase.Word>, WordMapper>();
            services.AddSingleton<IMapper<Models.Parser.Word, Domain.Models.Parser.Word>, WordMapper>();

            services.AddSingleton<IMapper<Domain.Models.Dictionary.Definition, Infrastructure.Models.DataBase.Definition>, DefinitionMapper>();

            services.AddSingleton<Domain.Interfaces.IWordRepository, WordRepository>();
            services.AddSingleton<Domain.Interfaces.IParserService, ParserService>();
            services.AddSingleton<Domain.Interfaces.IExportService, ExportService>();
            services.AddSingleton<Domain.Interfaces.IAiService, AiService>();

            services.AddSingleton<ChatClient>(provider =>
            {
                string? key = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                if (key == null)
                {
                    throw new Exception("OPENAI_API_KEY is not set");
                }
                return new ChatClient("gpt-4.1", key);
            });

            services.AddSingleton<AiModel, OpenAi>();
            services.AddSingleton<IParser, CambridgeDictionaryParser>();

            services.AddHttpClient<Domain.Interfaces.IWordPopularityService, WordPopularityService>(client =>
            {
                client.BaseAddress = new Uri("https://books.google.com/ngrams/json");
                //Первый запрос почему-то тоже стал валиться на 10 секундах
                client.Timeout = TimeSpan.FromSeconds(45);

                // Имитация настоящего браузера хз помогло ли
                client.DefaultRequestHeaders.Add("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
                client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
            })
            .AddPolicyHandler((serviceProvider, request) =>
            {
                ILogger<WordPopularityService> logger = serviceProvider.GetService<ILogger<WordPopularityService>>()!;
                return GetRetryPolicy(logger);
            });

            services.AddKeyedSingleton<CookieContainer>(Models.Parser.Parser.CambridgeDictionary.ToString());

            // TODO: Возникают проблемы с отправкой запросов, может быть стоит использовать Microsoft.Playwright
            services.AddHttpClient(Models.Parser.Parser.CambridgeDictionary.ToString(), client =>
            {
                client.BaseAddress = new Uri("https://dictionary.cambridge.org/dictionary/english/");
                //Для первого запроса всегда валилаься на таймауте 10 сек, попробовал увеличить, если будут ошибки, то стоит подумать над другим решением
                //Также можно попробовать делать warmup
                client.Timeout = TimeSpan.FromSeconds(45);

                // Имитация настоящего браузера хз помогло ли
                client.DefaultRequestHeaders.Add("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
                client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
                client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
                client.DefaultRequestHeaders.Add("DNT", "1");
                client.DefaultRequestHeaders.Add("Connection", "keep-alive");
                client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
            })
            .ConfigurePrimaryHttpMessageHandler(serviceProvider => new HttpClientHandler()
            {
                CookieContainer = serviceProvider.GetRequiredKeyedService<CookieContainer>(Models.Parser.Parser.CambridgeDictionary.ToString()),
                UseCookies = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
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
