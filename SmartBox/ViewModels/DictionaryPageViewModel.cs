#pragma warning disable MVVMTK0045
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Shared;
using SmartBox.Interfaces;
using SmartBox.Models;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace SmartBox.ViewModels
{
    public partial class DictionaryPageViewModel : ObservableObject
    {
        #region Constants
        private readonly string[] Separators = [",", ", "];

        private const string ErrorDialogTitle = "Error";
        private const string UpdateUIErrorMessage = "Operation completed, but failed to update the interface";
        private const string SearchWordsErrorMessage = "Failed to search words";
        private const string LearnWordsErrorMessage = "Failed to mark words as learned";
        private const string MarkWordsErrorMessage = "Failed to mark words for further study";
        private const string LoadWordsErrorMessage = "Failed to get words for learning";
        private const string AddWordErrorMessage = "Failed to add new word";
        #endregion

        #region Fields
        private readonly ILogger<DictionaryPageViewModel> _logger;
        private readonly IDictionaryService _dictionaryService;
        private readonly IAlertService _alertService;

        private readonly IMapper<Application.Models.Dictionary.Word, Models.Dictionary.Word> _wordMapper;
        private readonly IMapper<Models.Dictionary.PartOfSpeech, Application.Models.Dictionary.PartOfSpeech> _partOfSpeechMapper;
        #endregion

        #region Properties
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddCommand))]
        private ObservableCollection<Models.Dictionary.Word> words = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LearnCommand))]
        [NotifyCanExecuteChangedFor(nameof(MarkCommand))]
        private ObservableCollection<object> rawSelectedWords = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddCommand))]
        private string searchText = string.Empty;

        [ObservableProperty]
        private string commentText = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddCommand))]
        private Models.Dictionary.PartOfSpeech selectedPartOfSpeech;

        [ObservableProperty]
        private ObservableCollection<Models.Dictionary.PartOfSpeech> partOfSpeechOptions = new(Enum.GetValues<Models.Dictionary.PartOfSpeech>());

        private Models.Dictionary.Word[] SelectedWords => RawSelectedWords.OfType<Models.Dictionary.Word>().ToArray();
        #endregion

        public DictionaryPageViewModel(ILogger<DictionaryPageViewModel> logger, 
                                       IDictionaryService dictionaryService, 
                                       IAlertService alertService,
                                       IMapper<Application.Models.Dictionary.Word, Models.Dictionary.Word> wordMapper,
                                       IMapper<Models.Dictionary.PartOfSpeech, Application.Models.Dictionary.PartOfSpeech> partOfSpeechMapper)
        {
            _logger = logger;

            _dictionaryService = dictionaryService;
            _alertService = alertService;

            _wordMapper = wordMapper;
            _partOfSpeechMapper = partOfSpeechMapper;

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

                Application.Models.Dictionary.Word[] appWords = await _dictionaryService.SearchWordsAsync(SearchText);

                Models.Dictionary.Word[] words = _wordMapper.MapToArray(appWords);

                await MainThread.InvokeOnMainThreadAsync(() =>  
                { 
                    Words = new ObservableCollection<Models.Dictionary.Word>(words); 
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
            try
            {
                IEnumerable<int> idsForLearning = SelectedWords.Select(word => word.Id);

                await _dictionaryService.LearnWordsAsync(idsForLearning);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, LearnWordsErrorMessage);

                await _alertService.DisplayAlertAsync(ErrorDialogTitle, $"{LearnWordsErrorMessage}: {ex.Message}");

                return;
            }

            try
            {
                foreach (Models.Dictionary.Word word in SelectedWords)
                {
                    word.ShouldStudy = false;
                    word.Learned = true;
                }

                RawSelectedWords = new ObservableCollection<object>();
            }
            catch  (Exception ex)
            {
                _logger.LogError(ex, UpdateUIErrorMessage);

                await _alertService.DisplayAlertAsync(ErrorDialogTitle, $"{UpdateUIErrorMessage}: {ex.Message}");
            }
        }

        [RelayCommand(CanExecute = nameof(CanMarkWords))]
        private async Task MarkAsync()
        {
            try
            {
                IEnumerable<int> idsForMarking = SelectedWords.Select(word => word.Id);

                await _dictionaryService.MarkWordsAsync(idsForMarking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, MarkWordsErrorMessage);

                await _alertService.DisplayAlertAsync(ErrorDialogTitle, $"{MarkWordsErrorMessage}: {ex.Message}");

                return;
            }

            try
            {
                foreach (Models.Dictionary.Word word in SelectedWords)
                {
                    word.ShouldStudy = true;
                }

                RawSelectedWords = new ObservableCollection<object>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, UpdateUIErrorMessage);

                await _alertService.DisplayAlertAsync(ErrorDialogTitle, $"{UpdateUIErrorMessage}: {ex.Message}");
            }
        }

        [RelayCommand(CanExecute = nameof(CanAddWord))]
        private async Task AddAsync()
        {
            Application.Models.Dictionary.Word addedWord;
            try
            {
                Application.Models.Dictionary.PartOfSpeech partOfSpeech = _partOfSpeechMapper.Map(SelectedPartOfSpeech);

                addedWord = await _dictionaryService.AddWordAsync(SearchText.Split(Separators, StringSplitOptions.TrimEntries), partOfSpeech, CommentText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, AddWordErrorMessage);

                await _alertService.DisplayAlertAsync(ErrorDialogTitle, $"{AddWordErrorMessage}: {ex.Message}");

                return;
            }

            try
            {
                Models.Dictionary.Word uiWord = _wordMapper.Map(addedWord);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Words.Insert(0, uiWord);
                });

                CommentText = string.Empty;
                SelectedPartOfSpeech = default;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, UpdateUIErrorMessage);

                await _alertService.DisplayAlertAsync(ErrorDialogTitle, $"{UpdateUIErrorMessage}: {ex.Message}");
            }
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

            if (Words.Any(el => el.PartOfSpeech == SelectedPartOfSpeech && el.Values.Contains(SearchText))) 
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

        partial void OnWordsChanged(ObservableCollection<Models.Dictionary.Word>? oldValue, ObservableCollection<Models.Dictionary.Word> newValue)
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
        private async Task<ObservableCollection<Models.Dictionary.Word>> GetWordsForLearningAsync()
        {
            try
            {
                Application.Models.Dictionary.Word[] appWords = await _dictionaryService.GetWordsForLearningAsync();

                Models.Dictionary.Word[] words = _wordMapper.MapToArray(appWords);

                return new ObservableCollection<Models.Dictionary.Word>(words);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, LoadWordsErrorMessage);

                await _alertService.DisplayAlertAsync(ErrorDialogTitle, $"{LoadWordsErrorMessage}: {ex.Message}");
            }

            return new ObservableCollection<Models.Dictionary.Word>();
        }
        #endregion
    }
}
