using Anvil.API;
using Anvil.Services;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.Module.DeveloperTools;

/// <summary>
/// Generates randomized appearances and names for NPC citizens.
/// Handles the "jes_randomname" script for creature spawn customization.
/// </summary>
[ServiceBinding(typeof(CitizenGenerator))]
public class CitizenGenerator
{
    private static readonly Random Random = new();

    [ScriptHandler("jes_randomname")]
    public void RandomizeCitizen(CallInfo callInfo)
    {
        NwCreature? npc = callInfo.ObjectSelf as NwCreature;
        if (npc == null || !npc.IsValid) return;

        int shouldSit = NWScript.GetLocalInt(npc, "sit");

        // Execute sit script if needed
        if (shouldSit == 1)
        {
            NWScript.ExecuteScript("spawn_sit", npc);
        }

        // Full randomization for generic commoners
        if (npc.Tag == "amia_npc_commoner")
        {
            RandomizeCommoner(npc);
        }
        else
        {
            // Named NPCs - apply customizations based on variables
            RandomizeNamedNpc(npc);
        }
    }

    private void RandomizeCommoner(NwCreature npc)
    {
        // Randomize basic properties
        int randomGender = Random.Next(2);
        int randomRace = GetRandomRace(npc);

        // Set sound to silence
        npc.SoundSet = 9999;

        // Set gender and portrait
        Gender gender = randomGender == 0 ? Gender.Male : Gender.Female;
        npc.Gender = gender;
        npc.PortraitResRef = gender == Gender.Male ? "po_clsrogue_" : "po_clsroguef_";

        // Set race and appearance
        int racialTypeId = GetRacialTypeId(randomRace);
        CreaturePlugin.SetRacialType(npc, racialTypeId);

        // Drow use elf appearance (appearance type 1)
        int appearanceType = racialTypeId == 33 ? 1 : randomRace;
        NWScript.SetCreatureAppearanceType(npc, appearanceType);

        // Randomize colors (with Drow override)
        RandomizeColors(npc, racialTypeId);

        // Randomize head
        RandomizeHead(npc);

        // Randomize armor appearance
        RandomizeArmorAppearance(npc);

        // Set random first name only
        string firstName = GetRandomFirstName(racialTypeId, gender);

        // Only add last name if the variable is set
        int addLastName = NWScript.GetLocalInt(npc, "last_name");
        if (addLastName == 1)
        {
            string lastName = GetRandomLastName(racialTypeId);
            npc.Name = $"{firstName} {lastName}";
        }
        else
        {
            npc.Name = firstName;
        }

        // Heal to full health to prevent injury from CON changes
        npc.HP = npc.MaxHP;
    }

    private void RandomizeNamedNpc(NwCreature npc)
    {
        // Check customization variables
        int randomizeGender = NWScript.GetLocalInt(npc, "gender");
        int randomizeRace = NWScript.GetLocalInt(npc, "race");
        int noHeadChange = NWScript.GetLocalInt(npc, "no_headchange");
        int noPortraitChange = NWScript.GetLocalInt(npc, "no_portraitchange");
        int randomizeRobe = NWScript.GetLocalInt(npc, "robe");
        int randomizeRobeColors = NWScript.GetLocalInt(npc, "robe_colors");

        Gender gender = npc.Gender;
        int currentRaceId = npc.Race.Id;

        // Randomize gender if requested
        if (randomizeGender == 1)
        {
            int randomGender = Random.Next(2);
            gender = randomGender == 0 ? Gender.Male : Gender.Female;
            npc.Gender = gender;

            // Only change portrait if not disabled
            if (noPortraitChange != 1)
            {
                npc.PortraitResRef = gender == Gender.Male ? "po_clsrogue_" : "po_clsroguef_";
            }
        }

        // Randomize race if requested
        if (randomizeRace == 1)
        {
            int randomRace = GetRandomRace(npc);
            int racialTypeId = GetRacialTypeId(randomRace);
            CreaturePlugin.SetRacialType(npc, racialTypeId);

            // Drow use elf appearance (appearance type 1)
            int appearanceType = racialTypeId == 33 ? 1 : randomRace;
            NWScript.SetCreatureAppearanceType(npc, appearanceType);
            currentRaceId = racialTypeId;
        }

        // Always randomize hair and skin colors
        RandomizeColors(npc, currentRaceId);

        // Randomize head unless disabled
        if (noHeadChange != 1)
        {
            RandomizeHead(npc);
        }

        // Randomize robe if requested
        if (randomizeRobe == 1)
        {
            RandomizeArmorModel(npc);
        }

        // Randomize robe colors if requested
        if (randomizeRobeColors == 1)
        {
            RandomizeArmorColors(npc);
        }

        // Always add a random last name based on current race
        AddRandomSurname(npc, currentRaceId);

        // Heal to full health to prevent injury from CON changes
        npc.HP = npc.MaxHP;
    }

    private void RandomizeColors(NwCreature npc, int racialTypeId = -1)
    {
        // Drow get specific colors
        if (racialTypeId == 33) // Drow racial type
        {
            npc.SetColor(ColorChannel.Skin, 29); // Drow skin color
            npc.SetColor(ColorChannel.Hair, 16); // Pure white hair
        }
        else
        {
            // Hair colors
            int[] hairColors = { 15, 23, 17, 10, 7 }; // Brown, Black, White, Blonde, Red
            int randomHairColor = hairColors[Random.Next(hairColors.Length)];
            npc.SetColor(ColorChannel.Hair, randomHairColor);

            // Skin colors
            int[] skinColors = { 0, 2, 4, 7, 12 };
            int randomSkinColor = skinColors[Random.Next(skinColors.Length)];
            npc.SetColor(ColorChannel.Skin, randomSkinColor);
        }
    }

    private void RandomizeArmorAppearance(NwCreature npc)
    {
        RandomizeArmorModel(npc);
        RandomizeArmorColors(npc);
    }

    private void RandomizeArmorModel(NwCreature npc)
    {
        NwItem? armor = npc.GetItemInSlot(InventorySlot.Chest);
        if (armor == null || !armor.IsValid) return;

        // Robe models
        int[] robeModels = { 3, 4, 20, 55, 114, 186, 202, 221, 235, 247 };
        int randomRobe = robeModels[Random.Next(robeModels.Length)];

        // Set robe model using Anvil API
        armor.Appearance.SetArmorModel(CreaturePart.Robe, (byte)randomRobe);

        // Clone and re-equip to apply changes
        npc.RunUnequip(armor);
        NwItem newArmor = armor.Clone(npc);

        if (newArmor.IsValid)
        {
            npc.RunEquip(newArmor, InventorySlot.Chest);
            armor.Destroy();
        }
        else
        {
            npc.RunEquip(armor, InventorySlot.Chest);
        }
    }

    private void RandomizeArmorColors(NwCreature npc)
    {
        NwItem? armor = npc.GetItemInSlot(InventorySlot.Chest);
        if (armor == null || !armor.IsValid) return;

        // Color combinations (cloth1/cloth2, leather1/leather2)
        (int cloth, int leather)[] colorCombos =
        {
            (22, 132), (126, 124), (21, 83), (22, 77),
            (125, 64), (81, 77), (65, 74)
        };
        var randomColors = colorCombos[Random.Next(colorCombos.Length)];

        // Set robe colors using Anvil API
        armor.Appearance.SetArmorPieceColor(CreaturePart.Robe, ItemAppearanceArmorColor.Cloth1, (byte)randomColors.cloth);
        armor.Appearance.SetArmorPieceColor(CreaturePart.Robe, ItemAppearanceArmorColor.Cloth2, (byte)randomColors.cloth);
        armor.Appearance.SetArmorPieceColor(CreaturePart.Robe, ItemAppearanceArmorColor.Leather1, (byte)randomColors.leather);
        armor.Appearance.SetArmorPieceColor(CreaturePart.Robe, ItemAppearanceArmorColor.Leather2, (byte)randomColors.leather);

        // Clone and re-equip to apply changes
        npc.RunUnequip(armor);
        NwItem newArmor = armor.Clone(npc);

        if (newArmor.IsValid)
        {
            npc.RunEquip(newArmor, InventorySlot.Chest);
            armor.Destroy();
        }
        else
        {
            npc.RunEquip(armor, InventorySlot.Chest);
        }
    }

    private int GetRandomRace(NwCreature npc)
    {
        // Check if there's a single allowed race
        int allowedRace = NWScript.GetLocalInt(npc, "allow");
        if (allowedRace > 0) // 0 would be dwarf, so we check > 0 to ensure it's intentionally set
        {
            return GetRaceIndex(allowedRace);
        }

        int maxAttempts = 100; // Prevent infinite loops
        int attempts = 0;

        while (attempts < maxAttempts)
        {
            int randomRace = Random.Next(8); // Now includes Drow (0-7)
            int racialTypeId = GetRacialTypeId(randomRace);

            if (IsRaceAllowed(npc, racialTypeId))
            {
                return randomRace;
            }

            attempts++;
        }

        // If we couldn't find an allowed race, use default or current race
        int defaultRace = NWScript.GetLocalInt(npc, "default");
        if (defaultRace > 0)
        {
            return GetRaceIndex(defaultRace);
        }

        // Fall back to human
        return 6;
    }

    private bool IsRaceAllowed(NwCreature npc, int racialTypeId)
    {
        // Check for single ban
        int bannedRace = NWScript.GetLocalInt(npc, "ban");
        if (bannedRace > 0 && bannedRace == racialTypeId)
        {
            return false;
        }

        // Check for multiple bans
        int banCount = NWScript.GetLocalInt(npc, "ban_count");
        if (banCount > 0 && banCount < 8) // Updated to 8 races
        {
            for (int i = 1; i <= banCount; i++)
            {
                int bannedRaceId = NWScript.GetLocalInt(npc, $"ban_{i}");
                if (bannedRaceId > 0 && bannedRaceId == racialTypeId)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private int GetRacialTypeId(int raceIndex)
    {
        return raceIndex switch
        {
            0 => NWScript.RACIAL_TYPE_DWARF,
            1 => NWScript.RACIAL_TYPE_ELF,
            2 => NWScript.RACIAL_TYPE_GNOME,
            3 => NWScript.RACIAL_TYPE_HALFLING,
            4 => NWScript.RACIAL_TYPE_HALFELF,
            5 => NWScript.RACIAL_TYPE_HALFORC,
            6 => NWScript.RACIAL_TYPE_HUMAN,
            7 => 33, // Drow
            _ => NWScript.RACIAL_TYPE_HUMAN
        };
    }

    private int GetRaceIndex(int racialTypeId)
    {
        return racialTypeId switch
        {
            NWScript.RACIAL_TYPE_DWARF => 0,
            NWScript.RACIAL_TYPE_ELF => 1,
            NWScript.RACIAL_TYPE_GNOME => 2,
            NWScript.RACIAL_TYPE_HALFLING => 3,
            NWScript.RACIAL_TYPE_HALFELF => 4,
            NWScript.RACIAL_TYPE_HALFORC => 5,
            NWScript.RACIAL_TYPE_HUMAN => 6,
            33 => 7, // Drow
            _ => 6
        };
    }

    private void AddRandomSurname(NwCreature npc, int racialType)
    {
        string lastName = GetRandomLastName(racialType);

        if (!string.IsNullOrEmpty(lastName))
        {
            npc.Name = $"{npc.Name} {lastName}";
        }
    }

    private string GetRandomFirstName(int racialType, Gender gender)
    {
        string[] namePool = racialType switch
        {
            NWScript.RACIAL_TYPE_HUMAN => gender == Gender.Male
                ? CitizenNameData.HumanMaleFirstNames
                : CitizenNameData.HumanFemaleFirstNames,
            NWScript.RACIAL_TYPE_ELF => gender == Gender.Male
                ? CitizenNameData.ElfMaleFirstNames
                : CitizenNameData.ElfFemaleFirstNames,
            NWScript.RACIAL_TYPE_DWARF => gender == Gender.Male
                ? CitizenNameData.DwarfMaleFirstNames
                : CitizenNameData.DwarfFemaleFirstNames,
            NWScript.RACIAL_TYPE_GNOME => gender == Gender.Male
                ? CitizenNameData.GnomeMaleFirstNames
                : CitizenNameData.GnomeFemaleFirstNames,
            NWScript.RACIAL_TYPE_HALFELF => gender == Gender.Male
                ? CitizenNameData.HalfElfMaleFirstNames
                : CitizenNameData.HalfElfFemaleFirstNames,
            NWScript.RACIAL_TYPE_HALFLING => gender == Gender.Male
                ? CitizenNameData.HalflingMaleFirstNames
                : CitizenNameData.HalflingFemaleFirstNames,
            NWScript.RACIAL_TYPE_HALFORC => gender == Gender.Male
                ? CitizenNameData.HalfOrcMaleFirstNames
                : CitizenNameData.HalfOrcFemaleFirstNames,
            33 => gender == Gender.Male // Drow
                ? CitizenNameData.DrowMaleFirstNames
                : CitizenNameData.DrowFemaleFirstNames,
            _ => CitizenNameData.HumanMaleFirstNames
        };

        return namePool[Random.Next(namePool.Length)];
    }

    private string GetRandomLastName(int racialType)
    {
        string[] namePool = racialType switch
        {
            NWScript.RACIAL_TYPE_HUMAN => CitizenNameData.HumanLastNames,
            NWScript.RACIAL_TYPE_ELF => CitizenNameData.ElfLastNames,
            NWScript.RACIAL_TYPE_DWARF => CitizenNameData.DwarfLastNames,
            NWScript.RACIAL_TYPE_GNOME => CitizenNameData.GnomeLastNames,
            NWScript.RACIAL_TYPE_HALFELF => CitizenNameData.HalfElfLastNames,
            NWScript.RACIAL_TYPE_HALFLING => CitizenNameData.HalflingLastNames,
            NWScript.RACIAL_TYPE_HALFORC => CitizenNameData.HalfOrcLastNames,
            33 => CitizenNameData.DrowLastNames, // Drow
            _ => CitizenNameData.HumanLastNames
        };

        return namePool[Random.Next(namePool.Length)];
    }

    private void RandomizeHead(NwCreature npc)
    {
        // Get the appropriate head range for this race/gender
        (int minHead, int maxHead) = GetHeadRange(npc);

        // Start with a random head within the valid range
        int startHead = Random.Next(minHead, maxHead + 1);
        int headNumber = FindValidHead(npc, startHead, minHead, maxHead);

        // Validate the head is within the expected range before applying
        if (headNumber > 0 && headNumber >= minHead && headNumber <= maxHead)
        {
            npc.SetCreatureBodyPart(CreaturePart.Head, headNumber);
        }
    }

    private (int min, int max) GetHeadRange(NwCreature npc)
    {
        string prefix = GetHeadPrefix(npc);

        return prefix switch
        {
            "pmd" => (1, 32),    // Male Dwarf
            "pfd" => (1, 27),    // Female Dwarf
            "pme" => (1, 49),    // Male Elf (also Drow)
            "pfe" => (1, 141),   // Female Elf (also Drow)
            "pma" => (1, 85),    // Male Gnome/Halfling
            "pfa" => (1, 59),    // Female Gnome/Halfling
            "pmo" => (1, 44),    // Male Half-Orc
            "pfo" => (1, 22),    // Female Half-Orc
            "pmh" => (1, 239),   // Male Human/Half-Elf
            "pfh" => (1, 213),   // Female Human/Half-Elf
            _ => (1, 50)         // Default fallback
        };
    }

    private int FindValidHead(NwCreature npc, int startHead, int minHead, int maxHead)
    {
        // Try all heads in range, going DOWN from the random start point
        int range = maxHead - minHead + 1;

        for (int i = 0; i < range; i++)
        {
            int testHead = startHead - i;

            // Wrap around within the valid range if we go below minimum
            if (testHead < minHead)
            {
                testHead = maxHead - (minHead - testHead - 1);
            }

            if (IsValidHeadModel(npc, testHead))
            {
                return testHead;
            }
        }

        // Fallback to minimum head if nothing found
        return minHead;
    }

    private bool IsValidHeadModel(NwCreature npc, int modelNumber)
    {
        if (modelNumber < 1) return false;

        string prefix = GetHeadPrefix(npc);
        int phenotype = GetPhenotype(npc); // This normalizes to 0 or 2

        // Get the valid range for this race/gender and reject heads outside it
        (int minHead, int maxHead) = GetHeadRange(npc);
        if (modelNumber < minHead || modelNumber > maxHead)
        {
            return false;
        }

        // Check if head is in the primary blocked list for this race/gender
        if (BlockedHeads.TryGetValue(prefix, out HashSet<int>? blockedSet))
        {
            if (blockedSet.Contains(modelNumber))
            {
                return false;
            }
        }

        // Check if head is in the phenotype 0 blocked list (if character is phenotype 0)
        if (phenotype == 0 && Phenotype0BlockedHeads.TryGetValue(prefix, out HashSet<int>? phenotype0BlockedSet))
        {
            if (phenotype0BlockedSet.Contains(modelNumber))
            {
                return false;
            }
        }

        // Check if head is in the phenotype 2 blocked list (if character is phenotype 2)
        if (phenotype == 2 && Phenotype2BlockedHeads.TryGetValue(prefix, out HashSet<int>? phenotype2BlockedSet))
        {
            if (phenotype2BlockedSet.Contains(modelNumber))
            {
                return false;
            }
        }

        // Check if the head model file exists using ResourceManager
        // Use the normalized phenotype (0 or 2) for the model reference
        string modelResRef = $"{prefix}{phenotype}_head{modelNumber:D3}";
        string alias = NWScript.ResManGetAliasFor(modelResRef, NWScript.RESTYPE_MDL);

        return !string.IsNullOrEmpty(alias);
    }

    private string GetHeadPrefix(NwCreature npc)
    {
        string genderLetter = npc.Gender == Gender.Female ? "f" : "m";

        // First, try to determine race from racial type (handles mounted creatures correctly)
        int racialType = npc.Race.Id;

        string raceLetter = racialType switch
        {
            NWScript.RACIAL_TYPE_DWARF => "d",
            NWScript.RACIAL_TYPE_ELF => "e",
            33 => "e", // Drow use elf heads
            NWScript.RACIAL_TYPE_GNOME => "a",
            NWScript.RACIAL_TYPE_HALFLING => "a",
            NWScript.RACIAL_TYPE_HALFELF => "h",
            NWScript.RACIAL_TYPE_HALFORC => "o",
            NWScript.RACIAL_TYPE_HUMAN => "h",
            _ => GetRaceLetterFromAppearance(npc.Appearance.RowIndex)
        };

        return $"p{genderLetter}{raceLetter}";
    }

    private string GetRaceLetterFromAppearance(int appearanceId)
    {
        // Fallback to appearance-based detection for creatures without standard racial types
        return appearanceId switch
        {
            0 => "d",    // Dwarf
            1 => "e",    // Elf (also Drow)
            2 => "a",    // Gnome
            3 => "a",    // Halfling
            4 => "h",    // Half-Elf
            5 => "o",    // Half-Orc
            6 => "h",    // Human
            _ => "h"     // Default to human for unknown appearances
        };
    }

    private int GetPhenotype(NwCreature npc)
    {
        int phenotype = (int)npc.Phenotype;
        // Only 0 or 2 are valid, default to 0 if not 2
        return phenotype == 2 ? 2 : 0;
    }

    // Blocked heads per race/gender combination (only heads within valid ranges)
    private static readonly Dictionary<string, HashSet<int>> BlockedHeads = new()
    {
        ["pfd"] = [],
        ["pmd"] = [19],
        ["pfe"] = [19, 27, 38, 112, 120, 121, 122],
        ["pme"] = [23, 30, 31, 33, 34, 35, 36],
        ["pfa"] = [25, 26, 27, 28, 29, 32, 34, 54],
        ["pma"] = [28, 29, 30, 31, 32, 33, 34, 42, 43, 44, 72],
        ["pfo"] = [13],
        ["pmo"] = [26, 27, 34, 35, 40],
        ["pfh"] = [26, 27, 39, 40, 41, 55, 57, 58, 59, 101, 175, 179, 180, 181, 182, 183, 184],
        ["pmh"] = [42, 43, 48, 49, 51, 62, 114, 115, 116, 117, 118, 119, 120, 121, 123, 128, 130, 133, 134, 135, 136,
                   137, 138, 139, 178, 179, 180, 181, 182, 183, 184, 185, 186, 187, 188, 189, 190, 191, 192, 193, 194,
                   195, 196, 197, 198, 199, 201, 205, 207, 208, 222, 232, 236]
    };

    private static readonly Dictionary<string, HashSet<int>> Phenotype2BlockedHeads = new()
    {
        ["pfd"] = [],
        ["pmd"] = [],
        ["pfe"] = [141],    // Female Elf (1-141): 141 is in range
        ["pme"] = [],
        ["pfa"] = [],
        ["pma"] = [],
        ["pfo"] = [],
        ["pmo"] = [],
        ["pfh"] = [144],    // Female Human/Half-Elf (1-213): 144 is in range
        ["pmh"] = [124, 129, 130, 131, 132, 143, 145, 146, 149, 150, 162]  // Male Human/Half-Elf (1-239): all in range
    };

    private static readonly Dictionary<string, HashSet<int>> Phenotype0BlockedHeads = new()
    {
        ["pmh"] = [50, 53, 63]
    };
}

