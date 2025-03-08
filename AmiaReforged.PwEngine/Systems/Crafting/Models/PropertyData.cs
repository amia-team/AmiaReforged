using System.Text.RegularExpressions;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Models;

public class PropertyData
{
    public class SavingThrow
    {
        public SavingThrow(ItemProperty property)
        {
            ItemPropertyModel model = new()
            {
                Property = property
            };

            ThrowType = model.SubTypeName;
            Bonus = int.Parse(model.PropertyBonus);
        }

        public string ThrowType { get; set; }
        public int Bonus { get; set; }
    }

    public class DamageBonus
    {
        public DamageBonus(ItemProperty property)
        {
            ItemPropertyModel model = new()
            {
                Property = property
            };

            // Damage die always follow fornat: xdy where x and y are integers...Only ever one of these... 
            Match match = Regex.Match(model.Label, pattern: @"(\d+)d(\d+)");

            // Split the label into the number of dice and the size of the die. It should have found something like 1d8, so split on the 'd'.
            string[] split = match.Value.Split('d');
            DamageType = model.SubTypeName;
            NumDie = int.Parse(split[0]);
            DieSize = int.Parse(split[1]);
        }

        public string DamageType { get; set; }
        public int NumDie { get; set; }
        public int DieSize { get; set; }
    }

    public class DamageResistance
    {
        public DamageResistance(ItemProperty itemProperty)
        {
            ItemPropertyModel incoming = new()
            {
                Property = itemProperty
            };

            ResistanceType = incoming.SubTypeName;

            // Splits Resist_5/- into its constituent parts and selects the numeric component
            ResistanceValue = int.Parse(incoming.PropertyBonus.Split(separator: "_")[1].Split(separator: "/")[0]);
        }

        public string ResistanceType { get; set; }

        public int ResistanceValue { get; set; }
    }

    public class BonusSpellSlot
    {
        public BonusSpellSlot(ItemProperty property)
        {
            ItemPropertyModel model = new()
            {
                Property = property
            };

            Class = model.SubTypeName;
            string modelPropertyBonus = model.PropertyBonus.Replace(oldValue: "Level", newValue: "");
            Level = int.Parse(modelPropertyBonus);
        }

        public string Class { get; init; }
        public int Level { get; init; }
    }

    public class OnHit
    {
        public OnHit(ItemProperty property)
        {
            OriginalProperty = property;
            ItemPropertyModel model = new()
            {
                Property = property
            };
            string trimmedLabel = model.Label.Replace(oldValue: "On Hit: ", newValue: "")
                .Replace(oldValue: "Bonus_", newValue: "");
            string[] split = trimmedLabel.Split(separator: " ");
            Type = split[0];
            SaveDc = split[1];
        }

        public string Type { get; set; }
        public string HitChance { get; set; }
        public string SaveDc { get; set; }

        public ItemProperty OriginalProperty { get; }
    }
}