#pragma warning disable MVVMTK0045

using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Shared;
using SmartBox.Interfaces;
using SmartBox.Models.Parser;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace SmartBox.ViewModels
{
    [QueryProperty(nameof(WordId), "wordId")]
    public partial class ParserPageViewModel : ObservableObject
    {
        #region Constants
        private readonly string[] Separators = [", ", ","];
        private const string ParseErrorMessage = "An error occurred while parsing";
        private const string ExportPlaceholderFormat = "To export: {0}";
        private const string ExportErrorMessage = "An error occurred while exporting";
        private const string LoadWordFromDbErrorMessageFormat = "Error loading word from database with ID {0}";
        private const string EnrichErrorMessage = "An error occurred while enriching definitions";
        private const string AddErrorMessage = "An error occurred while adding a word";
        private const string NoNonDbWordSelectedError = "No words selected for addition";
        private const string InvalidDbWordSelectionError = "Definitions can be added only for a single word from the database";

        #endregion

        #region Fields
        private readonly ILogger<ParserPageViewModel> _logger;

        private readonly IParserService _parserService;
        private readonly IAlertService _alertService;
        public IPromptService? _promptService;

        private readonly IMapper<Application.Models.Parser.Word, SmartBox.Models.Parser.Word> _wordToUIParserMapper;
        private readonly IMapper<SmartBox.Models.Parser.WordPack, Application.Models.Parser.Word> _wordpackToAppParserMapper;
        private readonly IMapper<SmartBox.Models.Parser.Meaning, Application.Models.Parser.Definition> _definitionToAppMapper;

        #endregion

        #region Properties
        [ObservableProperty]
        private ObservableCollection<Word> treeViewNodes = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ProcessInputCommand))]
        private string entryText = string.Empty;

        [ObservableProperty]
        private ObservableCollection<WordPack> wordsForExport = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RemoveWordsForExportCommand))]
        private WordPack? selectedWordsForExport;

        [ObservableProperty]
        private int wordId;

        [ObservableProperty]
        private bool isAiEnabled = false;

        private WordPack? CompiledSelectedWordsForExport => GetCompiledSelectedWords();

        public string ExportPlaceholder => string.Format(ExportPlaceholderFormat, WordsForExport.Count);

        #endregion

        public ParserPageViewModel(ILogger<ParserPageViewModel> logger,
                                   IParserService parserService,
                                   IAlertService alertService,
                                   IMapper<Application.Models.Parser.Word, Word> wordToUIParserMapper,
                                   IMapper<WordPack, Application.Models.Parser.Word> wordpackToAppParserMapper,
                                   IMapper<SmartBox.Models.Parser.Meaning, Application.Models.Parser.Definition> definitionToAppMapper)
        {
            _logger = logger;
            _logger.LogTrace("Constructor started - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);

            _parserService = parserService;
            _alertService = alertService;

            _wordToUIParserMapper = wordToUIParserMapper;
            _wordpackToAppParserMapper = wordpackToAppParserMapper;
            _definitionToAppMapper = definitionToAppMapper;

            _logger.LogTrace("Subscribing to WordsForExport.CollectionChanged event");
            WordsForExport.CollectionChanged += OnWordsForExportCollectionChanged;

            _logger.LogTrace("Constructor completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
        }

        #region Public Methods

        #endregion

        #region Commands
        [RelayCommand(CanExecute = nameof(CanAdd))]
        private async Task AddAsync()
        {
            _logger.LogTrace("AddAsync started - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);

            try
            {
                _logger.LogTrace("Filtering non-DB words - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                IEnumerable<Word> nonDbWord = TreeViewNodes
                    .Where(word => word.Id == 0 && word.IsSelected && word.IsEnabled);

                _logger.LogTrace("Found {Count} non-DB words - Thread: {ThreadId}", nonDbWord.Count(), Thread.CurrentThread.ManagedThreadId);

                if (nonDbWord.Any() == false)
                {
                    _logger.LogTrace("No non-DB words selected, showing alert - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                    await _alertService.DisplayAlertAsync(NoNonDbWordSelectedError);
                    return;
                }

                _logger.LogTrace("Checking DB words count - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                var dbWordsCount = TreeViewNodes.Where(word => word.Id != 0).Count();
                _logger.LogTrace("Found {Count} DB words - Thread: {ThreadId}", dbWordsCount, Thread.CurrentThread.ManagedThreadId);

                if (dbWordsCount != 1)
                {
                    _logger.LogTrace("Invalid DB word selection count, showing alert - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                    await _alertService.DisplayAlertAsync(InvalidDbWordSelectionError);
                    return;
                }

                int wordId = TreeViewNodes.First(el => el.Id != 0).Id;
                _logger.LogTrace("Using word ID: {WordId} - Thread: {ThreadId}", wordId, Thread.CurrentThread.ManagedThreadId);

                _logger.LogTrace("Building definitions for addition - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                IEnumerable<Meaning> definitionsForAddition = nonDbWord
                    .SelectMany(w => w.Children)
                    .Where(m => m.IsSelected)
                    .Select(m => new Meaning
                    {
                        Value = m.Value,
                        Children = new ObservableCollection<Example>(m.Children.Where(e => e.IsSelected))
                    });

                _logger.LogTrace("Mapping definitions to app models - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                Application.Models.Parser.Definition[] appDefinitions = _definitionToAppMapper.MapToArray(definitionsForAddition);
                _logger.LogTrace("Mapped {Count} definitions - Thread: {ThreadId}", appDefinitions.Length, Thread.CurrentThread.ManagedThreadId);

                _logger.LogTrace("Calling parser service AddNewDefinitions - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                await _parserService.AddNewDefinitions(wordId, appDefinitions);
                _logger.LogTrace("AddNewDefinitions completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);

                _logger.LogTrace("Getting updated word from DB - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                Application.Models.Parser.Word updatedWord = await _parserService.GetWordFromDb(wordId);
                _logger.LogTrace("Retrieved updated word - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);

                _logger.LogTrace("Mapping updated word to UI model - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                Word uiUpdatedWord = _wordToUIParserMapper.Map(updatedWord);
                uiUpdatedWord.DisableAll();
                uiUpdatedWord.IsSelected = true;

                _logger.LogTrace("Updating TreeViewNodes collection - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                TreeViewNodes = new ObservableCollection<Word>() { uiUpdatedWord };
                _logger.LogTrace("TreeViewNodes updated - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddAsync failed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                await _alertService.DisplayAlertAsync($"{AddErrorMessage}: {ex.Message}");
            }
            finally
            {
                _logger.LogTrace("AddAsync completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
        }

        [RelayCommand(CanExecute = nameof(CanEncrich))]
        private async Task EnrichAsync()
        {
            _logger.LogTrace("EnrichAsync started - Thread: {ThreadId}, IsAiEnabled: {IsAiEnabled}",
                Thread.CurrentThread.ManagedThreadId, IsAiEnabled);

            try
            {
                _logger.LogTrace("Extracting entries from TreeViewNodes - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                string[] entries = TreeViewNodes.Where(el => el.Id != 0)
                                                .SelectMany(el => el.Value.Split(Separators, StringSplitOptions.None))
                                                .Distinct()
                                                .ToArray();
                _logger.LogTrace("Extracted {Count} entries: [{Entries}] - Thread: {ThreadId}",
                    entries.Length, string.Join(", ", entries), Thread.CurrentThread.ManagedThreadId);

                _logger.LogTrace("Calling parser service GetDefinitionsAndExamples - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                List<Application.Models.Parser.Word> rawAdditionalDefinitions = await _parserService.GetDefinitionsAndExamples(entries, IsAiEnabled);
                _logger.LogTrace("GetDefinitionsAndExamples completed, found {Count} definitions - Thread: {ThreadId}",
                    rawAdditionalDefinitions.Count, Thread.CurrentThread.ManagedThreadId);

                _logger.LogTrace("Mapping additional definitions - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                List<Word> additionalDefinitions = _wordToUIParserMapper.MapToList(rawAdditionalDefinitions);

                _logger.LogTrace("Combining existing and additional definitions - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                TreeViewNodes = new ObservableCollection<Word>(TreeViewNodes.Where(el => el.Id != 0).Concat(additionalDefinitions));
                _logger.LogTrace("TreeViewNodes updated with {Count} total words - Thread: {ThreadId}",
                    TreeViewNodes.Count, Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EnrichAsync failed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                await _alertService.DisplayAlertAsync($"{EnrichErrorMessage}: {ex.Message}");
            }
            finally
            {
                _logger.LogTrace("EnrichAsync completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
        }

        [RelayCommand(CanExecute = nameof(CanProcessInput))]
        private async Task ProcessInputAsync()
        {
            _logger.LogTrace("ProcessInputAsync started - Thread: {ThreadId}, EntryText: '{EntryText}'",
                Thread.CurrentThread.ManagedThreadId, EntryText);

            try
            {
                _logger.LogTrace("Splitting entry text - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                string[] entries = EntryText.Split(Separators, StringSplitOptions.RemoveEmptyEntries).ToArray();
                _logger.LogTrace("Split into {Count} entries: [{Entries}] - Thread: {ThreadId}",
                    entries.Length, string.Join(", ", entries), Thread.CurrentThread.ManagedThreadId);

                if (entries.Length == 1 && entries[0].All(char.IsDigit) && Int32.TryParse(entries[0], out int id))
                {
                    _logger.LogTrace("Single numeric entry detected, showing word from DB with ID: {WordId} - Thread: {ThreadId}",
                        id, Thread.CurrentThread.ManagedThreadId);
                    await ShowWordFromDb(id);
                    return;
                }

                _logger.LogTrace("Processing as text entries - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                await ShowDefinitionsAndExamples(entries, IsAiEnabled);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ProcessInputAsync failed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                await _alertService.DisplayAlertAsync($"{ParseErrorMessage}: {ex.Message}");
            }
            finally
            {
                _logger.LogTrace("ProcessInputAsync completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
        }

        [RelayCommand]
        private async Task EditMeaningAsync(Meaning meaning)
        {
            _logger.LogTrace("EditMeaningAsync started - Thread: {ThreadId}, Meaning: '{MeaningValue}'",
                Thread.CurrentThread.ManagedThreadId, meaning?.Value ?? "null");

            try
            {
                if (_promptService == null)
                {
                    _logger.LogTrace("PromptService is null, cannot edit meaning - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                _logger.LogTrace("Showing prompt for meaning edit - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                string result = await _promptService.ShowPromptAsync("Edit Meaning", "Enter new meaning:", meaning.Value);
                _logger.LogTrace("Prompt result: '{Result}' - Thread: {ThreadId}", result ?? "null", Thread.CurrentThread.ManagedThreadId);

                if (!string.IsNullOrWhiteSpace(result))
                {
                    _logger.LogTrace("Updating meaning value from '{OldValue}' to '{NewValue}' - Thread: {ThreadId}",
                        meaning.Value, result, Thread.CurrentThread.ManagedThreadId);
                    meaning.Value = result;
                }
                else
                {
                    _logger.LogTrace("Result is empty, meaning not updated - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EditMeaningAsync failed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                throw;
            }
            finally
            {
                _logger.LogTrace("EditMeaningAsync completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
        }

        [RelayCommand(CanExecute = nameof(CanAddWordsForExport))]
        private void AddWordsForExport()
        {
            _logger.LogTrace("AddWordsForExport started - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);

            try
            {
                _logger.LogTrace("Getting compiled selected words - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                if (CompiledSelectedWordsForExport == null)
                {
                    _logger.LogTrace("No compiled words available, returning - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                _logger.LogTrace("Adding words to export collection - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                WordsForExport.Add(CompiledSelectedWordsForExport);
                _logger.LogTrace("Words added to export, total count: {Count} - Thread: {ThreadId}",
                    WordsForExport.Count, Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddWordsForExport failed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                throw;
            }
            finally
            {
                _logger.LogTrace("AddWordsForExport completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
        }

        [RelayCommand(CanExecute = nameof(CanRemoveWordsForExport))]
        private void RemoveWordsForExport()
        {
            _logger.LogTrace("RemoveWordsForExport started - Thread: {ThreadId}, SelectedWordsForExport: {IsNull}",
                Thread.CurrentThread.ManagedThreadId, SelectedWordsForExport == null ? "null" : "not null");

            try
            {
                if (SelectedWordsForExport == null)
                {
                    _logger.LogTrace("No selected words for export, returning - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                _logger.LogTrace("Removing selected words from export collection - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                WordsForExport.Remove(SelectedWordsForExport);
                SelectedWordsForExport = null;
                _logger.LogTrace("Words removed from export, remaining count: {Count} - Thread: {ThreadId}",
                    WordsForExport.Count, Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RemoveWordsForExport failed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                throw;
            }
            finally
            {
                _logger.LogTrace("RemoveWordsForExport completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
        }

        [RelayCommand(CanExecute = nameof(CanExport))]
        private async Task ExportAsync()
        {
            _logger.LogTrace("ExportAsync started - Thread: {ThreadId}, WordsForExport count: {Count}",
                Thread.CurrentThread.ManagedThreadId, WordsForExport.Count);

            try
            {
                _logger.LogTrace("Mapping words for export - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                Application.Models.Parser.Word[] wordsforExport = _wordpackToAppParserMapper.MapToArray(WordsForExport);
                _logger.LogTrace("Mapped {Count} words for export - Thread: {ThreadId}",
                    wordsforExport.Length, Thread.CurrentThread.ManagedThreadId);

                _logger.LogTrace("Getting export directory path - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string exportDirectory = Path.Combine(documentsPath, AppInfo.Name);
                _logger.LogTrace("Export directory: '{Directory}' - Thread: {ThreadId}",
                    exportDirectory, Thread.CurrentThread.ManagedThreadId);

                _logger.LogTrace("Calling parser service ExportWords - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                _parserService.ExportWords(exportDirectory, wordsforExport);
                _logger.LogTrace("ExportWords completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);

                _logger.LogTrace("Clearing export collections - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                WordsForExport.Clear();
                SelectedWordsForExport = null;
                _logger.LogTrace("Export collections cleared - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ExportAsync failed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                await _alertService.DisplayAlertAsync($"{ExportErrorMessage}: {ex.Message}");
            }
            finally
            {
                _logger.LogTrace("ExportAsync completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
        }
        #endregion

        #region CanExecute Methods
        private bool CanProcessInput()
        {
            _logger.LogTrace("CanProcessInput called - Thread: {ThreadId}, EntryText: '{EntryText}'",
                Thread.CurrentThread.ManagedThreadId, EntryText);

            bool result = string.IsNullOrWhiteSpace(EntryText) == false;
            _logger.LogTrace("CanProcessInput result: {Result} - Thread: {ThreadId}", result, Thread.CurrentThread.ManagedThreadId);
            return result;
        }

        private bool CanEncrich()
        {
            _logger.LogTrace("CanEncrich called - Thread: {ThreadId}, TreeViewNodes count: {Count}",
                Thread.CurrentThread.ManagedThreadId, TreeViewNodes.Count);

            bool result = TreeViewNodes.Any(el => el.Id != 0);
            _logger.LogTrace("CanEncrich result: {Result} - Thread: {ThreadId}", result, Thread.CurrentThread.ManagedThreadId);
            return result;
        }

        private bool CanAdd()
        {
            _logger.LogTrace("CanAdd called - Thread: {ThreadId}, TreeViewNodes count: {Count}",
                Thread.CurrentThread.ManagedThreadId, TreeViewNodes.Count);

            if (TreeViewNodes.Any(el => el.Id != 0) == false)
            {
                _logger.LogTrace("CanAdd result: false (no DB words) - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                return false;
            }

            IEnumerable<Word> nonDbWord = TreeViewNodes.Where(el => el.Id == 0);
            bool result = nonDbWord.Any(el => el.IsSelected);
            _logger.LogTrace("CanAdd result: {Result} - Thread: {ThreadId}", result, Thread.CurrentThread.ManagedThreadId);
            return result;
        }

        private bool CanRemoveWordsForExport()
        {
            _logger.LogTrace("CanRemoveWordsForExport called - Thread: {ThreadId}, SelectedWordsForExport: {IsNull}",
                Thread.CurrentThread.ManagedThreadId, SelectedWordsForExport == null ? "null" : "not null");

            bool result = SelectedWordsForExport != null;
            _logger.LogTrace("CanRemoveWordsForExport result: {Result} - Thread: {ThreadId}", result, Thread.CurrentThread.ManagedThreadId);
            return result;
        }

        private bool CanAddWordsForExport()
        {
            _logger.LogTrace("CanAddWordsForExport called - Thread: {ThreadId}, TreeViewNodes count: {Count}",
                Thread.CurrentThread.ManagedThreadId, TreeViewNodes.Count);

            bool result = TreeViewNodes.Any(el => el.IsSelected);
            _logger.LogTrace("CanAddWordsForExport result: {Result} - Thread: {ThreadId}", result, Thread.CurrentThread.ManagedThreadId);
            return result;
        }

        private bool CanExport()
        {
            _logger.LogTrace("CanExport called - Thread: {ThreadId}, WordsForExport count: {Count}",
                Thread.CurrentThread.ManagedThreadId, WordsForExport.Count);

            bool result = WordsForExport.Any();
            _logger.LogTrace("CanExport result: {Result} - Thread: {ThreadId}", result, Thread.CurrentThread.ManagedThreadId);
            return result;
        }
        #endregion

        #region Property Changed Handlers

        #endregion

        #region Event Handlers

        partial void OnWordsForExportChanged(ObservableCollection<WordPack>? oldValue, ObservableCollection<WordPack> newValue)
        {
            _logger.LogTrace("OnWordsForExportChanged started - Thread: {ThreadId}, Old count: {OldCount}, New count: {NewCount}",
                Thread.CurrentThread.ManagedThreadId, oldValue?.Count ?? 0, newValue?.Count ?? 0);

            try
            {
                if (oldValue != null)
                {
                    _logger.LogTrace("Unsubscribing from old collection events - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                    oldValue.CollectionChanged -= OnWordsForExportCollectionChanged;
                }

                if (newValue != null)
                {
                    _logger.LogTrace("Subscribing to new collection events - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                    newValue.CollectionChanged += OnWordsForExportCollectionChanged;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnWordsForExportChanged failed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                throw;
            }
            finally
            {
                _logger.LogTrace("OnWordsForExportChanged completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
        }

        private void OnWordsForExportCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            _logger.LogTrace("OnWordsForExportCollectionChanged started - Thread: {ThreadId}, Action: {Action}",
                Thread.CurrentThread.ManagedThreadId, e.Action);

            try
            {
                _logger.LogTrace("Notifying ExportCommand can execute changed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                ExportCommand.NotifyCanExecuteChanged();
                _logger.LogTrace("Notifying ExportPlaceholder property changed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                OnPropertyChanged(nameof(ExportPlaceholder));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnWordsForExportCollectionChanged failed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                throw;
            }
            finally
            {
                _logger.LogTrace("OnWordsForExportCollectionChanged completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
        }

        partial void OnTreeViewNodesChanged(ObservableCollection<Word>? oldValue, ObservableCollection<Word> newValue)
        {
            _logger.LogTrace("OnTreeViewNodesChanged started - Thread: {ThreadId}, Old count: {OldCount}, New count: {NewCount}",
                Thread.CurrentThread.ManagedThreadId, oldValue?.Count ?? 0, newValue?.Count ?? 0);

            try
            {
                if (oldValue != null)
                {
                    _logger.LogTrace("Unsubscribing from old TreeViewNodes events - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                    foreach (var word in oldValue)
                    {
                        word.PropertyChanged -= Word_PropertyChanged;
                        foreach (var meaning in word.Children)
                        {
                            meaning.PropertyChanged -= Meaning_PropertyChanged;
                            foreach (var example in meaning.Children)
                            {
                                example.PropertyChanged -= Example_PropertyChanged;
                            }
                        }
                    }
                    _logger.LogTrace("Unsubscribed from {Count} words - Thread: {ThreadId}", oldValue.Count, Thread.CurrentThread.ManagedThreadId);
                }

                if (newValue != null)
                {
                    _logger.LogTrace("Subscribing to new TreeViewNodes events - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                    foreach (var word in newValue)
                    {
                        word.PropertyChanged += Word_PropertyChanged;
                        foreach (var meaning in word.Children)
                        {
                            meaning.PropertyChanged += Meaning_PropertyChanged;
                            foreach (var example in meaning.Children)
                            {
                                example.PropertyChanged += Example_PropertyChanged;
                            }
                        }
                    }
                    _logger.LogTrace("Subscribed to {Count} words - Thread: {ThreadId}", newValue.Count, Thread.CurrentThread.ManagedThreadId);
                }

                _logger.LogTrace("Invoking on main thread to update commands - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _logger.LogTrace("Main thread action started - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                    AddWordsForExportCommand.NotifyCanExecuteChanged();
                    EnrichCommand.NotifyCanExecuteChanged();
                    _logger.LogTrace("Main thread action completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnTreeViewNodesChanged failed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                throw;
            }
            finally
            {
                _logger.LogTrace("OnTreeViewNodesChanged completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
        }

        private void Word_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            _logger.LogTrace("Word_PropertyChanged started - Thread: {ThreadId}, PropertyName: '{PropertyName}'",
                Thread.CurrentThread.ManagedThreadId, e.PropertyName ?? "null");

            try
            {
                if (e.PropertyName == nameof(Word.IsSelected))
                {
                    _logger.LogTrace("IsSelected property changed, notifying commands - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                    AddWordsForExportCommand.NotifyCanExecuteChanged();
                    AddCommand.NotifyCanExecuteChanged();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Word_PropertyChanged failed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                throw;
            }
            finally
            {
                _logger.LogTrace("Word_PropertyChanged completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
        }

        private void Meaning_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            _logger.LogTrace("Meaning_PropertyChanged started - Thread: {ThreadId}, PropertyName: '{PropertyName}'",
                Thread.CurrentThread.ManagedThreadId, e.PropertyName ?? "null");

            try
            {
                if (e.PropertyName == nameof(Meaning.IsSelected))
                {
                    _logger.LogTrace("IsSelected property changed, notifying commands - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                    AddWordsForExportCommand.NotifyCanExecuteChanged();
                    AddCommand.NotifyCanExecuteChanged();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Meaning_PropertyChanged failed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                throw;
            }
            finally
            {
                _logger.LogTrace("Meaning_PropertyChanged completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
        }

        private void Example_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            _logger.LogTrace("Example_PropertyChanged started - Thread: {ThreadId}, PropertyName: '{PropertyName}'",
                Thread.CurrentThread.ManagedThreadId, e.PropertyName ?? "null");

            try
            {
                if (e.PropertyName == nameof(Example.IsSelected))
                {
                    _logger.LogTrace("IsSelected property changed, notifying commands - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                    AddWordsForExportCommand.NotifyCanExecuteChanged();
                    AddCommand.NotifyCanExecuteChanged();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Example_PropertyChanged failed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                throw;
            }
            finally
            {
                _logger.LogTrace("Example_PropertyChanged completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
        }
        #endregion

        #region Private Helper Methods

        private async Task ShowDefinitionsAndExamples(string[] entries, bool isAiEnabled)
        {
            _logger.LogTrace("ShowDefinitionsAndExamples started - Thread: {ThreadId}, Entries: [{Entries}], IsAiEnabled: {IsAiEnabled}",
                Thread.CurrentThread.ManagedThreadId, string.Join(", ", entries), isAiEnabled);

            try
            {
                _logger.LogTrace("Calling parser service GetDefinitionsAndExamples - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                List<Application.Models.Parser.Word> words = await _parserService.GetDefinitionsAndExamples(entries, isAiEnabled);
                _logger.LogTrace("GetDefinitionsAndExamples completed, found {Count} words - Thread: {ThreadId}",
                    words.Count, Thread.CurrentThread.ManagedThreadId);

                _logger.LogTrace("Mapping words to UI models - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                TreeViewNodes = new ObservableCollection<Word>(_wordToUIParserMapper.MapToList(words));
                _logger.LogTrace("TreeViewNodes updated with {Count} words - Thread: {ThreadId}",
                    TreeViewNodes.Count, Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ShowDefinitionsAndExamples failed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                throw;
            }
            finally
            {
                _logger.LogTrace("ShowDefinitionsAndExamples completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async Task ShowWordFromDb(int id)
        {
            _logger.LogTrace("ShowWordFromDb started - Thread: {ThreadId}, WordId: {WordId}",
                Thread.CurrentThread.ManagedThreadId, id);

            try
            {
                _logger.LogTrace("Calling parser service GetWordFromDb - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                Application.Models.Parser.Word appWord = await _parserService.GetWordFromDb(id);
                _logger.LogTrace("GetWordFromDb completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);

                _logger.LogTrace("Mapping word to UI model - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                Word word = _wordToUIParserMapper.Map(appWord);
                word.DisableAll();
                word.IsSelected = true;

                _logger.LogTrace("Updating TreeViewNodes with single word - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                TreeViewNodes = new ObservableCollection<Word>() { word };
                _logger.LogTrace("TreeViewNodes updated - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ShowWordFromDb failed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                throw;
            }
            finally
            {
                _logger.LogTrace("ShowWordFromDb completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
        }

        private WordPack? GetCompiledSelectedWords()
        {
            _logger.LogTrace("GetCompiledSelectedWords started - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);

            try
            {
                _logger.LogTrace("Filtering selected words - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                List<Word> selectedWords = TreeViewNodes.Where(node => node.IsSelected).ToList();
                _logger.LogTrace("Found {Count} selected words - Thread: {ThreadId}",
                    selectedWords.Count, Thread.CurrentThread.ManagedThreadId);

                if (selectedWords.Count == 0)
                {
                    _logger.LogTrace("No selected words, returning null - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                    return null;
                }

                _logger.LogTrace("Creating WordPack for export - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                WordPack wordsForExport = new WordPack();

                foreach (Word selectedWord in selectedWords)
                {
                    _logger.LogTrace("Processing word: '{WordValue}' - Thread: {ThreadId}",
                        selectedWord.Value, Thread.CurrentThread.ManagedThreadId);

                    Word wordForExport = new Word()
                    {
                        Area = selectedWord.Area,
                        OtherForms = selectedWord.OtherForms,
                        PluralForms = selectedWord.PluralForms,
                        Type = selectedWord.Type,
                        Value = selectedWord.Value,
                    };

                    int meaningCount = 0;
                    foreach (Meaning meaning in selectedWord.Children)
                    {
                        if (meaning.IsSelected == false)
                        {
                            continue;
                        }

                        meaningCount++;
                        Meaning meaningForExport = new Meaning()
                        {
                            Value = meaning.Value,
                        };

                        int exampleCount = 0;
                        foreach (Example example in meaning.Children)
                        {
                            if (example.IsSelected == false)
                            {
                                continue;
                            }

                            exampleCount++;
                            Example exampleForExport = new Example()
                            {
                                Value = example.Value,
                            };

                            meaningForExport.Children.Add(exampleForExport);
                        }

                        _logger.LogTrace("Added meaning with {ExampleCount} examples - Thread: {ThreadId}",
                            exampleCount, Thread.CurrentThread.ManagedThreadId);
                        wordForExport.Children.Add(meaningForExport);
                    }

                    _logger.LogTrace("Added word with {MeaningCount} meanings - Thread: {ThreadId}",
                        meaningCount, Thread.CurrentThread.ManagedThreadId);
                    wordsForExport.Words.Add(wordForExport);
                }

                _logger.LogTrace("Compiled {WordCount} words for export - Thread: {ThreadId}",
                    wordsForExport.Words.Count, Thread.CurrentThread.ManagedThreadId);
                return wordsForExport;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetCompiledSelectedWords failed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                throw;
            }
            finally
            {
                _logger.LogTrace("GetCompiledSelectedWords completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
        }
        #endregion

        public async Task OnNavigatedToAsync()
        {
            _logger.LogTrace("OnNavigatedToAsync started - Thread: {ThreadId}, WordId: {WordId}",
                Thread.CurrentThread.ManagedThreadId, WordId);

            try
            {
                if (WordId <= 0)
                {
                    _logger.LogTrace("WordId is invalid, returning - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                _logger.LogTrace("Showing word from DB - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                await ShowWordFromDb(WordId);

                _logger.LogTrace("Resetting WordId to 0 - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                WordId = 0;
            }
            catch (Exception ex)
            {
                string formattedError = string.Format(LoadWordFromDbErrorMessageFormat, WordId);
                _logger.LogError(ex, "OnNavigatedToAsync failed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                await _alertService.DisplayAlertAsync($"{formattedError}: {ex.Message}");
            }
            finally
            {
                _logger.LogTrace("OnNavigatedToAsync completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
        }
    }
}