namespace Application.Models
{
    public class Word
    {
        public int Id { get; set; }
        public required string Value { get; set; }
        public decimal? Popularity { get; set; }
        public PartOfSpeech PartOfSpeech { get; set; }
        public bool ShouldStudy { get; set; }
        public bool Learned { get; set; }
        public string Comments { get; set; } = string.Empty;
    }
}
