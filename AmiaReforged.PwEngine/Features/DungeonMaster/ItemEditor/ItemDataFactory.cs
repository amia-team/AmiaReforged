using Anvil.API;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.ItemEditor;

internal static class ItemDataFactory
{
    public static ItemData From(NwItem item)
    {
        Dictionary<string, LocalVariableData> vars = new();

        // Enumerate all locals on the item and map them to our DTO
        foreach (ObjectVariable local in item.LocalVariables)
        {
            switch (local)
            {
                case LocalVariableInt li:
                    vars[li.Name] = new LocalVariableData
                    {
                        Type = LocalVariableType.Int,
                        IntValue = li.Value
                    };
                    break;

                case LocalVariableFloat lf:
                    vars[lf.Name] = new LocalVariableData
                    {
                        Type = LocalVariableType.Float,
                        FloatValue = lf.Value
                    };
                    break;

                case LocalVariableString ls:
                    vars[ls.Name] = new LocalVariableData
                    {
                        Type = LocalVariableType.String,
                        StringValue = ls.Value ?? string.Empty
                    };
                    break;

                case LocalVariableLocation lloc:
                    vars[lloc.Name] = new LocalVariableData
                    {
                        Type = LocalVariableType.Location,
                        LocationValue = lloc.Value
                    };
                    break;

                case LocalVariableObject<NwObject> lo:
                    vars[lo.Name] = new LocalVariableData
                    {
                        Type = LocalVariableType.Object,
                        ObjectValue = lo.Value
                    };
                    break;
            }
        }

        return new ItemData(
            item.Name,
            item.Description,
            item.Tag,
            vars
        );
    }

}
