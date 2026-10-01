using Domain.Models.Parser;

namespace Domain.Interfaces
{
    public interface IAiService
    {
        Task<Dictionary<WordMeaning, List<string>>> GetExamplesAsync(IReadOnlyCollection<WordMeaning> meanings, int examplesPerMeaning);

        Task<List<Word>> GetMeaningsAndExamplesAsync(string[] words, int examplesPerMeaning);
    }
}
