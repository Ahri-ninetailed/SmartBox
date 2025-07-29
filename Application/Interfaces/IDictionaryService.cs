using Application.Models;

namespace Application.Interfaces
{
    public interface IDictionaryService
    {
        Task<Word[]> GetWordsForLearningAsync();
    }
}
