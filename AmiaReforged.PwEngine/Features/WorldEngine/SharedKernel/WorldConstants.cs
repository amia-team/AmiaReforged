namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

internal static class WorldConstants
{
    public const string InitializedKey = "economy_initialized";
    public const string ConfigTypeBool = "bool";

    public const string ResourceNodeZoneTag = "worldengine_node_region";
    public static string NodeSpawnPointRef => "worldengine_sp";
    public static string GenericNodePlcRef => "worldengine_harv";
    public static string MarketValueBaseLvar => "market_value_base";
    public static string ItemVariableMaker => "item_maker";
    public static string ItemVariableQuality => "item_quality";
    public static string ItemVariableMaterial => "item_material";
    public static string ItemVariableType => "item_type";
    public static string LvarNodeTags => "node_tags";
    public static string ToolTypeVariable => "tool_type";
    public static string PcCachedLvar => "pckey_cached";
    public static string MaterialLvar => "materials";

    // Trigger-Based Node Spawning
    public static string LvarMaxNodesTotal => "max_nodes_total";
    public const int DefaultMaxNodesPerTrigger = 5;
    public const float MinNodeSpacing = 7.5f; // meters

    // Knowledge Progression Configuration Keys
    public const string KnowledgeProgressionBaseCost = "knowledge_progression_base_cost";
    public const string KnowledgeProgressionScalingFactor = "knowledge_progression_scaling_factor";
    public const string KnowledgeProgressionCurveType = "knowledge_progression_curve_type";
    public const string KnowledgePointDefaultSoftCap = "knowledge_point_soft_cap";
    public const string KnowledgePointDefaultHardCap = "knowledge_point_hard_cap";
    public const string KnowledgeSoftCapPenaltyMultiplier = "knowledge_soft_cap_penalty_multiplier";
    public const string ConfigTypeInt = "int";
    public const string ConfigTypeFloat = "float";
    public const string ConfigTypeString = "string";
}
