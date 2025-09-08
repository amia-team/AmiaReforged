namespace AmiaReforged.PwEngine.Systems.WorldEngine;

internal static class WorldConstants
{
    public const string InitializedKey = "economy_initialized";
    public const string ConfigTypeBool = "bool";

    public const string ResourceNodeZoneTag = "worldengine_node_region";
    public static string NodeSpawnPointRef { get; set; } = "worldengine_sp";
    public static string GenericNodePlcRef { get; set; } = "worldengine_harv";
    public static string MarketValueBaseLvar { get; } = "market_value_base";
    public static string ItemVariableMaker { get; } = "item_maker";
    public static string ItemVariableQuality { get; } = "item_quality";
    public static string ItemVariableMaterial { get; } = "item_material";
    public static string ItemVariableType { get; } = "item_type";
    public static string LvarNodeTags { get; } = "node_tags";
}
