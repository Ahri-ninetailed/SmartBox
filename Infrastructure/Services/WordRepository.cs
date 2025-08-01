using Domain.Interfaces;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Shared;

namespace Infrastructure.Services
{
    public class WordRepository : IWordRepository
    {
        private const string EmptyIdsCollectionError = "The ids collection cannot be empty";
        private const string EmptyWordsCollectionError = "The words collection cannot be empty";
        private const string EmptyEntryError = "The entry cannot be null or whitespace";
        private const string NullWordError = "The new word cannot be null";
        private const string EmptyWordValueError = "Word value cannot be null or whitespace";

        private readonly IDbContextFactory<MyDbContext> _dbFactory;

        private readonly IMapper<Word, Domain.Models.Word> _wordToDomMapper;
        private readonly IMapper<Domain.Models.Word, Word> _wordToInfsMapper;
        private readonly IMapper<Domain.Models.PartOfSpeech, PartOfSpeech?> _partOfSpeechToInfsMapper;

        public WordRepository(IDbContextFactory<MyDbContext> dbFactory,
                              IMapper<Word, Domain.Models.Word> wordToDomMapper,
                              IMapper<Domain.Models.Word, Word> wordToInfsMapper,
                              IMapper<Domain.Models.PartOfSpeech, PartOfSpeech?> partOfSpeechToInfsMapper)
        {
            _dbFactory = dbFactory;

            _wordToDomMapper = wordToDomMapper;
            _wordToInfsMapper = wordToInfsMapper;
            _partOfSpeechToInfsMapper = partOfSpeechToInfsMapper;
        }

        public async Task<Domain.Models.Word[]> GetWordsByIdsAsync(IEnumerable<int> ids)
        {
            if (ids.Any() == false)
            {
                throw new ArgumentException(EmptyIdsCollectionError);
            }

            using MyDbContext db = _dbFactory.CreateDbContext();

            Word[] words  = await db.EnglishWords.Where(el => ids.Contains(el.Id)).ToArrayAsync();

            return _wordToDomMapper.MapToArray(words);
        }

        public async Task SaveWordsAsync(Domain.Models.Word[] words)
        {
            if (words.Any() == false)
            {
                throw new ArgumentException(EmptyWordsCollectionError);
            }

            using MyDbContext db = _dbFactory.CreateDbContext();

            Word[] rows = _wordToInfsMapper.MapToArray(words);

            foreach (Word row in rows)
            {
                db.Entry(row).State = EntityState.Modified;
            }

            await db.SaveChangesAsync();
        }

        public async Task<Domain.Models.Word[]> GetWordsForLearningAsync(int count = 40)
        {
            using MyDbContext db = _dbFactory.CreateDbContext();

            IQueryable<Word> unlearnedWords = db.EnglishWords.Where(word => !word.Learned);

            List<Word> firstPack = await unlearnedWords
                                .Where(word => word.ShouldStudy)
                                .OrderByDescending(word => word.Popularity)
                                .Take(count)
                                .ToListAsync();

            if (firstPack.Count < count)
            {
                List<Word> secondPack = await unlearnedWords
                                .Where(word => !firstPack.Contains(word) && word.Popularity.HasValue)
                                .OrderByDescending(word => word.Popularity)
                                .Take(count - firstPack.Count)
                                .ToListAsync();

                firstPack.AddRange(secondPack);
            }

            Domain.Models.Word[] result = _wordToDomMapper.MapToArray(firstPack);

            return result;
        }

        public async Task<Domain.Models.Word[]> SearchWordsAsync(string entry)
        {
            if (string.IsNullOrWhiteSpace(entry))
            {
                throw new ArgumentException(EmptyEntryError);
            }

            using MyDbContext _db = _dbFactory.CreateDbContext();

            Word[] words = await _db.EnglishWords
                .Where(el => el.Value.Contains(entry))
                .Select(el => new // TODO: сортировку можно в бизнес логику вынести
                {
                    Word = el,
                    StartsWith = el.Value.StartsWith(entry),
                    IndexOf = el.Value.IndexOf(entry),
                    Length = el.Value.Length
                })
                .OrderByDescending(x => x.StartsWith)// Сначала те, что начинаются на value
                .ThenBy(x => x.IndexOf)              // Затем те, где value ближе к началу
                .ThenBy(x => x.Length)               // Потом более короткие
                .ThenBy(x => x.Word.Value)
                .Select(x => x.Word)
                .ToArrayAsync();

            Domain.Models.Word[] result = _wordToDomMapper.MapToArray(words);

            return result;
        }

        public async Task<Domain.Models.Word> AddWordAsync(Domain.Models.Word newWord)
        {
            if (newWord == null)
            {
                throw new ArgumentNullException(nameof(newWord), NullWordError);
            }

            Word word = _wordToInfsMapper.Map(newWord);

            using MyDbContext db = _dbFactory.CreateDbContext();

            db.EnglishWords.Add(word);

            await db.SaveChangesAsync();

            Domain.Models.Word result = _wordToDomMapper.Map(word);

            return result;
        }

        public async Task<bool> IsWordExistedAsync(string wordValue, Domain.Models.PartOfSpeech partOfSpeech)
        {
            if (string.IsNullOrWhiteSpace(wordValue))
            {
                throw new ArgumentException(EmptyWordValueError);
            }

            using MyDbContext db = _dbFactory.CreateDbContext();

            PartOfSpeech? dbPartOfSpeech = _partOfSpeechToInfsMapper.Map(partOfSpeech);

            bool result = await db.EnglishWords.AnyAsync(el => el.Value == wordValue && el.PartOfSpeech == dbPartOfSpeech);

            return result;
        }
    }
}
