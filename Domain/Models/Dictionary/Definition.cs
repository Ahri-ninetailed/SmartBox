namespace Domain.Models.Dictionary
{
    public class Definition
    {
        public int WordId { get; set; }

        public int Id { get; set; }

        public required string Value { get; set; }

        public List<Example> Examples { get; set; } = [];
    }
}
