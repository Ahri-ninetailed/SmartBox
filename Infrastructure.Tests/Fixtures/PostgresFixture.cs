using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Infrastructure.Tests.Fixtures
{
    /// <summary>
    /// Одноразовый PostgreSQL в Docker на весь прогон тестов. Та же мажорная версия, что в Supabase
    /// </summary>
    public class PostgresFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:15-alpine").Build();

        public Task InitializeAsync()
        {
            return _container.StartAsync();
        }

        public Task DisposeAsync()
        {
            return _container.DisposeAsync().AsTask();
        }

        /// <summary>
        /// Пустая БД со схемой из MyDbContext. У каждого теста своя, чтобы тесты не видели данные друг друга
        /// </summary>
        public async Task<TestDatabase> CreateDatabaseAsync()
        {
            string connectionString = new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
            {
                Database = $"test_{Guid.NewGuid():N}"
            }.ConnectionString;

            ServiceProvider provider = new ServiceCollection()
                .AddDbContextFactory<MyDbContext>(options => DependencyInjection.ConfigureDbContext(options, connectionString))
                .BuildServiceProvider();

            IDbContextFactory<MyDbContext> dbFactory = provider.GetRequiredService<IDbContextFactory<MyDbContext>>();

            using (MyDbContext db = dbFactory.CreateDbContext())
            {
                await db.Database.EnsureCreatedAsync();
            }

            return new TestDatabase(provider, dbFactory);
        }
    }

    public sealed class TestDatabase : IAsyncDisposable
    {
        private readonly ServiceProvider _provider;

        public IDbContextFactory<MyDbContext> DbFactory { get; }

        public TestDatabase(ServiceProvider provider, IDbContextFactory<MyDbContext> dbFactory)
        {
            _provider = provider;
            DbFactory = dbFactory;
        }

        public async ValueTask DisposeAsync()
        {
            using (MyDbContext db = DbFactory.CreateDbContext())
            {
                await db.Database.EnsureDeletedAsync();
            }

            await _provider.DisposeAsync();
        }
    }

    [CollectionDefinition(Name)]
    public class PostgresCollection : ICollectionFixture<PostgresFixture>
    {
        public const string Name = "Postgres";
    }
}
