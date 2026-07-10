using System.ComponentModel.DataAnnotations;
using AmiaReforged.AdminPanel.Models;
using FluentAssertions;
using NUnit.Framework;

namespace AmiaReforged.AdminPanel.Tests.Tests.Models;

[TestFixture]
public class DtoValidationTests
{
    private static List<ValidationResult> Validate(object dto)
    {
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, context, results, validateAllProperties: true);
        return results;
    }

    // ==================== ItemBlueprintDto ====================

    [Test]
    public void ItemBlueprintDto_RejectsEmptyTag()
    {
        var dto = new ItemBlueprintDto { ItemTag = "", Name = "Test", ResRef = "test_res" };
        List<ValidationResult> results = Validate(dto);
        results.Should().Contain(r => r.MemberNames.Contains(nameof(ItemBlueprintDto.ItemTag)));
    }

    [Test]
    public void ItemBlueprintDto_RejectsEmptyResRef()
    {
        var dto = new ItemBlueprintDto { ItemTag = "tag", ResRef = "", Name = "Test" };
        List<ValidationResult> results = Validate(dto);
        results.Should().Contain(r => r.MemberNames.Contains(nameof(ItemBlueprintDto.ResRef)));
    }

    [Test]
    public void ItemBlueprintDto_RejectsEmptyName()
    {
        var dto = new ItemBlueprintDto { ItemTag = "tag", ResRef = "res", Name = "" };
        List<ValidationResult> results = Validate(dto);
        results.Should().Contain(r => r.MemberNames.Contains(nameof(ItemBlueprintDto.Name)));
    }

    [Test]
    public void ItemBlueprintDto_RejectsNegativeBaseValue()
    {
        var dto = new ItemBlueprintDto { ItemTag = "tag", ResRef = "res", Name = "X", BaseValue = -1 };
        List<ValidationResult> results = Validate(dto);
        results.Should().Contain(r => r.MemberNames.Contains(nameof(ItemBlueprintDto.BaseValue)));
    }

    [Test]
    public void ItemBlueprintDto_AcceptsValidDto()
    {
        var dto = new ItemBlueprintDto { ItemTag = "sword_001", ResRef = "sword_001", Name = "Iron Sword", BaseValue = 100 };
        List<ValidationResult> results = Validate(dto);
        results.Should().BeEmpty();
    }

    // ==================== CoinhouseDto ====================

    [Test]
    public void CoinhouseDto_RejectsEmptyTag()
    {
        var dto = new CoinhouseDto { Tag = "" };
        List<ValidationResult> results = Validate(dto);
        results.Should().Contain(r => r.MemberNames.Contains(nameof(CoinhouseDto.Tag)));
    }

    [Test]
    public void CoinhouseDto_RejectsNegativeStoredGold()
    {
        var dto = new CoinhouseDto { Tag = "bank_01", StoredGold = -1 };
        List<ValidationResult> results = Validate(dto);
        results.Should().Contain(r => r.MemberNames.Contains(nameof(CoinhouseDto.StoredGold)));
    }

    [Test]
    public void CoinhouseDto_AcceptsZeroStoredGold()
    {
        var dto = new CoinhouseDto { Tag = "bank_01", StoredGold = 0 };
        List<ValidationResult> results = Validate(dto);
        results.Should().BeEmpty();
    }

    // ==================== TraitDefinitionDto ====================

    [Test]
    public void TraitDefinitionDto_RejectsEmptyTag()
    {
        var dto = new TraitDefinitionDto { Tag = "", Name = "Strong", Description = "Test" };
        List<ValidationResult> results = Validate(dto);
        results.Should().Contain(r => r.MemberNames.Contains(nameof(TraitDefinitionDto.Tag)));
    }

    [Test]
    public void TraitDefinitionDto_RejectsEmptyName()
    {
        var dto = new TraitDefinitionDto { Tag = "strong", Name = "", Description = "Test" };
        List<ValidationResult> results = Validate(dto);
        results.Should().Contain(r => r.MemberNames.Contains(nameof(TraitDefinitionDto.Name)));
    }

    // ==================== LoreDefinitionDto ====================

    [Test]
    public void LoreDefinitionDto_RejectsEmptyTitle()
    {
        var dto = new LoreDefinitionDto { LoreId = "lore_1", Title = "", Content = "text" };
        List<ValidationResult> results = Validate(dto);
        results.Should().Contain(r => r.MemberNames.Contains(nameof(LoreDefinitionDto.Title)));
    }

    [Test]
    public void LoreDefinitionDto_RejectsEmptyContent()
    {
        var dto = new LoreDefinitionDto { LoreId = "lore_1", Title = "Title", Content = "" };
        List<ValidationResult> results = Validate(dto);
        results.Should().Contain(r => r.MemberNames.Contains(nameof(LoreDefinitionDto.Content)));
    }

    // ==================== QuestDefinitionDto ====================

    [Test]
    public void QuestDefinitionDto_RejectsEmptyTitle()
    {
        var dto = new QuestDefinitionDto { QuestId = "q_1", Title = "", Description = "desc" };
        List<ValidationResult> results = Validate(dto);
        results.Should().Contain(r => r.MemberNames.Contains(nameof(QuestDefinitionDto.Title)));
    }

    [Test]
    public void QuestDefinitionDto_RejectsEmptyQuestId()
    {
        var dto = new QuestDefinitionDto { QuestId = "", Title = "Title", Description = "desc" };
        List<ValidationResult> results = Validate(dto);
        results.Should().Contain(r => r.MemberNames.Contains(nameof(QuestDefinitionDto.QuestId)));
    }

    // ==================== Valid DTO pass-through ====================

    [Test]
    public void RegionDefinitionDto_AcceptsValidDto()
    {
        var dto = new RegionDefinitionDto { Tag = "region_01", Name = "Test Region" };
        List<ValidationResult> results = Validate(dto);
        results.Should().BeEmpty();
    }

    [Test]
    public void InteractionDefinitionDto_AcceptsValidDto()
    {
        var dto = new InteractionDefinitionDto { Tag = "talk_01", Name = "Talk to Guard" };
        List<ValidationResult> results = Validate(dto);
        results.Should().BeEmpty();
    }

    [Test]
    public void IndustryDefinitionDto_AcceptsValidDto()
    {
        var dto = new IndustryDefinitionDto { Tag = "smithing", Name = "Smithing" };
        List<ValidationResult> results = Validate(dto);
        results.Should().BeEmpty();
    }
}
