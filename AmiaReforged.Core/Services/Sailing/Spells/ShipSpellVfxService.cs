using AmiaReforged.Core.Models.Sailing;
using Anvil.API;
using Anvil.Services;
using NLog;


namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(ShipSpellVfxService))]
public sealed class ShipSpellVfxService
{
    private static readonly Logger Log =
        LogManager.GetCurrentClassLogger();
    private readonly Action<ShipState, VfxType> _impact;
    private readonly Action<int, float, float> _animation;

    public ShipSpellVfxService()
    {
        _impact = PlaySpellVfx;
        _animation = NWN.Core.NWScript.ActionPlayAnimation;
        //Log.Info(
            //"Ship Spell VFX Service initialized.");
    }

    // Native emission boundary, also used by managed interaction regressions.
    private ShipSpellVfxService(Action<ShipState, VfxType> impact, Action<int, float, float> animation)
    { _impact = impact; _animation = animation; }

    // -------------------------------------------------------------
    // Fireball
    // -------------------------------------------------------------

    public void PlayFireball(
        ShipState targetShip)
    {
        _impact(
            targetShip,
            VfxType.ImpFlameM);
    }

    // -------------------------------------------------------------
    // Lightning Bolt
    // -------------------------------------------------------------

    public void PlayLightningBolt(
        ShipState targetShip)
    {
        _impact(
            targetShip,
            VfxType.ImpLightningM);
    }


    // -------------------------------------------------------------
    // Target Ship VFX
    // -------------------------------------------------------------

    private static void PlaySpellVfx(
        ShipState targetShip,
        VfxType vfx)
    {
        Location? DeckImpact()
        {
            NwArea? deck = NwModule.Instance.Areas.FirstOrDefault(area => area.IsValid &&
                string.Equals(area.ResRef, targetShip.DeckAreaResRef, StringComparison.OrdinalIgnoreCase));
            if (deck == null) return null;
            return NwObject.FindObjectsWithTag<NwWaypoint>("SAILING_DECK_SPAWN")
                .FirstOrDefault(w => w.IsValid && w.Area == deck)?.Location;
        }
        Location? RepresentationImpact()
        {
            if (string.IsNullOrWhiteSpace(targetShip.RepresentationTag)) return null;
            var representations = NwObject.FindObjectsWithTag<NwPlaceable>(targetShip.RepresentationTag)
                .Where(p => p.IsValid && p.Area is { IsValid: true } area &&
                    string.Equals(area.ResRef, targetShip.AreaResRef, StringComparison.OrdinalIgnoreCase)).ToArray();
            return representations.Length == 1 ? representations[0].Location : null;
        }
        // Preserve existing interior VFX for deck occupants. Abstract NPCs can
        // instead emit at their actual ocean representation, without invented coordinates.
        Location? impact = ResolveImpact(DeckImpact, RepresentationImpact);
        if (impact == null)
        { Log.Warn("Ship spell VFX unavailable: ship={0}, reason=no usable representation or deck spawn.", targetShip.ShipId); return; }
        impact.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(vfx));
        Log.Info("Ship spell VFX emitted: ship={0}, effect={1}, area={2}.", targetShip.ShipId, vfx, impact.Area.ResRef);
    }

    private static T? ResolveImpact<T>(Func<T?> deck, Func<T?> representation) where T : class => deck() ?? representation();



public void PlayCasterSpellVfx(
    NwCreature caster,
    NwSpell spell)
{
    if (!caster.IsValid)
        return;

    //Log.Info(
        //$"CASTER VFX BEFORE ASSIGN: " +
        //$"Caster={caster.Name}, " +
        //$"Spell={spell.Name}");

    PlayCasterAnimation(action => NWN.Core.NWScript.AssignCommand(caster, action));
}

private void PlayCasterAnimation(Action<System.Action> assign)
{
    assign(() =>
    {
        //Log.Info(
            //$"CASTER VFX INSIDE ASSIGN: " +
            //$"Caster={caster.Name}, " +
            //$"Spell={spell.Name}");

        _animation(
            NWN.Core.NWScript.ANIMATION_LOOPING_CONJURE1,
            1.0f,
            2.0f);
    });
}
}
