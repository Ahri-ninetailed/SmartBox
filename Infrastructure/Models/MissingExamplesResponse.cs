using System.Text.Json.Serialization;

namespace Infrastructure.Models
{
    public class MissingExamplesResponse
    {
        [JsonPropertyName("words")]
        public List<AiDefinitionExamples> Words { get; set; } = null!;
    }

    public class AiDefinitionExamples
    {
        [JsonPropertyName("value")]
        public string Value { get; set; } = null!;

        [JsonPropertyName("definition")]
        public string Definition { get; set; } = null!;

        [JsonPropertyName("examples")]
        public List<string> Examples { get; set; } = null!;
    }
}
