namespace SmartBox.Models
{
    public class Word
    {
        public int Id { get; set; }
        public required string Value { get; set; }
        public double Popularity { get; set; }
        public string? Type { get; set; }
        public bool ShouldStudy { get; set; }
        public bool Learned { get; set; }
        public string? Comments { get; set; }
    }
}
