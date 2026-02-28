namespace AmiaReforged.AdminPanel.Models;

// ==================== Item Blueprint DTOs ====================

public class ItemBlueprintDto
{
    public string ResRef { get; set; } = string.Empty;
    public string ItemTag { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string[]? Materials { get; set; }
    public string? JobSystemType { get; set; }
    public int BaseItemType { get; set; }
    public AppearanceDataDto? Appearance { get; set; }
    public int BaseValue { get; set; } = 1;
    public int WeightIncreaseConstant { get; set; } = -1;
    public string? SourceFile { get; set; }
}

public class AppearanceDataDto
{
    public int ModelType { get; set; }
    public int? SimpleModelNumber { get; set; }
    public WeaponPartDataDto? Data { get; set; }
}

public class WeaponPartDataDto
{
    public int TopPartModel { get; set; }
    public int MiddlePartModel { get; set; }
    public int BottomPartModel { get; set; }
    public int TopPartColor { get; set; }
    public int MiddlePartColor { get; set; }
    public int BottomPartColor { get; set; }
}

// ==================== Resource Node DTOs ====================

public class ResourceNodeDefinitionDto
{
    public string Tag { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int PlcAppearance { get; set; }
    public string? Type { get; set; }
    public int Uses { get; set; } = 50;
    public int BaseHarvestRounds { get; set; }
    public HarvestContextDto? Requirement { get; set; }
    public HarvestOutputDto[]? Outputs { get; set; }
    public FloraPropertiesDto? FloraProperties { get; set; }
}

public class HarvestContextDto
{
    public string? RequiredItemType { get; set; }
    public string? RequiredItemMaterial { get; set; }
}

public class HarvestOutputDto
{
    public string ItemDefinitionTag { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int Chance { get; set; } = 100;
}

public class FloraPropertiesDto
{
    public string? PreferredClimate { get; set; }
    public string? RequiredSoilQuality { get; set; }
}

// ==================== Common Response DTOs ====================

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class ImportResult
{
    public int Succeeded { get; set; }
    public int Failed { get; set; }
    public int Total { get; set; }
    public List<string> Errors { get; set; } = new();
}
