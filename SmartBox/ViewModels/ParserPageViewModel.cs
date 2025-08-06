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
    public partial class ParserPageViewModel : ObservableObject
    {
        #region Constants
        private const string ParseErrorMessage = "An error occurred while parsing";
        private const string ExportPlaceholderFormat = "To export: {0}";
        private const string ExportErrorMessage = "An error occurred while exporting";

        #endregion

        #region Fields
        private readonly ILogger<ParserPageViewModel> _logger;

        private readonly IParserService _parserService;
        private readonly IAlertService _alertService;
        public IPromptService? _promptService;

        private readonly IMapper<Application.Models.Parser.Word, Word> _wordToUIParserMapper;
        private readonly IMapper<WordPack, Application.Models.Parser.Word> _wordpackToAppParserMapper;

        #endregion

        #region Properties
        [ObservableProperty]
        private ObservableCollection<Word> treeViewNodes = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ParseCommand))]
        private string entryText = string.Empty;

        [ObservableProperty]
        private ObservableCollection<WordPack> wordsForExport = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RemoveWordsForExportCommand))]
        private WordPack? selectedWordsForExport;

        private WordPack? CompiledSelectedWordsForExport => GetCompiledSelectedWords();

        public string ExportPlaceholder => string.Format(ExportPlaceholderFormat, WordsForExport.Count);
        #endregion

        public ParserPageViewModel(ILogger<ParserPageViewModel> logger,
                                   IParserService parserService,
                                   IAlertService alertService,
                                   IMapper<Application.Models.Parser.Word, Word> wordToUIParserMapper,
                                   IMapper<WordPack, Application.Models.Parser.Word> wordpackToAppParserMapper)
        {
            _logger = logger;

            _parserService = parserService;
            _alertService = alertService;

            _wordToUIParserMapper = wordToUIParserMapper;
            _wordpackToAppParserMapper = wordpackToAppParserMapper;

            WordsForExport.CollectionChanged += OnWordsForExportCollectionChanged;
        }

        #region Public Methods

        #endregion

        #region Commands
        [RelayCommand(CanExecute = nameof(CanParse))]
        private async Task ParseAsync()
        {
            try
            {
               string[] entries = EntryText.Split(new[] { ",", ", " }, StringSplitOptions.RemoveEmptyEntries).ToArray();

                List<Application.Models.Parser.Word> words = await _parserService.ExtractWordDataAsync(entries);

                TreeViewNodes = new ObservableCollection<Word>(_wordToUIParserMapper.MapToList(words));
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ExportErrorMessage);

                await _alertService.DisplayAlertAsync($"{ExportErrorMessage}: {ex.Message}");
            }
        }
        #endregion

        #region CanExecute Methods
        private bool CanParse()
        {
            return string.IsNullOrWhiteSpace(EntryText) == false;
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
            });
        }

        private void Word_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Word.IsSelected))
            {
                AddWordsForExportCommand.NotifyCanExecuteChanged();
            }
        }

        private void Meaning_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Meaning.IsSelected))
            {
                AddWordsForExportCommand.NotifyCanExecuteChanged();
            }
        }

        private void Example_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Example.IsSelected))
            {
                AddWordsForExportCommand.NotifyCanExecuteChanged();
            }
        }
        #endregion

        #region Private Helper Methods
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
    }
}