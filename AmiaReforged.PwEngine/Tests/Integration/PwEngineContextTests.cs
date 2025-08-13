using System.ComponentModel;
using System.Data.Common;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Systems.WorldEngine.Models;
using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql;
using NUnit.Framework;
using Testcontainers.PostgreSql;
using IContainer = DotNet.Testcontainers.Containers.IContainer;

namespace AmiaReforged.PwEngine.Tests.Integration;

[TestFixture]
[NUnit.Framework.Category("Integration")]
public class PwEngineContextTests
{
    private IContainer _pgContainer = null!;
    private string _connStr = null!;
    private NpgsqlConnection _conn = null!;
    private DbTransaction _tx = null!;
    private DbContextOptions<PwEngineContext> _dbOptions = null!;


    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _pgContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase(EngineDbConfig.Database)
            .WithUsername(EngineDbConfig.Username)
            .WithPassword(EngineDbConfig.Password)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .Build();

        await _pgContainer.StartAsync();

        NpgsqlConnectionStringBuilder connectionBuilder = new()
        {
            Host = _pgContainer.Hostname,
            Password = EngineDbConfig.Password,
            Username = EngineDbConfig.Username,
            Database = EngineDbConfig.Database,
            Port = _pgContainer.GetMappedPublicPort(5432)
        };

        _connStr = connectionBuilder.ConnectionString;

        await using PwEngineContext migrateCtx = new(connectionBuilder.ConnectionString);
        await migrateCtx.Database.MigrateAsync();
    }

    [SetUp]
    public async Task SetUp()
    {
        // Per-test connection + transaction for automatic rollback
        _conn = new NpgsqlConnection(_connStr);
        await _conn.OpenAsync();
        _tx = await _conn.BeginTransactionAsync();

        _dbOptions = new DbContextOptionsBuilder<PwEngineContext>()
            .UseNpgsql(_conn) // important: reuse the same open connection
            .Options;
    }


    [TearDown]
    public async Task TearDown()
    {
        await _tx.RollbackAsync();
        await _tx.DisposeAsync();

        await _conn.CloseAsync();
        await _conn.DisposeAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _pgContainer.DisposeAsync();
    }


    [Test]
    public async Task Character_Owner_has_converter_comparer_and_column_name()
    {
        // Use a fresh context for metadata checks (no writes performed here).
        await using PwEngineContext ctx = new PwEngineContext(_connStr);

        IEntityType? entity = ctx.Model.FindEntityType(typeof(Character));
        Assert.That(entity, Is.Not.Null, "Character entity not found in the EF model.");

        IProperty? ownerProp = entity!.FindProperty(nameof(Character.Owner));
        Assert.That(ownerProp, Is.Not.Null, "Owner property not found on Character.");

        // Converter and comparer are configured at model level
        ValueConverter? converter = ownerProp!.GetValueConverter();
        ValueComparer comparer = ownerProp.GetValueComparer();

        Assert.That(converter, Is.Not.Null, "Owner should have a value converter configured.");
        Assert.That(comparer, Is.Not.Null, "Owner should have a value comparer configured.");

        // Column name mapping
        string? tableName = entity.GetTableName();
        string? schema = entity.GetSchema();
        Assert.That(tableName, Is.Not.Null.And.Not.Empty, "Character entity should be mapped to a table.");

        StoreObjectIdentifier storeObject = StoreObjectIdentifier.Table(tableName!, schema);
        string? columnName = ownerProp.GetColumnName(storeObject);

        Assert.That(columnName, Is.EqualTo("owner"), "Owner column name should be 'owner'.");

        // Optional: verify an index exists on Owner with the expected database name
        bool hasOwnerIndex = entity.GetIndexes()
            .Any(ix => ix.Properties.Contains(ownerProp) && string.Equals(ix.GetDatabaseName(), "ix_characters_owner", StringComparison.Ordinal));

        Assert.That(hasOwnerIndex, Is.True, "Expected an index named 'ix_characters_owner' on Owner.");

        // Optional: cross-check that the physical column exists in the database
        await using NpgsqlCommand cmd = new NpgsqlCommand(@"
            select 1
            from information_schema.columns
            where table_schema = coalesce(@schema, current_schema())
              and table_name = @table
              and column_name = @column
            limit 1;", _conn, (NpgsqlTransaction)_tx);

        cmd.Parameters.AddWithValue("schema", (object?)schema ?? DBNull.Value);
        cmd.Parameters.AddWithValue("table", tableName!);
        cmd.Parameters.AddWithValue("column", columnName!);

        object? exists = await cmd.ExecuteScalarAsync();
        Assert.That(exists, Is.Not.Null, $"Column '{columnName}' was not found on table '{schema ?? "public"}.{tableName}'.");

    }
}
