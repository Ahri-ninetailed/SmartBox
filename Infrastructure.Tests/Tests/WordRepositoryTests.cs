using Infrastructure.Mappers;
using Infrastructure.Models.DataBase;
using Infrastructure.Services;
using Infrastructure.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests.Tests
{
    /// <summary>
    /// Запросы проверяются на настоящем PostgreSQL: InMemory не умеет ILIKE и местами сортирует иначе
    /// </summary>
    [Collection(PostgresCollection.Name)]
    public class WordRepositoryTests : IAsyncLifetime
    {
        private readonly PostgresFixture _postgres;
        private TestDatabase _database = null!;
        private IDbContextFactory<MyDbContext> _dbFactory = null!;
        private WordRepository _repository = null!;

        public WordRepositoryTests(PostgresFixture postgres)
        {
            _postgres = postgres;
        }

        public async Task InitializeAsync()
        {
            _database = await _postgres.CreateDatabaseAsync();
            _dbFactory = _database.DbFactory;

            PartOfSpeechMapper partOfSpeechMapper = new();
            WordMapper wordMapper = new(partOfSpeechMapper, partOfSpeechMapper, partOfSpeechMapper);

            _repository = new WordRepository(_dbFactory, wordMapper, wordMapper, partOfSpeechMapper, new DefinitionMapper());
        }

        public async Task DisposeAsync()
        {
            await _database.DisposeAsync();
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

        [Theory]
        [InlineData("christmas")]
        [InlineData("CHRISTMAS")]
        [InlineData("Day Before")]
        public async Task SearchByMeaningAsync_QueryInAnyCase_FindsDefinition(string meaning)
        {
            await SeedAsync(
                MakeWord("eve", PartOfSpeech.Other, "the day before Christmas"),
                MakeWord("dawn", PartOfSpeech.Other, "the first light of the day"));

            Domain.Models.Dictionary.Word[] words = await _repository.SearchByMeaningAsync(meaning, null);

            Assert.Equal(["eve"], words.Select(word => word.Values[0]));
        }

        [Fact]
        public async Task SearchByMeaningAsync_PercentInQuery_MatchedLiterally()
        {
            await SeedAsync(
                MakeWord("sale", PartOfSpeech.Other, "a 50% discount"),
                MakeWord("crowd", PartOfSpeech.Other, "500 people in one place"));

            Domain.Models.Dictionary.Word[] words = await _repository.SearchByMeaningAsync("50%", null);

            Assert.Equal(["sale"], words.Select(word => word.Values[0]));
        }

        [Fact]
        public async Task SearchByMeaningAsync_UnderscoreAndBackslashInQuery_MatchedLiterally()
        {
            await SeedAsync(
                MakeWord("snake", PartOfSpeech.Other, @"names written in snake_case or C:\path"),
                MakeWord("camel", PartOfSpeech.Other, "names written in snakeXcase"));

            Assert.Equal(["snake"], (await _repository.SearchByMeaningAsync("e_c", null)).Select(word => word.Values[0]));
            Assert.Equal(["snake"], (await _repository.SearchByMeaningAsync(@"C:\path", null)).Select(word => word.Values[0]));
        }

        [Fact]
        public async Task SearchByMeaningAsync_PartOfSpeechSet_ReturnsOnlyThatPartOfSpeech()
        {
            await SeedAsync(
                MakeWord("run", PartOfSpeech.Verb, "to move fast"),
                MakeWord("run", PartOfSpeech.Other, "a fast move on foot"));

            Domain.Models.Dictionary.Word[] words = await _repository.SearchByMeaningAsync("fast", Domain.Models.Dictionary.PartOfSpeech.Verb);

            Assert.Equal(Domain.Models.Dictionary.PartOfSpeech.Verb, Assert.Single(words).PartOfSpeech);
        }

        [Fact]
        public async Task SearchWordsAsync_WordsStartingWithEntry_ComeFirstAndShorterFirst()
        {
            await SeedAsync(
                MakeWord("underrun", PartOfSpeech.Other),
                MakeWord("running", PartOfSpeech.Other),
                MakeWord("run", PartOfSpeech.Other),
                MakeWord("walk", PartOfSpeech.Other));

            Domain.Models.Dictionary.Word[] words = await _repository.SearchWordsAsync(["run"]);

            Assert.Equal(["run", "running", "underrun"], words.Select(word => word.Values[0]));
        }

        [Fact]
        public async Task SearchWordsAsync_WordWithSeveralValues_FoundBySecondValue()
        {
            await SeedAsync(MakeWord("antechamber, anteroom", PartOfSpeech.Other));

            Domain.Models.Dictionary.Word[] words = await _repository.SearchWordsAsync(["anteroom"]);

            Assert.Equal(["antechamber", "anteroom"], Assert.Single(words).Values);
        }

        [Fact]
        public async Task IsWordExistedAsync_SameValuesInOtherOrder_ReturnsTrueOnlyForSamePartOfSpeech()
        {
            await SeedAsync(MakeWord("odds and ends, odds and sods", PartOfSpeech.Other));

            Assert.True(await _repository.IsWordExistedAsync(["odds and sods", "odds and ends"], Domain.Models.Dictionary.PartOfSpeech.Other));
            Assert.False(await _repository.IsWordExistedAsync(["odds and sods", "odds and ends"], Domain.Models.Dictionary.PartOfSpeech.Verb));
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

        private static Word MakeWord(string value, PartOfSpeech partOfSpeech, params string[] definitions) =>
            new()
            {
                Value = value,
                PartOfSpeech = partOfSpeech,
                Definitions = definitions.Select(definition => new Definition { Value = definition }).ToList()
            };

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
