#pragma warning disable MVVMTK0045
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapster;
using SmartBox.Interfaces;
using SmartBox.Models;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace SmartBox.ViewModels
{
    public partial class DictionaryPageViewModel : ObservableObject
    {
        private readonly IDictionaryService _dictionaryService;
        private readonly IAlertService _alertService;

        [ObservableProperty]
        private ObservableCollection<Word> words;

        [ObservableProperty]
        private ObservableCollection<object> rawSelectedWords = new();

        [ObservableProperty]
        private string searchText;

        private Word[] SelectedWords => RawSelectedWords.OfType<Word>().ToArray();

        public DictionaryPageViewModel(IDictionaryService dictionaryService, IAlertService alertService)
        {
            RawSelectedWords.CollectionChanged += OnSelectedWordsChanged;

            words = new ObservableCollection<Word>();

            searchText = string.Empty;

            _dictionaryService = dictionaryService;
            _alertService = alertService;
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

        [RelayCommand(CanExecute = nameof(IsLearnButtonEnabled))]
        private async Task LearnAsync()
        {
            IEnumerable<int> idsForLearning = SelectedWords.Select(word => word.Id);

            try
            {
                //await _dictionaryService.LearnWordsAsync(idsForLearning);
            }
            catch (Exception ex)
            {
                await _alertService.DisplayAlertAsync("Error", $"Failed to mark words as learned: {ex.Message}");

                return;
            }

            foreach (Word word in SelectedWords)
            {
                Words.Remove(word);
            }
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

        private void OnSelectedWordsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            LearnCommand.NotifyCanExecuteChanged();
        }

        private bool IsLearnButtonEnabled()
        {
            return RawSelectedWords.Any();
        }
    }
}
