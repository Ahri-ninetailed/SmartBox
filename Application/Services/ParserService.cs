using Application.Interfaces;
using Application.Models.Parser;
using Shared;

namespace Application.Services
{
    public class ParserService : IParserService
    {
        private readonly Domain.Interfaces.IParserService _parserService;
        private readonly Domain.Interfaces.IExcelService _excelService;
        private readonly Domain.Interfaces.IAiService _aiService;
        private readonly Domain.Interfaces.IWordRepository _wordRepository;

        private readonly IMapper<Domain.Models.Parser.Word, Application.Models.Parser.Word> _wordToAppParserMapper;
        private readonly IMapper<Application.Models.Parser.Word, Domain.Models.Parser.Word> _wordToDomainParserMapper;
        private readonly IMapper<Domain.Models.Dictionary.Word, Application.Models.Parser.Word> _wordDomDictionaryToAppParserMapper;

        public ParserService(Domain.Interfaces.IParserService parserService,
                             Domain.Interfaces.IExcelService excelService,
                             Domain.Interfaces.IAiService aiService,
                             Domain.Interfaces.IWordRepository wordRepository,
                             IMapper<Domain.Models.Parser.Word, Word> wordToAppParserMapper,
                             IMapper<Word, Domain.Models.Parser.Word> wordToDomainParserMapper,
                             IMapper<Domain.Models.Dictionary.Word, Word> wordDomDictionaryToAppParserMapper)
        {
            _parserService = parserService;
            _excelService = excelService;
            _aiService = aiService;
            _wordRepository = wordRepository;

            _wordToAppParserMapper = wordToAppParserMapper;
            _wordToDomainParserMapper = wordToDomainParserMapper;
            _wordDomDictionaryToAppParserMapper = wordDomDictionaryToAppParserMapper;
        }

        public void ExportWords(string directoryPath, Word[] source)
        {
            Domain.Models.Parser.Word[] domainWords = _wordToDomainParserMapper.MapToArray(source);

            _excelService.GenerateLexilizeExcel(directoryPath, domainWords);
        }

        public async Task<List<Word>> GetDefinitionsAndExamples(string[] entries)
        {
            List<Task<List<Domain.Models.Parser.Word>>> parseTasks = entries
                .Select(entry => _parserService.ParseWordDataAsync(entry))
                .ToList();

            List<Domain.Models.Parser.Word>[] parsedResults = await Task.WhenAll(parseTasks);

            List<Domain.Models.Parser.Word> wordData = parsedResults.SelectMany(x => x).ToList();

            /*string[] uniqueWords = wordData.Count == 0 ? entries : wordData.Select(w => w.Value).Distinct().ToArray();

            Task<List<Domain.Models.Parser.Word>> aiWordsTask = _aiService.GetMeaningsAndExamplesAsync(uniqueWords);

            Task fillMissingExamplesTask = _aiService.FillMissingExamplesAsync(wordData);

            await Task.WhenAll(aiWordsTask, fillMissingExamplesTask);

            List<Domain.Models.Parser.Word> aiWords = await aiWordsTask;

            wordData.AddRange(aiWords);*/

            List<Word> result = _wordToAppParserMapper.MapToList(wordData);

            return result;
        }

        public async Task<Word> GetWordFromDb(int wordId)
        {
            Domain.Models.Dictionary.Word[] word = await _wordRepository.GetWordsByIdsAsync(new[] { wordId }, true);

            if (word.Length == 0)
            {
                // TODO: вынести в конснанту
                throw new KeyNotFoundException($"Word with ID {wordId} not found in the database.");
            }

            Word result = _wordDomDictionaryToAppParserMapper.Map(word[0]);

            return result;
        }
    }
}
