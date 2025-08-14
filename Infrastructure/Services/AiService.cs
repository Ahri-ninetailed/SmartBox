using Domain.Models.Parser;
using System.Text.Json;

namespace Infrastructure.Services
{
    public class AiService : Domain.Interfaces.IAiService
    {
        private const int AcceptableAmountOfExamples = 3;

        private readonly Interfaces.AiModel _openAiModel;

        public AiService(Interfaces.AiModel openAiModel)
        {
            _openAiModel = openAiModel;
        }

        public async Task FillMissingExamplesAsync(IEnumerable<Word> words)
        {
            HashSet<(string word, string meaning)> meaningsForFilling = GetMeaningsWithEmptyExamples(words);

            if (meaningsForFilling.Count == 0)
            {
                return;
            }

            string json = await _openAiModel.GetExamplesForMeaningsAsync(meaningsForFilling);

            Dictionary<string, List<string>> parsedMeaningsAndExamples = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json)!;

            parsedMeaningsAndExamples = parsedMeaningsAndExamples.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.Select(s => s.TrimEnd('.')).ToList());

            FillMissingExamples(parsedMeaningsAndExamples, words);
        }

        public async Task<List<Word>> GetMeaningsAndExamplesAsync(string[] words)
        {
            string json = await _openAiModel.GetMeaningsAndExamplesAsync(words);

            JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true };

            Models.AiWord[] parsedWords = JsonSerializer.Deserialize<Models.AiWord[]>(json, options)!;

            return parsedWords
                .SelectMany(parsed => parsed.TypesAndMeanings.Select(typeAndMeanings =>
                    new Word
                    {
                        Value = parsed.Value,
                        Area = "AI",
                        Type = MapPartOfSpeech(typeAndMeanings.Key),
                        MeaningsAndExamples = typeAndMeanings.Value.ToDictionary(
                            meaning => char.ToUpper(meaning.Key[0]) + meaning.Key.Substring(1),
                            meaning => meaning.Value.Select(ex => ex.TrimEnd('.')).ToList()
                        )
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
                .SelectMany(word => word.MeaningsAndExamples
                    .Where(kvp => kvp.Value.Count < AcceptableAmountOfExamples)
                    .Select(kvp => (word.Value, kvp.Key)))
                .ToHashSet();
        }

        private void FillMissingExamples(Dictionary<string, List<string>> meaningsAndExamples, IEnumerable<Word> words)
        {
            foreach (Word word in words)
            {
                if (word.MeaningsAndExamples.All(el => el.Value.Count >= AcceptableAmountOfExamples))
                {
                    continue;
                }

                foreach (KeyValuePair<string, List<string>> kvp in meaningsAndExamples)
                {
                    if (!word.MeaningsAndExamples.ContainsKey(kvp.Key))
                    {
                        continue;
                    }

                    word.MeaningsAndExamples[kvp.Key].AddRange(kvp.Value);
                }
            }
        }
    }
}
