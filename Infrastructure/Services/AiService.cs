using Domain.Interfaces;
using Domain.Models.Parser;
using Infrastructure.Interfaces;
using System.Text.Json;

namespace Infrastructure.Services
{
    /// <summary>
    /// Собирает промпты, отправляет их в модель и приводит ответ к доменным моделям
    /// </summary>
    public class AiService : IAiService
    {
        private readonly IAiModel _openAiModel;
        private readonly string _missingExamplesPromptTemplate;
        private readonly string _meaningsAndExamplesPromptTemplate;
        private readonly string _missingExamplesSchema;
        private readonly string _meaningsAndExamplesSchema;

        public AiService(
            IAiModel openAiModel,
            string missingExamplesPromptTemplate,
            string meaningsAndExamplesPromptTemplate,
            string missingExamplesSchema,
            string meaningsAndExamplesSchema)
        {
            _openAiModel = openAiModel;
            _missingExamplesPromptTemplate = missingExamplesPromptTemplate;
            _meaningsAndExamplesPromptTemplate = meaningsAndExamplesPromptTemplate;
            _missingExamplesSchema = missingExamplesSchema;
            _meaningsAndExamplesSchema = meaningsAndExamplesSchema;
        }

        public async Task<Dictionary<WordMeaning, List<string>>> GetExamplesAsync(IReadOnlyCollection<WordMeaning> meanings, int examplesPerMeaning)
        {
            string prompt = GetPromptForMissingExamples(meanings, examplesPerMeaning);

            string json = await _openAiModel.GetAnswerAsync(prompt, "missing_examples", _missingExamplesSchema);

            var response = JsonSerializer.Deserialize<Models.MissingExamplesResponse>(json);
            if (response?.Words == null)
            {
                throw new InvalidOperationException("Failed to parse AI response while filling missing examples.");
            }

            return response.Words
                .ToDictionary(
                    d => new WordMeaning(d.Value, d.Definition.TrimEnd('.')),
                    d => d.Examples.Select(s => s.TrimEnd('.')).ToList());
        }

        public async Task<List<Word>> GetMeaningsAndExamplesAsync(string[] words, int examplesPerMeaning)
        {
            string prompt = GetPromptForMeaningsAndExamples(words, examplesPerMeaning);

            string json = await _openAiModel.GetAnswerAsync(prompt, "meanings_and_examples", _meaningsAndExamplesSchema);

            var response = JsonSerializer.Deserialize<Models.MeaningsAndExamplesResponse>(json)!;

            return response.Words
                .SelectMany(parsed => parsed.Meanings.Select(meaning =>
                    new Word
                    {
                        Value = parsed.Value.TrimEnd(','),
                        Area = "AI",
                        Type = MapPartOfSpeech(meaning.PartOfSpeech),
                        Definitions = meaning.Definitions.Select(def =>
                            new Definition
                            {
                                Value = (char.ToUpper(def.Definition[0]) + def.Definition.Substring(1)).TrimEnd('.'),
                                Examples = def.Examples.Select(ex => new Example { Value = ex.TrimEnd('.') }).ToList()
                            }).ToList()
                    }))
                .ToList();
        }

        private PartOfSpeech MapPartOfSpeech(string key)
        {
            if (Enum.TryParse<PartOfSpeech>(key, ignoreCase: true, out PartOfSpeech result))
            {
                return result;
            }

            if (key.Contains("adverb", StringComparison.OrdinalIgnoreCase))
            {
                return PartOfSpeech.Other;
            }

            if (key.Contains("verb", StringComparison.OrdinalIgnoreCase))
            {
                return PartOfSpeech.Verb;
            }

            if (key.Contains("noun", StringComparison.OrdinalIgnoreCase))
            {
                return PartOfSpeech.Noun;
            }

            return PartOfSpeech.Other;
        }

        private string GetPromptForMissingExamples(IEnumerable<WordMeaning> meanings, int examplesPerMeaning)
        {
            string pairs = JsonSerializer.Serialize(meanings.Select(x => new { word = x.Word, definition = x.Meaning }));

            return _missingExamplesPromptTemplate
                .Replace("{{ExampleCount}}", examplesPerMeaning.ToString())
                .Replace("{{Pairs}}", pairs);
        }

        private string GetPromptForMeaningsAndExamples(string[] words, int examplesPerMeaning)
        {
            string wordList = string.Join(", ", words.Select(s => $"\"{s}\""));

            return _meaningsAndExamplesPromptTemplate
                .Replace("{{ExampleCount}}", examplesPerMeaning.ToString())
                .Replace("{{Words}}", wordList);
        }
    }
}
