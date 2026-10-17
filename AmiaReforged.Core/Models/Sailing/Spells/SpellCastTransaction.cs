namespace AmiaReforged.Core.Models.Sailing;

/// <summary>Reserve a cast's resource once, refund only an explicitly unsuccessful effect.</summary>
public static class SpellCastTransaction
{
    public static bool Execute(Action reserve, Action refund, Func<bool> effect)
    {
        reserve();
        // An exception after partial effect processing is not a safe reason to grant another free cast.
        bool succeeded = effect();
        if (!succeeded) refund();
        return succeeded;
    }
}
