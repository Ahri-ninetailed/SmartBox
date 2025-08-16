using Application.Interfaces;
using Application.Models;
using Application.Models.Dictionary;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Polly;
using Shared;

namespace Application.Services
{
    public class DictionaryService : IDictionaryService
    {
        private const string Separator = ", ";

        private const string RetryAttemptMessage = "Retry attempt {AttemptNumber} for {OperationName}. Exception: {ExceptionType}";
        private const string LearnedWordsMessage = "Learned {WordCount} words with IDs: {WordIds} and values: {WordValues}";
        private const string MarkedWordsMessage = "Marked {WordCount} words for study with IDs: {WordIds} and values: {WordValues}";
        private const string WordAlreadyExistsError = "Word '{0}' with part of speech '{1}' already exists in the dictionary";
        private const string GetWordPopularityErrorMessage = "Failed to get word popularity for '{WordValue}'";
        private const string AddedWordMessage = "Added word '{Word}' (Id: {Id}, PartOfSpeech: {PartOfSpeech}) to the database.";
        private const string WordNotFoundError = "Word with ID '{0}' was not found in the dictionary";
        private const string ChangedPartOfSpeechMessage = "Changed part of speech for word '{WordValues}' (Id: {WordId}) from '{OldPartOfSpeech}' to '{NewPartOfSpeech}'";
        private const string ValuesNullOrEmptyError = "Values array cannot be null, empty, or contain null/whitespace elements.";

        private readonly ResiliencePipeline _retryPipeline;
        private readonly ILogger<DictionaryService> _logger;
        private readonly IWordRepository _wordRepository;
        private readonly IWordPopularityService _wordPopularityService;

        private readonly IMapper<Application.Models.Dictionary.PartOfSpeech, Domain.Models.Dictionary.PartOfSpeech> _partOfSpeechToDomMapper;
        private readonly IMapper<Domain.Models.Dictionary.Word, Application.Models.Dictionary.Word> _wordToAppMapper;

        public DictionaryService(ILogger<DictionaryService> logger,
                                 IWordRepository wordRepository,
                                 IMapper<Models.Dictionary.PartOfSpeech, Domain.Models.Dictionary.PartOfSpeech> partOfSpeechToDomMapper,
                                 IMapper<Domain.Models.Dictionary.Word, Models.Dictionary.Word> wordToAppMapper,
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

        public async Task<Models.Dictionary.Word> AddWordAsync(string[] wordValues, Models.Dictionary.PartOfSpeech partOfSpeech, string comment)
        {
            if (wordValues.Length == 0 || wordValues.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException(ValuesNullOrEmptyError, nameof(wordValues));
            }

            ResilienceContext context = ResilienceContextPool.Shared.Get(operationKey: nameof(AddWordAsync));

            wordValues = wordValues.Select(el => el.ToLower().Trim()).ToArray();

            try
            {
                Domain.Models.Dictionary.PartOfSpeech domainPartOfSpeech = _partOfSpeechToDomMapper.Map(partOfSpeech);

                bool isWordExisted = await _wordRepository.IsWordExistedAsync(wordValues, domainPartOfSpeech);

                if (isWordExisted)
                {
                    throw new InvalidOperationException(string.Format(WordAlreadyExistsError, string.Join(Separator, wordValues), domainPartOfSpeech));
                }

                decimal? wordPopularity = null;
                try
                {
                    wordPopularity = await _wordPopularityService.GetWordPopularityAsync(wordValues);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, GetWordPopularityErrorMessage, string.Join(Separator, wordValues));
                }

                Domain.Models.Dictionary.Word newWord = new Domain.Models.Dictionary.Word()
                {
                    Values = wordValues,
                    PartOfSpeech = domainPartOfSpeech,
                    Comments = comment,
                    Popularity = wordPopularity
                };

                Domain.Models.Dictionary.Word word = await _wordRepository.AddWordAsync(newWord);

                _logger.LogInformation(AddedWordMessage, string.Join(Separator,word.Values), word.Id, word.PartOfSpeech);

                Models.Dictionary.Word result = _wordToAppMapper.Map(word);

                return result;
            }
            finally
            {
                ResilienceContextPool.Shared.Return(context);
            }
        }

        public async Task ChangeWordPartOfSpeech(int id, PartOfSpeech partOfSpeech)
        {
            ResilienceContext context = ResilienceContextPool.Shared.Get(operationKey: nameof(ChangeWordPartOfSpeech));

            try
            {
                await _retryPipeline.ExecuteAsync(async _ =>
                {
                    Domain.Models.Dictionary.Word[] words = await _wordRepository.GetWordsByIdsAsync([id], false);

                    if (words.Length == 0)
                    {
                        throw new InvalidOperationException(string.Format(WordNotFoundError, id));
                    }

                    Domain.Models.Dictionary.PartOfSpeech newPartOfSpeech = _partOfSpeechToDomMapper.Map(partOfSpeech);

                    Domain.Models.Dictionary.PartOfSpeech oldPartOfSpeech = words[0].PartOfSpeech;

                    words[0].ChangePartOfSpeech(newPartOfSpeech);

                    await _wordRepository.SaveWordsAsync(words);

                    _logger.LogInformation(ChangedPartOfSpeechMessage,
                        string.Join(Separator, words[0].Values),
                        words[0].Id,
                        oldPartOfSpeech,
                        newPartOfSpeech);
                }, context);
            }
            finally
            {
                ResilienceContextPool.Shared.Return(context);
            }
        }

        public async Task<Models.Dictionary.Word[]> GetWordsForLearningAsync()
        {
            ResilienceContext context = ResilienceContextPool.Shared.Get(operationKey: nameof(GetWordsForLearningAsync));

            try
            {
                return await _retryPipeline.ExecuteAsync(async _ =>
                {
                    Domain.Models.Dictionary.Word[] words = await _wordRepository.GetWordsForLearningAsync();

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
                    Domain.Models.Dictionary.Word[] words = await _wordRepository.GetWordsByIdsAsync(ids, false);

                    foreach (Domain.Models.Dictionary.Word word in words)
                    {
                        word.Learn();
                    }

                    await _wordRepository.SaveWordsAsync(words);

                    _logger.LogInformation(LearnedWordsMessage,
                        words.Length,
                        string.Join(Separator, words.Select(w => w.Id)),
                        string.Join("; ", words.Select(w => string.Join(Separator, w.Values))));
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
                    Domain.Models.Dictionary.Word[] words = await _wordRepository.GetWordsByIdsAsync(ids, false);

                    foreach (Domain.Models.Dictionary.Word word in words)
                    {
                        word.MarkForStudy();
                    }

                    await _wordRepository.SaveWordsAsync(words);

                    _logger.LogInformation(
                        MarkedWordsMessage,
                        words.Length,
                        string.Join(Separator, words.Select(w => w.Id)),
                        string.Join("; ", words.Select(w => string.Join(Separator, w.Values))));
                }, context);
            }
            finally
            {
                ResilienceContextPool.Shared.Return(context);
            }
        }

        public async Task<Models.Dictionary.Word[]> SearchWordsAsync(string[] entries)
        {
            ResilienceContext context = ResilienceContextPool.Shared.Get(operationKey: nameof(SearchWordsAsync));

            entries = entries.Select(el => el.ToLower().Trim()).ToArray();

            try
            {
                return await _retryPipeline.ExecuteAsync(async _ =>
                {
                    Domain.Models.Dictionary.Word[] words = await _wordRepository.SearchWordsAsync(entries);

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
