using Domain.Models.Dictionary;

namespace Domain.Interfaces
{
    public interface IWordRepository
    {
        Task<Word[]> GetWordsForLearningAsync(int count = 40);

        Task<Word[]> SearchWordsAsync(string[] entries, int count = 100);

        Task<Word[]> SearchByMeaningAsync(string meaning, PartOfSpeech? partOfSpeech, int count = 500);

        Task<Word[]> GetWordsByIdsAsync(IEnumerable<int> ids, bool includeRelatedData);

        Task SaveWordsAsync(Word[] words);

        Task<Word> AddWordAsync(Word newWord);

        Task<bool> IsWordExistedAsync(string[] wordValues, PartOfSpeech partOfSpeech);

        Task SyncDefinitionsAndExamplesAsync(int wordId, Definition[] definitions);
    }
}
