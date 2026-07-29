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
    //  Interaction Editor — delegated to InteractionEditor.razor
    // ═══════════════════════════════════════════════════════════════════

    private async Task OpenNewInteractionEditor()
    {
        if (_interactionEditorOpen && _interactionEditorRef != null)
            await _interactionEditorRef.Close();

        _interactionEditorOpen = true;
        _interactionEditorIsCreating = true;
        _interactionEditorTag = null;

        if (_interactionEditorRef != null)
            await _interactionEditorRef.OpenCreate();
    }

    private async Task OpenInteractionEditor(string interactionTag)
    {
        if (_interactionEditorOpen && _interactionEditorRef != null)
            await _interactionEditorRef.Close();

        _interactionEditorOpen = true;
        _interactionEditorIsCreating = false;
        _interactionEditorTag = interactionTag;

        try
        {
            InteractionDefinitionDto? loaded = await InteractionApi.GetByTagAsync(interactionTag);
            if (loaded == null)
            {
                _interactionEditorOpen = false;
                StateHasChanged();
                return;
            }

            if (_interactionEditorRef != null)
                await _interactionEditorRef.OpenEdit(loaded);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to open interaction editor for {Tag}", interactionTag);
            _interactionEditorOpen = false;
            StateHasChanged();
        }
    }

    private async Task HandleInteractionEditorClosed(InteractionEditorResult result)
    {
        _interactionEditorOpen = false;
        _interactionEditorIsCreating = false;
        _interactionEditorTag = null;

        if (result.Saved && EditorState.ActiveEntityType == WorldEngineEntityType.Interactions)
        {
            await LoadEntityList(reset: true);
        }

        StateHasChanged();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Codex Editor — delegated to Editors/CodexEditor.razor
    // ═══════════════════════════════════════════════════════════════════

    private CodexEditor? _codexEditorRef;

    private async Task OpenNewCodexEditor(CodexEditor.CodexSubType subType)
    {
        if (_codexEditorOpen) CloseCodexEditor();

        _codexEditorOpen = true;
        StateHasChanged();
        await Task.Delay(50);
        await _codexEditorRef!.OpenNewAsync(subType);
    }

    private async Task OpenCodexEditor(string entityId, CodexEditor.CodexSubType subType)
    {
        if (_codexEditorOpen) CloseCodexEditor();

        _codexEditorOpen = true;
        StateHasChanged();
        await Task.Delay(50);
        await _codexEditorRef!.OpenExistingAsync(entityId, subType);
    }

    private void CloseCodexEditor()
    {
        _codexEditorOpen = false;
        StateHasChanged();
    }
}
