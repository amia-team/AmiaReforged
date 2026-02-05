using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard.Pray;

public sealed class DeitySelectionPresenter : ScryPresenter<DeitySelectionView>
{
    private readonly NwPlayer _player;
    private readonly NwPlaceable _idol;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    // Geometry bind to force window position
    private readonly NuiBind<NuiRect> _geometryBind = new("window_geometry");
    private static readonly NuiRect WindowPosition = new(50f, 50f, 500f, 480f);

    public override DeitySelectionView View { get; }
    public override NuiWindowToken Token() => _token;

    public DeitySelectionPresenter(DeitySelectionView view, NwPlayer player, NwPlaceable idol, PrayerService prayerService)
    {
        View = view;
        _player = player;
        _idol = idol;
        // prayerService parameter kept for backwards compatibility but no longer used
    }

    public override void InitBefore()
    {
        string deityName = NWScript.GetLocalString(_idol, "name");

        _window = new NuiWindow(View.RootLayout(), $"Idol of {deityName}")
        {
            Geometry = _geometryBind,
            Resizable = true,
            Closable = true,
            Collapsed = false
        };
    }

    public override void Create()
    {
        if (_window is null)
        {
            _player.SendServerMessage("The deity selection window could not be created.", ColorConstants.Orange);
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);

        // Force the window position using the bind
        Token().SetBindValue(_geometryBind, WindowPosition);

        UpdateView();
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent ev)
    {
        if (ev.EventType != NuiEventType.Click) return;

        switch (ev.ElementId)
        {
            case "btn_change_deity":
                HandleChangeDeity();
                break;
            case "btn_close":
                Close();
                break;
        }
    }

    public override void UpdateView()
    {
        NwCreature? creature = _player.LoginCreature;
        if (creature == null) return;

        // Get deity info from idol
        string deityName = NWScript.GetLocalString(_idol, "name");
        string deityAlignment = GetDeityAlignmentString();
        string deityDomains = GetDeityDomainsString();

        Token().SetBindValue(View.DeityName, deityName);
        Token().SetBindValue(View.DeityAlignment, deityAlignment);
        Token().SetBindValue(View.DeityDomains, deityDomains);

        // Get player info
        string currentDeity = NWScript.GetDeity(creature);
        if (string.IsNullOrEmpty(currentDeity))
            currentDeity = "(None)";

        string playerAlignment = GetPlayerAlignmentString(creature);
        string playerHeader = $"{creature.Name} - Details";

        Token().SetBindValue(View.PlayerDeity, currentDeity);
        Token().SetBindValue(View.PlayerAlignment, playerAlignment);
        Token().SetBindValue(View.PlayerHeader, playerHeader);

        // Check alignment compatibility
        bool alignmentMatches = MatchAlignment(creature);
        bool axisMatches = MatchAlignmentAxis(creature);
        bool isDivineCaster = IsDivineCaster(creature);
        bool isOpposingAxis = IsOpposingGoodEvilAxis(creature);
        bool isDruid = NWScript.GetLevelByClass(NWScript.CLASS_TYPE_DRUID, creature) > 0;
        bool isCleric = NWScript.GetLevelByClass(NWScript.CLASS_TYPE_CLERIC, creature) > 0;
        bool hasDomainMatch = HasMatchingDomain(creature);

        // Set alignment status based on divine caster vs layperson
        if (isDivineCaster)
        {
            // Divine casters have strict requirements
            if (isDruid && !IsValidDruidGod())
            {
                Token().SetBindValue(View.AlignmentStatus, "This deity does not accept druids.");
                Token().SetBindValue(View.AlignmentStatusColor, ColorConstants.Maroon);
            }
            else if (!alignmentMatches)
            {
                Token().SetBindValue(View.AlignmentStatus, "Divine casters must have a compatible alignment.");
                Token().SetBindValue(View.AlignmentStatusColor, ColorConstants.Maroon);
            }
            else if (isCleric && !hasDomainMatch)
            {
                Token().SetBindValue(View.AlignmentStatus, "Clerics must have at least one matching domain.");
                Token().SetBindValue(View.AlignmentStatusColor, ColorConstants.Maroon);
            }
            else
            {
                Token().SetBindValue(View.AlignmentStatus, "Your alignment is compatible with this deity.");
                Token().SetBindValue(View.AlignmentStatusColor, ColorConstants.Green);
            }
        }
        else
        {
            // Non-divine casters can choose any deity but have varying prayer chances
            if (alignmentMatches)
            {
                Token().SetBindValue(View.AlignmentStatus, "Exact alignment match: 60% prayer chance, 40% party-wide.");
                Token().SetBindValue(View.AlignmentStatusColor, ColorConstants.Green);
            }
            else if (axisMatches)
            {
                Token().SetBindValue(View.AlignmentStatus, "Same Good/Evil axis: 50% prayer chance, 25% party-wide.");
                Token().SetBindValue(View.AlignmentStatusColor, ColorConstants.Navy);
            }
            else if (isOpposingAxis)
            {
                Token().SetBindValue(View.AlignmentStatus, "Opposing alignment: You will be smited if you pray!");
                Token().SetBindValue(View.AlignmentStatusColor, ColorConstants.Maroon);
            }
            else
            {
                Token().SetBindValue(View.AlignmentStatus, "Other Alighment: 40% prayer chance (personal).");
                Token().SetBindValue(View.AlignmentStatusColor, ColorConstants.Yellow);
            }
        }

        // Set button states
        bool canChangeDeity = CanChangeDeity(creature, alignmentMatches, isDivineCaster);

        Token().SetBindValue(View.CanChangeDeity, canChangeDeity);

        // Set change deity tooltip based on why it might be disabled
        string changeDeityTooltip = GetChangeDeityTooltip(creature, alignmentMatches, isDivineCaster, isDruid);
        Token().SetBindValue(View.ChangeDeityTooltip, changeDeityTooltip);

        // Set change deity button label based on current state
        string existingDeity = NWScript.GetDeity(creature);
        if (string.IsNullOrEmpty(existingDeity))
        {
            Token().SetBindValue(View.ChangeDeityLabel, "Worship Deity");
        }
        else
        {
            Token().SetBindValue(View.ChangeDeityLabel, "Change Deity");
        }
    }

    private string GetChangeDeityTooltip(NwCreature creature, bool alignmentMatches, bool isDivineCaster, bool isDruid)
    {
        // Check if over level 10 with existing deity
        if (creature.Level > 10 && !string.IsNullOrEmpty(NWScript.GetDeity(creature)))
        {
            return "You cannot change your deity after level 10 without a request.";
        }

        // Divine caster alignment check
        if (isDivineCaster && !alignmentMatches)
        {
            return "Divine casters must have a compatible alignment.";
        }

        // Druid deity check
        if (isDruid && !IsValidDruidGod())
        {
            return "This deity does not accept druids.";
        }

        // Cleric domain check
        int clericLevels = NWScript.GetLevelByClass(NWScript.CLASS_TYPE_CLERIC, creature);
        if (clericLevels > 0 && !HasMatchingDomain(creature))
        {
            return "Clerics must have at least one matching domain.";
        }

        // Default tooltip when enabled
        return "Set this deity as your patron.";
    }


    private void HandleChangeDeity()
    {
        NwCreature? creature = _player.LoginCreature;
        if (creature == null) return;

        string deityName = NWScript.GetLocalString(_idol, "name");
        bool isDivineCaster = IsDivineCaster(creature);

        // Check if player can change deity
        if (creature.Level > 10 && !string.IsNullOrEmpty(NWScript.GetDeity(creature)))
        {
            _player.SendServerMessage("You can only swap deities under DM supervision after level 10!", ColorConstants.Orange);
            return;
        }

        // Divine casters have strict alignment requirements
        if (isDivineCaster)
        {
            if (!MatchAlignment(creature))
            {
                _player.SendServerMessage("Divine casters must have a compatible alignment for this deity!", ColorConstants.Orange);
                return;
            }

            // Druids can only choose deities who accept druids
            if (NWScript.GetLevelByClass(NWScript.CLASS_TYPE_DRUID, creature) > 0 && !IsValidDruidGod())
            {
                _player.SendServerMessage("This deity does not accept druids!", ColorConstants.Orange);
                return;
            }

            // Clerics must have at least one matching domain
            if (NWScript.GetLevelByClass(NWScript.CLASS_TYPE_CLERIC, creature) > 0 && !HasMatchingDomain(creature))
            {
                _player.SendServerMessage("Clerics must have at least one matching domain!", ColorConstants.Orange);
                return;
            }
        }

        // Set the new deity
        NWScript.SetDeity(creature, deityName);

        // Save character
        NwTask.Run(async () =>
        {
            await NwTask.Delay(TimeSpan.FromMilliseconds(500));
            NWScript.ExportSingleCharacter(creature);
        });

        // Handle fallen state removal for druids
        int druidLevels = NWScript.GetLevelByClass(NWScript.CLASS_TYPE_DRUID, creature);
        int rangerLevels = NWScript.GetLevelByClass(NWScript.CLASS_TYPE_RANGER, creature);
        int divineChampionLevels = NWScript.GetLevelByClass(NWScript.CLASS_TYPE_DIVINECHAMPION, creature);
        int fallen = NWScript.GetLocalInt(creature, "Fallen");

        if (druidLevels > 0 && fallen != 0 && IsValidDruidGod())
        {
            // Check they don't have the permanent fallen item
            NwItem? fallenItem = creature.Inventory.Items.FirstOrDefault(i => i.ResRef == "dg_fall");
            if (fallenItem == null)
            {
                NWScript.DeleteLocalInt(creature, "Fallen");
                _player.SendServerMessage("You are no longer a fallen druid.", ColorConstants.Green);
            }
        }
        else if ((rangerLevels > 0 || divineChampionLevels > 0) && fallen != 0)
        {
            NwItem? fallenItem = creature.Inventory.Items.FirstOrDefault(i => i.ResRef == "dg_fall");
            if (fallenItem == null)
            {
                NWScript.DeleteLocalInt(creature, "Fallen");
                _player.SendServerMessage("You are no longer fallen.", ColorConstants.Green);
            }
        }

        // Feedback and visual effect
        NwTask.Run(async () =>
        {
            await NwTask.Delay(TimeSpan.FromSeconds(1));
            _player.SendServerMessage($"Your deity is {deityName} from now on!", ColorConstants.Green);

            // Play alignment effect
            CastAlignmentEffect(creature);
        });

        // Play worship animation
        NwTask.Run(async () =>
        {
            await NwTask.Delay(TimeSpan.FromSeconds(6));
            NWScript.AssignCommand(creature, () =>
            {
                NWScript.ActionPlayAnimation(NWScript.ANIMATION_LOOPING_PAUSE, 1.0f, 1.0f);
            });
        });

        Close();
    }

    private bool CanChangeDeity(NwCreature creature, bool alignmentMatches, bool isDivineCaster)
    {
        // Can't change if over level 10 and already have a deity
        if (creature.Level > 10 && !string.IsNullOrEmpty(NWScript.GetDeity(creature)))
        {
            return false;
        }

        // Divine casters have strict requirements
        if (isDivineCaster)
        {
            // Must have matching alignment
            if (!alignmentMatches)
            {
                return false;
            }

            // Druids can only choose deities who accept druids
            int druidLevels = NWScript.GetLevelByClass(NWScript.CLASS_TYPE_DRUID, creature);
            if (druidLevels > 0 && !IsValidDruidGod())
            {
                return false;
            }

            // Clerics must have at least one matching domain
            int clericLevels = NWScript.GetLevelByClass(NWScript.CLASS_TYPE_CLERIC, creature);
            if (clericLevels > 0 && !HasMatchingDomain(creature))
            {
                return false;
            }

            return true;
        }

        // Non-divine casters can choose any deity
        return true;
    }

    private bool HasMatchingDomain(NwCreature creature)
    {
        // Get the creature's domains
        int pcDomain1 = NWScript.GetDomain(creature, 1);
        int pcDomain2 = NWScript.GetDomain(creature, 2);

        // Check if either domain matches any of the idol's domains
        for (int i = 1; i <= 6; i++)
        {
            int idolDomain = NWScript.GetLocalInt(_idol, $"dom_{i}");

            if (pcDomain1 == idolDomain || pcDomain2 == idolDomain)
            {
                return true;
            }
        }

        return false;
    }

    private bool MatchAlignment(NwCreature creature)
    {
        int lawChaos = NWScript.GetAlignmentLawChaos(creature);
        int goodEvil = NWScript.GetAlignmentGoodEvil(creature);

        string creatureAlignment = "";

        if (lawChaos == NWScript.ALIGNMENT_LAWFUL)
            creatureAlignment += "L";
        else if (lawChaos == NWScript.ALIGNMENT_CHAOTIC)
            creatureAlignment += "C";
        else
            creatureAlignment += "N";

        if (goodEvil == NWScript.ALIGNMENT_GOOD)
            creatureAlignment += "G";
        else if (goodEvil == NWScript.ALIGNMENT_EVIL)
            creatureAlignment += "E";
        else
            creatureAlignment += "N";

        string alignmentVar = $"al_{creatureAlignment}";
        int acceptsAlignment = NWScript.GetLocalInt(_idol, alignmentVar);

        return acceptsAlignment == 1;
    }

    private bool MatchAlignmentAxis(NwCreature creature)
    {
        // Get the deity's actual alignment from the idol (e.g., "LG", "NE", "CN")
        string deityAlignment = NWScript.GetLocalString(_idol, "alignment");
        if (string.IsNullOrEmpty(deityAlignment) || deityAlignment.Length < 2)
            return false;

        // Get the Good/Evil axis character from the deity's alignment (second character)
        char deityAxis = deityAlignment[1]; // G, N, or E

        // Get the creature's Good/Evil alignment
        int creatureGoodEvil = NWScript.GetAlignmentGoodEvil(creature);

        // Check if the creature's axis matches the deity's axis
        return deityAxis switch
        {
            'G' => creatureGoodEvil == NWScript.ALIGNMENT_GOOD,
            'E' => creatureGoodEvil == NWScript.ALIGNMENT_EVIL,
            'N' => creatureGoodEvil == NWScript.ALIGNMENT_NEUTRAL,
            _ => false
        };
    }

    private bool IsDivineCaster(NwCreature creature)
    {
        // Check if the character has levels in divine casting classes
        int clericLevels = NWScript.GetLevelByClass(NWScript.CLASS_TYPE_CLERIC, creature);
        int druidLevels = NWScript.GetLevelByClass(NWScript.CLASS_TYPE_DRUID, creature);
        int paladinLevels = NWScript.GetLevelByClass(NWScript.CLASS_TYPE_PALADIN, creature);
        int rangerLevels = NWScript.GetLevelByClass(NWScript.CLASS_TYPE_RANGER, creature);
        int blackguardLevels = NWScript.GetLevelByClass(NWScript.CLASS_TYPE_BLACKGUARD, creature);
        int divineChampionLevels = NWScript.GetLevelByClass(NWScript.CLASS_TYPE_DIVINECHAMPION, creature);

        return clericLevels > 0 || druidLevels > 0 || paladinLevels > 0 ||
               rangerLevels > 0 || blackguardLevels > 0 || divineChampionLevels > 0;
    }

    private bool IsOpposingGoodEvilAxis(NwCreature creature)
    {
        // Get the deity's actual alignment from the idol (e.g., "LG", "NE", "CN")
        string deityAlignment = NWScript.GetLocalString(_idol, "alignment");
        if (string.IsNullOrEmpty(deityAlignment) || deityAlignment.Length < 2)
            return false;

        // Get the Good/Evil axis character from the deity's alignment (second character)
        char deityAxis = deityAlignment[1]; // G, N, or E

        // Get the creature's Good/Evil alignment
        int creatureGoodEvil = NWScript.GetAlignmentGoodEvil(creature);

        // Good creature trying to worship Evil deity
        if (creatureGoodEvil == NWScript.ALIGNMENT_GOOD && deityAxis == 'E')
        {
            return true;
        }

        // Evil creature trying to worship Good deity
        if (creatureGoodEvil == NWScript.ALIGNMENT_EVIL && deityAxis == 'G')
        {
            return true;
        }

        return false;
    }

    private bool IsValidDruidGod()
    {
        if (NWScript.GetLocalInt(_idol, "druid_deity") == 1)
            return true;

        for (int i = 1; i <= 6; i++)
        {
            int idolDomain = NWScript.GetLocalInt(_idol, $"dom_{i}");
            if (idolDomain == 1 || idolDomain == 14 || idolDomain == 43 || idolDomain == 17)
            {
                return true;
            }
        }

        return false;
    }

    private string GetDeityAlignmentString()
    {
        List<string> alignments = new();

        if (NWScript.GetLocalInt(_idol, "al_LG") == 1) alignments.Add("LG");
        if (NWScript.GetLocalInt(_idol, "al_NG") == 1) alignments.Add("NG");
        if (NWScript.GetLocalInt(_idol, "al_CG") == 1) alignments.Add("CG");
        if (NWScript.GetLocalInt(_idol, "al_LN") == 1) alignments.Add("LN");
        if (NWScript.GetLocalInt(_idol, "al_NN") == 1) alignments.Add("TN");
        if (NWScript.GetLocalInt(_idol, "al_CN") == 1) alignments.Add("CN");
        if (NWScript.GetLocalInt(_idol, "al_LE") == 1) alignments.Add("LE");
        if (NWScript.GetLocalInt(_idol, "al_NE") == 1) alignments.Add("NE");
        if (NWScript.GetLocalInt(_idol, "al_CE") == 1) alignments.Add("CE");

        return alignments.Count > 0 ? string.Join(", ", alignments) : "Any";
    }

    private string GetDeityDomainsString()
    {
        List<string> domains = new();

        // First, check if any domain slot has a non-zero value
        // This tells us if the idol has domains configured at all
        bool hasAnyNonZeroDomain = false;
        for (int i = 1; i <= 6; i++)
        {
            if (NWScript.GetLocalInt(_idol, $"dom_{i}") > 0)
            {
                hasAnyNonZeroDomain = true;
                break;
            }
        }

        // Now collect domains
        for (int i = 1; i <= 6; i++)
        {
            int domainId = NWScript.GetLocalInt(_idol, $"dom_{i}");

            if (domainId > 0)
            {
                // Non-zero domain ID, add it
                string domainName = GetDomainName(domainId);
                if (!string.IsNullOrEmpty(domainName))
                    domains.Add(domainName);
            }
            else if (domainId == 0 && hasAnyNonZeroDomain)
            {
                // Domain ID is 0 and other domains exist, so this is intentionally Air
                domains.Add("Air");
            }
            // If domainId == 0 and no other domains exist, it's unset - skip it
        }

        return domains.Count > 0 ? string.Join(", ", domains) : "None";
    }

    private string GetDomainName(int domainId)
    {
        return domainId switch
        {
            0 => "Air",
            1 => "Animal",
            //2 => "Chaos",
            3 => "Death",
            4 => "Destruction",
            5 => "Earth",
            6 => "Evil",
            7 => "Fire",
            8 => "Good",
            9 => "Healing",
            10 => "Knowledge",
            //11 => "Law",
            //12 => "Luck",
            13 => "Magic",
            14 => "Plant",
            15 => "Protection",
            16 => "Strength",
            17 => "Sun",
            18 => "Travel",
            19 => "Trickery",
            20 => "War",
            21 => "Water",
            22 => "Balance",
            23 => "Cavern",
            24 => "Chaos",
            25 => "Charm",
            26 => "Cold",
            27 => "Community",
            28 => "Courage",
            29 => "Craft",
            30 => "Darkness",
            31 => "Dragon",
            32 => "Dream",
            33 => "Drow",
            34 => "Dwarf",
            35 => "Elf",
            36 => "Fate",
            37 => "Gnome",
            38 => "Halfling",
            39 => "Hatred",
            40 => "Illusion",
            41 => "Law",
            42 => "Luck",
            43 => "Moon",
            44 => "Nobility",
            45 => "Orc",
            46 => "Portal",
            47 => "Renewal",
            48 => "Repose",
            49 => "Retribution",
            50 => "Rune",
            51 => "Scalykind",
            52 => "Slime",
            53 => "Spell",
            54 => "Time",
            55 => "Trade",
            56 => "Tyranny",
            57 => "Undeath",
            58 => "Suffering",
            _ => ""
        };
    }

    private string GetPlayerAlignmentString(NwCreature creature)
    {
        int lawChaos = NWScript.GetAlignmentLawChaos(creature);
        int goodEvil = NWScript.GetAlignmentGoodEvil(creature);

        string lcStr = lawChaos switch
        {
            NWScript.ALIGNMENT_LAWFUL => "Lawful",
            NWScript.ALIGNMENT_CHAOTIC => "Chaotic",
            _ => "Neutral"
        };

        string geStr = goodEvil switch
        {
            NWScript.ALIGNMENT_GOOD => "Good",
            NWScript.ALIGNMENT_EVIL => "Evil",
            _ => "Neutral"
        };

        if (lcStr == "Neutral" && geStr == "Neutral")
            return "True Neutral";

        return $"{lcStr} {geStr}";
    }

    private void CastAlignmentEffect(NwCreature creature)
    {
        string alignment = NWScript.GetLocalString(_idol, "alignment");
        int visual = 0;

        if (alignment is "LG" or "NG" or "CG")
            visual = NWScript.VFX_IMP_GOOD_HELP;
        else if (alignment is "LN" or "NN" or "CN")
            visual = NWScript.VFX_IMP_UNSUMMON;
        else if (alignment is "LE" or "NE" or "CE")
            visual = NWScript.VFX_IMP_EVIL_HELP;

        if (visual > 0)
        {
            Effect visualEffect = Effect.VisualEffect((VfxType)visual);
            creature.ApplyEffect(EffectDuration.Instant, visualEffect);
        }
    }

    public override void Close()
    {
        _token.Close();
    }
}
