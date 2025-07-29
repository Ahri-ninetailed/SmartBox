#pragma warning disable MVVMTK0045

using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapster;
using SmartBox.Models;
using System.Collections.ObjectModel;

namespace SmartBox.ViewModels
{
    public partial class DictionaryPageViewModel : ObservableObject
    {
        private readonly IDictionaryService _dictionaryService;

        [ObservableProperty]
        private ObservableCollection<Word> words;

        [ObservableProperty]
        private ObservableCollection<object> rawSelectedWords = new();

        [ObservableProperty]
        private string? searchText;

        private IEnumerable<Word> SelectedWords => RawSelectedWords.OfType<Word>();

        public DictionaryPageViewModel(IDictionaryService dictionaryService)
        {
            words = new ObservableCollection<Word>();

            _dictionaryService = dictionaryService;
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            
        }

        public async Task InitializeAsync()
        {
            Words = await GetWordsForLearningAsync();
        }

        private async Task<ObservableCollection<Word>> GetWordsForLearningAsync()
        {
            Application.Models.Word[] rawWords = await _dictionaryService.GetWordsForLearningAsync();

            Word[] result = rawWords.Adapt<Word[]>();

            return new ObservableCollection<Word>(result);
        }
    }
}
