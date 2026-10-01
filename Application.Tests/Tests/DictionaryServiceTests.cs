using Application.Services;
using Domain.Interfaces;
using Domain.Models.Dictionary;
using Microsoft.Extensions.Logging.Abstractions;

namespace Application.Tests.Tests
{
    public class DictionaryServiceTests
    {
        private readonly FakeWordRepository _repository = new();
        private readonly FakeWordPopularityService _popularityService = new();
        private readonly DictionaryService _service;

        public DictionaryServiceTests()
        {
            _service = new DictionaryService(NullLogger<DictionaryService>.Instance, _repository, _popularityService);
        }

        [Fact]
        public async Task AddWordAsync_ValuesWithCapitalsAndSpaces_CheckedAndSavedNormalized()
        {
            Word word = await _service.AddWordAsync([" Odds and Ends", "ODDS AND SODS "], PartOfSpeech.Other, "comment");

            string[] expected = ["odds and ends", "odds and sods"];
            Assert.Equal(expected, _repository.CheckedValues);
            Assert.Equal(expected, _popularityService.RequestedValues);
            Assert.Equal(expected, word.Values);
            Assert.Equal(0.5m, word.Popularity);
        }

        [Fact]
        public async Task AddWordAsync_EmptyValue_ThrowsWithoutTouchingRepository()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _service.AddWordAsync(["run", " "], PartOfSpeech.Verb, ""));

            Assert.Null(_repository.CheckedValues);
            Assert.Null(_repository.AddedWord);
        }

        [Fact]
        public async Task AddWordAsync_WordExists_ThrowsWithoutSaving()
        {
            _repository.WordExists = true;

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AddWordAsync(["run"], PartOfSpeech.Verb, ""));

            Assert.Null(_repository.AddedWord);
        }

        [Fact]
        public async Task AddWordAsync_PopularityFailed_WordSavedWithoutPopularity()
        {
            _popularityService.Error = new HttpRequestException("ngrams unavailable");

            Word word = await _service.AddWordAsync(["run"], PartOfSpeech.Verb, "");

            Assert.Null(word.Popularity);
        }

        [Fact]
        public async Task SearchWordsAsync_Entries_PassedNormalized()
        {
            await _service.SearchWordsAsync([" Run ", "WALK"]);

            string[] expected = ["run", "walk"];
            Assert.Equal(expected, _repository.SearchedEntries);
        }

        [Fact]
        public async Task SearchByMeaningAsync_Meaning_PassedAsIs()
        {
            // Регистронезависимость поиска обеспечивает репозиторий
            await _service.SearchByMeaningAsync("Day Before", null);

            Assert.Equal("Day Before", _repository.SearchedMeaning);
        }

        private class FakeWordRepository : IWordRepository
        {
            public bool WordExists { get; set; }
            public string[]? CheckedValues { get; private set; }
            public Word? AddedWord { get; private set; }
            public string[]? SearchedEntries { get; private set; }
            public string? SearchedMeaning { get; private set; }

            public Task<bool> IsWordExistedAsync(string[] wordValues, PartOfSpeech partOfSpeech)
            {
                CheckedValues = wordValues;
                return Task.FromResult(WordExists);
            }

            public Task<Word> AddWordAsync(Word newWord)
            {
                AddedWord = newWord;
                return Task.FromResult(newWord);
            }

            public Task<Word[]> SearchWordsAsync(string[] entries, int count = 100)
            {
                SearchedEntries = entries;
                return Task.FromResult(Array.Empty<Word>());
            }

            public Task<Word[]> SearchByMeaningAsync(string meaning, PartOfSpeech? partOfSpeech, int count = 500)
            {
                SearchedMeaning = meaning;
                return Task.FromResult(Array.Empty<Word>());
            }

            public Task<Word[]> GetWordsForLearningAsync(int count = 40) => throw new NotSupportedException();
            public Task<Word[]> GetWordsByIdsAsync(IEnumerable<int> ids, bool includeRelatedData) => throw new NotSupportedException();
            public Task SaveWordsAsync(Word[] words) => throw new NotSupportedException();
            public Task SyncDefinitionsAndExamplesAsync(int wordId, Definition[] definitions) => throw new NotSupportedException();
        }

        private class FakeWordPopularityService : IWordPopularityService
        {
            public Exception? Error { get; set; }
            public string[]? RequestedValues { get; private set; }

            public Task<decimal?> GetWordPopularityAsync(string[] words)
            {
                RequestedValues = words;
                return Error == null ? Task.FromResult<decimal?>(0.5m) : Task.FromException<decimal?>(Error);
            }
        }
    }
}
