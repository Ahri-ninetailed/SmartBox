using Domain.Interfaces;
using Domain.Models.Parser;
using Infrastructure.Interfaces;
using System.Text.Json;

namespace Infrastructure.Services
{
    public class AiService : IAiService
    {
        private readonly IAiModel _openAiModel;
        private readonly int _fillMissingExamplesIfBelow;
        private readonly int _examplesPerMeaningForFilling;
        private readonly int _examplesPerMeaningForNewWords;
        private readonly string _missingExamplesPromptTemplate;
        private readonly string _meaningsAndExamplesPromptTemplate;
        private readonly string _missingExamplesSchema;
        private readonly string _meaningsAndExamplesSchema;

        public AiService(
            IAiModel openAiModel,
            int fillMissingExamplesIfBelow,
            int examplesPerMeaningForFilling,
            int examplesPerMeaningForNewWords,
            string missingExamplesPromptTemplate,
            string meaningsAndExamplesPromptTemplate,
            string missingExamplesSchema,
            string meaningsAndExamplesSchema)
        {
            _openAiModel = openAiModel;
            _fillMissingExamplesIfBelow = fillMissingExamplesIfBelow;
            _examplesPerMeaningForFilling = examplesPerMeaningForFilling;
            _examplesPerMeaningForNewWords = examplesPerMeaningForNewWords;
            _missingExamplesPromptTemplate = missingExamplesPromptTemplate;
            _meaningsAndExamplesPromptTemplate = meaningsAndExamplesPromptTemplate;
            _missingExamplesSchema = missingExamplesSchema;
            _meaningsAndExamplesSchema = meaningsAndExamplesSchema;
        }

        public async Task FillMissingExamplesAsync(IEnumerable<Word> words)
        {
            HashSet<(string word, string meaning)> meaningsForFilling = GetMeaningsWithEmptyExamples(words);

            if (meaningsForFilling.Count == 0)
            {
                return;
            }

            string prompt = GetPromptForMissingExamples(meaningsForFilling);

            string json = await _openAiModel.GetAnswerAsync(prompt, "missing_examples", _missingExamplesSchema);

            var response = JsonSerializer.Deserialize<Models.MissingExamplesResponse>(json);
            if (response?.Definitions == null)
            {
                throw new InvalidOperationException("Failed to parse AI response while filling missing examples.");
            }

            Dictionary<string, List<string>> parsedMeaningsAndExamples = response.Definitions
                .ToDictionary(
                    d => d.Definition.TrimEnd('.'),
                    d => d.Examples.Select(s => s.TrimEnd('.')).ToList());

            FillMissingExamples(parsedMeaningsAndExamples, words);
        }

        public async Task<List<Word>> GetMeaningsAndExamplesAsync(string[] words)
        {
            string prompt = GetPromptForMeaningsAndExamples(words);

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

        internal HashSet<(string word, string meaning)> GetMeaningsWithEmptyExamples(IEnumerable<Word> words)
        {
            return words
                .SelectMany(word => word.Definitions
                    .Where(def => def.Examples.Count < _fillMissingExamplesIfBelow)
                    .Select(def => (word.Value, def.Value)))
                .ToHashSet();
        }

        internal void FillMissingExamples(Dictionary<string, List<string>> meaningsAndExamples, IEnumerable<Word> words)
        {
            foreach (Word word in words)
            {
                if (word.Definitions.All(def => def.Examples.Count >= _fillMissingExamplesIfBelow))
                {
                    continue;
                }

                foreach (KeyValuePair<string, List<string>> kvp in meaningsAndExamples)
                {
                    Definition? definition = word.Definitions.FirstOrDefault(def => def.Value == kvp.Key);

                    if (definition == null)
                    {
                        continue;
                    }

                    definition.Examples.AddRange(kvp.Value.Select(ex => new Example { Value = ex }));
                }
            }
        }

        private string GetPromptForMissingExamples(HashSet<(string word, string meaning)> meaningsForFilling)
        {
            string pairs = JsonSerializer.Serialize(meaningsForFilling.Select(x => new { word = x.word, definition = x.meaning }));

            return _missingExamplesPromptTemplate
                .Replace("{{ExampleCount}}", _examplesPerMeaningForFilling.ToString())
                .Replace("{{Pairs}}", pairs);
        }

        private string GetPromptForMeaningsAndExamples(string[] words)
        {
            string wordList = string.Join(", ", words.Select(s => $"\"{s}\""));

            return _meaningsAndExamplesPromptTemplate
                .Replace("{{ExampleCount}}", _examplesPerMeaningForNewWords.ToString())
                .Replace("{{Words}}", wordList);
        }
    }
}
