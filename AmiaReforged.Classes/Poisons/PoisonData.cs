using Anvil.API;

namespace AmiaReforged.Classes.Poisons;

public static class PoisonData
{
    public record PoisonValues
    (
        PoisonType PoisonType,
        string? Name,
        int? PrimaryDieSides,
        int? PrimaryDiceAmount,
        Ability? PrimaryAbilityDamage,
        string? PrimaryScript,
        int? SecondaryDieSides,
        int? SecondaryDiceAmount,
        Ability? SecondaryAbilityDamage,
        string? SecondaryScript
    );

    private static readonly Dictionary<string, Ability> AbilityAbbreviationMap = new()
    {
        { "STR", Ability.Strength },
        { "DEX", Ability.Dexterity },
        { "CON", Ability.Constitution },
        { "INT", Ability.Intelligence },
        { "WIS", Ability.Wisdom },
        { "CHA", Ability.Charisma },
    };

    public static PoisonValues? GetPoisonValues(PoisonType poisonType)
    {
        TwoDimArray? poisonTable = NwGameTables.GetTable("Poison");
        if (poisonTable == null) return null;

        int? id = poisonTable.GetInt((int)poisonType, "Id");
        if (id == null) return null;

        string? primaryAbilityString = poisonTable.GetString(id.Value, "Default_1");
        Ability? primaryAbility = null;
        if (primaryAbilityString != null && AbilityAbbreviationMap.TryGetValue(primaryAbilityString, out Ability parsedPrimaryAbility))
            primaryAbility = parsedPrimaryAbility;

        string? secondaryAbilityString = poisonTable.GetString(id.Value, "Default_2");
        Ability? secondaryAbility = null;
        if (secondaryAbilityString != null && AbilityAbbreviationMap.TryGetValue(secondaryAbilityString, out Ability parsedSecondaryAbility))
            secondaryAbility = parsedSecondaryAbility;

        PoisonValues poisonValues = new
        (
            PoisonType: poisonType,
            Name: poisonTable.GetStrRef(id.Value, "Name").ToString(),
            PrimaryDieSides: poisonTable.GetInt(id.Value, "Dice_1"),
            PrimaryDiceAmount: poisonTable.GetInt(id.Value, "Dam_1"),
            PrimaryAbilityDamage: primaryAbility,
            PrimaryScript: poisonTable.GetString(id.Value, "Script_1"),
            SecondaryDieSides: poisonTable.GetInt(id.Value, "Dice_2"),
            SecondaryDiceAmount: poisonTable.GetInt(id.Value, "Dam_2"),
            SecondaryAbilityDamage: secondaryAbility,
            SecondaryScript: poisonTable.GetString(id.Value, "Script_2")
        );

        return poisonValues;
    }
}
