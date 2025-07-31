using Application.Models;

namespace Application.Interfaces
{
    public interface IDictionaryService
    {
        Task<Word[]> GetWordsForLearningAsync();

        Task<Word[]> SearchWordsAsync(string entry);

        Task LearnWordsAsync(IEnumerable<int> ids);

        Task MarkWordsAsync(IEnumerable<int> ids);
    }
}
