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
        private string searchText;

        private IEnumerable<Word> SelectedWords => RawSelectedWords.OfType<Word>();

        public DictionaryPageViewModel(IDictionaryService dictionaryService)
        {
            words = new ObservableCollection<Word>();

            searchText = string.Empty;

            _dictionaryService = dictionaryService;
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                Words = await GetWordsForLearningAsync();

                return;
            }

            Application.Models.Word[] appWords = await _dictionaryService.SearchWordsAsync(SearchText);

            Word[] words = appWords.Adapt<Word[]>();

            Words = new ObservableCollection<Word>(words);
        }

        [RelayCommand]
        private async Task LearnAsync()
        {
            if (SelectedWords.Count() == 0)
            {
                return;
            }

            IEnumerable<int> idsForLearning = SelectedWords.Select(word => word.Id);

            await _dictionaryService.LearnWordsAsync(idsForLearning);
        }

        public async Task InitializeAsync()
        {
            Words = await GetWordsForLearningAsync();
        }

        private async Task<ObservableCollection<Word>> GetWordsForLearningAsync()
        {
            Application.Models.Word[] appWords = await _dictionaryService.GetWordsForLearningAsync();

            Word[] words = appWords.Adapt<Word[]>();

            return new ObservableCollection<Word>(words);
        }
    }
}
