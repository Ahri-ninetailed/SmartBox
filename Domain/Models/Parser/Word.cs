namespace Domain.Models.Parser
{
    public class Word
    {
        public int Id { get; set; }
        public required string Value { get; set; }

        public string? Area { get; set; }

        public PartOfSpeech Type { get; set; }

        public List<Definition> Definitions { get; set; } = [];

        public HashSet<string> PluralForms { get; set; } = new HashSet<string>();

        public HashSet<string> OtherForms { get; set; } = new HashSet<string>();

        /// <summary>
        /// Само слово и его варианты из Cambridge (also ...). Формы множественного числа сюда не входят
        /// </summary>
        public IEnumerable<string> ValueAndOtherForms => OtherForms.Prepend(Value);
    }
}
