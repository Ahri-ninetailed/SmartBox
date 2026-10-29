#pragma warning disable MVVMTK0045
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Shared;
using SmartBox.Interfaces;
using SmartBox.Models.Parser;
using System.Collections.ObjectModel;

namespace SmartBox.ViewModels
{
    [QueryProperty(nameof(WordId), "wordId")]
    public partial class DefinitionsPageViewModel : ObservableObject
    {
        private const string LoadWordFromDbErrorMessageFormat = "Error loading word from database with ID {0}";

        private readonly ILogger<DefinitionsPageViewModel> _logger;

        private readonly IParserService _parserService;
        private readonly IAlertService _alertService;

        private readonly IMapper<Domain.Models.Parser.Word, SmartBox.Models.Parser.Word> _wordToUIParserMapper;

        [ObservableProperty]
        private int wordId;

        [ObservableProperty]
        private ObservableCollection<Word> treeViewNodes = new();

        public DefinitionsPageViewModel(ILogger<DefinitionsPageViewModel> logger,
                           IParserService parserService,
                           IAlertService alertService,
                           IMapper<Domain.Models.Parser.Word, Word> wordToUIParserMapper)
        {
            _logger = logger;

            _parserService = parserService;
            _alertService = alertService;

            _wordToUIParserMapper = wordToUIParserMapper;
        }

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

        private async Task ShowWordFromDb(int id)
        {
            Domain.Models.Parser.Word domainWord = await _parserService.GetWordFromDbAsync(id);

            Word word = _wordToUIParserMapper.Map(domainWord);
            word.DisableAll();
            word.IsSelected = true;

            TreeViewNodes = new ObservableCollection<Word>() { word };
        }
    }
}
