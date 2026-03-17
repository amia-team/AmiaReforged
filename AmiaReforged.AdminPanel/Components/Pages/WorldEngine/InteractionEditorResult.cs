namespace AmiaReforged.AdminPanel.Components.Pages.WorldEngine;

/// <summary>Result passed back to the parent when the interaction editor closes.</summary>
public class InteractionEditorResult
{
    public bool Saved { get; init; }
    public string? InteractionTag { get; init; }
    public bool IsNew { get; init; }
    public string? Message { get; init; }
}
