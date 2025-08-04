using Infrastructure.Models.Parser;

namespace Infrastructure.Interfaces
{
    public interface IParser
    {
        Task<List<Word>> ExtractWordDataAsync(string html);
    }
}
