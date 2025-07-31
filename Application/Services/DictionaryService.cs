using Application.Interfaces;
using Application.Models;
using Domain.Interfaces;
using Mapster;
using Microsoft.Extensions.Logging;
using Polly;

namespace Application.Services
{
    public class DictionaryService : IDictionaryService
    {
        private readonly ILogger<DictionaryService> _logger;
        private readonly IWordRepository _wordRepository;
        private readonly ResiliencePipeline _retryPipeline;

        public DictionaryService(IWordRepository wordRepository, ILogger<DictionaryService> logger)
        {
            _logger = logger;
            _wordRepository = wordRepository;

            _retryPipeline = new ResiliencePipelineBuilder()
                .AddRetry(new()
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(1),
                    ShouldHandle = new PredicateBuilder()
                        .Handle<InvalidOperationException>(ex => 
                            ex.Message.Contains("transient failure", StringComparison.OrdinalIgnoreCase)),
                    OnRetry = args =>
                    {
                        string operationName = args.Context.OperationKey ?? "Unknown";

                        _logger.LogWarning("Retry attempt {AttemptNumber} for {OperationName}. Exception: {ExceptionType}",
                                         args.AttemptNumber, operationName, args.Outcome.Exception?.GetType().Name);
                        
                        return ValueTask.CompletedTask;
                    }
                })
                .Build();
        }

        public async Task<Word[]> GetWordsForLearningAsync()
        {
            ResilienceContext context = ResilienceContextPool.Shared.Get(operationKey: nameof(GetWordsForLearningAsync));

            try
            {
                return await _retryPipeline.ExecuteAsync(async _ =>
                {
                    Domain.Models.Word[] words = await _wordRepository.GetWordsForLearningAsync();

                    return words.Adapt<Word[]>();
                }, context);
            }
            finally
            {
                ResilienceContextPool.Shared.Return(context);
            }
        }

        public async Task LearnWordsAsync(IEnumerable<int> ids)
        {
            ResilienceContext context = ResilienceContextPool.Shared.Get(operationKey: nameof(LearnWordsAsync));

            try
            {
                await _retryPipeline.ExecuteAsync(async _ =>
                {
                    Domain.Models.Word[] words = await _wordRepository.GetWordsByIds(ids);

                    foreach (Domain.Models.Word word in words)
                    {
                        word.Learn();
                    }

                    await _wordRepository.SaveWords(words);
                }, context);
            }
            finally
            {
                ResilienceContextPool.Shared.Return(context);
            }
        }

        public async Task<Word[]> SearchWordsAsync(string entry)
        {
            ResilienceContext context = ResilienceContextPool.Shared.Get(operationKey: nameof(SearchWordsAsync));

            try
            {
                return await _retryPipeline.ExecuteAsync(async _ =>
                {
                    Domain.Models.Word[] words = await _wordRepository.SearchWordsAsync(entry);

                    return words.Adapt<Word[]>();
                }, context);
            }
            finally
            {
                ResilienceContextPool.Shared.Return(context);
            }
        }
    }
}
