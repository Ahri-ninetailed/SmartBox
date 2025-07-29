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
            Domain.Models.Word[] rawResult = await _wordRepository.GetWordsForLearningAsync();

            return rawResult.Adapt<Word[]>();
        }
    }
}
