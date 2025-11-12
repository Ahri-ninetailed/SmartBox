namespace Domain.Models.Parser
{
    public class Definition
    {
        public int Id { get; set; }
        public required string Value { get; set; }
        public List<Example> Examples { get; set; } = [];
    }
}
