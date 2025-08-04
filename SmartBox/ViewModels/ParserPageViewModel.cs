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

        private readonly IMapper<Application.Models.Parser.Word, Word> _wordToUIParserMapper;

        #endregion

        #region Properties
        [ObservableProperty]
        private ObservableCollection<Word> treeViewNodes = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ParseCommand))]
        private string entryText = string.Empty;

        [ObservableProperty]
        private string resultText = string.Empty;
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
        #endregion

        #region CanExecute Methods
        private bool CanParse()
        {
            return string.IsNullOrWhiteSpace(EntryText) == false;
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
                OnAnyCheckboxChanged();
            }
        }

        private void Meaning_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Meaning.IsSelected))
            {
                OnAnyCheckboxChanged();
            }
        }

        private void Example_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Example.IsSelected))
            {
                OnAnyCheckboxChanged();
            }
        }
        #endregion

        #region Private Helper Methods
        private void OnAnyCheckboxChanged()
        {
            var selectedWords = TreeViewNodes.Where(node => node.IsSelected).ToArray();

            string? preposition = null;
            if (selectedWords.Any(el => el.Type == PartOfSpeech.Verb || el.Type == PartOfSpeech.PhrasalVerb))
            {
                preposition =  "to ";
            }

            var selectedMeanings = selectedWords.SelectMany(word => word.Children)
                                                .Where(meaning => meaning.IsSelected)
                                                .ToArray();
            var selectedExamples = selectedMeanings.SelectMany(meaning => meaning.Children)
                                                   .Where(example => example.IsSelected)
                                                   .Select(example => example.Value);

            var uniqueWords = selectedWords.Select(w => w.Value)
                                           .Concat(selectedWords.SelectMany(w => w.OtherForms))
                                           .Select(w => w.ToLower())
                                           .Distinct();

            string wordsPart = string.Join(", ", uniqueWords);

            int meaningCount = selectedMeanings.Length;
            string meaningCountPart = meaningCount > 0 ? $" ({meaningCount})" : "";

            string meaningsPart = $"{string.Join(". ", selectedMeanings.Select(m => m.Value))}";
            string examplesPart = selectedExamples.Any() ? $" ({string.Join(". ", selectedExamples)})" : "";

            ResultText = $"{preposition}{wordsPart}{meaningCountPart}{examplesPart} -> {meaningsPart}";
        }
        #endregion
    }
}
