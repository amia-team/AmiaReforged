using AmiaReforged.PwEngine.Systems.WorldEngine.Definitions;
using AmiaReforged.PwEngine.Systems.WorldEngine.Definitions.Common;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Models;

public class Reaction
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required ReactionKind Kind { get; init; }
    public required ReactionContext Context { get; init; }
    public required ReactionItemOutput[] Output { get; init; } = [];
    public required ReactionCompletionTime Duration { get; init; }
    public required YieldModifier[] YieldModifiers { get; init; } = [];
}

public record YieldModifier(
    FieldRankAdjustments[]? Adjustments,
    KnowledgeAdjustments[]? KnowledgeAdjustments,
    CharacterStatAdjustments? StatAdjustments);

public record FieldRankAdjustments(
    Field Field,
    int BonusPerceptPerRank = 0,
    RoundingType Rounding = RoundingType.Floor,
    int CapPercent = 100);

public enum RoundingType
{
    Floor,
    Cieling,
    Rounding,
    Truncating,
}

public record KnowledgeAdjustments(
    Knowledge Knowledge,
    float Multiplier = 1.0f,
    float MinMultiplier = 1.0f,
    float MaxMultiplier = 1.0f);

public record ReactionCompletionTime(
    int Rounds,
    FieldRankAdjustments[]? FieldRankAdjustments,
    KnowledgeAdjustments[]? KnowledgeAdjustments,
    CharacterStatAdjustments? StatAdjustments);

public record CharacterStatAdjustments(AttributeAdjustments[]? Attributes, SkillAdjustments[]? Skills);

public record AttributeAdjustments(
    AttributeType Attribute,
    int RankStep = 1,
    int ModifierStep = 1,
    float BonusPercentPerStep = 0.1f,
    int CapPercent = 100);

public record SkillAdjustments(
    SkillType Skill,
    int RankStep = 5,
    float BonusPercentPerStep = 0.1f,
    int CapPercent = 100);

public enum SkillType
{
    CraftArmor,
    CraftWeapon,
    CraftTrap,
    Discipline,
    Spellcraft,
    Hide,
    MoveSilently,
    Search,
    PickPocket,
    OpenLock,
    Heal
}

public enum AttributeType
{
    Strength,
    Dexterity,
    Constitution,
    Intelligence,
    Wisdom,
    Charisma,
}

public record ReactionItemOutput(ItemType Type, ReactionOutputQuantity Quantity, int PercentChance = 100);

public record ReactionOutputQuantity(
    int Amount,
    DistributionType Distribution = DistributionType.Uniform,
    int MinAmount = 1,
    int MaxAmount = 1);

public enum DistributionType
{
    Uniform,
    Triangular,
    Weighted
}

public record ReactionContext(Field[] FieldsUsed, NodeKind[] NodeKinds, ToolEnum[] Tools);

public enum NodeKind
{
    OreVein,
    Boulder,
    Tree,
    Geode,
    Flora,
    Corpse,
    Workshop
}

public enum ReactionKind
{
    YieldItems = 0
}
