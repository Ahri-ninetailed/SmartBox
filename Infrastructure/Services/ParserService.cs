using Domain.Interfaces;
using Infrastructure.Interfaces;
using Shared;

namespace Infrastructure.Services
{
    public class ParserService : IParserService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IParser _cambridgeDictionaryParser;

        private readonly IMapper<Models.Parser.Word, Domain.Models.Parser.Word> _wordToDomParserMapper;

        public ParserService(IHttpClientFactory httpClientFactory, 
                             IParser cambridgeDictionaryParser,
                             IMapper<Models.Parser.Word, Domain.Models.Parser.Word> wordToDomParserMapper)
        {
            _httpClientFactory = httpClientFactory;
            _cambridgeDictionaryParser = cambridgeDictionaryParser;

            _wordToDomParserMapper = wordToDomParserMapper;
        }

        public async Task<List<Domain.Models.Parser.Word>> ParseWord(string entry)
        {
            HttpClient cambridgeDictionaryClient = _httpClientFactory.CreateClient(Models.Parser.Parser.CambridgeDictionary.ToString());

            HttpResponseMessage cambridgeDictionaryResponse = await cambridgeDictionaryClient.GetAsync(entry.Replace(' ', '-'));

            string html = await cambridgeDictionaryResponse.Content.ReadAsStringAsync();

            List<Models.Parser.Word> parsedWords = await _cambridgeDictionaryParser.GetParsedWords(html);

            List<Domain.Models.Parser.Word> result = _wordToDomParserMapper.MapToList(parsedWords);

            return result;
        }
    }
}
