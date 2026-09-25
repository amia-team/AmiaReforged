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

    public ShipSpellVfxService()
    {
        //Log.Info(
            //"Ship Spell VFX Service initialized.");
    }

    // -------------------------------------------------------------
    // Fireball
    // -------------------------------------------------------------

    public void PlayFireball(
        ShipState targetShip)
    {
        PlaySpellVfx(
            targetShip,
            VfxType.ImpFlameM);
    }

    // -------------------------------------------------------------
    // Lightning Bolt
    // -------------------------------------------------------------

    public void PlayLightningBolt(
        ShipState targetShip)
    {
        PlaySpellVfx(
            targetShip,
            VfxType.ImpLightningM);
    }


    // -------------------------------------------------------------
    // Target Ship VFX
    // -------------------------------------------------------------

    private void PlaySpellVfx(
        ShipState targetShip,
        VfxType vfx)
    {
        NwArea? deckArea =
            NwModule.Instance.Areas.FirstOrDefault(
                area =>
                    string.Equals(
                        area.ResRef,
                        targetShip.DeckAreaResRef,
                        StringComparison.OrdinalIgnoreCase));

        if (deckArea == null)
        {
            Log.Warn(
                $"Cannot play ship spell VFX: " +
                $"Deck area '{targetShip.DeckAreaResRef}' " +
                $"was not found for ship " +
                $"'{targetShip.ShipName}'.");

            return;
        }

        Location location =
            Location.Create(
                deckArea,
                new System.Numerics.Vector3(
                    42.0f,
                    42.0f,
                    0.0f),
                0.0f);

        //Log.Info(
            //$"Playing ship spell VFX: " +
            //$"VFX={vfx}, " +
            //$"Ship={targetShip.ShipName}, " +
            //$"Deck={targetShip.DeckAreaResRef}, " +
            //$"Location=(42,42,0).");

        // VFX implementation goes here.
    }



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

    NWN.Core.NWScript.AssignCommand(caster, () =>
    {
        //Log.Info(
            //$"CASTER VFX INSIDE ASSIGN: " +
            //$"Caster={caster.Name}, " +
            //$"Spell={spell.Name}");

        NWN.Core.NWScript.ActionPlayAnimation(
            NWN.Core.NWScript.ANIMATION_LOOPING_CONJURE1,
            1.0f,
            2.0f);
    });
}
}
