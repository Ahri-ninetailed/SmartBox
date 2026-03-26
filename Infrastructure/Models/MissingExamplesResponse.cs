using System.Text.Json.Serialization;

namespace Infrastructure.Models
{
    public class MissingExamplesResponse
    {
        [JsonPropertyName("definitions")]
        public List<AiDefinitionExamples> Definitions { get; set; } = null!;
    }

    public class AiDefinitionExamples
    {
        [JsonPropertyName("definition")]
        public string Definition { get; set; } = null!;

        [JsonPropertyName("examples")]
        public List<string> Examples { get; set; } = null!;
    }
}
