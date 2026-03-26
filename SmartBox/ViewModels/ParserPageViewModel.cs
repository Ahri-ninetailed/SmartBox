#pragma warning disable MVVMTK0045

using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Application.Enums;
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
        private const string SyncErrorMessage = "An error occurred while syncing a word";
        private const string InvalidDbWordSelectionError = "Definitions can be added only for a single word from the database";
        private const string CopyErrorMessage = "An error occurred while copying to clipboard";
        private const string NoWordsSelectedForCopyError = "No words selected for copying";

        #endregion

        #region Fields
        private readonly ILogger<ParserPageViewModel> _logger;

        private readonly IParserService _parserService;
        private readonly IAlertService _alertService;
        private readonly IClipboardService _clipboardService;

        private readonly IMapper<Domain.Models.Parser.Word, SmartBox.Models.Parser.Word> _wordToUIParserMapper;
        private readonly IMapper<SmartBox.Models.Parser.WordPack, Domain.Models.Parser.Word> _wordpackToAppParserMapper;
        private readonly IMapper<SmartBox.Models.Parser.Meaning, Domain.Models.Parser.Definition> _definitionToAppMapper;

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

        [ObservableProperty]
        private bool isAiLocalOnly = false;

        private WordPack? CompiledSelectedWordsForExport => GetCompiledSelectedWords();

        private AiMode CurrentAiMode
        {
            get
            {
                if (IsAiLocalOnly)
                {
                    return AiMode.LocalOnly;
                }

                if (IsAiEnabled)
                {
                    return AiMode.Full;
                }

                return AiMode.Disabled;
            }
        }

        public string ExportPlaceholder => string.Format(ExportPlaceholderFormat, WordsForExport.Count);

        #endregion

        public ParserPageViewModel(ILogger<ParserPageViewModel> logger,
                                   IParserService parserService,
                                   IAlertService alertService,
                                   IClipboardService clipboardService,
                                   IMapper<Domain.Models.Parser.Word, Word> wordToUIParserMapper,
                                   IMapper<WordPack, Domain.Models.Parser.Word> wordpackToAppParserMapper,
                                   IMapper<SmartBox.Models.Parser.Meaning, Domain.Models.Parser.Definition> definitionToAppMapper)
        {
            _logger = logger;

            _parserService = parserService;
            _alertService = alertService;
            _clipboardService = clipboardService;

            _wordToUIParserMapper = wordToUIParserMapper;
            _wordpackToAppParserMapper = wordpackToAppParserMapper;
            _definitionToAppMapper = definitionToAppMapper;

            WordsForExport.CollectionChanged += OnWordsForExportCollectionChanged;
        }

        #region Public Methods

        #endregion

        #region Commands
        [RelayCommand(CanExecute = nameof(CanUnlock))]
        private void Unlock()
        {
            foreach (Word element in TreeViewNodes.Where(el => el.IsEnabled == false))
            {
                element.EnableAll();
            }

            UnlockCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand(CanExecute = nameof(CanSync))]
        private async Task SyncAsync()
        {
            try
            {
                if (TreeViewNodes.Where(word => word.Id != 0).Count() != 1)
                {
                    await _alertService.DisplayAlertAsync(InvalidDbWordSelectionError);
                    return;
                }

                int wordId = TreeViewNodes.First(el => el.Id != 0).Id;

                IEnumerable<Meaning> definitionsForSync = TreeViewNodes
                    .SelectMany(w => w.Children)
                    .Where(m => m.IsSelected)
                    .Select(m => new Meaning
                    {
                        Id = m.Id,
                        Value = m.Value,
                        Children = new ObservableCollection<Example>(m.Children.Where(e => e.IsSelected))
                    });

                Domain.Models.Parser.Definition[] parserDefinitions = _definitionToAppMapper.MapToArray(definitionsForSync);

                await _parserService.SyncDefinitionsAndExamplesAsync(wordId, parserDefinitions);

                Domain.Models.Parser.Word updatedWord = await _parserService.GetWordFromDb(wordId);

                Word uiUpdatedWord = _wordToUIParserMapper.Map(updatedWord);
                uiUpdatedWord.DisableAll();
                uiUpdatedWord.IsSelected = true;

                TreeViewNodes = new ObservableCollection<Word>() { uiUpdatedWord };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, SyncErrorMessage);
                await _alertService.DisplayAlertAsync($"{SyncErrorMessage}: {ex.Message}");
            }
        }

        [RelayCommand(CanExecute = nameof(CanEncrich))]
        private async Task EnrichAsync()
        {
            try
            {
                string[] entries = TreeViewNodes.Where(el => el.Id != 0)
                                                .SelectMany(el => el.Value.Split(Separators, StringSplitOptions.None))
                                                .Distinct()
                                                .ToArray();

                List<Domain.Models.Parser.Word> rawAdditionalDefinitions = await _parserService.GetDefinitionsAndExamples(entries, CurrentAiMode);

                List<Word> additionalDefinitions = _wordToUIParserMapper.MapToList(rawAdditionalDefinitions);

                TreeViewNodes = new ObservableCollection<Word>(TreeViewNodes.Where(el => el.Id != 0).Concat(additionalDefinitions));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, EnrichErrorMessage);
                await _alertService.DisplayAlertAsync($"{EnrichErrorMessage}: {ex.Message}");
            }
        }

        [RelayCommand(CanExecute = nameof(CanProcessInput))]
        private async Task ProcessInputAsync()
        {
            try
            {
                string[] entries = EntryText.Split(Separators, StringSplitOptions.RemoveEmptyEntries).ToArray();

                if (entries.Length == 1 && entries[0].All(char.IsDigit) && Int32.TryParse(entries[0], out int id))
                {
                    await ShowWordFromDb(id);

                    return;
                }

                await ShowDefinitionsAndExamples(entries, CurrentAiMode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ParseErrorMessage);

                await _alertService.DisplayAlertAsync($"{ParseErrorMessage}: {ex.Message}");
            }
        }

        [RelayCommand(CanExecute = nameof(CanAddWordsForExport))]
        private void AddWordsForExport()
        {
            if (CompiledSelectedWordsForExport == null)
            {
                return;
            }

            WordsForExport.Add(CompiledSelectedWordsForExport);
        }

        [RelayCommand(CanExecute = nameof(CanRemoveWordsForExport))]
        private void RemoveWordsForExport()
        {
            if (SelectedWordsForExport == null)
            {
                return;
            }

            WordsForExport.Remove(SelectedWordsForExport);
            SelectedWordsForExport = null;
        }

        [RelayCommand(CanExecute = nameof(CanCopy))]
        private async Task CopyAsync()
        {
            try
            {
                if (CompiledSelectedWordsForExport == null)
                {
                    throw new InvalidOperationException(NoWordsSelectedForCopyError);
                }

                Domain.Models.Parser.Word word = _wordpackToAppParserMapper.Map(CompiledSelectedWordsForExport);

                string stringToCopy = _parserService.GetStringForCopyToBuffer(word);

                await _clipboardService.SetTextAsync(stringToCopy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, CopyErrorMessage);
                await _alertService.DisplayAlertAsync($"{CopyErrorMessage}: {ex.Message}");
            }
        }

        [RelayCommand(CanExecute = nameof(CanExport))]
        private async Task ExportAsync()
        {
            try
            {
                Domain.Models.Parser.Word[] wordsforExport = _wordpackToAppParserMapper.MapToArray(WordsForExport);

                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string exportDirectory = Path.Combine(documentsPath, AppInfo.Name);

                string exportedFilePath = _parserService.ExportWords(exportDirectory, wordsforExport);

                await _clipboardService.SetFileAsync(exportedFilePath);

                WordsForExport.Clear();

                SelectedWordsForExport = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ExportErrorMessage);

                await _alertService.DisplayAlertAsync($"{ExportErrorMessage}: {ex.Message}");
            }
        }
        #endregion

        #region CanExecute Methods
        private bool CanProcessInput()
        {
            return string.IsNullOrWhiteSpace(EntryText) == false;
        }

        private bool CanEncrich()
        {
            return TreeViewNodes.Any(el => el.Id != 0);
        }

        private bool CanSync()
        {
            return TreeViewNodes.Any(el => el.Id != 0);
        }

        private bool CanUnlock()
        {
            return TreeViewNodes.Any(word =>
                !word.IsEnabled ||
                word.Children.Any(definition =>
                    !definition.IsEnabled ||
                    definition.Children.Any(example => !example.IsEnabled)));
        }

        private bool CanRemoveWordsForExport()
        {
            return SelectedWordsForExport != null;
        }

        private bool CanAddWordsForExport()
        {
            return TreeViewNodes.Any(el => el.IsSelected);
        }

        private bool CanExport()
        {
            return WordsForExport.Any();
        }

        private bool CanCopy()
        {
            return TreeViewNodes.Any(el => el.IsSelected);
        }
        #endregion

        #region Property Changed Handlers

        partial void OnIsAiLocalOnlyChanged(bool value)
        {
            if (value && !IsAiEnabled)
            {
                IsAiEnabled = true;
            }
        }

        partial void OnIsAiEnabledChanged(bool value)
        {
            if (!value && IsAiLocalOnly)
            {
                IsAiLocalOnly = false;
            }
        }

        #endregion

        #region Event Handlers

        partial void OnWordsForExportChanged(ObservableCollection<WordPack>? oldValue, ObservableCollection<WordPack> newValue)
        {
            if (oldValue != null)
            {
                oldValue.CollectionChanged -= OnWordsForExportCollectionChanged;
            }

            if (newValue != null)
            {
                newValue.CollectionChanged += OnWordsForExportCollectionChanged;
            }
        }

        private void OnWordsForExportCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            ExportCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(ExportPlaceholder));
        }

        partial void OnTreeViewNodesChanged(ObservableCollection<Word>? oldValue, ObservableCollection<Word> newValue)
        {
            if (oldValue != null)
            {
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
            }

            if (newValue != null)
            {
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
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                AddWordsForExportCommand.NotifyCanExecuteChanged();
                EnrichCommand.NotifyCanExecuteChanged();
                SyncCommand.NotifyCanExecuteChanged();
                UnlockCommand.NotifyCanExecuteChanged();
                CopyCommand.NotifyCanExecuteChanged();
            });
        }

        private void Word_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Word.IsSelected))
            {
                AddWordsForExportCommand.NotifyCanExecuteChanged();
                CopyCommand.NotifyCanExecuteChanged();
            }
        }

        private void Meaning_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Meaning.IsSelected))
            {
                AddWordsForExportCommand.NotifyCanExecuteChanged();
                CopyCommand.NotifyCanExecuteChanged();
            }
        }

        private void Example_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Example.IsSelected))
            {
                AddWordsForExportCommand.NotifyCanExecuteChanged();
                CopyCommand.NotifyCanExecuteChanged();
            }
        }
        #endregion

        #region Private Helper Methods

        private async Task ShowDefinitionsAndExamples(string[] entries, AiMode aiMode)
        {
            List<Domain.Models.Parser.Word> words = await _parserService.GetDefinitionsAndExamples(entries, aiMode);

            TreeViewNodes = new ObservableCollection<Word>(_wordToUIParserMapper.MapToList(words));

            if (TreeViewNodes.Count == 0)
            {
                await _alertService.DisplayAlertAsync("No definitions found for the provided input", "Info");
            }
        }

        private async Task ShowWordFromDb(int id)
        {
            Domain.Models.Parser.Word domainWord = await _parserService.GetWordFromDb(id);

            Word word = _wordToUIParserMapper.Map(domainWord);
            word.DisableAll();
            word.IsSelected = true;

            TreeViewNodes = new ObservableCollection<Word>() { word };
        }

        private WordPack? GetCompiledSelectedWords()
        {
            List<Word> selectedWords = TreeViewNodes.Where(node => node.IsSelected).ToList();

            if (selectedWords.Count == 0)
            {
                return null;
            }

            WordPack wordsForExport = new WordPack();

            foreach (Word selectedWord in selectedWords)
            {
                Word wordForExport = new Word()
                {
                    Area = selectedWord.Area,
                    OtherForms = selectedWord.OtherForms,
                    PluralForms = selectedWord.PluralForms,
                    Type = selectedWord.Type,
                    Value = selectedWord.Value,
                };

                foreach (Meaning meaning in selectedWord.Children)
                {
                    if (meaning.IsSelected == false)
                    {
                        continue;
                    }

                    Meaning meaningForExport = new Meaning()
                    {
                        Value = meaning.Value,
                    };

                    foreach (Example example in meaning.Children)
                    {
                        if (example.IsSelected == false)
                        {
                            continue;
                        }

                        Example exampleForExport = new Example()
                        {
                            Value = example.Value,
                        };

                        meaningForExport.Children.Add(exampleForExport);
                    }

                    wordForExport.Children.Add(meaningForExport);
                }

                wordsForExport.Words.Add(wordForExport);
            }

            return wordsForExport;
        }
        #endregion

        public async Task OnNavigatedToAsync()
        {
            if (WordId <= 0)
            {
                return;
            }

            try
            {
                await ShowWordFromDb(WordId);

                WordId = 0;
            }
            catch (Exception ex)
            {
                string formattedError = string.Format(LoadWordFromDbErrorMessageFormat, WordId);

                _logger.LogError(ex, formattedError);

                await _alertService.DisplayAlertAsync($"{formattedError}: {ex.Message}");
            }
        }
    }
}