using Application.Interfaces;
using Application.Models.Parser;
using Shared;

namespace Application.Services
{
    public class ParserService : IParserService
    {
        private readonly Domain.Interfaces.IParserService _parserService;
        private readonly Domain.Interfaces.IExcelService _excelService;

        private readonly IMapper<Domain.Models.Parser.Word, Word> _wordToAppParserMapper;
        private readonly IMapper<Word, Domain.Models.Parser.Word> _wordToDomainParserMapper;

        public ParserService(Domain.Interfaces.IParserService parserService,
                             Domain.Interfaces.IExcelService excelService,
                             IMapper<Domain.Models.Parser.Word, Word> wordToAppParserMapper,
                             IMapper<Word, Domain.Models.Parser.Word> wordToDomainParserMapper)
        {
            _parserService = parserService;
            _excelService = excelService;

            _wordToAppParserMapper = wordToAppParserMapper;
            _wordToDomainParserMapper = wordToDomainParserMapper;
        }

        public void ExportWords(string directoryPath, Word[] source)
        {
            Domain.Models.Parser.Word[] domainWords = _wordToDomainParserMapper.MapToArray(source);

            _excelService.GenerateLexilizeExcel(directoryPath, domainWords);
        }

        public async Task<List<Word>> ExtractWordDataAsync(string entry)
        {
            List<Domain.Models.Parser.Word> wordData = await _parserService.ExtractWordDataAsync(entry);

            List<Word> result = _wordToAppParserMapper.MapToList(wordData);

            return result;
        }
    }
}
