using System.Text.Json.Serialization;

namespace Infrastructure.Models
{
    public class AiWord
    {
        [JsonPropertyName("word")]
        public string Value { get; set; } = null!;

        [JsonPropertyName("meanings")]
        public Dictionary<string, Dictionary<string, List<string>>> TypesAndMeanings { get; set; } = null!;
    }
}
