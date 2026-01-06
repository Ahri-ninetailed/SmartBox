using Application.Interfaces;
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

        public ParserService(Domain.Interfaces.IParserService parserService,
                             Domain.Interfaces.IExportService excelService,
                             Domain.Interfaces.IAiService aiService,
                             Domain.Interfaces.IWordRepository wordRepository,
                             IMapper<Domain.Models.Parser.Definition, Domain.Models.Dictionary.Definition> parserToDictionaryDefinitionMapper,
                             IMapper<Domain.Models.Dictionary.Word, Domain.Models.Parser.Word> dictionaryToParserWordMapper)
        {
            _parserService = parserService;
            _exportService = excelService;
            _aiService = aiService;
            _wordRepository = wordRepository;
            _parserToDictionaryDefinitionMapper = parserToDictionaryDefinitionMapper;
            _dictionaryToParserWordMapper = dictionaryToParserWordMapper;
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

        public void ExportWords(string directoryPath, Domain.Models.Parser.Word[] source)
        {
            _exportService.GenerateLexilizeExcel(directoryPath, source);
        }

        public async Task<List<Domain.Models.Parser.Word>> GetDefinitionsAndExamples(string[] entries, bool isAiEnabled, bool isAiLocalOnly)
        {
            entries = entries.Select(el => el.ToLower().Trim()).ToArray();

                List<Task<List<Domain.Models.Parser.Word>>> parseTasks = entries
                    .Select(entry => _parserService.ParseWordDataAsync(entry))
                    .ToList();

                List<Domain.Models.Parser.Word>[] parsedResults = await Task.WhenAll(parseTasks);
            List<Domain.Models.Parser.Word> wordData = parsedResults.SelectMany(x => x).ToList();

            if (isAiEnabled)
            {
                string[] uniqueWords;
                if (isAiLocalOnly)
                {
                    uniqueWords = entries
                        .Distinct().ToArray();
                }   
                else
                {
                    uniqueWords = entries
                       .Concat(wordData.Select(w => w.Value))
                       .Concat(wordData.SelectMany(el => el.OtherForms))
                       .Distinct().ToArray();
                }

                Task<List<Domain.Models.Parser.Word>> aiWordsTask = _aiService.GetMeaningsAndExamplesAsync(uniqueWords);

                Task fillMissingExamplesTask = _aiService.FillMissingExamplesAsync(wordData);

                await Task.WhenAll(aiWordsTask, fillMissingExamplesTask);

                List<Domain.Models.Parser.Word> aiWords = await aiWordsTask;

                wordData.AddRange(aiWords);
            }

            return wordData;
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
    }
}
