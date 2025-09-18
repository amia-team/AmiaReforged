using AmiaReforged.Core.UserInterface;
using Anvil.API;
using NLog;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.Spellbook.OpenSpellbook;

public sealed class SpellbookMemorizer
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly NwPlayer _player;
    private readonly SpellbookViewModel _spellbook;

    public SpellbookMemorizer(SpellbookViewModel spellbook, NwPlayer player)
    {
        _spellbook = spellbook;
        _player = player;
    }


    public void MemorizeSpellsToPlayer()
    {
        int classId = int.Parse(_spellbook.Class);
        if (_player.LoginCreature == null) return;

        NwCreature playerCreature = _player.LoginCreature;

        if (playerCreature.Classes.All(c => c.Class.Id != classId)) return;
        CreatureClassInfo classInfo = playerCreature.Classes.First(c => c.Class.Id == classId);


        for (byte i = 0; i <= 9; i++)
        {
            foreach (MemorizedSpellSlot spellSlot in classInfo.GetMemorizedSpellSlots(i))
            {
                spellSlot.ClearMemorizedSpell();
            }

            Log.Info(message: "Cleared spell slots.");
        }

        for (byte spellLevel = 0; spellLevel <= 9; spellLevel++)
        {
            Log.Info($"Memorizing spells for level {(int)spellLevel}");

            IReadOnlyList<PreparedSpellModel>? spells = _spellbook.SpellBook?[spellLevel];

            for (int spellSlot = 0; spellSlot < classInfo.GetMemorizedSpellSlots(spellLevel).Count; spellSlot++)
            {
                if (spells == null) continue;
                if (spellSlot > spells.Count)
                {
                    Log.Info(message: "No more spells to memorize.");
                    break;
                }
                
                PreparedSpellModel? spell = spells.ElementAtOrDefault(spellSlot);
                
                if (spell == null) continue;
                
                MemorizedSpellSlot memorized = classInfo.GetMemorizedSpellSlots(spellLevel)[spellSlot];
                if (!spell.IsPopulated) continue;

                NwSpell? currentSpell = NwSpell.FromSpellId(spell.SpellId);

                if (currentSpell == null)
                {
                    Log.Info(message: "Spell is not a valid spell.");
                    continue;
                }

                memorized.Spell = currentSpell;
                memorized.MetaMagic = spell.MetaMagic;
                memorized.IsDomainSpell = spell.IsDomainSpell;
                memorized.IsReady = false;
            }
        }
    }
}