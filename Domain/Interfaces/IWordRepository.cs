using Domain.Models;

namespace Domain.Interfaces
{
    public interface IWordRepository
    {
        Task<Word[]> GetWordsForLearningAsync(int count = 40);
    }
}
