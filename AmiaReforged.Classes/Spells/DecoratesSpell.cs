using JetBrains.Annotations;

namespace AmiaReforged.Classes.Spells;


[MeansImplicitUse(ImplicitUseKindFlags.Default, ImplicitUseTargetFlags.Itself)]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class DecoratesSpell : Attribute
{
    public Type SpellType { get; }

    public DecoratesSpell(Type spellType)
    {
        SpellType = spellType;
    }
}