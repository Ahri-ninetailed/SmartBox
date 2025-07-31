using Domain.Models;

namespace Domain.Interfaces
{
    public interface IWordRepository
    {
        Task<Word[]> GetWordsForLearningAsync(int count = 40);

        Task<Word[]> SearchWordsAsync(string entry);

        Task<Word[]> GetWordsByIds(IEnumerable<int> ids);

        Task SaveWords(Word[] words);
    }
}
