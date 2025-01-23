namespace AmiaReforged.PwEngine.Systems.Crafting.Models.PropertyValidationRules;

public class ValidationResult
{
    public ValidationEnum Enum { get; init; }
    public string? ErrorMessage { get; init; }
}