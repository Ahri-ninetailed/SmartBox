using Infrastructure.Interfaces;
using Infrastructure.Mappers;
using Infrastructure.Services;
using Infrastructure.Services.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
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


        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration config, string backupsDirectory)
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
            string promptsPath = Path.Combine(AppContext.BaseDirectory, "Prompts");
            string missingExamplesPrompt = File.ReadAllText(Path.Combine(promptsPath, "MissingExamples.txt"));
            string meaningsAndExamplesPrompt = File.ReadAllText(Path.Combine(promptsPath, "MeaningsAndExamples.txt"));
            string missingExamplesSchema = File.ReadAllText(Path.Combine(promptsPath, "MissingExamplesResponse.json"));
            string meaningsAndExamplesSchema = File.ReadAllText(Path.Combine(promptsPath, "MeaningsAndExamplesResponse.json"));
            services.AddSingleton<Domain.Interfaces.IAiService>(provider =>
                new AiService(provider.GetRequiredService<IAiModel>(),
                              missingExamplesPrompt,
                              meaningsAndExamplesPrompt,
                              missingExamplesSchema,
                              meaningsAndExamplesSchema));

            services.AddSingleton<ChatClient>(provider =>
            {
                string model = config["OpenAi:Model"] ?? throw new InvalidOperationException("OpenAi:Model is not set in config");
                string apiKeyEnvVar = config["OpenAi:ApiKeyEnvVar"] ?? throw new InvalidOperationException("OpenAi:ApiKeyEnvVar is not set in config");
                string key = Environment.GetEnvironmentVariable(apiKeyEnvVar) ?? throw new InvalidOperationException($"{apiKeyEnvVar} environment variable is not set");
                return new ChatClient(model, key);
            });

            int? maxOutputTokens = config.GetValue<int?>("OpenAi:MaxOutputTokens");
            services.AddSingleton<IAiModel>(provider =>
                new OpenAi(provider.GetRequiredService<ILogger<OpenAi>>(),
                           provider.GetRequiredService<ChatClient>(),
                           maxOutputTokens));
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
                ILogger<WordPopularityService> logger = serviceProvider.GetRequiredService<ILogger<WordPopularityService>>();
                return GetRetryPolicy(logger);
            });

            services.AddKeyedSingleton<CookieContainer>(Models.Parser.Parser.CambridgeDictionary.ToString());

            // Общий лимит на запрос вместе с ретраями
            int cambridgeDictionaryTimeoutSeconds = config.GetValue<int?>("CambridgeDictionary:TimeoutSeconds") ?? throw new InvalidOperationException("CambridgeDictionary:TimeoutSeconds is not set in config");

            // TODO: Возникают проблемы с отправкой запросов, может быть стоит использовать Microsoft.Playwright
            services.AddHttpClient(Models.Parser.Parser.CambridgeDictionary.ToString(), client =>
            {
                client.BaseAddress = new Uri("https://dictionary.cambridge.org/dictionary/english/");
                //Для первого запроса всегда валилаься на таймауте 10 сек, попробовал увеличить, если будут ошибки, то стоит подумать над другим решением
                //Также можно попробовать делать warmup
                client.Timeout = TimeSpan.FromSeconds(cambridgeDictionaryTimeoutSeconds);

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
                ILogger logger = serviceProvider.GetRequiredService<ILoggerFactory>()
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

            string dbEnvVar = config["Database:ConnectionStringEnvVar"] ?? throw new InvalidOperationException("Database:ConnectionStringEnvVar is not set in config");
            string sqlConnectionString = Environment.GetEnvironmentVariable(dbEnvVar) ?? throw new InvalidOperationException($"{dbEnvVar} environment variable is not set");

            services.AddDbContextFactory<MyDbContext>(options => ConfigureDbContext(options, sqlConnectionString));

            // Сама переменная необязательна: если ее нет, pg_dump ищется автоматически
            string pgDumpPathEnvVar = config["Backup:PgDumpPathEnvVar"] ?? throw new InvalidOperationException("Backup:PgDumpPathEnvVar is not set in config");
            services.AddSingleton<Domain.Interfaces.IDatabaseBackupStorage>(provider =>
                new DatabaseBackupStorage(provider.GetRequiredService<ILogger<DatabaseBackupStorage>>(),
                                          sqlConnectionString,
                                          backupsDirectory,
                                          pgDumpPathEnvVar));

            return services;
        }

        /// <summary>
        /// Общая настройка для приложения и интеграционных тестов, чтобы тесты проверяли ту же конфигурацию
        /// </summary>
        internal static void ConfigureDbContext(DbContextOptionsBuilder options, string connectionString)
        {
            // Повторяет только временные ошибки: обрыв соединения, таймаут, перегрузка пулера
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null));

            // По умолчанию повторы пишутся как Information, а логи EF ниже Warning отфильтрованы
            options.ConfigureWarnings(warnings => warnings.Log((CoreEventId.ExecutionStrategyRetrying, LogLevel.Warning)));

            options.EnableDetailedErrors();
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
