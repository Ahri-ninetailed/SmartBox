namespace Domain.Models.Parser
{
    public class Word
    {
        public required string Value { get; set; }

        public string? Area { get; set; }

        public PartsOfSpeech Type { get; set; }

        public Dictionary<string, List<string>> MeaningsAndExamples { get; set; } = new Dictionary<string, List<string>>();

        public HashSet<string> PluralForms { get; set; } = new HashSet<string>();

        public HashSet<string> OtherForms { get; set; } = new HashSet<string>();
    }
}
