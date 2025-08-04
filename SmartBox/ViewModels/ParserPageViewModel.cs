#pragma warning disable MVVMTK0045

using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SmartBox.ViewModels
{
    public partial class ParserPageViewModel : ObservableObject
    {
        private readonly IParserService _parserService;

        public ParserPageViewModel(IParserService parserService)
        {
            _parserService = parserService;
        }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ParseCommand))]
        private string entryText = string.Empty;

        [RelayCommand]
        private async Task ParseAsync()
        {
            var a = await _parserService.ExtractWordDataAsync(EntryText);
        }
    }
}
