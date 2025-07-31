using Domain.Interfaces;
using Infrastructure.Models;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services
{
    public class WordRepository : IWordRepository
    {
        private readonly IDbContextFactory<MyDbContext> _dbFactory;

        public WordRepository(IDbContextFactory<MyDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<Domain.Models.Word[]> GetWordsByIds(IEnumerable<int> ids)
        {
            using MyDbContext db = _dbFactory.CreateDbContext();

            Word[] words  = await db.EnglishWords.Where(el => ids.Contains(el.Id)).ToArrayAsync();

            return words.Adapt<Domain.Models.Word[]>();
        }

        public async Task SaveWords(Domain.Models.Word[] words)
        {
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
            try
            {
                using MyDbContext db = _dbFactory.CreateDbContext();

                IQueryable<Models.Word> unlearnedWords = db.EnglishWords.Where(word => !word.Learned);

                List<Models.Word> firstPack = await unlearnedWords
                                    .Where(word => word.ShouldStudy)
                                    .OrderByDescending(word => word.Popularity)
                                    .Take(count)
                                    .ToListAsync();

                if (firstPack.Count < count)
                {
                    List<Models.Word> secondPack = await unlearnedWords
                                    .Where(word => !firstPack.Contains(word) && word.Popularity.HasValue)
                                    .OrderByDescending(word => word.Popularity)
                                    .Take(count - firstPack.Count)
                                    .ToListAsync();

                    firstPack.AddRange(secondPack);
                }

                Domain.Models.Word[] result = firstPack.Adapt<Domain.Models.Word[]>();

                return result;
            }
            catch (Exception ex)
            {
                // TODO: начать логировать
                // TODO: добавить полли
                //_logger.LogError(ex, "Ошибка при получении слов для изучения из бд");
                throw;
            }
        }

        public async Task<Domain.Models.Word[]> SearchWordsAsync(string entry)
        {
            try
            {
                using MyDbContext _db = _dbFactory.CreateDbContext();

                Models.Word[] words = await _db.EnglishWords
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
            catch (Exception ex)
            {
                // TODO: начать логировать
                // TODO: добавить полли
                //_logger.LogError(ex, "Ошибка при поиске слов в бд");
                throw;
            }
        }
    }
}
