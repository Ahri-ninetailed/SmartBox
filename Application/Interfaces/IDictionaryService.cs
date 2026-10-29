namespace Application.Interfaces
{
    public interface IDictionaryService
    {
        Task<Domain.Models.Dictionary.Word[]> GetWordsForLearningAsync();

        Task<Domain.Models.Dictionary.Word[]> SearchWordsAsync(string[] entries);

        Task<Domain.Models.Dictionary.Word[]> SearchByMeaningAsync(string meaning, Domain.Models.Dictionary.PartOfSpeech? partOfSpeech);

        Task LearnWordsAsync(IEnumerable<int> ids);

        Task MarkWordsAsync(IEnumerable<int> ids);

        Task<Domain.Models.Dictionary.Word> AddWordAsync(string[] wordValues, Domain.Models.Dictionary.PartOfSpeech partOfSpeech, string comment);

        Task ChangeWordPartOfSpeechAsync(int id, Domain.Models.Dictionary.PartOfSpeech partOfSpeech);
    }
}
