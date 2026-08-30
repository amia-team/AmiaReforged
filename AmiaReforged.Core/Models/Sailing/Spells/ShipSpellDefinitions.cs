using Anvil.API;

namespace AmiaReforged.Core.Models.Sailing;

public static class ShipSpellDefinitions
{
    public static readonly Dictionary<int, ShipSpellEffectDefinition>
        All =
            new()
            {
                {
                    (int)Spell.Fireball,
                    new ShipSpellEffectDefinition
                    {
                        SpellId = (int)Spell.Fireball,
                        DisplayName = "Fireball",
                        EffectType =
                            ShipSpellEffectType.Offensive,
                        HullDamage = 10,
                        MaxRange = 20.0f,
                        RequiresEnemyTarget = true,
                        RequiresEncounter = true
                    }
                },

                {
                    (int)Spell.LightningBolt,
                    new ShipSpellEffectDefinition
                    {
                        SpellId = (int)Spell.LightningBolt,
                        DisplayName = "Lightning Bolt",
                        EffectType =
                            ShipSpellEffectType.Offensive,
                        HullDamage = 10,
                        MaxRange = 25.0f,
                        RequiresEnemyTarget = true,
                        RequiresEncounter = true
                    }
                },

                {
                    (int)Spell.GustOfWind,
                    new ShipSpellEffectDefinition
                    {
                        SpellId = (int)Spell.GustOfWind,
                        DisplayName = "Gust of Wind",
                        EffectType =
                            ShipSpellEffectType.Movement,
                        SpeedMultiplier = 2.0f,
                        Duration =
                            TimeSpan.FromSeconds(60),
                        RequiresEnemyTarget = false,
                        RequiresEncounter = false
                    }
                }
            };
}