#pragma warning disable MVVMTK0045

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartBox.Models;
using System.Collections.ObjectModel;

namespace SmartBox.ViewModels
{
    public partial class DictionaryPageViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<Word> words;

        [ObservableProperty]
        private ObservableCollection<object> rawSelectedWords = new();

        [ObservableProperty]
        private string searchText;

        private IEnumerable<Word> SelectedWords => RawSelectedWords.OfType<Word>();

        public DictionaryPageViewModel()
        {
            Words = new ObservableCollection<Word>
            {
                new Word { Id = 1, Value = "apple", Popularity = 0.8, Type = "noun", ShouldStudy = true, Learned = false, Comments = "Common word" },
                new Word { Id = 2, Value = "benevolent", Popularity = 0.3, Type = "adjective", ShouldStudy = true, Learned = false, Comments = "Advanced vocabulary" },
                new Word { Id = 3, Value = "run", Popularity = 0.95, Type = "verb", ShouldStudy = false, Learned = true, Comments = "" },
                new Word { Id = 1, Value = "apple", Popularity = 0.8, Type = "noun", ShouldStudy = true, Learned = false, Comments = "Common word" },
                new Word { Id = 2, Value = "benevolent", Popularity = 0.3, Type = "adjective", ShouldStudy = true, Learned = false, Comments = "Advanced vocabulary" },
                new Word { Id = 3, Value = "run", Popularity = 0.95, Type = "verb", ShouldStudy = false, Learned = true, Comments = "" },
                new Word { Id = 1, Value = "apple", Popularity = 0.8, Type = "noun", ShouldStudy = true, Learned = false, Comments = "Common word" },
                new Word { Id = 2, Value = "benevolent", Popularity = 0.3, Type = "adjective", ShouldStudy = true, Learned = false, Comments = "Advanced vocabulary" },
                new Word { Id = 3, Value = "run", Popularity = 0.95, Type = "verb", ShouldStudy = false, Learned = true, Comments = "" },
                new Word { Id = 1, Value = "apple", Popularity = 0.8, Type = "noun", ShouldStudy = true, Learned = false, Comments = "Common word" },
                new Word { Id = 2, Value = "benevolent", Popularity = 0.3, Type = "adjective", ShouldStudy = true, Learned = false, Comments = "Advanced vocabulary" },
                new Word { Id = 3, Value = "run", Popularity = 0.95, Type = "verb", ShouldStudy = false, Learned = true, Comments = "" },
                new Word { Id = 1, Value = "apple", Popularity = 0.8, Type = "noun", ShouldStudy = true, Learned = false, Comments = "Common word" },
                new Word { Id = 2, Value = "benevolent", Popularity = 0.3, Type = "adjective", ShouldStudy = true, Learned = false, Comments = "Advanced vocabulary" },
                new Word { Id = 3, Value = "run", Popularity = 0.95, Type = "verb", ShouldStudy = false, Learned = true, Comments = "" },
                new Word { Id = 1, Value = "apple", Popularity = 0.8, Type = "noun", ShouldStudy = true, Learned = false, Comments = "Common word" },
                new Word { Id = 2, Value = "benevolent", Popularity = 0.3, Type = "adjective", ShouldStudy = true, Learned = false, Comments = "Advanced vocabulary" },
                new Word { Id = 3, Value = "run", Popularity = 0.95, Type = "verb", ShouldStudy = false, Learned = true, Comments = "" },
            };
        }
        [RelayCommand]
        private void Search()
        {

        }
    }
}
