using System.Text.Json;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.DefinitionLoaders;

public class ItemBlueprintLocalVariablesTests
{
    [Test]
    public void LoadsLocalVariables_WhenValid()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        string dir = Path.Combine(root, "Items", "Blueprints");
        Directory.CreateDirectory(dir);
        string file = Path.Combine(dir, "locals_valid.json");
        File.WriteAllText(file, "{\n  \"ResRef\": \"bvalid01\",\n  \"ItemTag\": \"bp_valid_locals\",\n  \"Name\": \"Valid Locals Item\",\n  \"Description\": \"Desc\",\n  \"Materials\": [\"Gem\"],\n  \"JobSystemType\": \"ResourceGem\",\n  \"BaseItemType\": 74,\n  \"Appearance\": { \"ModelType\": 0, \"SimpleModelNumber\": 2 },\n  \"LocalVariables\": [ { \"Name\": \"tier\", \"Type\": \"Int\", \"Value\": 2 }, { \"Name\": \"category\", \"Type\": \"String\", \"Value\": \"Test\" } ]\n}\n");
        Environment.SetEnvironmentVariable("RESOURCE_PATH", root);
        InMemoryItemDefinitionRepository repo = new();
        ItemBlueprintLoadingService loader = new(repo);
        loader.Load();
        Assert.That(loader.Failures(), Is.Empty);
        ItemBlueprint? def = repo.GetByTag("bp_valid_locals");
        Assert.That(def, Is.Not.Null);
        Assert.That(def!.LocalVariables, Is.Not.Null);
        Assert.That(def!.LocalVariables!.Count, Is.EqualTo(2));
        Assert.That(def!.LocalVariables!.Single(v => v.Name == "tier").Value.GetInt32(), Is.EqualTo(2));
        Assert.That(def!.LocalVariables!.Single(v => v.Name == "category").Value.GetString(), Is.EqualTo("Test"));
    }

    [Test]
    public void Fails_WhenIntLocalVariableIsString()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        string dir = Path.Combine(root, "Items", "Blueprints");
        Directory.CreateDirectory(dir);
        string file = Path.Combine(dir, "locals_invalid.json");
        File.WriteAllText(file, "{\n  \"ResRef\": \"binvalid01\",\n  \"ItemTag\": \"bp_invalid_locals\",\n  \"Name\": \"Invalid Locals Item\",\n  \"Description\": \"Desc\",\n  \"Materials\": [\"Gem\"],\n  \"JobSystemType\": \"ResourceGem\",\n  \"BaseItemType\": 74,\n  \"Appearance\": { \"ModelType\": 0, \"SimpleModelNumber\": 2 },\n  \"LocalVariables\": [ { \"Name\": \"tier\", \"Type\": \"Int\", \"Value\": \"two\" } ]\n}\n");
        Environment.SetEnvironmentVariable("RESOURCE_PATH", root);
        InMemoryItemDefinitionRepository repo = new();
        ItemBlueprintLoadingService loader = new(repo);
        loader.Load();
        Assert.That(loader.Failures(), Has.Count.EqualTo(1));
        Assert.That(repo.GetByTag("bp_invalid_locals"), Is.Null);
    }
}
