using Application.Models;

namespace Application.Interfaces
{
    public interface IDictionaryService
    {
        Task<Models.Dictionary.Word[]> GetWordsForLearningAsync();

        Task<Models.Dictionary.Word[]> SearchWordsAsync(string entry);

        Task LearnWordsAsync(IEnumerable<int> ids);

        Task MarkWordsAsync(IEnumerable<int> ids);

        Task<Models.Dictionary.Word> AddWordAsync(string[] wordValues, Models.Dictionary.PartOfSpeech partOfSpeech, string comment);
    }
}
