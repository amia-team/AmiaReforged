using AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll;

public class DiceRollManager
{
    public IRollHandler? GetRollHandler(DiceRollType rollType)
    {
        IEnumerable<Type> types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => typeof(IRollHandler).IsAssignableFrom(p) && !p.IsInterface);

        foreach (var type in types)
        {
            object[] attributes = type.GetCustomAttributes(typeof(DiceRollAttribute), false);
            if (attributes.Length <= 0) continue;
            
            if (attributes[0] is DiceRollAttribute attribute && attribute.RollType == rollType)
            {
                return (IRollHandler)Activator.CreateInstance(type)!;
            }
        }

        return null;
    }
}