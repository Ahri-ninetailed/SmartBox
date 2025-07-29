using Domain.Interfaces;
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
    }
}
