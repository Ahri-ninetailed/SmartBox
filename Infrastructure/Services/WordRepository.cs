using Domain.Interfaces;
using Infrastructure.Models.DataBase;
using Microsoft.EntityFrameworkCore;
using Shared;

namespace Infrastructure.Services
{
    public class WordRepository : IWordRepository
    {
        private const string Separator = ", ";

        private const string EmptyIdsCollectionError = "The 'ids' collection must not be null, empty, or contain zero values.";
        private const string EmptyWordsCollectionError = "The 'words' array must not be null, empty, or contain null elements.";
        private const string EmptyEntryError = "The entry cannot be null or whitespace";
        private const string NullWordError = "The new word cannot be null";
        private const string WordValuesNullOrEmptyError = "Word values array cannot be null, empty, or contain null/whitespace elements.";
        private const string WordIdMustBeGreaterThanZeroError = "Word ID must be greater than zero.";
        private const string DefinitionsArrayInvalidError = "Definitions array must not be null, empty, or contain null elements.";
        private const string WordNotFoundError = "Word with ID {0} was not found in the database.";

        private readonly IDbContextFactory<MyDbContext> _dbFactory;

        private readonly IMapper<Infrastructure.Models.DataBase.Word, Domain.Models.Dictionary.Word> _wordToDomMapper;
        private readonly IMapper<Domain.Models.Dictionary.Word, Infrastructure.Models.DataBase.Word> _wordToInfsMapper;
        private readonly IMapper<Domain.Models.Dictionary.PartOfSpeech, Infrastructure.Models.DataBase.PartOfSpeech?> _partOfSpeechToInfsMapper;
        private readonly IMapper<Domain.Models.Dictionary.Definition, Infrastructure.Models.DataBase.Definition> _definitionToInfsMapper;

        public WordRepository(IDbContextFactory<MyDbContext> dbFactory,
                              IMapper<Word, Domain.Models.Dictionary.Word> wordToDomMapper,
                              IMapper<Domain.Models.Dictionary.Word, Word> wordToInfsMapper,
                              IMapper<Domain.Models.Dictionary.PartOfSpeech, PartOfSpeech?> partOfSpeechToInfsMapper,
                              IMapper<Domain.Models.Dictionary.Definition, Infrastructure.Models.DataBase.Definition> definitionToInfsMapper)
        {
            _dbFactory = dbFactory;

            _wordToDomMapper = wordToDomMapper;
            _wordToInfsMapper = wordToInfsMapper;
            _partOfSpeechToInfsMapper = partOfSpeechToInfsMapper;
            _definitionToInfsMapper = definitionToInfsMapper;
        }

        public async Task<Domain.Models.Dictionary.Word[]> GetWordsByIdsAsync(IEnumerable<int> ids, bool includeRelatedData = false)
        {
            if (ids == null || ids.Any() == false || ids.Any(el => el == 0))
            {
                throw new ArgumentException(EmptyIdsCollectionError);
            }

            using MyDbContext db = _dbFactory.CreateDbContext();

            IQueryable<Word> query = db.EnglishWords.Where(el => ids.Contains(el.Id));

            if (includeRelatedData)
            {
                query = query
                    .Include(w => w.Definitions)
                    .ThenInclude(d => d.Examples);
            }

            Word[] words = await query.ToArrayAsync();

            return _wordToDomMapper.MapToArray(words);
        }

        public async Task SaveWordsAsync(Domain.Models.Dictionary.Word[] words)
        {
            if (words == null || words.Length == 0 || words.Any(el => el == null))
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

        public async Task<Domain.Models.Dictionary.Word[]> GetWordsForLearningAsync(int count = 40)
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

            Domain.Models.Dictionary.Word[] result = _wordToDomMapper.MapToArray(firstPack);

            return result;
        }

        public async Task<Domain.Models.Dictionary.Word[]> SearchWordsAsync(string[] entries)
        {
            if (entries == null || entries.Length == 0 || entries.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException("Entries array cannot be null, empty, or contain null/whitespace elements.");
            }

            using MyDbContext _db = _dbFactory.CreateDbContext();

            List<string> permutations = GetAllPermutations(entries).ToList();

            Word[] words = await _db.EnglishWords
                .Where(el => permutations.Any(perm => el.Value.Contains(perm)))
                .Select(el => new
                {
                    Word = el,
                    // Находим наиболее подходящую пермутацию для сортировки
                    BestMatch = permutations
                        .Where(perm => el.Value.Contains(perm))
                        .OrderByDescending(perm => perm.Length)
                        .ThenBy(perm => el.Value.IndexOf(perm))
                        .First(),
                    StartsWith = permutations.Any(perm => el.Value.StartsWith(perm)),
                    MinIndexOf = permutations
                        .Where(perm => el.Value.Contains(perm))
                        .Min(perm => el.Value.IndexOf(perm)),
                    Length = el.Value.Length
                })
                .OrderByDescending(x => x.StartsWith)    // Сначала те, что начинаются с любой пермутации
                .ThenBy(x => x.MinIndexOf)               // Затем те, где пермутация ближе к началу
                .ThenBy(x => x.Length)                   // Потом более короткие
                .ThenBy(x => x.Word.Value)
                .Select(x => x.Word)
                .ToArrayAsync();

            Domain.Models.Dictionary.Word[] result = _wordToDomMapper.MapToArray(words);

            return result;
        }

        public async Task<Domain.Models.Dictionary.Word> AddWordAsync(Domain.Models.Dictionary.Word newWord)
        {
            if (newWord == null)
            {
                throw new ArgumentNullException(nameof(newWord), NullWordError);
            }

            Word word = _wordToInfsMapper.Map(newWord);

            using MyDbContext db = _dbFactory.CreateDbContext();

            db.EnglishWords.Add(word);

            await db.SaveChangesAsync();

            Domain.Models.Dictionary.Word result = _wordToDomMapper.Map(word);

            return result;
        }

        public async Task<bool> IsWordExistedAsync(string[] wordValues, Domain.Models.Dictionary.PartOfSpeech partOfSpeech)
        {
            if (wordValues == null || wordValues.Length == 0 || wordValues.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException(WordValuesNullOrEmptyError);
            }

            using MyDbContext db = _dbFactory.CreateDbContext();

            PartOfSpeech? dbPartOfSpeech = _partOfSpeechToInfsMapper.Map(partOfSpeech);

            List<string> permutations = GetAllPermutations(wordValues).ToList();

            bool exists = await db.EnglishWords
                .AnyAsync(el => permutations.Contains(el.Value) && el.PartOfSpeech == dbPartOfSpeech);

            return exists;
        }

        private IEnumerable<string> GetAllPermutations(string[] wordValues)
        {
            return GetPermutations(wordValues, wordValues.Length)
                .Select(arr => string.Join(Separator, arr));
        }

        private IEnumerable<IEnumerable<T>> GetPermutations<T>(IEnumerable<T> list, int length)
        {
            if (length == 1) return list.Select(t => new T[] { t });

            return GetPermutations(list, length - 1)
                .SelectMany(t => list.Where(e => !t.Contains(e)),
                    (t1, t2) => t1.Concat(new T[] { t2 }));
        }

        public async Task AddDefinitionsAsync(int wordId, Domain.Models.Dictionary.Definition[] definitions)
        {
            if (wordId <= 0)
            {
                throw new ArgumentException(WordIdMustBeGreaterThanZeroError, nameof(wordId));
            }

            if (definitions == null || definitions.Length == 0 || definitions.Any(el => el == null))
            {
                throw new ArgumentException(DefinitionsArrayInvalidError, nameof(definitions));
            }

            Definition[] newDefinitions = _definitionToInfsMapper.MapToArray(definitions);

            using MyDbContext db = _dbFactory.CreateDbContext();

            Word? word = db.EnglishWords.Find(wordId);

            if (word == null)
            {
                throw new InvalidOperationException(string.Format(WordNotFoundError, wordId));
            }

            foreach (Definition definition in newDefinitions)
            {
                word.Definitions.Add(definition);
            }

            await db.SaveChangesAsync();
        }
    }
}
