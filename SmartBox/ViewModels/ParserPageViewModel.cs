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

            _parserService = parserService;
            _alertService = alertService;

            _wordToUIParserMapper = wordToUIParserMapper;
            _wordpackToAppParserMapper = wordpackToAppParserMapper;
            _definitionToAppMapper = definitionToAppMapper;

            WordsForExport.CollectionChanged += OnWordsForExportCollectionChanged;
        }

        #region Public Methods

        #endregion

        #region Commands
        [RelayCommand(CanExecute = nameof(CanAdd))]
        private async Task AddAsync()
        {
            try
            {
                IEnumerable<Word> nonDbWord = TreeViewNodes
                    .Where(word => word.Id == 0 && word.IsSelected && word.IsEnabled);

                if (nonDbWord.Any() == false)
                {
                    await _alertService.DisplayAlertAsync(NoNonDbWordSelectedError);
                    return;
                }

                if (TreeViewNodes.Where(word => word.Id != 0).Count() != 1)
                {
                    await _alertService.DisplayAlertAsync(InvalidDbWordSelectionError);
                    return;
                }

                int wordId = TreeViewNodes.First(el => el.Id != 0).Id;

                IEnumerable<Meaning> definitionsForAddition = nonDbWord
                    .SelectMany(w => w.Children)
                    .Where(m => m.IsSelected)
                    .Select(m => new Meaning
                    {
                        Value = m.Value,
                        Children = new ObservableCollection<Example>(m.Children.Where(e => e.IsSelected))
                    });

                Application.Models.Parser.Definition[] appDefinitions = _definitionToAppMapper.MapToArray(definitionsForAddition);

                await _parserService.AddNewDefinitions(wordId, appDefinitions);

                Application.Models.Parser.Word updatedWord = await _parserService.GetWordFromDb(wordId);

                Word uiUpdatedWord = _wordToUIParserMapper.Map(updatedWord);
                uiUpdatedWord.DisableAll();
                uiUpdatedWord.IsSelected = true;

                TreeViewNodes = new ObservableCollection<Word>() { uiUpdatedWord };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, AddErrorMessage);
                await _alertService.DisplayAlertAsync($"{AddErrorMessage}: {ex.Message}");
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

                List<Application.Models.Parser.Word> rawAdditionalDefinitions = await _parserService.GetDefinitionsAndExamples(entries);

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

                await ShowDefinitionsAndExamples(entries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ParseErrorMessage);

                await _alertService.DisplayAlertAsync($"{ParseErrorMessage}: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task EditMeaningAsync(Meaning meaning)
        {
            string result = await _promptService!.ShowPromptAsync("Edit Meaning", "Enter new meaning:", meaning.Value);
            if (!string.IsNullOrWhiteSpace(result))
            {
                meaning.Value = result;
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

        [RelayCommand(CanExecute = nameof(CanExport))]
        private async Task ExportAsync()
        {
            try
            {
                Application.Models.Parser.Word[] wordsforExport = _wordpackToAppParserMapper.MapToArray(WordsForExport);
                
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string exportDirectory = Path.Combine(documentsPath, AppInfo.Name);

                _parserService.ExportWords(exportDirectory, wordsforExport);

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

        private bool CanAdd()
        {
            if (TreeViewNodes.Any(el => el.Id != 0) == false)
            {
                return false;
            }

            IEnumerable<Word> nonDbWord = TreeViewNodes.Where(el => el.Id == 0);

            return nonDbWord.Any(el => el.IsSelected);
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
        #endregion

        #region Property Changed Handlers

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
            });
        }

        private void Word_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Word.IsSelected))
            {
                AddWordsForExportCommand.NotifyCanExecuteChanged();
                AddCommand.NotifyCanExecuteChanged();
            }
        }

        private void Meaning_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Meaning.IsSelected))
            {
                AddWordsForExportCommand.NotifyCanExecuteChanged();
                AddCommand.NotifyCanExecuteChanged();
            }
        }

        private void Example_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Example.IsSelected))
            {
                AddWordsForExportCommand.NotifyCanExecuteChanged();
                AddCommand.NotifyCanExecuteChanged();
            }
        }
        #endregion

        #region Private Helper Methods

        private async Task ShowDefinitionsAndExamples(string[] entries)
        {
            List<Application.Models.Parser.Word> words = await _parserService.GetDefinitionsAndExamples(entries);

            TreeViewNodes = new ObservableCollection<Word>(_wordToUIParserMapper.MapToList(words));
        }

        private async Task ShowWordFromDb(int id)
        {
            Application.Models.Parser.Word appWord = await _parserService.GetWordFromDb(id);

            Word word = _wordToUIParserMapper.Map(appWord);
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