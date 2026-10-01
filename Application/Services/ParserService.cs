using Application.Enums;
using Application.Interfaces;
using Application.Settings;
using Domain.Models.Parser;
using Shared;

namespace Application.Services
{
    public class ParserService : IParserService
    {
        const string WordNotFoundError = "Word with ID {0} not found in the database.";

        private readonly Domain.Interfaces.IParserService _parserService;
        private readonly Domain.Interfaces.IExportService _exportService;
        private readonly Domain.Interfaces.IAiService _aiService;
        private readonly Domain.Interfaces.IWordRepository _wordRepository;
        private readonly IMapper<Domain.Models.Parser.Definition, Domain.Models.Dictionary.Definition> _parserToDictionaryDefinitionMapper;
        private readonly IMapper<Domain.Models.Dictionary.Word, Domain.Models.Parser.Word> _dictionaryToParserWordMapper;
        private readonly AiSettings _aiSettings;

        public ParserService(Domain.Interfaces.IParserService parserService,
                             Domain.Interfaces.IExportService excelService,
                             Domain.Interfaces.IAiService aiService,
                             Domain.Interfaces.IWordRepository wordRepository,
                             IMapper<Domain.Models.Parser.Definition, Domain.Models.Dictionary.Definition> parserToDictionaryDefinitionMapper,
                             IMapper<Domain.Models.Dictionary.Word, Domain.Models.Parser.Word> dictionaryToParserWordMapper,
                             AiSettings aiSettings)
        {
            _parserService = parserService;
            _exportService = excelService;
            _aiService = aiService;
            _wordRepository = wordRepository;
            _parserToDictionaryDefinitionMapper = parserToDictionaryDefinitionMapper;
            _dictionaryToParserWordMapper = dictionaryToParserWordMapper;
            _aiSettings = aiSettings;
        }

        public async Task SyncDefinitionsAndExamplesAsync(int wordId, IEnumerable<Domain.Models.Parser.Definition> newDefinitions)
        {
            Domain.Models.Dictionary.Definition[] dictionaryDefinitions = newDefinitions
                .Select(def => _parserToDictionaryDefinitionMapper.Map(def))
                .ToArray();

            await _wordRepository.SyncDefinitionsAndExamplesAsync(wordId, dictionaryDefinitions);
        }

        public string GetStringForCopyToBuffer(Domain.Models.Parser.Word word)
        {
            return _exportService.GetStringForCopyToBuffer(word);
        }

        public string ExportAnkiWords(string directoryPath, Domain.Models.Parser.Word[] source)
        {
            return _exportService.GenerateAnkiTsv(directoryPath, source);
        }

        public async Task<List<Word>> GetDefinitionsAndExamples(string[] entries, AiMode aiMode)
        {
            string[] normalizedEntries = entries.Select(entry => entry.ToLower().Trim()).ToArray();

            List<Word> dictionaryWords = aiMode == AiMode.Exclusive
                ? new List<Word>()
                : await ParseEntriesAsync(normalizedEntries);

            if (aiMode == AiMode.Disabled)
            {
                return dictionaryWords;
            }

            string[] wordsForAi = GetWordsForAi(normalizedEntries, dictionaryWords, aiMode);

            Task<List<Word>> aiWordsTask = _aiService.GetMeaningsAndExamplesAsync(wordsForAi, _aiSettings.ExamplesPerMeaningForNewWords);
            // Дописывает примеры прямо в dictionaryWords
            Task fillMissingExamplesTask = FillMissingExamplesAsync(dictionaryWords);

            await Task.WhenAll(aiWordsTask, fillMissingExamplesTask);

            return dictionaryWords.Concat(await aiWordsTask).ToList();
        }

        public async Task<Domain.Models.Parser.Word> GetWordFromDb(int wordId)
        {
            Domain.Models.Dictionary.Word[] words = await _wordRepository.GetWordsByIdsAsync(new[] { wordId }, true);

            if (words.Length == 0)
            {
                throw new InvalidOperationException(string.Format(WordNotFoundError, wordId));
            }

            return _dictionaryToParserWordMapper.Map(words[0]);
        }

        private async Task FillMissingExamplesAsync(List<Word> words)
        {
            HashSet<WordMeaning> meaningsToFill = MissingExamples.Find(words, _aiSettings.FillMissingExamplesIfBelow);

            if (meaningsToFill.Count == 0)
            {
                return;
            }

            Dictionary<WordMeaning, List<string>> examples = await _aiService.GetExamplesAsync(meaningsToFill, _aiSettings.ExamplesPerMeaningForFilling);

            MissingExamples.Add(words, examples, _aiSettings.FillMissingExamplesIfBelow);
        }

        private async Task<List<Word>> ParseEntriesAsync(string[] entries)
        {
            List<Word>[] parsedResults = await Task.WhenAll(entries.Select(_parserService.ParseWordDataAsync));

            return parsedResults.SelectMany(words => words).ToList();
        }

        private static string[] GetWordsForAi(string[] entries, List<Word> dictionaryWords, AiMode aiMode)
        {
            IEnumerable<string> wordsForAi = aiMode == AiMode.Full
                ? entries.Concat(dictionaryWords.SelectMany(word => word.ValueAndOtherForms))
                : entries;

            return wordsForAi.Distinct().ToArray();
        }
    }
}
