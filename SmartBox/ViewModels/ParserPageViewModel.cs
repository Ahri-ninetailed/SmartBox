#pragma warning disable MVVMTK0045

using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using SmartBox.Models.Parser;
using Shared;

namespace SmartBox.ViewModels
{
    public partial class ParserPageViewModel : ObservableObject
    {
        #region Constants
        #endregion

        #region Fields
        private readonly IParserService _parserService;

        private readonly IMapper<Application.Models.Parser.Word, Word> _wordToUIParserMapper;

        #endregion

        #region Properties
        [ObservableProperty]
        private ObservableCollection<Word> treeViewNodes = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ParseCommand))]
        private string entryText = string.Empty;
        #endregion

        public ParserPageViewModel(IParserService parserService, IMapper<Application.Models.Parser.Word, Word> wordToUIParserMapper)
        {
            _parserService = parserService;

            _wordToUIParserMapper = wordToUIParserMapper;
        }

        #region Public Methods

        #endregion

        #region Commands
        [RelayCommand]
        private async Task ParseAsync()
        {
            var words = await _parserService.ExtractWordDataAsync("eve");

            TreeViewNodes = new ObservableCollection<Word>(_wordToUIParserMapper.MapToList(words));
        }
        #endregion

        #region CanExecute Methods
        #endregion

        #region Property Changed Handlers

        #endregion

        #region Event Handlers
        #endregion
    }
}
