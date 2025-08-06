#pragma warning disable MVVMTK0045

using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Shared;
using SmartBox.Interfaces;
using SmartBox.Models.Parser;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace SmartBox.ViewModels
{
    public partial class ParserPageViewModel : ObservableObject
    {
        #region Constants
        private const string ParseErrorMessage = "An error occurred while parsing";
        private const string ExportPlaceholderFormat = "To export: {0}";
        #endregion

        #region Fields
        private readonly ILogger<ParserPageViewModel> _logger;

        private readonly IParserService _parserService;
        private readonly IAlertService _alertService;
        public IPromptService? _promptService;

        private readonly IMapper<Application.Models.Parser.Word, Word> _wordToUIParserMapper;

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
        private string exportPlaceholder = string.Format(ExportPlaceholderFormat, 0);

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RemoveWordsForExportCommand))]
        private WordPack? selectedWordsForExport;

        private WordPack? CompiledSelectedWordsForExport => GetCompiledSelectedWords();
        #endregion

        public ParserPageViewModel(ILogger<ParserPageViewModel> logger,
                                   IParserService parserService,
                                   IAlertService alertService,
                                   IMapper<Application.Models.Parser.Word, Word> wordToUIParserMapper)
        {
            _logger = logger;

            _parserService = parserService;
            _alertService = alertService;

            _wordToUIParserMapper = wordToUIParserMapper;
        }

        #region Public Methods

        #endregion

        #region Commands
        [RelayCommand(CanExecute = nameof(CanParse))]
        private async Task ParseAsync()
        {
            try
            {
                var words = await _parserService.ExtractWordDataAsync(EntryText);

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
            UpdateExportPlaceholder();
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
            UpdateExportPlaceholder();
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
            return CompiledSelectedWordsForExport != null;
        }
        #endregion

        #region Property Changed Handlers

        #endregion

        #region Event Handlers

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
        private void UpdateExportPlaceholder()
        {
            ExportPlaceholder = string.Format(ExportPlaceholderFormat, WordsForExport.Count);
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
    }
}