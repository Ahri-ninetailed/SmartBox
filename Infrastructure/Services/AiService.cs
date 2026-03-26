using Domain.Interfaces;
using Domain.Models.Parser;
using Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Infrastructure.Services
{
    public class AiService : IAiService
    {
        private readonly ILogger<AiService> _logger;
        private readonly IAiModel _openAiModel;
        private readonly int _fillMissingExamplesIfBelow;
        private readonly int _examplesPerMeaningForFilling;
        private readonly int _examplesPerMeaningForNewWords;

        public AiService(
            ILogger<AiService> logger,
            IAiModel openAiModel,
            int fillMissingExamplesIfBelow,
            int examplesPerMeaningForFilling,
            int examplesPerMeaningForNewWords)
        {
            _logger = logger;
            _openAiModel = openAiModel;
            _fillMissingExamplesIfBelow = fillMissingExamplesIfBelow;
            _examplesPerMeaningForFilling = examplesPerMeaningForFilling;
            _examplesPerMeaningForNewWords = examplesPerMeaningForNewWords;
        }

        public async Task FillMissingExamplesAsync(IEnumerable<Word> words)
        {
            HashSet<(string word, string meaning)> meaningsForFilling = GetMeaningsWithEmptyExamples(words);

            if (meaningsForFilling.Count == 0)
            {
                return;
            }

            string prompt = GetPromptForMissingExamples(meaningsForFilling);

            _logger.LogInformation("Сформирован промпт для получения недостающих примеров:\n{Prompt}", prompt);

            string json = await _openAiModel.GetAnswerAsync(prompt);

            _logger.LogInformation("Ответ от модели получен:\n{Answer}", json);

            Dictionary<string, List<string>>? parsedMeaningsAndExamples = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json);
            if (parsedMeaningsAndExamples == null)
            {
                throw new InvalidOperationException("Failed to parse AI response while filling missing examples.");
            }

            parsedMeaningsAndExamples = parsedMeaningsAndExamples.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.Select(s => s.TrimEnd('.')).ToList());

            FillMissingExamples(parsedMeaningsAndExamples, words);
        }

        public async Task<List<Word>> GetMeaningsAndExamplesAsync(string[] words)
        {
            string prompt = GetPromptForMeaningsAndExamples(words);

            _logger.LogInformation("Сформирован промпт для получения определений и примеров:\n{Prompt}", prompt);

            string json = await _openAiModel.GetAnswerAsync(prompt);

            _logger.LogInformation("Ответ от модели получен:\n{Answer}", json);

            JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true };

            Models.AiWord[] parsedWords = JsonSerializer.Deserialize<Models.AiWord[]>(json, options)!;

            return parsedWords
                .SelectMany(parsed => parsed.TypesAndMeanings.Select(typeAndMeanings =>
                    new Word
                    {
                        Value = parsed.Value,
                        Area = "AI",
                        Type = MapPartOfSpeech(typeAndMeanings.Key),
                        Definitions = typeAndMeanings.Value.Select(meaning =>
                            new Definition
                            {
                                Value = char.ToUpper(meaning.Key[0]) + meaning.Key.Substring(1),
                                Examples = meaning.Value.Select(ex => new Example { Value = ex.TrimEnd('.') }).ToList()
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

            string prompt = $@"You will be given a list of word-definition pairs. For each pair, generate at least {_examplesPerMeaningForFilling} example sentences that illustrate the use of the word in the context of the given definition.

Important rules:
- Preserve the word and definition exactly as provided, including capitalization and punctuation.
- The examples must clearly reflect the meaning described in the definition.
- Return the result in JSON format.
- The structure must be: a dictionary where each key is the definition, and its value is a list of example sentences using the word in that context.
- Do not include any explanation, formatting, or markdown – only return raw JSON.

Input:
{pairs}

Output format example:
{{
  ""to have a strong unpleasant smell"": [
    ""Her breath reeked of garlic."",
    ""The garbage reeked after a week in the sun."",
    ""His clothes reeked of cigarette smoke after the party.""
  ],
  ""a strong unpleasant smell"": [
    ""The room was filled with the reek of stale beer."",
    ""The warm air was scented with the heady reek of diesel oil from the bus."",
    ""A foul reek rose from the abandoned building.""
  ]
}}
";
            return prompt;
        }

        private string GetPromptForMeaningsAndExamples(string[] words)
        {
            string prompt = $@"You are given a list of English words or phrases:

[{string.Join(", ", words.Select(s => $"\"{s}\""))}]

For each word or phrase, find all possible meanings, including rare or unique definitions. For each meaning, provide exactly {_examplesPerMeaningForNewWords} example sentences illustrating that meaning.

Return the result as a JSON array, where each element is an object with these fields:

- ""word"": the original word or phrase as it appears in the list (preserving case),
- ""meanings"": an object with keys being parts of speech (e.g. noun, verb, adjective),
- each part of speech maps to an object where keys are definitions (strings),
- each definition maps to a list of exactly {_examplesPerMeaningForNewWords} example sentences illustrating that meaning.

The output format example:

[
  {{
    ""word"": ""reek"",
    ""meanings"": {{
      ""verb"": {{
        ""to have a strong unpleasant smell"": [
          ""Her breath reeked of garlic"",
          ""The garbage reeked after days in the sun"",
          ""The old cellar reeked of mold"",
          ""His clothes reeked of smoke""
        ]
      }},
      ""noun"": {{
        ""a strong unpleasant smell"": [
          ""The room was filled with the reek of stale beer"",
          ""There was a reek of chemicals in the air"",
          ""A foul reek arose from the swamp"",
          ""The reek of burnt rubber filled the garage""
        ]
      }}
    }}
  }},
  {{
    ""word"": ""apex"",
    ""meanings"": {{
      ""noun"": {{
        ""the highest point or top of a shape or object"": [
          ""the apex of a triangle/pyramid"",
          ""The climbers reached the apex of the mountain"",
          ""The tree's apex was covered in snow"",
          ""The apex of the building was adorned with a flag""
        ],
        ""the pointed end of an organ"": [
          ""A chest x-ray showed an abnormality in the left lung apex"",
          ""The apex of the heart points downwards"",
          ""Pain was localized near the apex of the tooth"",
          ""The apex of the leaf was slightly curled""
        ]
      }}
    }}
  }}
]

Do not add any explanations or commentary. Return only the JSON content without any markdown formatting or code block markers.
";

            return prompt;
        }
    }
}
