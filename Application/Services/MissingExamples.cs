using Domain.Models.Parser;

namespace Application.Services
{
    /// <summary>
    /// Часть сценария дозаполнения примеров через AI: каким значениям не хватает примеров и как разложить ответ AI по словам
    /// </summary>
    internal static class MissingExamples
    {
        public static HashSet<WordMeaning> Find(IEnumerable<Word> words, int fillIfBelow)
        {
            return words
                .SelectMany(word => word.Definitions
                    .Where(def => def.Examples.Count < fillIfBelow)
                    .Select(def => new WordMeaning(word.Value, def.Value)))
                .ToHashSet();
        }

        public static void Add(IEnumerable<Word> words, Dictionary<WordMeaning, List<string>> examples, int fillIfBelow)
        {
            foreach (Word word in words)
            {
                if (word.Definitions.All(def => def.Examples.Count >= fillIfBelow))
                {
                    continue;
                }

                foreach (KeyValuePair<WordMeaning, List<string>> meaningExamples in examples)
                {
                    if (meaningExamples.Key.Word != word.Value)
                    {
                        continue;
                    }

                    Definition? definition = word.Definitions.FirstOrDefault(def => def.Value == meaningExamples.Key.Meaning);

                    if (definition == null)
                    {
                        continue;
                    }

                    definition.Examples.AddRange(meaningExamples.Value.Select(ex => new Example { Value = ex }));
                }
            }
        }
    }
}
