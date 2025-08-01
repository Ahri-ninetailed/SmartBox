using Domain.Models;

namespace Domain.Interfaces
{
    public interface IWordRepository
    {
        Task<Word[]> GetWordsForLearningAsync(int count = 40);

        Task<Word[]> SearchWordsAsync(string entry);

        Task<Word[]> GetWordsByIdsAsync(IEnumerable<int> ids);

        Task SaveWordsAsync(Word[] words);

        Task<Word> AddWordAsync(Word newWord);

        Task<bool> IsWordExistedAsync(string wordValue, PartOfSpeech partOfSpeech);
    }
}
