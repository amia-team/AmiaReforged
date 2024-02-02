﻿using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.System.Services;

public class SpellbookService
{
    private List<string> ButtonIds { get; set; } = new();

    public SpellbookService()
    {
        NwModule.Instance.OnNuiEvent += OnSpellbookButtonClick;
    }

    private void OnSpellbookButtonClick(ModuleEvents.OnNuiEvent obj)
    {
        bool clickedClassSpellbook = ButtonIds.Contains(obj.ElementId) && obj.EventType != NuiEventType.MouseUp;
        if (!clickedClassSpellbook) return;

        string className = obj.ElementId.Replace("SpellbookButton", "");

        CreatureClassInfo? classInfo =
            obj.Player.LoginCreature?.Classes.Where(c => c.Class.Name.ToString() == className).First();

        IReadOnlyList<MemorizedSpellSlot>? preparedSpells3 = classInfo?.GetMemorizedSpellSlots(3);

        Dictionary<byte, IReadOnlyList<MemorizedSpellSlot>> preparedSpells = new();

        for (byte i = 0; i < 9; i++)
        {
            if (classInfo is null || classInfo.GetMemorizedSpellSlots(i).Count == 0) continue;
            
            preparedSpells.TryAdd(i, classInfo.GetMemorizedSpellSlots(i));
        }

        foreach (KeyValuePair<byte, IReadOnlyList<MemorizedSpellSlot>> spellLevel in preparedSpells)
        {
            foreach (MemorizedSpellSlot spell in spellLevel.Value)
            {
                obj.Player.SendServerMessage(
                    $"Spell level: {spellLevel.Key} Spell name: {spell.Spell.Name} Spell meta: {spell.MetaMagic}",
                    Color.FromRGBA("#FF0000"));
            }
        }

        List<NuiImage> spellLevelIcons = new List<NuiImage>()
        {
            new("ir_level0"),
            new("ir_level1"),
            new("ir_level2"),
            new("ir_level3"),
            new("ir_level4"),
            new("ir_level5"),
            new("ir_level6"),
            new("ir_level789"),
            new("ir_level789"),
            new("ir_level789")
        };
        
        
    }

    public void OpenSpellbookWindow(ModuleEvents.OnNuiEvent obj)
    {
        List<string> classNames = new();

        foreach (CreatureClassInfo charClassInfo in obj.Player.LoginCreature?.Classes!)
        {
            if (charClassInfo.Class.IsSpellbookRestricted)
                classNames.Add(charClassInfo.Class.Name.ToString());

            if (charClassInfo.Class.HasDomains)
                classNames.Add(charClassInfo.Class.Name.ToString());
        }

        bool hasNoCasterClasses = classNames.Count is 0;
        if (hasNoCasterClasses)
        {
            obj.Player.SendServerMessage("You have no spellbooks to view.", Color.FromRGBA("#FF0000"));
            return;
        }

        List<NuiButton> classButtons = new List<NuiButton>();
        NuiRow classes = new NuiRow()
        {
            Children = new List<NuiElement>(classButtons),
        };

        foreach (string className in classNames)
        {
            string id = $"{className}SpellbookButton";

            NuiButton classButton = new NuiButton(className)
            {
                Tooltip = $"Click here to see your {className} spellbooks.",
                Enabled = true,
                Id = id
            };

            classButtons.Add(classButton);
            ButtonIds?.Add(id);
        }


        NuiColumn root = new NuiColumn()
        {
            Children = new List<NuiElement>(classButtons)
        };

        NuiWindow window = new(root, "Spellbooks")
        {
            Closable = true,
            Geometry = new NuiRect(500f, 100f, 300f, 400f)
        };

        window.Id = "spellbookWindow";

        obj.Player.TryCreateNuiWindow(window, out NuiWindowToken token);

    }
}