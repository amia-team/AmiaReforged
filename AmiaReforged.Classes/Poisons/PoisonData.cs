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

        int rowIndex = (int)poisonType;

        string? primaryAbilityString = poisonTable.GetString(rowIndex, "Default_1");
        Ability? primaryAbility = null;
        if (primaryAbilityString != null && AbilityAbbreviationMap.TryGetValue(primaryAbilityString, out Ability parsedPrimaryAbility))
            primaryAbility = parsedPrimaryAbility;

        string? secondaryAbilityString = poisonTable.GetString(rowIndex, "Default_2");
        Ability? secondaryAbility = null;
        if (secondaryAbilityString != null && AbilityAbbreviationMap.TryGetValue(secondaryAbilityString, out Ability parsedSecondaryAbility))
            secondaryAbility = parsedSecondaryAbility;

        PoisonValues poisonValues = new
        (
            PoisonType: poisonType,
            Name: poisonTable.GetStrRef(rowIndex, "Name").ToString(),
            PrimaryDieSides: poisonTable.GetInt(rowIndex, "Dice_1"),
            PrimaryDiceAmount: poisonTable.GetInt(rowIndex, "Dam_1"),
            PrimaryAbilityDamage: primaryAbility,
            PrimaryScript: poisonTable.GetString(rowIndex, "Script_1"),
            SecondaryDieSides: poisonTable.GetInt(rowIndex, "Dice_2"),
            SecondaryDiceAmount: poisonTable.GetInt(rowIndex, "Dam_2"),
            SecondaryAbilityDamage: secondaryAbility,
            SecondaryScript: poisonTable.GetString(rowIndex, "Script_2")
        );

        return poisonValues;
    }
}
