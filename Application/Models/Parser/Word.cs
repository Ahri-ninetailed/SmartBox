namespace Application.Models.Parser
{
    public class Word
    {
        public int  Id { get; set; }
        public required string Value { get; set; }

        public string? Area { get; set; }

        public PartOfSpeech Type { get; set; }

        public List<Definition> Definitions { get; set; } = [];

        public HashSet<string> PluralForms { get; set; } = new HashSet<string>();

        public HashSet<string> OtherForms { get; set; } = new HashSet<string>();
    }

    public class Definition
    {
        public int Id {  get; set; }
        public required string Value { get;set; }

        public List<Example> Examples { get; set; } = [];
    }

    public class Example
    {
        public int Id { get; set; }
        public required string Value { get; set; }
    }
}
