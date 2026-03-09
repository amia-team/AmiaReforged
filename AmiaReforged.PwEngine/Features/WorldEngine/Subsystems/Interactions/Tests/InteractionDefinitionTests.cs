using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using FluentAssertions;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Tests;

/// <summary>
/// Unit tests for <see cref="InteractionDefinition"/>, <see cref="InteractionResponse"/>,
/// and <see cref="InteractionResponseEffect"/> domain model behavior.
/// </summary>
[TestFixture]
public class InteractionDefinitionTests
{
    #region InteractionDefinition Defaults

    [Test]
    public void InteractionDefinition_should_default_TargetMode_to_Trigger()
    {
        InteractionDefinition definition = new() { Tag = "test", Name = "Test" };

        definition.TargetMode.Should().Be(InteractionTargetMode.Trigger);
    }

    [Test]
    public void InteractionDefinition_should_default_BaseRounds_to_4()
    {
        InteractionDefinition definition = new() { Tag = "test", Name = "Test" };

        definition.BaseRounds.Should().Be(4);
    }

    [Test]
    public void InteractionDefinition_should_default_MinRounds_to_2()
    {
        InteractionDefinition definition = new() { Tag = "test", Name = "Test" };

        definition.MinRounds.Should().Be(2);
    }

    [Test]
    public void InteractionDefinition_should_default_ProficiencyReducesRounds_to_true()
    {
        InteractionDefinition definition = new() { Tag = "test", Name = "Test" };

        definition.ProficiencyReducesRounds.Should().BeTrue();
    }

    [Test]
    public void InteractionDefinition_should_default_RequiresIndustryMembership_to_true()
    {
        InteractionDefinition definition = new() { Tag = "test", Name = "Test" };

        definition.RequiresIndustryMembership.Should().BeTrue();
    }

    [Test]
    public void InteractionDefinition_should_start_with_empty_Responses_list()
    {
        InteractionDefinition definition = new() { Tag = "test", Name = "Test" };

        definition.Responses.Should().BeEmpty();
    }

    #endregion

    #region InteractionResponse Defaults

    [Test]
    public void InteractionResponse_should_default_Weight_to_1()
    {
        InteractionResponse response = new() { ResponseTag = "r1" };

        response.Weight.Should().Be(1);
    }

    [Test]
    public void InteractionResponse_should_default_MinProficiency_to_null()
    {
        InteractionResponse response = new() { ResponseTag = "r1" };

        response.MinProficiency.Should().BeNull();
    }

    [Test]
    public void InteractionResponse_should_start_with_empty_Effects_list()
    {
        InteractionResponse response = new() { ResponseTag = "r1" };

        response.Effects.Should().BeEmpty();
    }

    #endregion

    #region SelectResponse — Weighted Random

    [Test]
    public void SelectResponse_should_return_null_when_no_responses_exist()
    {
        InteractionDefinition definition = new()
        {
            Tag = "test", Name = "Test", Responses = []
        };

        definition.SelectResponse(ProficiencyLevel.Novice).Should().BeNull();
    }

    [Test]
    public void SelectResponse_should_return_the_only_response_when_one_exists()
    {
        InteractionResponse sole = new() { ResponseTag = "only" };
        InteractionDefinition definition = new()
        {
            Tag = "test", Name = "Test", Responses = [sole]
        };

        definition.SelectResponse(ProficiencyLevel.Novice).Should().BeSameAs(sole);
    }

    [Test]
    public void SelectResponse_should_filter_by_MinProficiency()
    {
        InteractionResponse lowTier = new()
        {
            ResponseTag = "low", MinProficiency = null
        };
        InteractionResponse highTier = new()
        {
            ResponseTag = "high", MinProficiency = ProficiencyLevel.Expert
        };
        InteractionDefinition definition = new()
        {
            Tag = "test", Name = "Test", Responses = [lowTier, highTier]
        };

        // Novice should only get the low tier (Expert not met)
        InteractionResponse? result = definition.SelectResponse(ProficiencyLevel.Novice);
        result.Should().BeSameAs(lowTier);
    }

    [Test]
    public void SelectResponse_should_include_response_when_proficiency_meets_minimum()
    {
        InteractionResponse gated = new()
        {
            ResponseTag = "gated", MinProficiency = ProficiencyLevel.Apprentice
        };
        InteractionDefinition definition = new()
        {
            Tag = "test", Name = "Test", Responses = [gated]
        };

        // Apprentice meets Apprentice minimum
        definition.SelectResponse(ProficiencyLevel.Apprentice).Should().BeSameAs(gated);

        // Expert exceeds Apprentice minimum
        definition.SelectResponse(ProficiencyLevel.Expert).Should().BeSameAs(gated);
    }

    [Test]
    public void SelectResponse_should_return_null_when_all_responses_require_higher_proficiency()
    {
        InteractionResponse expert = new()
        {
            ResponseTag = "expert", MinProficiency = ProficiencyLevel.Expert
        };
        InteractionDefinition definition = new()
        {
            Tag = "test", Name = "Test", Responses = [expert]
        };

        definition.SelectResponse(ProficiencyLevel.Novice).Should().BeNull();
    }

    [Test]
    public void SelectResponse_should_respect_weights_over_many_iterations()
    {
        InteractionResponse heavy = new()
        {
            ResponseTag = "heavy", Weight = 100
        };
        InteractionResponse light = new()
        {
            ResponseTag = "light", Weight = 1
        };
        InteractionDefinition definition = new()
        {
            Tag = "test", Name = "Test", Responses = [heavy, light]
        };

        // Over 1000 calls, "heavy" should dominate
        int heavyCount = 0;
        for (int i = 0; i < 1000; i++)
        {
            InteractionResponse? result = definition.SelectResponse(ProficiencyLevel.Novice);
            if (result?.ResponseTag == "heavy") heavyCount++;
        }

        heavyCount.Should().BeGreaterThan(900, "the heavy-weight response should be selected ~99% of the time");
    }

    #endregion

    #region InteractionResponseEffect

    [Test]
    public void InteractionResponseEffect_should_have_required_EffectType_and_Value()
    {
        InteractionResponseEffect effect = new()
        {
            EffectType = InteractionResponseEffectType.FloatingText,
            Value = "You sense something nearby"
        };

        effect.EffectType.Should().Be(InteractionResponseEffectType.FloatingText);
        effect.Value.Should().Be("You sense something nearby");
    }

    [Test]
    public void InteractionResponseEffect_should_default_Metadata_to_empty_dictionary()
    {
        InteractionResponseEffect effect = new()
        {
            EffectType = InteractionResponseEffectType.Custom,
            Value = "handler_key"
        };

        effect.Metadata.Should().BeEmpty();
    }

    [Test]
    public void InteractionResponseEffect_should_carry_metadata()
    {
        InteractionResponseEffect effect = new()
        {
            EffectType = InteractionResponseEffectType.VfxAtLocation,
            Value = "ImpDustExplosion",
            Metadata = new Dictionary<string, object> { ["scale"] = 2.0 }
        };

        effect.Metadata.Should().ContainKey("scale");
    }

    #endregion
}
