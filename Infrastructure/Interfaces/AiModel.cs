using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Infrastructure.Interfaces
{
    public abstract class AiModel
    {
        private readonly int _exampleCount;
        private readonly ILogger _logger;

        public AiModel(ILogger logger, int exampleCount)
        {
            _logger = logger;
            _exampleCount = exampleCount;
        }

        public async Task<string> GetExamplesForMeaningsAsync(HashSet<(string word, string meaning)> meaningsForFilling)
        {
            string prompt = GetPromptForMissingExamples(meaningsForFilling);

            _logger.LogInformation("Сформирован промпт для получения недостающих примеров:\n{Prompt}", prompt);

            string answer = await GetAnswerAsync(prompt);

            _logger.LogInformation("Ответ от модели получен:\n{Answer}", answer);

            return answer;
        }

        private string GetPromptForMissingExamples(HashSet<(string word, string meaning)> meaningsForFilling)
        {
            string pairs = JsonSerializer.Serialize(meaningsForFilling.Select(x => new { word = x.word, definition = x.meaning }));

            string prompt = $@"You will be given a list of word-definition pairs. For each pair, generate at least {_exampleCount} example sentences that illustrate the use of the word in the context of the given definition.

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

        public async Task<string> GetMeaningsAndExamplesAsync(string[] words)
        {
            string prompt = GetPromptForMeaningsAndExamples(words);

            _logger.LogInformation("Сформирован промпт для получения определений и примеров:\n{Prompt}", prompt);

            string answer = await GetAnswerAsync(prompt);

            _logger.LogInformation("Ответ от модели получен:\n{Answer}", answer);

            return answer;
        }

        private string GetPromptForMeaningsAndExamples(string[] words)
        {
            string prompt = $@"You are given a list of English words or phrases:

[{string.Join(", ", words.Select(s => $"\"{s}\""))}]

For each word or phrase, find all possible meanings, including rare or unique definitions. For each meaning, provide exactly 4 example sentences illustrating that meaning.

Return the result as a JSON array, where each element is an object with these fields:

- ""word"": the original word or phrase as it appears in the list (preserving case),
- ""meanings"": an object with keys being parts of speech (e.g. noun, verb, adjective),
- each part of speech maps to an object where keys are definitions (strings),
- each definition maps to a list of exactly 4 example sentences illustrating that meaning.

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

        protected abstract Task<string> GetAnswerAsync(string prompt);
    }
}
