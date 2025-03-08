using AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.DiceRoll.RollHandlers;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.DiceRoll;

[ServiceBinding(typeof(DiceRollManager))]
public sealed class DiceRollManager
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public IRollHandler? GetRollHandler(DiceRollType rollType)
    {
        IEnumerable<Type> types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => typeof(IRollHandler).IsAssignableFrom(p) && !p.IsInterface);

        IEnumerable<Type> enumerable = types as Type[] ?? types.ToArray();

        foreach (var type in enumerable)
        {
            object[] attributes = type.GetCustomAttributes(typeof(DiceRollAttribute), false);
            if (attributes.Length <= 0) continue;

            if (attributes[0] is DiceRollAttribute attribute && attribute.RollType == rollType)
                return (IRollHandler)Activator.CreateInstance(type)!;
        }

        return null;
    }
}