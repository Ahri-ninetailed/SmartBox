using Application.Interfaces;
using Application.Models.Parser;
using Shared;

namespace Application.Services
{
    public class ParserService : IParserService
    {
        private readonly Domain.Interfaces.IParserService _parserService;

        private readonly IMapper<Domain.Models.Parser.Word, Word> _wordToAppParserMapper;

        public ParserService(Domain.Interfaces.IParserService parserService,
                             IMapper<Domain.Models.Parser.Word, Word> wordToAppParserMapper)
        {
            _parserService = parserService;

            _wordToAppParserMapper = wordToAppParserMapper;
        }

        public async Task<List<Word>> ExtractWordDataAsync(string entry)
        {
            List<Domain.Models.Parser.Word> wordData = await _parserService.ExtractWordDataAsync(entry);

            List<Word> result = _wordToAppParserMapper.MapToList(wordData);

            return result;
        }
    }
}
