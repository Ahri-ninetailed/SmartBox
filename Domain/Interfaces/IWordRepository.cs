using Domain.Models.Dictionary;

namespace Domain.Interfaces
{
    public interface IWordRepository
    {
        Task<Word[]> GetWordsForLearningAsync(int count = 40);

        Task<Word[]> SearchWordsAsync(string[] entries);

        Task<Word[]> GetWordsByIdsAsync(IEnumerable<int> ids, bool includeRelatedData);

        Task SaveWordsAsync(Word[] words);

        Task<Word> AddWordAsync(Word newWord);

        Task<bool> IsWordExistedAsync(string[] wordValues, PartOfSpeech partOfSpeech);
    }
}
