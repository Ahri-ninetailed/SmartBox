#pragma warning disable MVVMTK0045
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapster;
using Microsoft.Extensions.Logging;
using SmartBox.Interfaces;
using SmartBox.Models;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace SmartBox.ViewModels
{
    public partial class DictionaryPageViewModel : ObservableObject
    {
        private const string ErrorDialogTitle = "Error";
        private const string SearchWordsErrorMessage = "Failed to search words";
        private const string LearnWordsErrorMessage = "Failed to mark words as learned";
        private const string MarkWordsErrorMessage = "Failed to mark words for further study";
        private const string LoadWordsErrorMessage = "Failed to get words for learning";

        private readonly ILogger<DictionaryPageViewModel> _logger;
        private readonly IDictionaryService _dictionaryService;
        private readonly IAlertService _alertService;

        [ObservableProperty]
        private ObservableCollection<Word> words = new();

        [ObservableProperty]
        private ObservableCollection<object> rawSelectedWords = new();

        [ObservableProperty]
        private string searchText = string.Empty;

        private Word[] SelectedWords => RawSelectedWords.OfType<Word>().ToArray();

        public DictionaryPageViewModel(ILogger<DictionaryPageViewModel> logger, IDictionaryService dictionaryService, IAlertService alertService)
        {
            _logger = logger;

            RawSelectedWords.CollectionChanged += OnSelectedWordsChanged;

            _dictionaryService = dictionaryService;
            _alertService = alertService;
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    Words = await GetWordsForLearningAsync();

                    return;
                }

                Application.Models.Word[] appWords = await _dictionaryService.SearchWordsAsync(SearchText);

                Word[] words = appWords.Adapt<Word[]>();

                Words = new ObservableCollection<Word>(words);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, SearchWordsErrorMessage);

                await _alertService.DisplayAlertAsync(ErrorDialogTitle, $"{SearchWordsErrorMessage}: {ex.Message}");
            }
        }

        [RelayCommand(CanExecute = nameof(CanLearnWords))]
        private async Task LearnAsync()
        {
            IEnumerable<int> idsForLearning = SelectedWords.Select(word => word.Id);

            try
            {
                //await _dictionaryService.LearnWordsAsync(idsForLearning);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, LearnWordsErrorMessage);

                await _alertService.DisplayAlertAsync(ErrorDialogTitle, $"{LearnWordsErrorMessage}: {ex.Message}");

                return;
            }

            foreach (Word word in SelectedWords)
            {
                Words.Remove(word);
            }
        }

        [RelayCommand(CanExecute = nameof(CanMarkWords))]
        private async Task MarkAsync()
        {
            IEnumerable<int> idsForMarking = SelectedWords.Select(word => word.Id);

            try
            {
                //await _dictionaryService.MarkWordsAsync(idsForMarking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, MarkWordsErrorMessage);

                await _alertService.DisplayAlertAsync(ErrorDialogTitle, $"{MarkWordsErrorMessage}: {ex.Message}");

                return;
            }

            foreach (Word word in SelectedWords)
            {
                word.ShouldStudy = true;
            }

            ClearSelectedWords();
        }

        public async Task InitializeAsync()
        {
            Words = await GetWordsForLearningAsync();
        }

        private async Task<ObservableCollection<Word>> GetWordsForLearningAsync()
        {
            try
            {
                Application.Models.Word[] appWords = await _dictionaryService.GetWordsForLearningAsync();

                Word[] words = appWords.Adapt<Word[]>();

                return new ObservableCollection<Word>(words);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, LoadWordsErrorMessage);

                await _alertService.DisplayAlertAsync(ErrorDialogTitle, $"{LoadWordsErrorMessage}: {ex.Message}");
            }

            return new ObservableCollection<Word>();
        }

        private void OnSelectedWordsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            LearnCommand.NotifyCanExecuteChanged();
            MarkCommand.NotifyCanExecuteChanged();
        }

        private void ClearSelectedWords()
        {
            RawSelectedWords.CollectionChanged -= OnSelectedWordsChanged;
            RawSelectedWords.Clear();
            RawSelectedWords.CollectionChanged += OnSelectedWordsChanged;

            LearnCommand.NotifyCanExecuteChanged();
            MarkCommand.NotifyCanExecuteChanged();
        }

        private bool CanLearnWords()
        {
            return SelectedWords.Any() &&
                   SelectedWords.All(word => word.Learned == false);
        }

        private bool CanMarkWords()
        {
            return SelectedWords.Any() &&
                   SelectedWords.All(word => word.ShouldStudy == false && word.Learned == false);
        }
    }
}
