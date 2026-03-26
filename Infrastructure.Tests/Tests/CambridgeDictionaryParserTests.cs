using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Tests.Tests
{
    public class CambridgeDictionaryParserTests
    {
        private const string TestDataDirectory = "TestData";

        private readonly Interfaces.IParser _parser;

        public CambridgeDictionaryParserTests()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["OpenAi:Model"] = "gpt-4.1",
                    ["OpenAi:ApiKeyEnvVar"] = "OPENAI_API_KEY",
                    ["Ai:FillMissingExamplesIfBelow"] = "3",
                    ["Ai:ExamplesPerMeaningForFilling"] = "3",
                    ["Ai:ExamplesPerMeaningForNewWords"] = "4",
                    ["Database:ConnectionStringEnvVar"] = "ENGLISH_WORDS_STORAGE"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddInfrastructureServices(config);
            var provider = services.BuildServiceProvider();

            _parser = provider.GetRequiredService<Interfaces.IParser>();
        }

        [Theory]
        [InlineData("AllTheSame")]
        [InlineData("Apex")]
        [InlineData("Bar")]
        [InlineData("Call out")]
        [InlineData("Checkers")]
        [InlineData("Draught")]
        [InlineData("Eve")]
        [InlineData("SiteMaintenance")]
        [InlineData("Stake")]
        [InlineData("HotOnSomeonesHeels")]
        public async Task GetParsedWords_WithVariousHtmlInputs_ReturnsExpectedWordsAsync(string entry)
        {
            // Arrange
            var expectedResult = await LoadExpectedResultAsync(entry);
            var testHtml = await LoadTestHtmlAsync(entry);

            // Act
            var result = await _parser.ExtractWordDataAsync(testHtml);

            // Assert
            var normalizedResult = NormalizeResult(result);
            var normalizedExpected = NormalizeResult(expectedResult);
            Assert.Equal(normalizedExpected, normalizedResult, new WordEqualityComparer());
        }

        private async Task<List<Models.Parser.Word>> LoadExpectedResultAsync(string fileName)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), $@"{TestDataDirectory}\{fileName}.json");
            var json = await File.ReadAllTextAsync(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };
            return JsonSerializer.Deserialize<List<Models.Parser.Word>>(json, options)!;
        }

        private async Task<string> LoadTestHtmlAsync(string fileName)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), $@"{TestDataDirectory}\{fileName}.html");
            return await File.ReadAllTextAsync(filePath);
        }

        private static Models.Parser.Word[] NormalizeResult(IEnumerable<Models.Parser.Word> words)
        {
            var orderedResult = words.OrderBy(w => w.Area).ThenBy(w => w.Type).ThenBy(w => w.Value).ToArray();
            foreach (var word in orderedResult)
            {
                word.MeaningsAndExamples = word.MeaningsAndExamples
                    .OrderBy(kvp => kvp.Key)
                    .ThenBy(kvp => string.Join(",", kvp.Value.OrderBy(v => v)))
                    .ToDictionary();
            }
            return orderedResult;
        }
    }

    public class WordEqualityComparer : IEqualityComparer<Models.Parser.Word>
    {
        public bool Equals(Models.Parser.Word? x, Models.Parser.Word? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            bool areDictionariesEqual = AreDictionariesEqual(x.MeaningsAndExamples, y.MeaningsAndExamples);
            bool arePluralFormsEqual = x.PluralForms.SetEquals(y.PluralForms);
            bool areOtherFormsEqual = x.OtherForms.SetEquals(y.OtherForms);

            return x.Value == y.Value &&
                   x.Type == y.Type &&
                   x.Area == y.Area &&
                   areDictionariesEqual &&
                   arePluralFormsEqual &&
                   areOtherFormsEqual;
        }

        private bool AreDictionariesEqual(Dictionary<string, List<string>> dict1, Dictionary<string, List<string>> dict2)
        {
            if (dict1.Count != dict2.Count)
            {
                return false;
            }

            foreach (var kvp in dict1)
            {
                if (!dict2.TryGetValue(kvp.Key, out var list2))
                {
                    return false;
                }

                var list1 = kvp.Value;
                if (list1.Count != list2.Count)
                {
                    return false;
                }

                if (!kvp.Value.OrderBy(x => x).SequenceEqual(list2.OrderBy(x => x)))
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode(Models.Parser.Word obj)
        {
            return HashCode.Combine(
                obj.Value,
                obj.Type,
                obj.Area,
                obj.MeaningsAndExamples.Count,
                obj.PluralForms.Count,
                obj.OtherForms.Count
            );
        }
    }
}
