using Infrastructure.Mappers;
using Infrastructure.Models.DataBase;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tests.Tests
{
    public class WordRepositoryTests : IDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly IDbContextFactory<MyDbContext> _dbFactory;
        private readonly WordRepository _repository;

        public WordRepositoryTests()
        {
            string databaseName = Guid.NewGuid().ToString();

            _provider = new ServiceCollection()
                .AddDbContextFactory<MyDbContext>(options => options.UseInMemoryDatabase(databaseName))
                .BuildServiceProvider();

            _dbFactory = _provider.GetRequiredService<IDbContextFactory<MyDbContext>>();

            PartOfSpeechMapper partOfSpeechMapper = new();
            WordMapper wordMapper = new(partOfSpeechMapper, partOfSpeechMapper, partOfSpeechMapper);

            _repository = new WordRepository(_dbFactory, wordMapper, wordMapper, partOfSpeechMapper, new DefinitionMapper());
        }

        public void Dispose()
        {
            _provider.Dispose();
        }

        [Fact]
        public async Task GetWordsForLearningAsync_ShouldStudyWithoutPopularity_ComeBeforeShouldStudyWithPopularity()
        {
            await SeedAsync(
                MakeWord("study-high", 0.9m, shouldStudy: true),
                MakeWord("study-null", null, shouldStudy: true),
                MakeWord("study-low", 0.1m, shouldStudy: true));

            string[] values = await GetValuesForLearningAsync();

            Assert.Equal(["study-null", "study-high", "study-low"], values);
        }

        [Fact]
        public async Task GetWordsForLearningAsync_NotMarkedWithoutPopularity_ComeBeforeNotMarkedWithPopularity()
        {
            await SeedAsync(
                MakeWord("plain-high", 0.9m),
                MakeWord("plain-null", null),
                MakeWord("plain-low", 0.1m));

            string[] values = await GetValuesForLearningAsync();

            Assert.Equal(["plain-null", "plain-high", "plain-low"], values);
        }

        [Fact]
        public async Task GetWordsForLearningAsync_ShouldStudyWords_ComeBeforeNotMarkedWords()
        {
            await SeedAsync(
                MakeWord("plain-null", null),
                MakeWord("plain-high", 0.99m),
                MakeWord("study-low", 0.01m, shouldStudy: true),
                MakeWord("study-null", null, shouldStudy: true));

            string[] values = await GetValuesForLearningAsync();

            Assert.Equal(["study-null", "study-low", "plain-null", "plain-high"], values);
        }

        [Fact]
        public async Task GetWordsForLearningAsync_LearnedWords_Excluded()
        {
            await SeedAsync(
                MakeWord("learned-null", null, learned: true),
                MakeWord("learned-high", 0.9m, learned: true),
                MakeWord("plain-low", 0.1m));

            string[] values = await GetValuesForLearningAsync();

            Assert.Equal(["plain-low"], values);
        }

        [Fact]
        public async Task GetWordsForLearningAsync_ShouldStudyExceedCount_ShouldStudyWithoutPopularityKept()
        {
            await SeedAsync(
                MakeWord("study-high", 0.9m, shouldStudy: true),
                MakeWord("study-low", 0.1m, shouldStudy: true),
                MakeWord("study-null", null, shouldStudy: true),
                MakeWord("plain-null", null));

            string[] values = await GetValuesForLearningAsync(count: 2);

            Assert.Equal(["study-null", "study-high"], values);
        }

        [Fact]
        public async Task GetWordsForLearningAsync_RemainingSlots_FilledByNotMarkedWithoutPopularityFirst()
        {
            await SeedAsync(
                MakeWord("study-high", 0.9m, shouldStudy: true),
                MakeWord("plain-high", 0.99m),
                MakeWord("plain-null", null));

            string[] values = await GetValuesForLearningAsync(count: 2);

            Assert.Equal(["study-high", "plain-null"], values);
        }

        // helpers

        private async Task SeedAsync(params Word[] words)
        {
            using MyDbContext db = _dbFactory.CreateDbContext();

            db.EnglishWords.AddRange(words);

            await db.SaveChangesAsync();
        }

        private async Task<string[]> GetValuesForLearningAsync(int count = 40)
        {
            Domain.Models.Dictionary.Word[] words = await _repository.GetWordsForLearningAsync(count);

            return words.Select(word => word.Values[0]).ToArray();
        }

        private static Word MakeWord(string value, decimal? popularity, bool shouldStudy = false, bool learned = false) =>
            new()
            {
                Value = value,
                Popularity = popularity,
                PartOfSpeech = PartOfSpeech.Other,
                ShouldStudy = shouldStudy,
                Learned = learned
            };
    }
}
