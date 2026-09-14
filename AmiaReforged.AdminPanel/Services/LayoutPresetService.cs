using System.Text.Json;
using AmiaReforged.AdminPanel.Models;
using Microsoft.JSInterop;

namespace AmiaReforged.AdminPanel.Services;

/// <summary>
/// Per-entity layout preset (which rails are open, at what widths).
/// Replaces Golden Layout's saved-layout JSON.
/// Widths are also persisted per-split by splitter.js; this service owns the
/// merged preset (defaults + stored overrides) and the open/collapsed flags.
/// </summary>
public sealed record EditorPreset(
    bool PaletteOpen,
    bool InspectorOpen,
    int PaletteWidth,
    int InspectorWidth);

/// <summary>
/// Scoped (per-circuit) store for editor layout presets, persisted to
/// localStorage via splitter.js helpers. Falls back to defaults when JS is
/// unavailable (prerender, tests).
/// </summary>
public sealed class LayoutPresetService
{
    private const string StorageKey = "we-presets";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;
    private Dictionary<string, EditorPreset>? _cache;
    private bool _loaded;

    public LayoutPresetService(IJSRuntime js)
    {
        _js = js;
    }

    public static EditorPreset GetDefault(WorldEngineEntityType entityType) => entityType switch
    {
        WorldEngineEntityType.Regions => new EditorPreset(
            PaletteOpen: false, InspectorOpen: true, PaletteWidth: 240, InspectorWidth: 340),
        WorldEngineEntityType.AreaGraph => new EditorPreset(
            PaletteOpen: false, InspectorOpen: true, PaletteWidth: 240, InspectorWidth: 340),
        WorldEngineEntityType.Interactions => new EditorPreset(
            PaletteOpen: true, InspectorOpen: true, PaletteWidth: 240, InspectorWidth: 320),
        _ => new EditorPreset(
            PaletteOpen: false, InspectorOpen: false, PaletteWidth: 240, InspectorWidth: 320),
    };

    public async Task<EditorPreset> GetPresetAsync(
        WorldEngineEntityType entityType, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        return _cache!.TryGetValue(Key(entityType), out EditorPreset? stored)
            ? stored
            : GetDefault(entityType);
    }

    public async Task UpdatePresetAsync(
        WorldEngineEntityType entityType,
        Func<EditorPreset, EditorPreset> update,
        CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        string key = Key(entityType);
        EditorPreset current = _cache!.TryGetValue(key, out EditorPreset? stored)
            ? stored
            : GetDefault(entityType);
        _cache[key] = update(current);
        await PersistAsync(ct);
    }

    private static string Key(WorldEngineEntityType entityType) => entityType.ToString();

    private async Task EnsureLoadedAsync(CancellationToken ct)
    {
        if (_loaded) return;
        _loaded = true;
        _cache = [];

        try
        {
            _module ??= await _js.InvokeAsync<IJSObjectReference>("import", ["./js/splitter.js"]);
            string? json = await _module.InvokeAsync<string>("readJson", [StorageKey]);
            if (!string.IsNullOrWhiteSpace(json))
            {
                Dictionary<string, EditorPreset>? parsed =
                    JsonSerializer.Deserialize<Dictionary<string, EditorPreset>>(json, JsonOptions);
                if (parsed != null)
                {
                    foreach ((string k, EditorPreset v) in parsed)
                        _cache[k] = v;
                }
            }
        }
        catch
        {
            // JS unavailable — defaults stand.
        }
    }

    private async Task PersistAsync(CancellationToken ct)
    {
        try
        {
            if (_module == null) return;
            string json = JsonSerializer.Serialize(_cache, JsonOptions);
            await _module.InvokeVoidAsync("writeJson", [StorageKey, json]);
        }
        catch
        {
            // Persistence best-effort.
        }
    }
}
