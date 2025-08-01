#pragma warning disable MVVMTK0045

using CommunityToolkit.Mvvm.ComponentModel;

namespace SmartBox.Models
{
    public partial class Word : ObservableObject
    {
        public int Id { get; set; }
        public required string Value { get; set; }
        public decimal? Popularity { get; set; }
        public PartOfSpeech PartOfSpeech { get; set; }

        [ObservableProperty]
        private bool shouldStudy;
        [ObservableProperty]
        private bool learned;
        public string Comments { get; set; } = string.Empty;
    }
}
