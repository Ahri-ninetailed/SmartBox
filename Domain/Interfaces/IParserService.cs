using Domain.Models.Parser;

namespace Domain.Interfaces
{
    public interface IParserService
    {
        Task<List<Word>> ParseWord(string entry);
    }
}
