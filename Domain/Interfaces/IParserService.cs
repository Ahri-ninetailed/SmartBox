using Domain.Models.Parser;

namespace Domain.Interfaces
{
    public interface IParserService
    {
        Task<List<Word>> ParseWordDataAsync(string entry);
    }
}
