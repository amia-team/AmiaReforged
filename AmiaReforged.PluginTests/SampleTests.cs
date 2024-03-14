using Anvil.API;
using NUnit.Framework;

namespace AmiaReforged.PluginTests;

/// <summary>
/// This is an example of how to set up tests for your work on the server.
/// </summary>
[TestFixture(Category = "Examples")]
public class SampleTests
{
    [Test(Description = "A created temporary resource is available as a game resource.")]
    public void HelloThere()
    {
        Location loc = NwModule.Instance.StartingLocation;

        NwCreature creature = NwCreature.Create("nw_bandit001", loc);

        string newName = "Hello there";

        creature.Name = newName;

        Assert.That(creature.Name, Is.EqualTo(newName));

        creature.Destroy();

        Assert.That(creature.IsValid, Is.False);
    }
}