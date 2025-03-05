using System.Reflection;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells;

[ServiceBinding(typeof(SpellDecoratorFactory))]
public class SpellDecoratorFactory
{
    private readonly Dictionary<Type, List<Type>> _decorators = new();

    public SpellDecoratorFactory()
    {
        // Scan for all classes decorated with the DecoratesSpell attribute
        var decoratedTypes = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.GetCustomAttribute<DecoratesSpell>() != null);

        foreach (var type in decoratedTypes)
        {
            var attribute = type.GetCustomAttribute<DecoratesSpell>();
            if (attribute != null)
            {
                if (!_decorators.ContainsKey(attribute.SpellType)) _decorators[attribute.SpellType] = new();
                _decorators[attribute.SpellType].Add(type);
            }
        }
    }

    public ISpell ApplyDecorators(ISpell spell)
    {
        Type spellType = spell.GetType();

        // Never actually null, but the compiler doesn't know that
        if (_decorators.TryGetValue(spellType, out List<Type>? decoratorTypes))
            foreach (Type decoratorType in decoratorTypes)
            {
                spell = (ISpell)Activator.CreateInstance(decoratorType, spell)!;
            }

        return spell;
    }
}