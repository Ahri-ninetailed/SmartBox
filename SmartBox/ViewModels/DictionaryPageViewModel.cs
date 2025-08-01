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
        #region Constants
        private const string ErrorDialogTitle = "Error";
        private const string SearchWordsErrorMessage = "Failed to search words";
        private const string LearnWordsErrorMessage = "Failed to mark words as learned";
        private const string MarkWordsErrorMessage = "Failed to mark words for further study";
        private const string LoadWordsErrorMessage = "Failed to get words for learning";
        #endregion

        #region Fields
        private readonly ILogger<DictionaryPageViewModel> _logger;
        private readonly IDictionaryService _dictionaryService;
        private readonly IAlertService _alertService;
        #endregion

        #region Properties
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddCommand))]
        private ObservableCollection<Word> words = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LearnCommand))]
        [NotifyCanExecuteChangedFor(nameof(MarkCommand))]
        private ObservableCollection<object> rawSelectedWords = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddCommand))]
        private string searchText = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddCommand))]
        private PartOfSpeech selectedPartOfSpeech;

        [ObservableProperty]
        private ObservableCollection<PartOfSpeech> partOfSpeechOptions = new(Enum.GetValues<PartOfSpeech>());

        private Word[] SelectedWords => RawSelectedWords.OfType<Word>().ToArray();
        #endregion

        public DictionaryPageViewModel(ILogger<DictionaryPageViewModel> logger, IDictionaryService dictionaryService, IAlertService alertService)
        {
            _logger = logger;

            _dictionaryService = dictionaryService;
            _alertService = alertService;

            RawSelectedWords.CollectionChanged += OnSelectedWordsChanged;

            Words.CollectionChanged += OnWordsCollectionChanged;
        }

        #region Public Methods
        public async Task InitializeAsync()
        {
            Words = await GetWordsForLearningAsync();
        }
        #endregion

        #region Commands
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

                await MainThread.InvokeOnMainThreadAsync(() =>  
                { 
                    Words = new ObservableCollection<Word>(words); 
                });
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

            RawSelectedWords = new ObservableCollection<object>();
        }

        [RelayCommand(CanExecute = nameof(CanAddWord))]
        private async Task AddAsync()
        {

        }
        #endregion

        #region CanExecute Methods
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

        private bool CanAddWord()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                return false;
            }

            if (Words.Any(el => el.PartOfSpeech == SelectedPartOfSpeech && el.Value == SearchText)) 
            {
                return false;
            }

            return true;
        }
        #endregion

        #region Property Changed Handlers

        #endregion

        #region Event Handlers

        partial void OnRawSelectedWordsChanged(ObservableCollection<object>? oldValue, ObservableCollection<object> newValue)
        {
            if (oldValue != null)
            {
                oldValue.CollectionChanged -= OnSelectedWordsChanged;
            }

            if (newValue != null)
            {
                newValue.CollectionChanged += OnSelectedWordsChanged;
            }
        }

        private void OnSelectedWordsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            LearnCommand.NotifyCanExecuteChanged();
            MarkCommand.NotifyCanExecuteChanged();
        }

        partial void OnWordsChanged(ObservableCollection<Word>? oldValue, ObservableCollection<Word> newValue)
        {
            if (oldValue != null)
            {
                oldValue.CollectionChanged -= OnWordsCollectionChanged;
            }

            if (newValue != null)
            {
                newValue.CollectionChanged += OnWordsCollectionChanged;
            }
        }

        private void OnWordsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            AddCommand.NotifyCanExecuteChanged();
        }

        #endregion

        #region Private Helper Methods
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
        #endregion
    }
}
