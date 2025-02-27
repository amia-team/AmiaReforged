using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Techniques.Spirit;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.Monk.Services;

// [ServiceBinding(typeof(SpiritTechniqueHandler))]
public class SpiritTechniqueHandler
{
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();

  public SpiritTechniqueHandler()
  {
    // Register method to listen for the OnSpellCast event.
    NwModule.Instance.OnSpellCast += CastSpiritTechnique;
    Log.Info("Monk Spirit Technique Handler initialized.");
  }

  private void CastSpiritTechnique(OnSpellCast castData)
  {
    if (castData.Caster is not NwCreature monk) return;
    if (castData.Spell?.FeatReference is null) return;
    if (monk.GetClassInfo(ClassType.Monk) is null) return;
  
    NwFeat spiritKiPointFeat = NwFeat.FromFeatId(MonkFeat.SpiritKiPoint)!;

    int technique = castData.Spell.FeatReference.Id;
    bool isSpiritTechnique = technique is MonkFeat.KiShout or MonkFeat.QuiveringPalm;
    if (!isSpiritTechnique) return;

    if (!monk.KnowsFeat(spiritKiPointFeat) || monk.GetFeatRemainingUses(spiritKiPointFeat) < 1)
    {
      castData.PreventSpellCast = true;
      if (monk.IsPlayerControlled(out NwPlayer? player)) player.SendServerMessage
          ($"Cannot use {castData.Spell.FeatReference.Name} because your spirit ki is depleted.", MonkColors.MonkColorScheme);
      return;
    }
    
    switch (technique)
    {
      case MonkFeat.KiShout :
        KiShout.CastKiShout(castData);
        break;
      case MonkFeat.QuiveringPalm :
        QuiveringPalm.CastQuiveringPalm(castData);
        break;
    }

    monk.DecrementRemainingFeatUses(spiritKiPointFeat);
  }
}