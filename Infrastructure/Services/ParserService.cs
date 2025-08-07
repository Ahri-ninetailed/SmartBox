using Domain.Interfaces;
using Infrastructure.Interfaces;
using Microsoft.Playwright;
using Shared;

namespace Infrastructure.Services
{
    public class ParserService : IParserService
    {
        private const string EntryNullOrWhiteSpaceError = "Entry cannot be null or whitespace.";
        private const string CambridgeDictionaryBaseUrl = "https://dictionary.cambridge.org/dictionary/english/";

        private readonly Task<IBrowser> _browserTask;
        private readonly IParser _cambridgeDictionaryParser;

        private readonly IMapper<Infrastructure.Models.Parser.Word, Domain.Models.Parser.Word> _wordToDomParserMapper;

        public ParserService(Task<IBrowser> browserTask, 
                             IParser cambridgeDictionaryParser,
                             IMapper<Models.Parser.Word, Domain.Models.Parser.Word> wordToDomParserMapper)
        {
            _browserTask = browserTask;
            _cambridgeDictionaryParser = cambridgeDictionaryParser;

            _wordToDomParserMapper = wordToDomParserMapper;
        }

        // HACK: спасибо CambridgeDictionary за вечные запросы к серверу
        public async Task<List<Domain.Models.Parser.Word>> ParseWordDataAsync(string entry)
        {
            if (string.IsNullOrWhiteSpace(entry))
            {
                throw new ArgumentException(EntryNullOrWhiteSpaceError, nameof(entry));
            }

            string url = BuildCambridgeUrl(entry);

            string html = await GetPageContentAsync(url);

            var parsedWords = await _cambridgeDictionaryParser.ExtractWordDataAsync(html);
            return _wordToDomParserMapper.MapToList(parsedWords);

        }

        /*
         public async Task<List<Domain.Models.Parser.Word>> ParseWordDataAsync(string entry)
        {
            if (string.IsNullOrWhiteSpace(entry))
            {
                throw new ArgumentException(EntryNullOrWhiteSpaceError, nameof(entry));
            }

            HttpClient cambridgeDictionaryClient = _httpClientFactory.CreateClient(Models.Parser.Parser.CambridgeDictionary.ToString());

            HttpResponseMessage cambridgeDictionaryResponse = await cambridgeDictionaryClient.GetAsync(entry.Replace(' ', '-'));

            string html = await cambridgeDictionaryResponse.Content.ReadAsStringAsync();

            List<Models.Parser.Word> parsedWords = await _cambridgeDictionaryParser.ParseWordDataAsync(html);

            List<Domain.Models.Parser.Word> result = _wordToDomParserMapper.MapToList(parsedWords);

            return result;
        }
         */

        private static string BuildCambridgeUrl(string entry)
        {
            return $"{CambridgeDictionaryBaseUrl}{entry.Replace(' ', '-')}";
        }

        private async Task<string> GetPageContentAsync(string url)
        {
            var browser = await _browserTask;
            var page = await browser.NewPageAsync();
            try
            {
                await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
                return await page.ContentAsync();
            }
            finally
            {
                await page.CloseAsync();
            }
        }
    }
}
