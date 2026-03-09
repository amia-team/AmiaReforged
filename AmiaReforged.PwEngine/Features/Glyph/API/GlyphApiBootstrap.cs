using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Integration;
using AmiaReforged.PwEngine.Features.Glyph.Persistence;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.Glyph.API;

/// <summary>
/// Bootstraps the Glyph API by setting static service references on the controller.
/// The controller uses static methods (required by the route table's reflection-based
/// discovery), so dependencies must be provided via static fields.
/// </summary>
[ServiceBinding(typeof(GlyphApiBootstrap))]
public class GlyphApiBootstrap
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public GlyphApiBootstrap(
        IGlyphRepository repository,
        IGlyphNodeDefinitionRegistry nodeRegistry,
        GlyphEncounterHookService encounterHooks,
        GlyphTraitHookService traitHooks,
        GlyphInteractionHookService interactionHooks)
    {
        GlyphController.Repository = repository;
        GlyphController.NodeRegistry = nodeRegistry;
        GlyphController.EncounterHooks = encounterHooks;
        GlyphController.TraitHooks = traitHooks;
        GlyphController.InteractionHooks = interactionHooks;

        Log.Info("Glyph API bootstrap complete — controller wired to services.");
    }
}
