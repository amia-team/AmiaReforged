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
    //  Entity rendering
    // ═══════════════════════════════════════════════════════════════════

    private RenderFragment RenderEntityEditor(EditorTab tab, object data) => builder =>
    {
        switch (tab.EntityType)
        {
            case WorldEngineEntityType.Items when data is ItemBlueprintDto item:
                RenderItemEditor(builder, item);
                break;
            case WorldEngineEntityType.ResourceNodes when data is ResourceNodeDefinitionDto node:
                RenderResourceNodeEditor(builder, node);
                break;
            case WorldEngineEntityType.Regions when data is RegionDefinitionDto region:
                RenderRegionEditor(builder, region);
                break;
            case WorldEngineEntityType.Traits when data is TraitDefinitionDto trait:
                RenderTraitEditor(builder, trait);
                break;
            case WorldEngineEntityType.Glyphs when data is GlyphDefinitionDto glyph:
                RenderGlyphEditor(builder, glyph);
                break;
            case WorldEngineEntityType.Industries when data is IndustryDefinitionDto industry:
                RenderIndustryEditor(builder, industry);
                break;
            case WorldEngineEntityType.Interactions when data is InteractionDefinitionDto interaction:
                RenderInteractionEditor(builder, interaction);
                break;
            case WorldEngineEntityType.Coinhouses when data is CoinhouseDto coinhouse:
                RenderCoinhouseEditor(builder, coinhouse);
                break;
        }
    };

    // ── Item ────────────────────────────────────────────────────────
    private static void RenderItemEditor(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder b, ItemBlueprintDto dto)
    {
        int s = 0;
        b.OpenElement(s++, "div"); b.AddAttribute(s++, "class", "we-entity-form");
        AddField(b, ref s, "Tag", dto.ItemTag);
        AddField(b, ref s, "Name", dto.Name);
        AddField(b, ref s, "ResRef", dto.ResRef);
        AddField(b, ref s, "Description", dto.Description);
        AddField(b, ref s, "Base Item Type", dto.BaseItemType.ToString());
        AddField(b, ref s, "Base Value", dto.BaseValue.ToString());
        AddField(b, ref s, "Item Form", dto.ItemForm);
        AddField(b, ref s, "Materials", dto.Materials != null ? string.Join(", ", dto.Materials) : "—");
        AddField(b, ref s, "Is Template", dto.IsTemplate ? "Yes" : "No");
        AddField(b, ref s, "Variants", dto.Variants?.Count.ToString() ?? "0");
        b.CloseElement();
    }

    // ── Resource Node ───────────────────────────────────────────────
    private static void RenderResourceNodeEditor(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder b, ResourceNodeDefinitionDto dto)
    {
        int s = 0;
        b.OpenElement(s++, "div"); b.AddAttribute(s++, "class", "we-entity-form");
        AddField(b, ref s, "Tag", dto.Tag);
        AddField(b, ref s, "Name", dto.Name);
        AddField(b, ref s, "Type", dto.Type);
        AddField(b, ref s, "Description", dto.Description);
        AddField(b, ref s, "Uses", dto.Uses.ToString());
        AddField(b, ref s, "Harvest Rounds", dto.BaseHarvestRounds.ToString());
        AddField(b, ref s, "PLC Appearance", dto.PlcAppearance.ToString());
        AddField(b, ref s, "Quality", $"{dto.MinQuality ?? "—"} → {dto.MaxQuality ?? "—"}");
        AddField(b, ref s, "Outputs", dto.Outputs?.Length.ToString() ?? "0");
        b.CloseElement();
    }

    // ── Region ──────────────────────────────────────────────────────
    private static void RenderRegionEditor(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder b, RegionDefinitionDto dto)
    {
        int s = 0;
        b.OpenElement(s++, "div"); b.AddAttribute(s++, "class", "we-entity-form");
        AddField(b, ref s, "Tag", dto.Tag);
        AddField(b, ref s, "Name", dto.Name);
        AddField(b, ref s, "Areas", dto.Areas.Count.ToString());
        if (dto.DefaultChaos != null)
        {
            AddField(b, ref s, "Default Chaos", $"D:{dto.DefaultChaos.Danger} C:{dto.DefaultChaos.Corruption} N:{dto.DefaultChaos.Density} M:{dto.DefaultChaos.Mutation}");
        }
        b.CloseElement();
    }

    // ── Trait ────────────────────────────────────────────────────────
    private static void RenderTraitEditor(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder b, TraitDefinitionDto dto)
    {
        int s = 0;
        b.OpenElement(s++, "div"); b.AddAttribute(s++, "class", "we-entity-form");
        AddField(b, ref s, "Tag", dto.Tag);
        AddField(b, ref s, "Name", dto.Name);
        AddField(b, ref s, "Category", dto.Category);
        AddField(b, ref s, "Point Cost", dto.PointCost.ToString());
        AddField(b, ref s, "Death Behavior", dto.DeathBehavior);
        AddField(b, ref s, "DM Only", dto.DmOnly ? "Yes" : "No");
        AddField(b, ref s, "Requires Unlock", dto.RequiresUnlock ? "Yes" : "No");
        AddField(b, ref s, "Effects", dto.Effects.Count.ToString());
        AddField(b, ref s, "Description", dto.Description);
        b.CloseElement();
    }

    // ── Glyph ───────────────────────────────────────────────────────
    private static void RenderGlyphEditor(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder b, GlyphDefinitionDto dto)
    {
        int s = 0;
        b.OpenElement(s++, "div"); b.AddAttribute(s++, "class", "we-entity-form");
        AddField(b, ref s, "ID", dto.Id.ToString());
        AddField(b, ref s, "Name", dto.Name);
        AddField(b, ref s, "Description", dto.Description);
        AddField(b, ref s, "Event Type", dto.EventType);
        AddField(b, ref s, "Category", dto.Category);
        AddField(b, ref s, "Active", dto.IsActive ? "Yes" : "No");
        AddField(b, ref s, "Created", dto.CreatedAt.ToString("yyyy-MM-dd"));
        AddField(b, ref s, "Updated", dto.UpdatedAt.ToString("yyyy-MM-dd"));
        b.CloseElement();
    }

    // ── Industry ────────────────────────────────────────────────────
    private static void RenderIndustryEditor(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder b, IndustryDefinitionDto dto)
    {
        int s = 0;
        b.OpenElement(s++, "div"); b.AddAttribute(s++, "class", "we-entity-form");
        AddField(b, ref s, "Tag", dto.Tag);
        AddField(b, ref s, "Name", dto.Name);
        AddField(b, ref s, "Knowledge Entries", dto.Knowledge.Count.ToString());
        AddField(b, ref s, "Recipes", dto.Recipes.Count.ToString());
        b.CloseElement();
    }

    // ── Interaction ─────────────────────────────────────────────────
    private static void RenderInteractionEditor(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder b, InteractionDefinitionDto dto)
    {
        int s = 0;
        b.OpenElement(s++, "div"); b.AddAttribute(s++, "class", "we-entity-form");
        AddField(b, ref s, "Tag", dto.Tag);
        AddField(b, ref s, "Name", dto.Name);
        AddField(b, ref s, "Description", dto.Description);
        AddField(b, ref s, "Target Mode", dto.TargetMode);
        AddField(b, ref s, "Base Rounds", dto.BaseRounds.ToString());
        AddField(b, ref s, "Min Rounds", dto.MinRounds.ToString());
        AddField(b, ref s, "Proficiency Reduces", dto.ProficiencyReducesRounds ? "Yes" : "No");
        AddField(b, ref s, "Requires Industry", dto.RequiresIndustryMembership ? "Yes" : "No");
        AddField(b, ref s, "Industry Tags", dto.RequiredIndustryTags.Count > 0 ? string.Join(", ", dto.RequiredIndustryTags) : "—");
        AddField(b, ref s, "Knowledge Tags", dto.RequiredKnowledgeTags.Count > 0 ? string.Join(", ", dto.RequiredKnowledgeTags) : "—");
        AddField(b, ref s, "Area ResRefs", dto.AllowedAreaResRefs.Count > 0 ? string.Join(", ", dto.AllowedAreaResRefs) : "Any");
        AddField(b, ref s, "Responses", dto.Responses.Count.ToString());
        b.CloseElement();
    }

    // ── Coinhouse ───────────────────────────────────────────────────
    private static void RenderCoinhouseEditor(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder b, CoinhouseDto dto)
    {
        int s = 0;
        b.OpenElement(s++, "div"); b.AddAttribute(s++, "class", "we-entity-form");
        AddField(b, ref s, "Tag", dto.Tag);
        AddField(b, ref s, "Settlement", dto.Settlement.ToString());
        AddField(b, ref s, "Stored Gold", dto.StoredGold.ToString("N0"));
        AddField(b, ref s, "Accounts", dto.AccountCount.ToString());
        AddField(b, ref s, "Total Deposits", dto.TotalDeposits.ToString("N0"));
        AddField(b, ref s, "Total Credits", dto.TotalCredits.ToString("N0"));
        b.CloseElement();
    }

    // ── Shared field renderer ───────────────────────────────────────
    private static void AddField(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder b, ref int seq, string label, string? value)
    {
        b.OpenElement(seq++, "div");  b.AddAttribute(seq++, "class", "we-entity-form__field");
        b.OpenElement(seq++, "label"); b.AddContent(seq++, label); b.CloseElement();
        b.OpenElement(seq++, "span");  b.AddContent(seq++, value ?? "—"); b.CloseElement();
        b.CloseElement();
    }
}
