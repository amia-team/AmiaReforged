using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard.Pray;

public sealed class DeitySelectionPresenter : ScryPresenter<DeitySelectionView>
{
    private readonly NwPlayer _player;
    private readonly NwPlaceable _idol;
    private readonly PrayerService _prayerService;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    // Geometry bind to force window position
    private readonly NuiBind<NuiRect> _geometryBind = new("window_geometry");
    private static readonly NuiRect WindowPosition = new(300f, 150f, 380f, 400f);

    public override DeitySelectionView View { get; }
    public override NuiWindowToken Token() => _token;

    public DeitySelectionPresenter(DeitySelectionView view, NwPlayer player, NwPlaceable idol, PrayerService prayerService)
    {
        View = view;
        _player = player;
        _idol = idol;
        _prayerService = prayerService;
    }

    public override void InitBefore()
    {
        string deityName = NWScript.GetLocalString(_idol, "name");

        _window = new NuiWindow(View.RootLayout(), $"Idol of {deityName}")
        {
            Geometry = _geometryBind,
            Resizable = false,
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
            case "btn_pray":
                HandlePray();
                break;
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

        Token().SetBindValue(View.PlayerDeity, currentDeity);
        Token().SetBindValue(View.PlayerAlignment, playerAlignment);

        // Check alignment compatibility
        bool alignmentMatches = MatchAlignment(creature);
        bool axisMatches = MatchAlignmentAxis(creature);
        bool isDivineCaster = IsDivineCaster(creature);
        bool isOpposingAxis = IsOpposingGoodEvilAxis(creature);

        // Set alignment status
        if (alignmentMatches)
        {
            Token().SetBindValue(View.AlignmentStatus, "Your alignment is compatible with this deity.");
            Token().SetBindValue(View.AlignmentStatusColor, ColorConstants.Green);
        }
        else if (axisMatches)
        {
            Token().SetBindValue(View.AlignmentStatus, "Your alignment partially matches this deity.");
            Token().SetBindValue(View.AlignmentStatusColor, ColorConstants.Yellow);
        }
        else if (isOpposingAxis)
        {
            // Good trying to worship Evil or vice versa
            Token().SetBindValue(View.AlignmentStatus, "WARNING: This deity opposes your moral alignment!");
            Token().SetBindValue(View.AlignmentStatusColor, ColorConstants.Red);
        }
        else
        {
            Token().SetBindValue(View.AlignmentStatus, "Your alignment differs from this deity (0% prayer chance).");
            Token().SetBindValue(View.AlignmentStatusColor, ColorConstants.Orange);
        }

        // Set button states
        // Divine casters MUST have matching alignment to change deity
        // Laypeople can worship anyone (but may face consequences)
        bool canChangeDeity = CanChangeDeity(creature, alignmentMatches, isDivineCaster);
        bool canPray = alignmentMatches || axisMatches || !isDivineCaster; // Laypeople can always try to pray

        Token().SetBindValue(View.CanChangeDeity, canChangeDeity);
        Token().SetBindValue(View.CanPray, canPray);

        // Set change deity button label based on current state
        string existingDeity = NWScript.GetDeity(creature);
        if (string.IsNullOrEmpty(existingDeity))
        {
            Token().SetBindValue(View.ChangeDeityLabel, "Choose Deity");
        }
        else
        {
            Token().SetBindValue(View.ChangeDeityLabel, "Change Deity");
        }
    }

    private void HandlePray()
    {
        NwCreature? creature = _player.LoginCreature;
        if (creature == null) return;

        // Play worship animation
        NWScript.AssignCommand(creature, () =>
        {
            NWScript.ActionPlayAnimation(NWScript.ANIMATION_LOOPING_WORSHIP, 1.0f, 30.0f);
        });

        // Check for opposing alignment - deity smites the heretic!
        if (IsOpposingGoodEvilAxis(creature))
        {
            SmiteHeretic(creature);
            Close();
            return;
        }

        // Use the prayer service to handle the prayer
        _prayerService.PrayFromDashboard(_player, creature);

        Close();
    }

    private void SmiteHeretic(NwCreature creature)
    {
        string deityName = NWScript.GetLocalString(_idol, "name");
        int creatureGoodEvil = NWScript.GetAlignmentGoodEvil(creature);

        // Determine if this is a good or evil deity based on accepted alignments
        bool deityIsGood = NWScript.GetLocalInt(_idol, "al_LG") == 1 ||
                          NWScript.GetLocalInt(_idol, "al_NG") == 1 ||
                          NWScript.GetLocalInt(_idol, "al_CG") == 1;
        bool deityIsEvil = NWScript.GetLocalInt(_idol, "al_LE") == 1 ||
                          NWScript.GetLocalInt(_idol, "al_NE") == 1 ||
                          NWScript.GetLocalInt(_idol, "al_CE") == 1;

        // Delay the smite for dramatic effect
        NwTask.Run(async () =>
        {
            await NwTask.Delay(TimeSpan.FromSeconds(3));

            // Calculate damage - half current HP
            int damage = creature.HP / 2;
            if (damage < 1) damage = 1;

            // Apply lightning strike VFX
            Effect lightningVfx = Effect.VisualEffect(VfxType.ImpLightningS);
            creature.ApplyEffect(EffectDuration.Instant, lightningVfx);

            // Apply damage
            Effect damageEffect = Effect.Damage(damage, DamageType.Divine);
            creature.ApplyEffect(EffectDuration.Instant, damageEffect);

            // Send appropriate message based on deity alignment
            if (deityIsGood && creatureGoodEvil == NWScript.ALIGNMENT_EVIL)
            {
                _player.SendServerMessage($"{deityName} smites you for your wickedness!", ColorConstants.Red);
                _player.SendServerMessage("The righteous fury of the heavens strikes you down!", ColorConstants.Orange);
            }
            else if (deityIsEvil && creatureGoodEvil == NWScript.ALIGNMENT_GOOD)
            {
                _player.SendServerMessage($"{deityName} punishes your pathetic plea for mercy!", ColorConstants.Red);
                _player.SendServerMessage("Dark powers lash out at your foolish piety!", ColorConstants.Orange);
            }
            else
            {
                _player.SendServerMessage($"{deityName} rejects your prayer with violent displeasure!", ColorConstants.Red);
            }

            _player.SendServerMessage($"[You took {damage} divine damage]", ColorConstants.Gray);
        });
    }

    private void HandleChangeDeity()
    {
        NwCreature? creature = _player.LoginCreature;
        if (creature == null) return;

        string deityName = NWScript.GetLocalString(_idol, "name");

        // Check if player can change deity
        if (creature.Level > 10 && !string.IsNullOrEmpty(NWScript.GetDeity(creature)))
        {
            _player.SendServerMessage("You can only swap deities under DM supervision after level 10!", ColorConstants.Orange);
            return;
        }

        if (!MatchAlignment(creature))
        {
            _player.SendServerMessage("You do not have the right alignment for this god!", ColorConstants.Orange);
            return;
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

        // Divine casters MUST have matching alignment
        if (isDivineCaster)
        {
            return alignmentMatches;
        }

        // Laypeople can worship any deity (they just won't get benefits if misaligned)
        return true;
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
        int goodEvil = NWScript.GetAlignmentGoodEvil(creature);
        string axis = "";

        if (goodEvil == NWScript.ALIGNMENT_GOOD)
            axis = "G";
        else if (goodEvil == NWScript.ALIGNMENT_EVIL)
            axis = "E";
        else
            axis = "N";

        if (axis == "G")
        {
            return NWScript.GetLocalInt(_idol, "al_LG") == 1 ||
                   NWScript.GetLocalInt(_idol, "al_NG") == 1 ||
                   NWScript.GetLocalInt(_idol, "al_CG") == 1;
        }
        else if (axis == "E")
        {
            return NWScript.GetLocalInt(_idol, "al_LE") == 1 ||
                   NWScript.GetLocalInt(_idol, "al_NE") == 1 ||
                   NWScript.GetLocalInt(_idol, "al_CE") == 1;
        }
        else
        {
            return NWScript.GetLocalInt(_idol, "al_LN") == 1 ||
                   NWScript.GetLocalInt(_idol, "al_NN") == 1 ||
                   NWScript.GetLocalInt(_idol, "al_CN") == 1;
        }
    }

    private bool IsDivineCaster(NwCreature creature)
    {
        // Check if the character has levels in divine casting classes
        int clericLevels = NWScript.GetLevelByClass(NWScript.CLASS_TYPE_CLERIC, creature);
        int druidLevels = NWScript.GetLevelByClass(NWScript.CLASS_TYPE_DRUID, creature);
        int paladinLevels = NWScript.GetLevelByClass(NWScript.CLASS_TYPE_PALADIN, creature);
        int rangerLevels = NWScript.GetLevelByClass(NWScript.CLASS_TYPE_RANGER, creature);
        int blackguardLevels = NWScript.GetLevelByClass(NWScript.CLASS_TYPE_BLACKGUARD, creature);

        return clericLevels > 0 || druidLevels > 0 || paladinLevels > 0 || rangerLevels > 0 || blackguardLevels > 0;
    }

    private bool IsOpposingGoodEvilAxis(NwCreature creature)
    {
        int creatureGoodEvil = NWScript.GetAlignmentGoodEvil(creature);

        // Check what alignments the deity accepts
        bool deityAcceptsGood = NWScript.GetLocalInt(_idol, "al_LG") == 1 ||
                                NWScript.GetLocalInt(_idol, "al_NG") == 1 ||
                                NWScript.GetLocalInt(_idol, "al_CG") == 1;
        bool deityAcceptsEvil = NWScript.GetLocalInt(_idol, "al_LE") == 1 ||
                                NWScript.GetLocalInt(_idol, "al_NE") == 1 ||
                                NWScript.GetLocalInt(_idol, "al_CE") == 1;

        // Good creature trying to worship evil-only deity
        if (creatureGoodEvil == NWScript.ALIGNMENT_GOOD && deityAcceptsEvil && !deityAcceptsGood)
        {
            return true;
        }

        // Evil creature trying to worship good-only deity
        if (creatureGoodEvil == NWScript.ALIGNMENT_EVIL && deityAcceptsGood && !deityAcceptsEvil)
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

        for (int i = 1; i <= 6; i++)
        {
            int domainId = NWScript.GetLocalInt(_idol, $"dom_{i}");
            if (domainId > 0)
            {
                string domainName = GetDomainName(domainId);
                if (!string.IsNullOrEmpty(domainName))
                    domains.Add(domainName);
            }
        }

        return domains.Count > 0 ? string.Join(", ", domains) : "None";
    }

    private string GetDomainName(int domainId)
    {
        return domainId switch
        {
            0 => "Air",
            1 => "Animal",
            2 => "Chaos",
            3 => "Death",
            4 => "Destruction",
            5 => "Earth",
            6 => "Evil",
            7 => "Fire",
            8 => "Good",
            9 => "Healing",
            10 => "Knowledge",
            11 => "Law",
            12 => "Luck",
            13 => "Magic",
            14 => "Plant",
            15 => "Protection",
            16 => "Strength",
            17 => "Sun",
            18 => "Travel",
            19 => "Trickery",
            20 => "War",
            21 => "Water",
            40 => "Darkness",
            41 => "Drow",
            42 => "Elf",
            43 => "Moon",
            44 => "Orc",
            45 => "Scalykind",
            46 => "Spider",
            47 => "Undeath",
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
