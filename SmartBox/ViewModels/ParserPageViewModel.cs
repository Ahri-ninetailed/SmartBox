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
        private ObservableCollection<Word> wordsForExport = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RemoveWordForExportCommand))]
        private Word? selectedWordForExport;

        private Word? CompiledSelectedWordForExport => GetCompiledSelectedWord();
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

        [RelayCommand(CanExecute = nameof(CanAddWordForExport))]
        private void AddWordForExport()
        {
            if (CompiledSelectedWordForExport == null)
            {
                return;
            }

            WordsForExport.Add(CompiledSelectedWordForExport);
        }

        [RelayCommand(CanExecute = nameof(CanRemoveWordForExport))]
        private void RemoveWordForExport()
        {
            if (SelectedWordForExport == null)
            {
                return;
            }

            WordsForExport.Remove(SelectedWordForExport);

            SelectedWordForExport = null;
        }
        #endregion

        #region CanExecute Methods
        private bool CanParse()
        {
            return string.IsNullOrWhiteSpace(EntryText) == false;
        }

        private bool CanRemoveWordForExport()
        {
            return SelectedWordForExport != null;
        }

        private bool CanAddWordForExport()
        {
            return CompiledSelectedWordForExport != null;
        }
        #endregion

        #region Property Changed Handlers
        partial void OnSelectedWordForExportChanged(Word? value)
        {

        }
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
                AddWordForExportCommand.NotifyCanExecuteChanged();
            }
        }

        private void Meaning_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Meaning.IsSelected))
            {
                AddWordForExportCommand.NotifyCanExecuteChanged();
            }
        }

        private void Example_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Example.IsSelected))
            {
                AddWordForExportCommand.NotifyCanExecuteChanged();
            }
        }
        #endregion

        #region Private Helper Methods

        private Word? GetCompiledSelectedWord()
        {
            var selectedWords = TreeViewNodes.Where(node => node.IsSelected).ToList();

            if (selectedWords.Count == 0)
            {
                return null;
            }

            PartOfSpeech partOfSpeech = selectedWords.Any(el => el.Type == PartOfSpeech.Verb || el.Type == PartOfSpeech.PhrasalVerb)
                ? PartOfSpeech.Verb
                : PartOfSpeech.Noun;

            Word compiledWord = new Word
            {
                Value = selectedWords.First().Value.ToLower(),
                Type = partOfSpeech,
                Area = null,
                OtherForms = selectedWords.SelectMany(el => el.OtherForms).ToArray(),
                PluralForms = selectedWords.SelectMany(el => el.PluralForms).ToArray(),
            };

            foreach (Word word in selectedWords)
            {
                foreach (Meaning meaning in word.Children.Where(m => m.IsSelected))
                {
                    Meaning newMeaning = new Meaning
                    {
                        Value = meaning.Value,
                    };

                    foreach (Example example in meaning.Children.Where(e => e.IsSelected))
                    {
                        newMeaning.Children.Add(new Example
                        {
                            Value = example.Value,
                        });
                    }

                    compiledWord.Children.Add(newMeaning);
                }
            }

            return compiledWord;
        }
        #endregion
    }
}