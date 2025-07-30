using Application.Interfaces;
using Application.Models;
using Domain.Interfaces;
using Mapster;

namespace Application.Services
{
    public class DictionaryService : IDictionaryService
    {
        private readonly IWordRepository _wordRepository;

        public DictionaryService(IWordRepository wordRepository)
        {
            _wordRepository = wordRepository;
        }

        public async Task<Word[]> GetWordsForLearningAsync()
        {
            Domain.Models.Word[] words = await _wordRepository.GetWordsForLearningAsync();

            return words.Adapt<Word[]>();
        }

        public async Task<Word[]> SearchWordsAsync(string entry)
        {
            Domain.Models.Word[] words = await _wordRepository.SearchWordsAsync(entry);

            return words.Adapt<Word[]>();
        }
    }
}
