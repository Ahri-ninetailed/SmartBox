using Application.Interfaces;
using Application.Models.Parser;
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

        private readonly IMapper<Domain.Models.Parser.Word, Application.Models.Parser.Word> _wordToAppParserMapper;
        private readonly IMapper<Application.Models.Parser.Word, Domain.Models.Parser.Word> _wordToDomainParserMapper;
        private readonly IMapper<Domain.Models.Dictionary.Word, Application.Models.Parser.Word> _wordDomDictionaryToAppParserMapper;
        private readonly IMapper<Application.Models.Parser.Definition, Domain.Models.Dictionary.Definition> _definitionToDomainMapper;

        public ParserService(Domain.Interfaces.IParserService parserService,
                             Domain.Interfaces.IExportService excelService,
                             Domain.Interfaces.IAiService aiService,
                             Domain.Interfaces.IWordRepository wordRepository,
                             IMapper<Domain.Models.Parser.Word, Word> wordToAppParserMapper,
                             IMapper<Word, Domain.Models.Parser.Word> wordToDomainParserMapper,
                             IMapper<Domain.Models.Dictionary.Word, Word> wordDomDictionaryToAppParserMapper,
                             IMapper<Application.Models.Parser.Definition, Domain.Models.Dictionary.Definition> definitionToDomainMapper)
        {
            _parserService = parserService;
            _exportService = excelService;
            _aiService = aiService;
            _wordRepository = wordRepository;

            _wordToAppParserMapper = wordToAppParserMapper;
            _wordToDomainParserMapper = wordToDomainParserMapper;
            _wordDomDictionaryToAppParserMapper = wordDomDictionaryToAppParserMapper;
            _definitionToDomainMapper = definitionToDomainMapper;
        }

        public async Task SyncDefinitionsAndExamplesAsync(int wordId, IEnumerable<Definition> newDefinitions)
        {
            Domain.Models.Dictionary.Definition[] domainDefinitions = _definitionToDomainMapper.MapToArray(newDefinitions);

            await _wordRepository.SyncDefinitionsAndExamplesAsync(wordId, domainDefinitions);
        }

        public string GetStringForCopyToBuffer(Word word)
        {
            Domain.Models.Parser.Word domainWord = _wordToDomainParserMapper.Map(word);

            return _exportService.GetStringForCopyToBuffer(domainWord);
        }

        public void ExportWords(string directoryPath, Word[] source)
        {
            Domain.Models.Parser.Word[] domainWords = _wordToDomainParserMapper.MapToArray(source);

            _exportService.GenerateLexilizeExcel(directoryPath, domainWords);
        }

        public async Task<List<Word>> GetDefinitionsAndExamples(string[] entries, bool isAiEnabled)
        {
            entries = entries.Select(el => el.ToLower().Trim()).ToArray();

            List<Task<List<Domain.Models.Parser.Word>>> parseTasks = entries
                .Select(entry => _parserService.ParseWordDataAsync(entry))
                .ToList();

            List<Domain.Models.Parser.Word>[] parsedResults = await Task.WhenAll(parseTasks);
            List<Domain.Models.Parser.Word> wordData = parsedResults.SelectMany(x => x).ToList();

            if (isAiEnabled)
            {
                string[] uniqueWords = entries
                    .Concat(wordData.Select(w => w.Value))
                    .Concat(wordData.SelectMany(el => el.OtherForms))
                    .Distinct().ToArray();

                Task<List<Domain.Models.Parser.Word>> aiWordsTask = _aiService.GetMeaningsAndExamplesAsync(uniqueWords);

                Task fillMissingExamplesTask = _aiService.FillMissingExamplesAsync(wordData);

                await Task.WhenAll(aiWordsTask, fillMissingExamplesTask);

                List<Domain.Models.Parser.Word> aiWords = await aiWordsTask;

                wordData.AddRange(aiWords);
            }

            List<Word> result = _wordToAppParserMapper.MapToList(wordData);

            return result;
        }

        public async Task<Word> GetWordFromDb(int wordId)
        {
            Domain.Models.Dictionary.Word[] word = await _wordRepository.GetWordsByIdsAsync(new[] { wordId }, true);

            if (word.Length == 0)
            {
                throw new InvalidOperationException(string.Format(WordNotFoundError, wordId));
            }

            Word result = _wordDomDictionaryToAppParserMapper.Map(word[0]);

            return result;
        }
    }
}
