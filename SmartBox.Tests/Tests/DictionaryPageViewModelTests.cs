using Application.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using SmartBox.Interfaces;
using SmartBox.Mappers;
using SmartBox.ViewModels;

namespace SmartBox.Tests.Tests
{
    public class DictionaryPageViewModelTests
    {
        private readonly FakeDictionaryService _dictionaryService = new();
        private readonly FakeAlertService _alertService = new();
        private readonly DictionaryPageViewModel _viewModel;

        public DictionaryPageViewModelTests()
        {
            PartOfSpeechMapper partOfSpeechMapper = new();
            WordMapper wordMapper = new(partOfSpeechMapper, partOfSpeechMapper, partOfSpeechMapper);

            _viewModel = new DictionaryPageViewModel(NullLogger<DictionaryPageViewModel>.Instance, _dictionaryService, _alertService, wordMapper, partOfSpeechMapper);
        }

        [Fact]
        public async Task PageOpened_SearchFieldsFireWithEmptyText_WordsForLearningLoadedOnce()
        {
            await OpenPageAsync();

            Assert.Equal(1, _dictionaryService.WordsForLearningLoads);
        }

        [Fact]
        public async Task PageOpenedOffline_SearchFieldsFireWithEmptyText_OneAlert()
        {
            _dictionaryService.Error = new HttpRequestException("No such host is known");

            await OpenPageAsync();

            Assert.Single(_alertService.Messages);
        }

        [Fact]
        public async Task SearchByMeaning_SameTextOtherPartOfSpeech_SearchesAgain()
        {
            await OpenPageAsync();

            _viewModel.SelectedPartOfSpeech = SmartBox.Models.Dictionary.PartOfSpeech.Verb;
            await _viewModel.SearchByMeaningCommand.ExecuteAsync(null);

            Assert.Equal(2, _dictionaryService.WordsForLearningLoads);
        }

        // Так открывается страница: загрузка списка и срабатывание обоих полей поиска с начальным пустым текстом
        private async Task OpenPageAsync()
        {
            await _viewModel.InitializeAsync();
            await _viewModel.SearchCommand.ExecuteAsync(null);
            await _viewModel.SearchByMeaningCommand.ExecuteAsync(null);
        }

        private class FakeDictionaryService : IDictionaryService
        {
            public Exception? Error { get; set; }
            public int WordsForLearningLoads { get; private set; }

            public Task<Domain.Models.Dictionary.Word[]> GetWordsForLearningAsync()
            {
                WordsForLearningLoads++;

                return Error == null
                    ? Task.FromResult(Array.Empty<Domain.Models.Dictionary.Word>())
                    : Task.FromException<Domain.Models.Dictionary.Word[]>(Error);
            }

            public Task<Domain.Models.Dictionary.Word[]> SearchWordsAsync(string[] entries) => throw new NotSupportedException();
            public Task<Domain.Models.Dictionary.Word[]> SearchByMeaningAsync(string meaning, Domain.Models.Dictionary.PartOfSpeech? partOfSpeech) => throw new NotSupportedException();
            public Task LearnWordsAsync(IEnumerable<int> ids) => throw new NotSupportedException();
            public Task MarkWordsAsync(IEnumerable<int> ids) => throw new NotSupportedException();
            public Task<Domain.Models.Dictionary.Word> AddWordAsync(string[] wordValues, Domain.Models.Dictionary.PartOfSpeech partOfSpeech, string comment) => throw new NotSupportedException();
            public Task ChangeWordPartOfSpeechAsync(int id, Domain.Models.Dictionary.PartOfSpeech partOfSpeech) => throw new NotSupportedException();
        }

        private class FakeAlertService : IAlertService
        {
            public List<string> Messages { get; } = [];

            public Task DisplayAlertAsync(string message, string title = "Error", string cancel = "Ok")
            {
                Messages.Add(message);
                return Task.CompletedTask;
            }
        }
    }
}
