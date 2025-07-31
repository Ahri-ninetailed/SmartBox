using Application.Interfaces;
using Application.Models;
using Domain.Interfaces;
using Mapster;
using Polly;

namespace Application.Services
{
    public class DictionaryService : IDictionaryService
    {
        private readonly IWordRepository _wordRepository;
        private readonly ResiliencePipeline _retryPipeline;

        public DictionaryService(IWordRepository wordRepository)
        {
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
                        // TODO: логгирование
                       /* _logger.LogWarning("Retry attempt {AttemptNumber} for operation. Exception: {Exception}",
                                         args.AttemptNumber, args.Outcome.Exception?.Message);*/
                        return ValueTask.CompletedTask;
                    }
                })
                .Build();
        }

        public async Task<Word[]> GetWordsForLearningAsync()
        {
            return await _retryPipeline.ExecuteAsync(async _ =>
            {
                Domain.Models.Word[] words = await _wordRepository.GetWordsForLearningAsync();

                return words.Adapt<Word[]>();
            });
        }

        public async Task LearnWordsAsync(IEnumerable<int> ids)
        {
            await _retryPipeline.ExecuteAsync(async _ =>
            {
                Domain.Models.Word[] words = await _wordRepository.GetWordsByIds(ids);

                foreach (Domain.Models.Word word in words)
                {
                    word.Learn();
                }

                await _wordRepository.SaveWords(words);
            });
        }

        public async Task<Word[]> SearchWordsAsync(string entry)
        {
            return await _retryPipeline.ExecuteAsync(async _ =>
            {
                Domain.Models.Word[] words = await _wordRepository.SearchWordsAsync(entry);

                return words.Adapt<Word[]>();
            });
        }
    }
}
