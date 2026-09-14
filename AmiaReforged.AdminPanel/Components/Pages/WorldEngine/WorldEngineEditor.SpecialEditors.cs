using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using AmiaReforged.AdminPanel.Components.Pages.WorldEngine.Editors;
using AmiaReforged.AdminPanel.Models;
using AmiaReforged.AdminPanel.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace AmiaReforged.AdminPanel.Components.Pages.WorldEngine;

public partial class WorldEngineEditor
{
    // ═══════════════════════════════════════════════════════════════════
    //  Interaction Editor — tab-driven (InteractionEditor.razor self-loads
    //  via EntityTag/OpenOnParameters; no overlay state)
    // ═══════════════════════════════════════════════════════════════════

    private Task OpenNewInteractionEditor()
    {
        EditorTab tab = EditorState.OpenTab(
            WorldEngineEntityType.Interactions, "New Interaction", entityKey: null);
        _interactionNewTabs.Add(tab.Id);
        return Task.CompletedTask;
    }

    // Tabs with a null EntityKey are "new" interactions (no tag yet).
    private readonly HashSet<string> _interactionNewTabs = [];

    private bool IsNewInteractionTab(EditorTab tab) =>
        tab.EntityKey is null || _interactionNewTabs.Contains(tab.Id);

    private async Task HandleInteractionTabClosed(string tabId, InteractionEditorResult result)
    {
        _interactionNewTabs.Remove(tabId);
        CloseTab(tabId);

        if (result.Saved && EditorState.ActiveEntityType == WorldEngineEntityType.Interactions)
        {
            await LoadEntityList(reset: true);
        }

        StateHasChanged();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Codex Editor — tab-driven (Editors/CodexEditor.razor self-loads via
    //  EntityKey/OpenOnParameters; no overlay state)
    // ═══════════════════════════════════════════════════════════════════

    // Lore vs Quest per tab (tabs only carry EntityKey).
    private readonly Dictionary<string, CodexEditor.CodexSubType> _codexTabSubTypes = new();

    private void OpenCodexTab(string? entityKey, CodexEditor.CodexSubType subType, string title)
    {
        EditorTab tab = EditorState.OpenTab(WorldEngineEntityType.Codex, title, entityKey);
        _codexTabSubTypes[tab.Id] = subType;
        // Data loads inside CodexEditor; see OnActiveTabChanged skip.
    }

    private CodexEditor.CodexSubType GetCodexSubType(string tabId) =>
        _codexTabSubTypes.TryGetValue(tabId, out CodexEditor.CodexSubType subType)
            ? subType
            : CodexEditor.CodexSubType.Lore;

    private Task OpenNewCodexEditor(CodexEditor.CodexSubType subType)
    {
        OpenCodexTab(null, subType, subType == CodexEditor.CodexSubType.Quest ? "New Quest" : "New Lore");
        return Task.CompletedTask;
    }
}
