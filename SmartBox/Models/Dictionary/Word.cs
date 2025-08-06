#pragma warning disable MVVMTK0045

using CommunityToolkit.Mvvm.ComponentModel;

namespace SmartBox.Models.Dictionary
{
    public partial class Word : ObservableObject
    {
        public int Id { get; set; }

        public string[] Values = [];
        public string Value
        {
            get
            {
                return string.Join(", ", Values);
            }
            set
            {
                Values = value.Split(new[] { ", ", "," }, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        public decimal? Popularity { get; set; }

        [ObservableProperty]
        private PartOfSpeech partOfSpeech;

        [ObservableProperty]
        private bool shouldStudy;
        [ObservableProperty]
        private bool learned;
        public string Comments { get; set; } = string.Empty;
    }
}
