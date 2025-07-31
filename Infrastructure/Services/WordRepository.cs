using Domain.Interfaces;
using Infrastructure.Models;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services
{
    public class WordRepository : IWordRepository
    {
        private const string EmptyIdsCollectionError = "The ids collection cannot be empty";
        private const string EmptyWordsCollectionError = "The words collection cannot be empty";
        private const string EmptyEntryError = "The entry cannot be null or whitespace";

        private readonly IDbContextFactory<MyDbContext> _dbFactory;

        public WordRepository(IDbContextFactory<MyDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<Domain.Models.Word[]> GetWordsByIds(IEnumerable<int> ids)
        {
            if (ids.Any() == false)
            {
                throw new ArgumentException(EmptyIdsCollectionError);
            }

            using MyDbContext db = _dbFactory.CreateDbContext();

            Word[] words  = await db.EnglishWords.Where(el => ids.Contains(el.Id)).ToArrayAsync();

            return words.Adapt<Domain.Models.Word[]>();
        }

        public async Task SaveWords(Domain.Models.Word[] words)
        {
            if (words.Any() == false)
            {
                throw new ArgumentException(EmptyWordsCollectionError);
            }

            using MyDbContext db = _dbFactory.CreateDbContext();

            Word[] rows = words.Adapt<Word[]>();

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

            Domain.Models.Word[] result = firstPack.Adapt<Domain.Models.Word[]>();

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

            Domain.Models.Word[] result = words.Adapt<Domain.Models.Word[]>();

            return result;
        }
    }
}
