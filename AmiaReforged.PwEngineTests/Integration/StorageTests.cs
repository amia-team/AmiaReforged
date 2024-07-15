using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Systems.JobSystem.Entities;
using AmiaReforged.PwEngine.Systems.JobSystem.Storage;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace AmiaReforged.PwEngineTest.Integration;

[TestFixture]
public class StorageTests
{
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder().WithImage("postgres:latest")
            .WithDatabase(PostgresConfig.Database)
            .WithPassword(PostgresConfig.Password)
            .WithUsername(PostgresConfig.Username)
            .WithPortBinding(PostgresConfig.Port)
            .WithExposedPort(PostgresConfig.Port)
            .Build();

    private readonly StorageService _sut = new(new PwContextFactory());

    [OneTimeSetUp]
    public void SetUp()
    {
        _container.StartAsync().Wait();

        PwEngineContext context = new();
        context.Database.Migrate();

        // Seed data
        SeedData.SeedDatabase();
    }

    [Test]
    public async Task ShouldDenyUnauthorizedUser()
    {
        await using PwEngineContext context = new();

        ItemStorage? itemStorage = await _sut.GetStorage(2, 1);
        Assert.That(itemStorage, Is.Null);
    }

    [Test]
    public async Task ShouldAllowAuthorizedUser()
    {
        await using PwEngineContext context = new();

        ItemStorage? itemStorage = await _sut.GetStorage(1, 1);
        Assert.That(itemStorage, Is.Not.Null);
    }

    [Test]
    public async Task ShouldIncludeItems()
    {
        await using PwEngineContext context = new();
        
        ItemStorage? itemStorage = await _sut.GetStorage(1, 1);
        
        Assert.That(itemStorage, Is.Not.Null);
        
        Assert.Multiple(() =>
        {
            Assert.That(itemStorage.Items, Is.Not.Null);
            Assert.That(itemStorage.Items, Has.Count.EqualTo(1));
        });
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _container.StopAsync().Wait();
    }
}