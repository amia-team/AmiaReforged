using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops;

public sealed class NwItemLocalVariableWriter(NwItem item) : IItemLocalVariableWriter
{
    public void SetInt(string name, int value)
    {
        NWScript.SetLocalInt(item, name, value);
    }

    public void SetString(string name, string value)
    {
        NWScript.SetLocalString(item, name, value);
    }

    public void SetJson(string name, string json)
    {
        NWScript.SetLocalString(item, name, json);
    }
}

public sealed class NwItemAppearanceWriter(NwItem item) : IItemAppearanceWriter
{
    public void SetSimpleModel(int modelType, int? simpleModelNumber)
    {
        if (simpleModelNumber is null)
        {
            return;
        }

        // NWN simple models are addressed via ItemAppearanceType.SimpleModel when modelType is 0.
        if (modelType == 0)
        {
            item.Appearance.SetSimpleModel((ushort)simpleModelNumber.Value);
        }
    }
}
