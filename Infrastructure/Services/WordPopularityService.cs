using Domain.Interfaces;
using System.Text.Json;

namespace Infrastructure.Services
{
    public class WordPopularityService : IWordPopularityService
    {
        private const string ErrorWordsNullOrEmpty = "Words cannot be null or empty";
        private const string ErrorWordsContainWhitespace = "Words cannot contain null or whitespace";

        private readonly Dictionary<string, string> QueryParams = new Dictionary<string, string>()
        {
            ["year_start"] = "1930",
            ["year_end"] = "2022",
            ["corpus"] = "en",
            ["smoothing"] = "0",
            ["case_insensitive"] = "false",
        };
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };

        private readonly HttpClient _httpClient;

        public WordPopularityService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<decimal?> GetWordPopularityAsync(string[] words)
        {
            if (words == null || words.Length == 0)
            {
                throw new ArgumentNullException(nameof(words), ErrorWordsNullOrEmpty);
            }

            if (words.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException(ErrorWordsContainWhitespace, nameof(words));
            }

            string query = BuildQueryString(words);

            HttpResponseMessage response = await _httpClient.GetAsync($"?{query}");

            response.EnsureSuccessStatusCode();

            string jsonContent = await response.Content.ReadAsStringAsync();

            NgramModel[]? result = JsonSerializer.Deserialize<NgramModel[]>(jsonContent, _jsonOptions);

            if (result == null || result.Length == 0)
            {
                return null;
            }

            return result.Select(el => el.Timeseries.Average()).Average();
        }

        private string BuildQueryString(string[] words)
        {
            Dictionary<string, string> allParams = new Dictionary<string, string>(QueryParams)
            {
                ["content"] = string.Join(',', words)
            };

            return string.Join("&", allParams
                .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
        }

        class NgramModel
        {
            public string Ngram { get; set; } = string.Empty;
            public decimal[] Timeseries { get; set; } = Array.Empty<decimal>();
        }
    }
}
