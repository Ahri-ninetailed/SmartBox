using System.Text.Json.Serialization;

namespace Infrastructure.Models
{
    public class MeaningsAndExamplesResponse
    {
        [JsonPropertyName("words")]
        public List<AiWord> Words { get; set; } = null!;
    }

    public class AiWord
    {
        [JsonPropertyName("word")]
        public string Value { get; set; } = null!;

        [JsonPropertyName("meanings")]
        public List<AiMeaning> Meanings { get; set; } = null!;
    }

    public class AiMeaning
    {
        [JsonPropertyName("part_of_speech")]
        public string PartOfSpeech { get; set; } = null!;

        [JsonPropertyName("definitions")]
        public List<AiDefinition> Definitions { get; set; } = null!;
    }

    public class AiDefinition
    {
        [JsonPropertyName("definition")]
        public string Definition { get; set; } = null!;

        [JsonPropertyName("examples")]
        public List<string> Examples { get; set; } = null!;
    }
}
