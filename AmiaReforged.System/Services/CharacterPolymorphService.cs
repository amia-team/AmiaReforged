using AmiaReforged.Core.UserInterface;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NLog.Fluent;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.System.Services;

[ServiceBinding(typeof(CharacterPolymorphService))]
public class CharacterPolymorphService
{
    private Dictionary<string, List<ClassPreparedSpells>?> SpellBooks { get; } = new();
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private const string Shapechange = "shapechange";
    private const string Polymorph = "polymorph";

    public CharacterPolymorphService(EventService eventService)
    {
        Log.Info("CharacterPolymorphService initialized.");
        Log.Info("Subscribing to polymorph events.");


        Action<OnSpellCast> spellCast = OnSpellCast;
        eventService.SubscribeAll<OnSpellCast, OnSpellCast.Factory>(spellCast, EventCallbackType.Before);
        EventsPlugin.SubscribeEvent(EventsPlugin.NWNX_ON_POLYMORPH_BEFORE, "polymorph_before");
        EventsPlugin.SubscribeEvent(EventsPlugin.NWNX_ON_UNPOLYMORPH_AFTER, "unpolymorph_afte");
    }

    private void OnSpellCast(OnSpellCast obj)
    {
        if (!obj.Caster.IsPlayerControlled(out NwPlayer? player)) return;
        if (obj.Spell?.MasterSpell is null) return;

        bool isPolymorphSelf = obj.Spell.MasterSpell.Name.ToString().ToLower().Contains(Polymorph);
        bool isShapeChange = obj.Spell.MasterSpell.Name.ToString().ToLower().Contains(Shapechange);

        Log.Info($"{obj.Spell.Name} is a polymorph spell: {isPolymorphSelf} or a shapechange spell: {isShapeChange}");

        if (isPolymorphSelf || isShapeChange)
        {

            List<ClassPreparedSpells> spellBooks = new();
            foreach (CreatureClassInfo classInfo in player.LoginCreature.Classes)
            {
                Log.Info($"Spell Event: Checking class info {classInfo.Class.NameLower.ToString()}");
                if (!classInfo.Class.IsSpellCaster) continue;
                if (classInfo.KnownSpells.Count <= 0) continue;
                if (classInfo.Class.HasMemorizedSpells) continue;
                Log.Info($"Spell Cast Event: Saving spells for {player.LoginCreature.Name}.");
                spellBooks.Add(SaveInnateSpells(classInfo));
            }

            if (spellBooks.Count == 0) return;
            SpellBooks.TryAdd(player.CDKey, spellBooks);
        }
    }


    [ScriptHandler("polymorph_before")]
    private void OnPolymorphBefore(CallInfo info)
    {
        if (!info.ObjectSelf.IsPlayerControlled(out NwPlayer? player)) return;

        if (player.LoginCreature != null && !player.LoginCreature.Classes.Any(c => c.Class.HasMemorizedSpells)) return;

        SaveMemorizedSpells(player);
    }

    private void SaveMemorizedSpells(NwPlayer player)
    {
        if (player.LoginCreature == null) return;
        Log.Info($"Saving spells for {player.LoginCreature.Name}.");
        List<ClassPreparedSpells> spellBooks = new();

        foreach (CreatureClassInfo classInfo in player.LoginCreature.Classes)
        {
            if (!classInfo.Class.IsSpellCaster) continue;
            if (classInfo.KnownSpells.Count <= 0) continue;
            if (!classInfo.Class.HasMemorizedSpells) continue;

            ClassPreparedSpells classPreparedSpells = SaveSpellbook(classInfo);
            Log.Info($"Saving spellbook for {classPreparedSpells.Class}.");

            spellBooks.Add(classPreparedSpells);
        }

        if (spellBooks.Count == 0) return;
        SpellBooks.TryAdd(player.CDKey, spellBooks);
    }


    private static ClassPreparedSpells SaveSpellbook(CreatureClassInfo classInfo)
    {
        Log.Info("Saving memorized");
        ClassPreparedSpells classPreparedSpells = new()
        {
            Class = classInfo.Class.NameLower.ToString()
        };

        for (byte i = 0; i <= 9; i++)
        {
            PreparedSpellModel[] preparedSpells = classInfo.GetMemorizedSpellSlots(i)
                .Where(x => x.IsPopulated)
                .Select(x => new PreparedSpellModel
                {
                    SpellId = x.Spell.Id,
                    IsReady = x.IsReady,
                    IsDomainSpell = x.IsDomainSpell,
                    MetaMagic = x.MetaMagic
                }).ToArray();

            classPreparedSpells.Spells.TryAdd(i, preparedSpells);
        }

        return classPreparedSpells;
    }

    private ClassPreparedSpells SaveInnateSpells(CreatureClassInfo classInfo)
    {
        Log.Info("saving innate spellslslsls");
        Dictionary<byte, byte> spellUses = new();
        for (byte i = 0; i <= 9; i++)
        {
            byte remainingSpellUses = classInfo.GetRemainingSpellSlots(i);
            if (remainingSpellUses == 0) continue;

            Log.Info($"Saving {remainingSpellUses} uses of level {i} spells.");

            spellUses.TryAdd(i, remainingSpellUses);
        }

        ClassPreparedSpells classPreparedSpells = new()
        {
            Class = classInfo.Class.NameLower.ToString(),
            IsInnate = true,
            InnateSpellUses = spellUses
        };

        return classPreparedSpells;
    }

    [ScriptHandler("unpolymorph_afte")]
    private void OnUnpolymorphAfter(CallInfo info)
    {
        if (!info.ObjectSelf.IsPlayerControlled(out NwPlayer? player)) return;

        NWScript.DelayCommand(0.6f, () => RestoreSpells(player));
    }

    private void RestoreSpells(NwPlayer player)
    {
        if (player.LoginCreature == null) return;

        if (!SpellBooks.TryGetValue(player.CDKey, out List<ClassPreparedSpells>? spellBooks)) return;
        if (spellBooks == null) return;

        foreach (ClassPreparedSpells book in spellBooks)
        {
            CreatureClassInfo? classInfo =
                player.LoginCreature.Classes.FirstOrDefault(x => x.Class.NameLower.ToString() == book.Class);
            if (classInfo == null) continue;
            if (classInfo.Class.HasMemorizedSpells)
            {
                Log.Info($"{classInfo.Class.NameLower} Has memorized spells.");
                HandleSpellbookUsers(book, classInfo);
            }
            else
            {
                Log.Info($"{classInfo.Class.NameLower} Has innate spells.");
                HandleInnateMagicUsers(book, classInfo);
            }
        }

        SpellBooks.Remove(player.CDKey);
    }

    private void HandleInnateMagicUsers(ClassPreparedSpells book, CreatureClassInfo classInfo)
    {
        for (byte i = 0; i <= 9; i++)
        {
            if (!book.InnateSpellUses.TryGetValue(i, out byte remainingSpellUses)) continue;
            classInfo.SetRemainingSpellSlots(i, remainingSpellUses);
        }
    }

    private static void HandleSpellbookUsers(ClassPreparedSpells book, CreatureClassInfo classInfo)
    {
        for (byte i = 0; i <= 9; i++)
        {
            IReadOnlyList<PreparedSpellModel> spellSlots = book.Spells[i];
            RestoreBonusSpellSlots(classInfo, i, spellSlots);
        }
    }

    private static void RestoreBonusSpellSlots(CreatureClassInfo classInfo, byte i,
        IReadOnlyList<PreparedSpellModel> spellSlots)
    {
        for (int j = 0; j < classInfo.GetMemorizedSpellSlots(i).Count; j++)
        {
            MemorizedSpellSlot memorized = classInfo.GetMemorizedSpellSlots(i)[j];

            if (!memorized.IsPopulated) continue;
            NwSpell? currentSpell = NwSpell.FromSpellId(spellSlots[j].SpellId);

            if (currentSpell == null) continue;
            if (memorized.IsReady) continue;
            if (memorized.Spell.Id != currentSpell.Id) continue;

            memorized.IsReady = spellSlots[j].IsReady;
        }
    }
}