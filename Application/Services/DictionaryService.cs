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
        private const string RetryAttemptMessage = "Retry attempt {AttemptNumber} for {OperationName}. Exception: {ExceptionType}";
        private const string LearnedWordsMessage = "Learned {WordCount} words with IDs: {WordIds}";
        private const string MarkedWordsMessage = "Marked {WordCount} words for study with IDs: {WordIds}";
        private const string WordAlreadyExistsError = "Word '{0}' with part of speech '{1}' already exists in the dictionary";


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

                        _logger.LogWarning(RetryAttemptMessage, args.AttemptNumber, operationName, args.Outcome.Exception?.GetType().Name);

                        return ValueTask.CompletedTask;
                    }
                })
                .Build();
        }

        public async Task<Word> AddWordAsync(string wordValue, PartOfSpeech partOfSpeech, string comment)
        {
            ResilienceContext context = ResilienceContextPool.Shared.Get(operationKey: nameof(AddWordAsync));

            try
            {
                Domain.Models.PartOfSpeech domainPartOfSpeech = partOfSpeech.Adapt<Domain.Models.PartOfSpeech>();

                bool isWordExisted = await _wordRepository.IsWordExistedAsync(wordValue, domainPartOfSpeech);

                if (isWordExisted)
                {
                    throw new InvalidOperationException(string.Format(WordAlreadyExistsError, wordValue, domainPartOfSpeech));
                }

                Domain.Models.Word newWord = new Domain.Models.Word()
                {
                    Value = wordValue,
                    PartOfSpeech = domainPartOfSpeech,
                    Comments = comment
                };

                Domain.Models.Word word = await _wordRepository.AddWordAsync(newWord);

                Word result = word.Adapt<Word>();

                return result;
            }
            finally
            {
                ResilienceContextPool.Shared.Return(context);
            }
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
                    Domain.Models.Word[] words = await _wordRepository.GetWordsByIdsAsync(ids);

                    foreach (Domain.Models.Word word in words)
                    {
                        word.Learn();
                    }

                    await _wordRepository.SaveWordsAsync(words);

                    _logger.LogInformation(LearnedWordsMessage, words.Length, string.Join(", ", words.Select(w => w.Id)));
                }, context);
            }
            finally
            {
                ResilienceContextPool.Shared.Return(context);
            }
        }

        public async Task MarkWordsAsync(IEnumerable<int> ids)
        {
            ResilienceContext context = ResilienceContextPool.Shared.Get(operationKey: nameof(MarkWordsAsync));

            try
            {
                await _retryPipeline.ExecuteAsync(async _ =>
                {
                    Domain.Models.Word[] words = await _wordRepository.GetWordsByIdsAsync(ids);

                    foreach (Domain.Models.Word word in words)
                    {
                        word.MarkForStudy();
                    }

                    await _wordRepository.SaveWordsAsync(words);

                    _logger.LogInformation(MarkedWordsMessage, words.Length, string.Join(", ", words.Select(w => w.Id)));
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
