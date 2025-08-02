using Application.Interfaces;
using Application.Models;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Polly;
using Shared;

namespace Application.Services
{
    public class DictionaryService : IDictionaryService
    {
        private const string RetryAttemptMessage = "Retry attempt {AttemptNumber} for {OperationName}. Exception: {ExceptionType}";
        private const string LearnedWordsMessage = "Learned {WordCount} words with IDs: {WordIds}";
        private const string MarkedWordsMessage = "Marked {WordCount} words for study with IDs: {WordIds}";
        private const string WordAlreadyExistsError = "Word '{0}' with part of speech '{1}' already exists in the dictionary";
        private const string GetWordPopularityErrorMessage = "Failed to get word popularity for '{WordValue}'";
        private const string AddedWordMessage = "Added word '{Word}' (Id: {Id}, PartOfSpeech: {PartOfSpeech}) to the database.";

        private readonly ResiliencePipeline _retryPipeline;
        private readonly ILogger<DictionaryService> _logger;
        private readonly IWordRepository _wordRepository;
        private readonly IWordPopularityService _wordPopularityService;

        private readonly IMapper<PartOfSpeech, Domain.Models.PartOfSpeech> _partOfSpeechToDomMapper;
        private readonly IMapper<Domain.Models.Word, Word> _wordToAppMapper;

        public DictionaryService(ILogger<DictionaryService> logger,
                                 IWordRepository wordRepository,
                                 IMapper<PartOfSpeech, Domain.Models.PartOfSpeech> partOfSpeechToDomMapper,
                                 IMapper<Domain.Models.Word, Word> wordToAppMapper,
                                 IWordPopularityService wordPopularityService)
        {
            _logger = logger;

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

            _partOfSpeechToDomMapper = partOfSpeechToDomMapper;
            _wordToAppMapper = wordToAppMapper;
            
            _wordRepository = wordRepository;
            _wordPopularityService = wordPopularityService;   
        }

        public async Task<Word> AddWordAsync(string wordValue, PartOfSpeech partOfSpeech, string comment)
        {
            ResilienceContext context = ResilienceContextPool.Shared.Get(operationKey: nameof(AddWordAsync));

            try
            {
                Domain.Models.PartOfSpeech domainPartOfSpeech = _partOfSpeechToDomMapper.Map(partOfSpeech);

                bool isWordExisted = await _wordRepository.IsWordExistedAsync(wordValue, domainPartOfSpeech);

                if (isWordExisted)
                {
                    throw new InvalidOperationException(string.Format(WordAlreadyExistsError, wordValue, domainPartOfSpeech));
                }

                decimal? wordPopularity = null;
                try
                {
                    // TODO: разделять в UI слое
                    string[] words = wordValue.Split(new[] { ", ", "," }, StringSplitOptions.RemoveEmptyEntries);
                    wordPopularity = await _wordPopularityService.GetWordPopularityAsync(words);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, GetWordPopularityErrorMessage, wordValue);
                }

                Domain.Models.Word newWord = new Domain.Models.Word()
                {
                    Value = wordValue,
                    PartOfSpeech = domainPartOfSpeech,
                    Comments = comment,
                    Popularity = wordPopularity
                };

                Domain.Models.Word word = await _wordRepository.AddWordAsync(newWord);

                _logger.LogInformation(AddedWordMessage, word.Value, word.Id, word.PartOfSpeech);

                Word result = _wordToAppMapper.Map(word);

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

                    return _wordToAppMapper.MapToArray(words);
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

                    return _wordToAppMapper.MapToArray(words);
                }, context);
            }
            finally
            {
                ResilienceContextPool.Shared.Return(context);
            }
        }
    }
}
