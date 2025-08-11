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

        private const string UpdateUIErrorMessage = "Operation completed, but failed to update the interface";
        private const string SearchWordsErrorMessage = "Failed to search words";
        private const string LearnWordsErrorMessage = "Failed to mark words as learned";
        private const string MarkWordsErrorMessage = "Failed to mark words for further study";
        private const string LoadWordsErrorMessage = "Failed to get words for learning";
        private const string AddWordErrorMessage = "Failed to add new word";
        private const string ChangePartOfSpeechErrorMessage = "Failed to change word part of speech";
        private const string InvalidWordSelectionForChangeError = "Please select exactly one word to change its part of speech";
        private const string NoSelectedWordForDefinitionsError = "No selected word selected.";

        #endregion

        #region Fields
        private readonly ILogger<DictionaryPageViewModel> _logger;
        private readonly IDictionaryService _dictionaryService;
        private readonly IAlertService _alertService;

        private readonly IMapper<Application.Models.Dictionary.Word, SmartBox.Models.Dictionary.Word> _wordMapper;
        private readonly IMapper<SmartBox.Models.Dictionary.PartOfSpeech, Application.Models.Dictionary.PartOfSpeech> _partOfSpeechMapper;
        #endregion

        #region Properties
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddCommand))]
        private ObservableCollection<Models.Dictionary.Word> words = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LearnCommand))]
        [NotifyCanExecuteChangedFor(nameof(MarkCommand))]
        [NotifyCanExecuteChangedFor(nameof(ChangeCommand))]
        [NotifyCanExecuteChangedFor(nameof(ShowDefinitionsCommand))]
        private ObservableCollection<object> rawSelectedWords = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddCommand))]
        private string searchText = string.Empty;

        public string[] SearchWords => SearchText.Split(Separators, StringSplitOptions.TrimEntries);

        [ObservableProperty]
        private string commentText = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddCommand))]
        [NotifyCanExecuteChangedFor(nameof(ChangeCommand))]
        private Models.Dictionary.PartOfSpeech selectedPartOfSpeech = Models.Dictionary.PartOfSpeech.Other;

        [ObservableProperty]
        private ObservableCollection<Models.Dictionary.PartOfSpeech> partOfSpeechOptions = new()
        {
            Models.Dictionary.PartOfSpeech.Other,
            Models.Dictionary.PartOfSpeech.Verb
        };

        private Models.Dictionary.Word[] SelectedWords => RawSelectedWords.OfType<Models.Dictionary.Word>().ToArray();
        #endregion

        public DictionaryPageViewModel(ILogger<DictionaryPageViewModel> logger,
                                       IDictionaryService dictionaryService,
                                       IAlertService alertService,
                                       IMapper<Application.Models.Dictionary.Word, Models.Dictionary.Word> wordMapper,
                                       IMapper<Models.Dictionary.PartOfSpeech, Application.Models.Dictionary.PartOfSpeech> partOfSpeechMapper)
        {
            _logger = logger;
            _logger.LogTrace("Constructor started - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);

            _dictionaryService = dictionaryService;
            _alertService = alertService;

            _wordMapper = wordMapper;
            _partOfSpeechMapper = partOfSpeechMapper;

            _logger.LogTrace("Subscribing to RawSelectedWords.CollectionChanged event");
            RawSelectedWords.CollectionChanged += OnSelectedWordsChanged;

            _logger.LogTrace("Subscribing to Words.CollectionChanged event");
            Words.CollectionChanged += OnWordsCollectionChanged;

            _logger.LogTrace("Constructor completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
        }

        #region Public Methods
        public async Task InitializeAsync()
        {
            _logger.LogTrace("InitializeAsync started - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);

            try
            {
                _logger.LogTrace("Calling GetWordsForLearningAsync");
                Words = await GetWordsForLearningAsync();
                _logger.LogTrace("GetWordsForLearningAsync completed, Words count: {Count}", Words.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InitializeAsync failed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                throw;
            }
            finally
            {
                _logger.LogTrace("InitializeAsync completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
        }
        #endregion

        #region Commands
        [RelayCommand(CanExecute = nameof(CanShowDefinitions))]
        private async Task ShowDefinitions()
        {
            _logger.LogTrace("ShowDefinitions started - Thread: {ThreadId}, SelectedWords count: {Count}",
                Thread.CurrentThread.ManagedThreadId, SelectedWords.Length);

            try
            {
                if (SelectedWords.Length != 1)
                {
                    _logger.LogTrace("Invalid selection, showing alert - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                    await _alertService.DisplayAlertAsync(NoSelectedWordForDefinitionsError);
                    return;
                }

                Models.Dictionary.Word selectedWord = SelectedWords.First();
                _logger.LogTrace("Selected word: {WordId} - Thread: {ThreadId}", selectedWord.Id, Thread.CurrentThread.ManagedThreadId);

                _logger.LogTrace("Clearing RawSelectedWords collection - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                RawSelectedWords = new ObservableCollection<object>();

                _logger.LogTrace("Navigating to parserpage - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                await Shell.Current.GoToAsync($"///parserpage?wordId={selectedWord.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ShowDefinitions failed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                throw;
            }
            finally
            {
                _logger.LogTrace("ShowDefinitions completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            _logger.LogTrace("SearchAsync started - Thread: {ThreadId}, SearchText: '{SearchText}'",
                Thread.CurrentThread.ManagedThreadId, SearchText);

            try
            {
                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    _logger.LogTrace("SearchText is empty, getting all words for learning - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                    Words = await GetWordsForLearningAsync();
                    return;
                }

                _logger.LogTrace("Calling dictionary service SearchWordsAsync - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                Application.Models.Dictionary.Word[] appWords = await _dictionaryService.SearchWordsAsync(SearchWords);
                _logger.LogTrace("SearchWordsAsync completed, found {Count} words - Thread: {ThreadId}",
                    appWords.Length, Thread.CurrentThread.ManagedThreadId);

                _logger.LogTrace("Mapping words - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                Models.Dictionary.Word[] words = _wordMapper.MapToArray(appWords);

                _logger.LogTrace("Invoking on main thread to update Words collection - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    _logger.LogTrace("Main thread action started - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                    Words = new ObservableCollection<Models.Dictionary.Word>(words);
                    _logger.LogTrace("Main thread action completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SearchAsync failed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                await _alertService.DisplayAlertAsync($"{SearchWordsErrorMessage}: {ex.Message}");
            }
            finally
            {
                _logger.LogTrace("SearchAsync completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
        }

        [RelayCommand(CanExecute = nameof(CanLearnWords))]
        private async Task LearnAsync()
        {
            _logger.LogTrace("LearnAsync started - Thread: {ThreadId}, SelectedWords count: {Count}",
                Thread.CurrentThread.ManagedThreadId, SelectedWords.Length);

            try
            {
                _logger.LogTrace("Extracting IDs for learning - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                IEnumerable<int> idsForLearning = SelectedWords.Select(word => word.Id);

                _logger.LogTrace("Calling dictionary service LearnWordsAsync - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                await _dictionaryService.LearnWordsAsync(idsForLearning);
                _logger.LogTrace("LearnWordsAsync completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LearnAsync service call failed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                await _alertService.DisplayAlertAsync($"{LearnWordsErrorMessage}: {ex.Message}");
                return;
            }

            try
            {
                _logger.LogTrace("Updating UI properties - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                foreach (Models.Dictionary.Word word in SelectedWords)
                {
                    _logger.LogTrace("Updating word {WordId} properties - Thread: {ThreadId}", word.Id, Thread.CurrentThread.ManagedThreadId);
                    word.ShouldStudy = false;
                    word.Learned = true;
                }

                _logger.LogTrace("Clearing RawSelectedWords collection - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                RawSelectedWords = new ObservableCollection<object>();
                _logger.LogTrace("UI update completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LearnAsync UI update failed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                await _alertService.DisplayAlertAsync($"{UpdateUIErrorMessage}: {ex.Message}");
            }
            finally
            {
                _logger.LogTrace("LearnAsync completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
        }

        [RelayCommand(CanExecute = nameof(CanMarkWords))]
        private async Task MarkAsync()
        {
            _logger.LogTrace("MarkAsync started - Thread: {ThreadId}, SelectedWords count: {Count}",
                Thread.CurrentThread.ManagedThreadId, SelectedWords.Length);

            try
            {
                _logger.LogTrace("Extracting IDs for marking - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                IEnumerable<int> idsForMarking = SelectedWords.Select(word => word.Id);

                _logger.LogTrace("Calling dictionary service MarkWordsAsync - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                await _dictionaryService.MarkWordsAsync(idsForMarking);
                _logger.LogTrace("MarkWordsAsync completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MarkAsync service call failed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                await _alertService.DisplayAlertAsync($"{MarkWordsErrorMessage}: {ex.Message}");
                return;
            }

            try
            {
                _logger.LogTrace("Updating UI properties - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                foreach (Models.Dictionary.Word word in SelectedWords)
                {
                    _logger.LogTrace("Updating word {WordId} ShouldStudy property - Thread: {ThreadId}", word.Id, Thread.CurrentThread.ManagedThreadId);
                    word.ShouldStudy = true;
                }

                _logger.LogTrace("Clearing RawSelectedWords collection - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                RawSelectedWords = new ObservableCollection<object>();
                _logger.LogTrace("UI update completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MarkAsync UI update failed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                await _alertService.DisplayAlertAsync($"{UpdateUIErrorMessage}: {ex.Message}");
            }
            finally
            {
                _logger.LogTrace("MarkAsync completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
        }

        [RelayCommand(CanExecute = nameof(CanAddWord))]
        private async Task AddAsync()
        {
            _logger.LogTrace("AddAsync started - Thread: {ThreadId}, SearchWords: [{SearchWords}], PartOfSpeech: {PartOfSpeech}",
                Thread.CurrentThread.ManagedThreadId, string.Join(", ", SearchWords), SelectedPartOfSpeech);

            Application.Models.Dictionary.Word addedWord;
            try
            {
                _logger.LogTrace("Mapping part of speech - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                Application.Models.Dictionary.PartOfSpeech partOfSpeech = _partOfSpeechMapper.Map(SelectedPartOfSpeech);

                _logger.LogTrace("Calling dictionary service AddWordAsync - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                addedWord = await _dictionaryService.AddWordAsync(SearchWords, partOfSpeech, CommentText);
                _logger.LogTrace("AddWordAsync completed, added word ID: {WordId} - Thread: {ThreadId}", addedWord.Id, Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddAsync service call failed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                await _alertService.DisplayAlertAsync($"{AddWordErrorMessage}: {ex.Message}");
                return;
            }

            try
            {
                _logger.LogTrace("Mapping added word for UI - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                Models.Dictionary.Word uiWord = _wordMapper.Map(addedWord);

                _logger.LogTrace("Invoking on main thread to add word to collection - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    _logger.LogTrace("Main thread action started - adding word to Words collection - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                    Words.Insert(0, uiWord);
                    _logger.LogTrace("Main thread action completed - word added - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                });

                _logger.LogTrace("Clearing form fields - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                CommentText = string.Empty;
                SelectedPartOfSpeech = Models.Dictionary.PartOfSpeech.Other;
                _logger.LogTrace("UI update completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddAsync UI update failed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                await _alertService.DisplayAlertAsync($"{UpdateUIErrorMessage}: {ex.Message}");
            }
            finally
            {
                _logger.LogTrace("AddAsync completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
        }

        [RelayCommand(CanExecute = nameof(CanChangeWord))]
        private async Task ChangeAsync()
        {
            _logger.LogTrace("ChangeAsync started - Thread: {ThreadId}, SelectedWords count: {Count}, NewPartOfSpeech: {PartOfSpeech}",
                Thread.CurrentThread.ManagedThreadId, SelectedWords.Length, SelectedPartOfSpeech);

            try
            {
                if (SelectedWords.Length != 1)
                {
                    _logger.LogTrace("Invalid selection count - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                    throw new InvalidOperationException(InvalidWordSelectionForChangeError);
                }

                _logger.LogTrace("Mapping new part of speech - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                Application.Models.Dictionary.PartOfSpeech newPartOfSpeech = _partOfSpeechMapper.Map(SelectedPartOfSpeech);

                _logger.LogTrace("Calling dictionary service ChangeWordPartOfSpeech for word {WordId} - Thread: {ThreadId}",
                    SelectedWords[0].Id, Thread.CurrentThread.ManagedThreadId);
                await _dictionaryService.ChangeWordPartOfSpeech(SelectedWords[0].Id, newPartOfSpeech);
                _logger.LogTrace("ChangeWordPartOfSpeech completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);

                _logger.LogTrace("Updating UI word part of speech - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                SelectedWords[0].PartOfSpeech = SelectedPartOfSpeech;
                _logger.LogTrace("UI update completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ChangeAsync failed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                await _alertService.DisplayAlertAsync($"{ChangePartOfSpeechErrorMessage}: {ex.Message}");
            }
            finally
            {
                _logger.LogTrace("ChangeAsync completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
        }
        #endregion

        #region CanExecute Methods
        private bool CanLearnWords()
        {
            _logger.LogTrace("CanLearnWords called - Thread: {ThreadId}, SelectedWords count: {Count}",
                Thread.CurrentThread.ManagedThreadId, SelectedWords.Length);

            bool result = SelectedWords.Any() && SelectedWords.All(word => word.Learned == false);

            _logger.LogTrace("CanLearnWords result: {Result} - Thread: {ThreadId}", result, Thread.CurrentThread.ManagedThreadId);
            return result;
        }

        private bool CanMarkWords()
        {
            _logger.LogTrace("CanMarkWords called - Thread: {ThreadId}, SelectedWords count: {Count}",
                Thread.CurrentThread.ManagedThreadId, SelectedWords.Length);

            bool result = SelectedWords.Any() && SelectedWords.All(word => word.ShouldStudy == false && word.Learned == false);

            _logger.LogTrace("CanMarkWords result: {Result} - Thread: {ThreadId}", result, Thread.CurrentThread.ManagedThreadId);
            return result;
        }

        private bool CanShowDefinitions()
        {
            _logger.LogTrace("CanShowDefinitions called - Thread: {ThreadId}, SelectedWords count: {Count}",
                Thread.CurrentThread.ManagedThreadId, SelectedWords.Length);

            bool result = SelectedWords.Length == 1;

            _logger.LogTrace("CanShowDefinitions result: {Result} - Thread: {ThreadId}", result, Thread.CurrentThread.ManagedThreadId);
            return result;
        }

        private bool CanChangeWord()
        {
            _logger.LogTrace("CanChangeWord called - Thread: {ThreadId}, SelectedWords count: {Count}, SelectedPartOfSpeech: {PartOfSpeech}",
                Thread.CurrentThread.ManagedThreadId, SelectedWords.Length, SelectedPartOfSpeech);

            bool result = SelectedWords.Length == 1 &&
                         SelectedWords[0].PartOfSpeech == Models.Dictionary.PartOfSpeech.Unknown &&
                         SelectedPartOfSpeech != Models.Dictionary.PartOfSpeech.Unknown;

            _logger.LogTrace("CanChangeWord result: {Result} - Thread: {ThreadId}", result, Thread.CurrentThread.ManagedThreadId);
            return result;
        }

        private bool CanAddWord()
        {
            _logger.LogTrace("CanAddWord called - Thread: {ThreadId}, SearchText: '{SearchText}', SelectedPartOfSpeech: {PartOfSpeech}",
                Thread.CurrentThread.ManagedThreadId, SearchText, SelectedPartOfSpeech);

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                _logger.LogTrace("CanAddWord result: false (empty search text) - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                return false;
            }

            if (Words.Any(el => el.PartOfSpeech == SelectedPartOfSpeech && SearchWords.Any(searchWord => el.Values.Contains(searchWord))))
            {
                _logger.LogTrace("CanAddWord result: false (word already exists) - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                return false;
            }

            _logger.LogTrace("CanAddWord result: true - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            return true;
        }
        #endregion

        #region Property Changed Handlers

        #endregion

        #region Event Handlers

        partial void OnRawSelectedWordsChanged(ObservableCollection<object>? oldValue, ObservableCollection<object> newValue)
        {
            _logger.LogTrace("OnRawSelectedWordsChanged started - Thread: {ThreadId}, Old count: {OldCount}, New count: {NewCount}",
                Thread.CurrentThread.ManagedThreadId, oldValue?.Count ?? 0, newValue?.Count ?? 0);

            try
            {
                if (oldValue != null)
                {
                    _logger.LogTrace("Unsubscribing from old collection events - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                    oldValue.CollectionChanged -= OnSelectedWordsChanged;
                }

                if (newValue != null)
                {
                    _logger.LogTrace("Subscribing to new collection events - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                    newValue.CollectionChanged += OnSelectedWordsChanged;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnRawSelectedWordsChanged failed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                throw;
            }
            finally
            {
                _logger.LogTrace("OnRawSelectedWordsChanged completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
        }

        private void OnSelectedWordsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            _logger.LogTrace("OnSelectedWordsChanged started - Thread: {ThreadId}, Action: {Action}",
                Thread.CurrentThread.ManagedThreadId, e.Action);

            try
            {
                _logger.LogTrace("Notifying command can execute changed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                LearnCommand.NotifyCanExecuteChanged();
                MarkCommand.NotifyCanExecuteChanged();
                ChangeCommand.NotifyCanExecuteChanged();
                ShowDefinitionsCommand.NotifyCanExecuteChanged();
                _logger.LogTrace("All commands notified - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnSelectedWordsChanged failed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                throw;
            }
            finally
            {
                _logger.LogTrace("OnSelectedWordsChanged completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
        }

        partial void OnWordsChanged(ObservableCollection<Models.Dictionary.Word>? oldValue, ObservableCollection<Models.Dictionary.Word> newValue)
        {
            _logger.LogTrace("OnWordsChanged started - Thread: {ThreadId}, Old count: {OldCount}, New count: {NewCount}",
                Thread.CurrentThread.ManagedThreadId, oldValue?.Count ?? 0, newValue?.Count ?? 0);

            try
            {
                if (oldValue != null)
                {
                    _logger.LogTrace("Unsubscribing from old Words collection events - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                    oldValue.CollectionChanged -= OnWordsCollectionChanged;
                }

                if (newValue != null)
                {
                    _logger.LogTrace("Subscribing to new Words collection events - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                    newValue.CollectionChanged += OnWordsCollectionChanged;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnWordsChanged failed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                throw;
            }
            finally
            {
                _logger.LogTrace("OnWordsChanged completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
        }

        private void OnWordsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            _logger.LogTrace("OnWordsCollectionChanged started - Thread: {ThreadId}, Action: {Action}",
                Thread.CurrentThread.ManagedThreadId, e.Action);

            try
            {
                _logger.LogTrace("Notifying AddCommand can execute changed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                AddCommand.NotifyCanExecuteChanged();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnWordsCollectionChanged failed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                throw;
            }
            finally
            {
                _logger.LogTrace("OnWordsCollectionChanged completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
        }

        #endregion

        #region Private Helper Methods
        private async Task<ObservableCollection<Models.Dictionary.Word>> GetWordsForLearningAsync()
        {
            _logger.LogTrace("GetWordsForLearningAsync started - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);

            try
            {
                _logger.LogTrace("Calling dictionary service GetWordsForLearningAsync - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                Application.Models.Dictionary.Word[] appWords = await _dictionaryService.GetWordsForLearningAsync();
                _logger.LogTrace("GetWordsForLearningAsync service call completed, found {Count} words - Thread: {ThreadId}",
                    appWords.Length, Thread.CurrentThread.ManagedThreadId);

                _logger.LogTrace("Mapping words - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                Models.Dictionary.Word[] words = _wordMapper.MapToArray(appWords);

                var result = new ObservableCollection<Models.Dictionary.Word>(words);
                _logger.LogTrace("GetWordsForLearningAsync completed successfully, returning {Count} words - Thread: {ThreadId}",
                    result.Count, Thread.CurrentThread.ManagedThreadId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetWordsForLearningAsync failed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                await _alertService.DisplayAlertAsync($"{LoadWordsErrorMessage}: {ex.Message}");
            }

            _logger.LogTrace("GetWordsForLearningAsync returning empty collection - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            return new ObservableCollection<Models.Dictionary.Word>();
        }
        #endregion
    }
}