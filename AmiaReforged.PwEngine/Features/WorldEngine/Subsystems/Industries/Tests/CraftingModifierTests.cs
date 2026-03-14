using AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Tests;

[TestFixture]
public class CraftingModifierTests
{
    // ==================== CraftingModifier.Matches ====================

    [Test]
    public void CraftingModifier_RecipeScope_MatchesExactRecipeId()
    {
        CraftingModifier modifier = new("iron_sword", CraftingModifierScope.Recipe,
            CraftingStep.Quality, 1f, EffectOperation.Additive);

        Assert.That(modifier.Matches("iron_sword", "blacksmithing"), Is.True);
        Assert.That(modifier.Matches("steel_sword", "blacksmithing"), Is.False);
    }

    [Test]
    public void CraftingModifier_IndustryScope_MatchesIndustryTag()
    {
        CraftingModifier modifier = new("blacksmithing", CraftingModifierScope.Industry,
            CraftingStep.Quantity, 0.5f, EffectOperation.Additive);

        Assert.That(modifier.Matches("iron_sword", "blacksmithing"), Is.True);
        Assert.That(modifier.Matches("oak_plank", "woodworking"), Is.False);
    }

    [Test]
    public void CraftingModifier_GlobalScope_MatchesEverything()
    {
        CraftingModifier modifier = new("*", CraftingModifierScope.Global,
            CraftingStep.CraftingTime, 1f, EffectOperation.Subtractive);

        Assert.That(modifier.Matches("iron_sword", "blacksmithing"), Is.True);
        Assert.That(modifier.Matches("oak_plank", "woodworking"), Is.True);
    }

    [Test]
    public void CraftingModifier_RecipeScope_IsCaseInsensitive()
    {
        CraftingModifier modifier = new("Iron_Sword", CraftingModifierScope.Recipe,
            CraftingStep.Quality, 1f, EffectOperation.Additive);

        Assert.That(modifier.Matches("iron_sword", "blacksmithing"), Is.True);
    }

    // ==================== AggregatedCraftingModifiers.None ====================

    [Test]
    public void None_HasDefaultValues()
    {
        AggregatedCraftingModifiers none = AggregatedCraftingModifiers.None;

        Assert.That(none.QualityBonus, Is.EqualTo(0));
        Assert.That(none.QuantityMultiplier, Is.EqualTo(1.0f));
        Assert.That(none.SuccessChanceBonus, Is.EqualTo(0f));
        Assert.That(none.TimeReductionRounds, Is.EqualTo(0));
        Assert.That(none.IsEmpty, Is.True);
    }

    // ==================== AggregatedCraftingModifiers.Aggregate — Quality ====================

    [Test]
    public void Aggregate_SingleAdditiveQuality()
    {
        List<CraftingModifier> modifiers =
        [
            new CraftingModifier("*", CraftingModifierScope.Global, CraftingStep.Quality, 2f,
                EffectOperation.Additive)
        ];

        AggregatedCraftingModifiers result = AggregatedCraftingModifiers.Aggregate(modifiers);

        Assert.That(result.QualityBonus, Is.EqualTo(2));
        Assert.That(result.IsEmpty, Is.False);
    }

    [Test]
    public void Aggregate_MultipleAdditiveQuality_AreSummed()
    {
        List<CraftingModifier> modifiers =
        [
            new CraftingModifier("*", CraftingModifierScope.Global, CraftingStep.Quality, 1f,
                EffectOperation.Additive),
            new CraftingModifier("blacksmithing", CraftingModifierScope.Industry, CraftingStep.Quality, 1f,
                EffectOperation.Additive)
        ];

        AggregatedCraftingModifiers result = AggregatedCraftingModifiers.Aggregate(modifiers);

        Assert.That(result.QualityBonus, Is.EqualTo(2));
    }

    [Test]
    public void Aggregate_AdditiveQualityThenSubtractive()
    {
        List<CraftingModifier> modifiers =
        [
            new CraftingModifier("*", CraftingModifierScope.Global, CraftingStep.Quality, 3f,
                EffectOperation.Additive),
            new CraftingModifier("*", CraftingModifierScope.Global, CraftingStep.Quality, 1f,
                EffectOperation.Subtractive)
        ];

        AggregatedCraftingModifiers result = AggregatedCraftingModifiers.Aggregate(modifiers);

        Assert.That(result.QualityBonus, Is.EqualTo(2));
    }

    // ==================== AggregatedCraftingModifiers.Aggregate — Quantity ====================

    [Test]
    public void Aggregate_QuantityPercentMult()
    {
        List<CraftingModifier> modifiers =
        [
            new CraftingModifier("*", CraftingModifierScope.Global, CraftingStep.Quantity, 1.5f,
                EffectOperation.PercentMult)
        ];

        AggregatedCraftingModifiers result = AggregatedCraftingModifiers.Aggregate(modifiers);

        Assert.That(result.QuantityMultiplier, Is.EqualTo(1.5f));
    }

    [Test]
    public void Aggregate_QuantityAdditiveThenPercentMult()
    {
        // Base is 1.0 for quantity multiplier
        // Additive: 1.0 + 0.5 = 1.5
        // PercentMult: 1.5 * 2.0 = 3.0
        List<CraftingModifier> modifiers =
        [
            new CraftingModifier("*", CraftingModifierScope.Global, CraftingStep.Quantity, 0.5f,
                EffectOperation.Additive),
            new CraftingModifier("*", CraftingModifierScope.Global, CraftingStep.Quantity, 2.0f,
                EffectOperation.PercentMult)
        ];

        AggregatedCraftingModifiers result = AggregatedCraftingModifiers.Aggregate(modifiers);

        Assert.That(result.QuantityMultiplier, Is.EqualTo(3.0f));
    }

    // ==================== AggregatedCraftingModifiers.Aggregate — SuccessChance ====================

    [Test]
    public void Aggregate_SuccessChanceAdditive()
    {
        List<CraftingModifier> modifiers =
        [
            new CraftingModifier("*", CraftingModifierScope.Global, CraftingStep.SuccessChance, 0.1f,
                EffectOperation.Additive),
            new CraftingModifier("*", CraftingModifierScope.Global, CraftingStep.SuccessChance, 0.05f,
                EffectOperation.Additive)
        ];

        AggregatedCraftingModifiers result = AggregatedCraftingModifiers.Aggregate(modifiers);

        Assert.That(result.SuccessChanceBonus, Is.EqualTo(0.15f).Within(0.001f));
    }

    // ==================== AggregatedCraftingModifiers.Aggregate — CraftingTime ====================

    [Test]
    public void Aggregate_TimeReduction()
    {
        List<CraftingModifier> modifiers =
        [
            new CraftingModifier("*", CraftingModifierScope.Global, CraftingStep.CraftingTime, 2f,
                EffectOperation.Additive)
        ];

        AggregatedCraftingModifiers result = AggregatedCraftingModifiers.Aggregate(modifiers);

        Assert.That(result.TimeReductionRounds, Is.EqualTo(2));
    }

    // ==================== AggregatedCraftingModifiers.Aggregate — Empty list ====================

    [Test]
    public void Aggregate_EmptyList_ReturnsNone()
    {
        AggregatedCraftingModifiers result = AggregatedCraftingModifiers.Aggregate([]);

        Assert.That(result, Is.EqualTo(AggregatedCraftingModifiers.None));
    }

    // ==================== AggregatedCraftingModifiers.Aggregate — Mixed steps ====================

    [Test]
    public void Aggregate_MixedSteps_AreIndependent()
    {
        List<CraftingModifier> modifiers =
        [
            new CraftingModifier("*", CraftingModifierScope.Global, CraftingStep.Quality, 1f,
                EffectOperation.Additive),
            new CraftingModifier("*", CraftingModifierScope.Global, CraftingStep.Quantity, 2.0f,
                EffectOperation.PercentMult),
            new CraftingModifier("*", CraftingModifierScope.Global, CraftingStep.SuccessChance, 0.1f,
                EffectOperation.Additive),
            new CraftingModifier("*", CraftingModifierScope.Global, CraftingStep.CraftingTime, 3f,
                EffectOperation.Additive)
        ];

        AggregatedCraftingModifiers result = AggregatedCraftingModifiers.Aggregate(modifiers);

        Assert.That(result.QualityBonus, Is.EqualTo(1));
        Assert.That(result.QuantityMultiplier, Is.EqualTo(2.0f));
        Assert.That(result.SuccessChanceBonus, Is.EqualTo(0.1f).Within(0.001f));
        Assert.That(result.TimeReductionRounds, Is.EqualTo(3));
        Assert.That(result.IsEmpty, Is.False);
    }

    // ==================== DefaultCraftingProcessor integration ====================

    [Test]
    public async Task DefaultProcessor_NoModifiers_PassesProductsUnchanged()
    {
        var processor = new DefaultCraftingProcessor();
        var recipe = CreateTestRecipe(quality: 3, quantity: 2, successChance: 0.8f);

        CraftingResult result = await processor.ProcessCraftingAsync(
            CharacterId(), recipe, AggregatedCraftingModifiers.None, new Dictionary<string, object>());

        Assert.That(result.Success, Is.True);
        Assert.That(result.ProductsCreated, Has.Count.EqualTo(1));
        Assert.That(result.ProductsCreated[0].Quality, Is.EqualTo(3));
        Assert.That(result.ProductsCreated[0].Quantity.Value, Is.EqualTo(2));
        Assert.That(result.ProductsCreated[0].SuccessChance, Is.EqualTo(0.8f));
    }

    [Test]
    public async Task DefaultProcessor_QualityBonusApplied()
    {
        var processor = new DefaultCraftingProcessor();
        var recipe = CreateTestRecipe(quality: 3);
        var modifiers = new AggregatedCraftingModifiers { QualityBonus = 2 };

        CraftingResult result = await processor.ProcessCraftingAsync(
            CharacterId(), recipe, modifiers, new Dictionary<string, object>());

        Assert.That(result.ProductsCreated[0].Quality, Is.EqualTo(5)); // 3 + 2
    }

    [Test]
    public async Task DefaultProcessor_QualityBonusClampsTo9()
    {
        var processor = new DefaultCraftingProcessor();
        var recipe = CreateTestRecipe(quality: 8);
        var modifiers = new AggregatedCraftingModifiers { QualityBonus = 5 };

        CraftingResult result = await processor.ProcessCraftingAsync(
            CharacterId(), recipe, modifiers, new Dictionary<string, object>());

        Assert.That(result.ProductsCreated[0].Quality, Is.EqualTo(9)); // clamped
    }

    [Test]
    public async Task DefaultProcessor_QuantityMultiplierApplied()
    {
        var processor = new DefaultCraftingProcessor();
        var recipe = CreateTestRecipe(quantity: 3);
        var modifiers = new AggregatedCraftingModifiers { QuantityMultiplier = 2.0f };

        CraftingResult result = await processor.ProcessCraftingAsync(
            CharacterId(), recipe, modifiers, new Dictionary<string, object>());

        Assert.That(result.ProductsCreated[0].Quantity.Value, Is.EqualTo(6)); // 3 * 2
    }

    [Test]
    public async Task DefaultProcessor_QuantityNeverBelowOne()
    {
        var processor = new DefaultCraftingProcessor();
        var recipe = CreateTestRecipe(quantity: 1);
        var modifiers = new AggregatedCraftingModifiers { QuantityMultiplier = 0.1f };

        CraftingResult result = await processor.ProcessCraftingAsync(
            CharacterId(), recipe, modifiers, new Dictionary<string, object>());

        Assert.That(result.ProductsCreated[0].Quantity.Value, Is.EqualTo(1)); // min 1
    }

    [Test]
    public async Task DefaultProcessor_SuccessChanceBonusApplied()
    {
        var processor = new DefaultCraftingProcessor();
        var recipe = CreateTestRecipe(successChance: 0.7f);
        var modifiers = new AggregatedCraftingModifiers { SuccessChanceBonus = 0.2f };

        CraftingResult result = await processor.ProcessCraftingAsync(
            CharacterId(), recipe, modifiers, new Dictionary<string, object>());

        Assert.That(result.ProductsCreated[0].SuccessChance, Is.EqualTo(0.9f).Within(0.001f));
    }

    [Test]
    public async Task DefaultProcessor_SuccessChanceClampsToOne()
    {
        var processor = new DefaultCraftingProcessor();
        var recipe = CreateTestRecipe(successChance: 0.9f);
        var modifiers = new AggregatedCraftingModifiers { SuccessChanceBonus = 0.5f };

        CraftingResult result = await processor.ProcessCraftingAsync(
            CharacterId(), recipe, modifiers, new Dictionary<string, object>());

        Assert.That(result.ProductsCreated[0].SuccessChance, Is.EqualTo(1.0f));
    }

    [Test]
    public async Task DefaultProcessor_NullQualityWithBonus_SetsQuality()
    {
        var processor = new DefaultCraftingProcessor();
        var recipe = CreateTestRecipe(quality: null);
        var modifiers = new AggregatedCraftingModifiers { QualityBonus = 2 };

        CraftingResult result = await processor.ProcessCraftingAsync(
            CharacterId(), recipe, modifiers, new Dictionary<string, object>());

        Assert.That(result.ProductsCreated[0].Quality, Is.EqualTo(2));
    }

    [Test]
    public async Task DefaultProcessor_NullQualityNoBonus_StaysNull()
    {
        var processor = new DefaultCraftingProcessor();
        var recipe = CreateTestRecipe(quality: null);

        CraftingResult result = await processor.ProcessCraftingAsync(
            CharacterId(), recipe, AggregatedCraftingModifiers.None, new Dictionary<string, object>());

        Assert.That(result.ProductsCreated[0].Quality, Is.Null);
    }

    // ==================== Helpers ====================

    private static SharedKernel.CharacterId CharacterId() =>
        SharedKernel.CharacterId.From(Guid.NewGuid());

    private static Recipe CreateTestRecipe(int? quality = 2, int quantity = 1, float? successChance = null)
    {
        return new Recipe
        {
            RecipeId = new SharedKernel.ValueObjects.RecipeId("test_recipe"),
            Name = "Test Recipe",
            IndustryTag = new SharedKernel.IndustryTag("test_industry"),
            Ingredients =
            [
                new SharedKernel.ValueObjects.Ingredient
                {
                    ItemTag = "test_input",
                    Quantity = SharedKernel.ValueObjects.Quantity.Parse(1)
                }
            ],
            Products =
            [
                new SharedKernel.ValueObjects.Product
                {
                    ItemTag = "test_output",
                    Quantity = SharedKernel.ValueObjects.Quantity.Parse(quantity),
                    Quality = quality,
                    SuccessChance = successChance
                }
            ]
        };
    }
}
