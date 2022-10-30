using System;
using System.Linq;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace AmiaReforged.Core.Test;

public class TestAmiaContext : IDisposable
{
    private readonly ITestOutputHelper _testOutputHelper;
    private AmiaContext Context { get; set; }

    private readonly Ban _testBan = new()
    {
        CdKey = "1234567890",
    };

    public TestAmiaContext(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        Context = new AmiaContext("Host=localhost;Database=amia;Username=amia;Password=thelittlestpogchamp");

        PerformTestSetup();
    }

    private void PerformTestSetup()
    {
        try
        {
            Context.Bans.Add(_testBan);
            Context.SaveChanges();
        }
        catch (Exception e)
        {
            _testOutputHelper.WriteLine(e.Message);
        }
    }


    [Fact]
    public void ShouldCreateBan()
    {
        AmiaContext ctx = new AmiaContext("Host=localhost;Database=amia;Username=amia;Password=thelittlestpogchamp");
        
        // try adding with resources
        try
        {
            ctx.Bans.Add(_testBan);
            ctx.SaveChanges();
        }
        catch (Exception e)
        {
            _testOutputHelper.WriteLine(e.ToString());
        }

        Ban ban = ctx.Bans.First();

        ban.CdKey.Should().Be("1234567890");
    }

    [Fact]
    public void ShouldDeleteBan()
    {
    }

    public void Dispose()
    {
        try
        {
            Context.Bans.Remove(_testBan);

            Context.SaveChanges();
        }
        catch (Exception)
        {
            _testOutputHelper.WriteLine("Failed to remove test ban");
        }
    }
}