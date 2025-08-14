using AmiaReforged.PwEngine.Systems.WorldEngine.Definitions.Common;
using Anvil.API;
using Microsoft.IdentityModel.Tokens;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Models;

public interface IItemStorage
{
    public IReadOnlyList<ItemSnapshot> GetItems();
}

public record ItemSnapshot(
    ItemType Type,
    string Name,
    string Tag,
    QualityEnum Quality,
    MaterialEnum Material,
    Guid? Creator,
    NwItem? Reference,
    byte[]? Deserialized);
