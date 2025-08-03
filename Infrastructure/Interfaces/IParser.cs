using Infrastructure.Models.Parser;

namespace Infrastructure.Interfaces
{
    public interface IParser
    {
        Task<List<Word>> GetParsedWords(string html);
    }
}
