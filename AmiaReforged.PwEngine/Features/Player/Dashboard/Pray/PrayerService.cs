using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard.Pray;

/// <summary>
/// Handles the prayer system for clerics, druids, and laypeople to pray to their deities.
/// Ported from ds_gods_idol.nss and inc_ds_gods.nss
/// </summary>
[ServiceBinding(typeof(PrayerService))]
public class PrayerService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private const int PRAYER_COOLDOWN_MINUTES = 60;
    private const string PRAYER_EFFECT_TAG = "PrayerEffect";
    private const string ILLUSION_PORTAL_SUMMON_VAR = "IllusionPortalSummonActive";
    private readonly HashSet<uint> _processingPrayers = new();
    private readonly WindowDirector _windowDirector;

    public PrayerService(WindowDirector windowDirector)
    {
        _windowDirector = windowDirector;

        // Note: We use the [ScriptHandler("ds_gods_idol")] attribute to handle idol usage
        // No need to manually register event handlers
    }

    /// <summary>
    /// Script handler for ds_gods_idol - triggers when a player uses an idol placeable with this script assigned
    /// </summary>
    [ScriptHandler("ds_gods_idol")]
    public void OnIdolScriptCalled(CallInfo callInfo)
    {
        // Get the placeable (idol) that was used - OBJECT_SELF in NWScript context is callInfo.ObjectSelf
        NwPlaceable? idol = callInfo.ObjectSelf as NwPlaceable;
        if (idol == null || !idol.IsValid)
        {
            return;
        }

        // Get the player who used the idol
        uint userObject = NWScript.GetLastUsedBy();
        NwCreature? user = userObject.ToNwObject<NwCreature>();

        if (user == null || !user.IsPlayerControlled)
        {
            return;
        }

        NwPlayer? player = user.ControllingPlayer;
        if (player == null)
        {
            return;
        }

        // Check if the deity selection window is already open - if so, close it (toggle)
        if (_windowDirector.IsWindowOpen(player, typeof(DeitySelectionPresenter)))
        {
            _windowDirector.CloseWindow(player, typeof(DeitySelectionPresenter));
            return;
        }

        // Open the deity selection NUI
        DeitySelectionView view = new();
        DeitySelectionPresenter presenter = new(view, player, idol, this);
        _windowDirector.OpenWindow(presenter);
    }

    /// <summary>
    /// Handles when a player clicks the Pray button in the dashboard.
    /// This is the "rest menu" path from the original script.
    /// </summary>
    public void PrayFromDashboard(NwPlayer player, NwCreature creature)
    {
        // Prevent duplicate prayer processing
        if (!_processingPrayers.Add(creature.ObjectId))
        {
            return; // Already processing a prayer for this creature
        }

        try
        {
            // Check if the deity selection window is already open - if so, close it before processing prayer
            if (_windowDirector.IsWindowOpen(player, typeof(DeitySelectionPresenter)))
            {
                _windowDirector.CloseWindow(player, typeof(DeitySelectionPresenter));
            }

            ProcessPrayer(player, creature);
        }
        finally
        {
            // Remove from processing set after a short delay
            _ = NwTask.Run(async () =>
            {
                await NwTask.Delay(TimeSpan.FromSeconds(1));
                _processingPrayers.Remove(creature.ObjectId);
            });
        }
    }

    private void ProcessPrayer(NwPlayer player, NwCreature creature)
    {
        // On your knees!
        NWScript.AssignCommand(creature, () =>
        {
            NWScript.ActionPlayAnimation(NWScript.ANIMATION_LOOPING_MEDITATE, 0.4f, 8.0f);
        });

        // Get deity
        string deity = NWScript.GetDeity(creature);

        if (string.IsNullOrEmpty(deity))
        {
            player.SendServerMessage("The gods do not care for the Faithless...", ColorConstants.Orange);
            return;
        }

        // Find idol for this deity
        string formattedName = CapitalizeWords(deity);
        if (formattedName == "QueenOfAirAndDarkness")
            formattedName = "QueenofAirandDarkness";

        NwPlaceable? idol = FindIdol(deity);

        if (idol == null)
        {
            player.SendServerMessage($"{deity} holds no domain in Amia...", ColorConstants.Orange);
            return;
        }

        // Check if character has the Fallen item (ds_fall)
        bool hasFallenItem = creature.Inventory.Items.Any(i => i.ResRef == "ds_fall");
        if (hasFallenItem)
        {
            player.SendServerMessage("You are Fallen and must Atone before you can seek your god's blessings.", ColorConstants.Red);
            SmiteHeretic(player, creature, idol, deity);
            return;
        }

        // Get class levels for divine caster checks
        int clericLevels = NWScript.GetLevelByClass(NWScript.CLASS_TYPE_CLERIC, creature);
        int druidLevels = NWScript.GetLevelByClass(NWScript.CLASS_TYPE_DRUID, creature);
        int rangerLevels = NWScript.GetLevelByClass(NWScript.CLASS_TYPE_RANGER, creature);
        int paladinLevels = NWScript.GetLevelByClass(NWScript.CLASS_TYPE_PALADIN, creature);
        int blackguardLevels = NWScript.GetLevelByClass(NWScript.CLASS_TYPE_BLACKGUARD, creature);
        int divineChampionLevels = NWScript.GetLevelByClass(NWScript.CLASS_TYPE_DIVINECHAMPION, creature);

        bool isDivineCaster = clericLevels > 0 || druidLevels > 0 || rangerLevels > 0 ||
                              paladinLevels > 0 || blackguardLevels > 0 || divineChampionLevels > 0;

        // Check alignment vs god
        bool alignmentMatches = MatchAlignment(creature, idol);

        // Divine casters with wrong alignment become Fallen
        if (isDivineCaster && !alignmentMatches)
        {
            MakeFallen(player, creature, idol, deity, $"Your alignment displeases {deity}!");
            return;
        }

        // Clerics must have both domains matching the deity
        if (clericLevels > 0)
        {
            bool hasMatchingDomain = HasMatchingDomain(creature, idol);
            if (!hasMatchingDomain)
            {
                MakeFallen(player, creature, idol, deity, $"Both of your Domains must match {deity}'s Domains!");
                return;
            }
        }

        // For non-divine casters, check for opposing alignment (Good vs Evil) which triggers a smite
        if (!isDivineCaster && !alignmentMatches)
        {
            // Check if they have an OPPOSING alignment (Good vs Evil) - this triggers a smite!
            bool isOpposingAxis = IsOpposingGoodEvilAxis(creature, idol);

            if (isOpposingAxis)
            {
                // Smite the heretic!
                SmiteHeretic(player, creature, idol, deity);
                return;
            }
            // Non-opposing laypersons continue to the prayer section below for their % chance
        }

        // Check prayer cooldown AFTER alignment check
        int cooldownRemaining = GetPrayerCooldownRemaining(creature);

        if (cooldownRemaining > 0)
        {
            int minutes = cooldownRemaining / 60;
            player.SendServerMessage($"{deity} must be too busy right now.", ColorConstants.Orange);
            player.SendServerMessage($"You can pray again in {minutes} minutes.", ColorConstants.Orange);
            return;
        }

        // Set prayer cooldown (60 minutes) - only if prayer will actually happen
        SetPrayerCooldown(creature);

        // Get the creature's actual domains (not matched domains)
        int creatureDomain1 = NWScript.GetDomain(creature);
        int creatureDomain2 = NWScript.GetDomain(creature, 2);

        // Calculate total divine level (sum of all divine class levels)
        int totalDivineLevel = clericLevels + druidLevels + rangerLevels + paladinLevels + blackguardLevels + divineChampionLevels;

        // Process prayer based on class
        if (clericLevels > 0 && clericLevels >= druidLevels && alignmentMatches)
        {
            // Is cleric and has compatible alignment and at least one matching domain
            // Party-wide blessing using total divine level
            // Clerics override ALL existing prayer effects party-wide
            NwTask.Run(async () =>
            {
                await NwTask.Delay(TimeSpan.FromSeconds(5));

                // Remove existing prayer effects from entire party (clerics override all prayers)
                RemovePrayerEffectsFromParty(creature);

                player.SendServerMessage($"{deity}'s power is demonstrated through your prayer!", ColorConstants.Green);
                player.SendServerMessage($"[Divine Level: {totalDivineLevel} - Party-wide blessing]", ColorConstants.Gray);

                await NwTask.Delay(TimeSpan.FromSeconds(1));
                CastAlignmentEffect(creature, idol, totalDivineLevel);

                // Grant BOTH domain bonuses (even if only one matches the deity)
                if (creatureDomain1 > 0)
                {
                    await NwTask.Delay(TimeSpan.FromMilliseconds(300));
                    CastDomainEffect(player, creature, creatureDomain1, totalDivineLevel);
                }

                if (creatureDomain2 > 0)
                {
                    await NwTask.Delay(TimeSpan.FromMilliseconds(300));
                    CastDomainEffect(player, creature, creatureDomain2, totalDivineLevel);
                }
            });
        }
        else if (druidLevels > 0 && alignmentMatches && IsValidDruidGod(idol))
        {
            // Is druid and has compatible alignment and valid druid god
            // Party-wide blessing using total divine level
            // Druids only override their own prayer effects (not party-wide like clerics)
            NwTask.Run(async () =>
            {
                await NwTask.Delay(TimeSpan.FromSeconds(5));

                // Remove only the druid's own prayer effects
                RemovePrayerEffects(creature);

                player.SendServerMessage($"{deity}'s power is demonstrated through your prayer!", ColorConstants.Green);
                player.SendServerMessage($"[Divine Level: {totalDivineLevel} - Party-wide blessing]", ColorConstants.Gray);

                await NwTask.Delay(TimeSpan.FromSeconds(1));
                CastAlignmentEffect(creature, idol, totalDivineLevel);
            });
        }
        else if (isDivineCaster && alignmentMatches)
        {
            // Other divine casters (Ranger, Paladin, Blackguard, Divine Champion) with matching alignment
            // 100% Personal Success Rate
            // Party-wide chance: 40% base + 2% per divine level
            int partyWideChance = Math.Min(100, 40 + totalDivineLevel * 2);
            int partyWideRoll = Random.Shared.Next(1, 101);
            bool isPartyWide = partyWideRoll <= partyWideChance;

            NwTask.Run(async () =>
            {
                await NwTask.Delay(TimeSpan.FromSeconds(5));

                // Non-cleric divine casters only remove their own prayer effects
                RemovePrayerEffects(creature);

                player.SendServerMessage($"{deity}'s power is demonstrated through your prayer!", ColorConstants.Green);

                if (isPartyWide)
                {
                    player.SendServerMessage($"[Party-wide chance: {partyWideChance}%. Roll: {partyWideRoll}. Party-wide!]", ColorConstants.Gray);
                }
                else
                {
                    player.SendServerMessage($"[Party-wide chance: {partyWideChance}%. Roll: {partyWideRoll}. Personal.]", ColorConstants.Gray);
                }

                await NwTask.Delay(TimeSpan.FromSeconds(1));

                if (isPartyWide)
                {
                    CastAlignmentEffect(creature, idol, totalDivineLevel);
                }
                else
                {
                    CastAlignmentEffectSelf(creature, idol, totalDivineLevel);
                }
            });
        }
        else
        {
            // Layperson prayer - determine success rate based on alignment
            bool axisMatches = MatchAlignmentAxis(creature, idol);
            int successRate;
            int partyWideChance = 0;

            if (alignmentMatches)
            {
                // Exact alignment match - 60% success, 40% party-wide
                successRate = 60;
                partyWideChance = 40;
            }
            else if (axisMatches)
            {
                // Same Good/Evil axis - 50% success, 25% party-wide
                successRate = 50;
                partyWideChance = 25;
                player.SendServerMessage($"You honor {deity} through your actions, if not your exact path...", ColorConstants.Yellow);
            }
            else
            {
                // No axis match (e.g., TN following LG Bahamut) - 40% success, personal only
                successRate = 40;
                partyWideChance = 0;
                player.SendServerMessage($"Your devotion to {deity} is tested by your divergent path...", ColorConstants.Yellow);
            }

            int roll = Random.Shared.Next(1, 101);
            int partyWideRoll = Random.Shared.Next(1, 101);

            if (roll <= successRate)
            {
                // Success! Now check if it's party-wide
                bool isPartyWide = partyWideChance > 0 && partyWideRoll <= partyWideChance;

                NwTask.Run(async () =>
                {
                    await NwTask.Delay(TimeSpan.FromSeconds(5));

                    // Laypeople only remove their own prayer effects
                    RemovePrayerEffects(creature);

                    if (isPartyWide)
                    {
                        player.SendServerMessage($"{deity} blesses you and your companions!", ColorConstants.Green);
                        player.SendServerMessage($"[Success Rate: {successRate}%. Roll: {roll}. Party-wide chance: {partyWideChance}%. Roll: {partyWideRoll}]", ColorConstants.Gray);
                    }
                    else
                    {
                        player.SendServerMessage($"{deity} blesses you!", ColorConstants.Green);
                        player.SendServerMessage($"[Success Rate: {successRate}%. Roll: {roll}]", ColorConstants.Gray);
                    }

                    await NwTask.Delay(TimeSpan.FromSeconds(1));

                    if (isPartyWide)
                    {
                        // Use party-wide effect but with 0 divine level for duration
                        CastAlignmentEffectPartyWide(creature, idol, 0);
                    }
                    else
                    {
                        // Personal effect only
                        CastAlignmentEffectSelf(creature, idol, 0);
                    }
                });
            }
            else
            {
                NwTask.Run(async () =>
                {
                    await NwTask.Delay(TimeSpan.FromSeconds(5));
                    player.SendServerMessage($"{deity} does not answer your prayer this time...", ColorConstants.Orange);
                    player.SendServerMessage($"[Success Rate: {successRate}%. Roll: {roll}]", ColorConstants.Gray);
                });
            }
        }
    }

    private void MakeFallen(NwPlayer player, NwCreature creature, NwPlaceable idol, string deityName, string reason)
    {
        // Check if they already have the Fallen item
        bool hasFallenItem = creature.Inventory.Items.Any(i => i.ResRef == "ds_fall");

        if (!hasFallenItem)
        {
            // Create the Fallen item in their inventory
            NwTask.Run(async () =>
            {
                NwItem? fallenItem = await NwItem.Create("ds_fall", creature);
                if (fallenItem != null)
                {
                    await NwTask.SwitchToMainThread();
                    player.SendServerMessage("You have become Fallen!", ColorConstants.Red);
                }
            });
        }

        // Set the Fallen local int
        NWScript.SetLocalInt(creature, "Fallen", 1);

        player.SendServerMessage(reason, ColorConstants.Red);
        player.SendServerMessage("You must Atone before you can seek your god's blessings.", ColorConstants.Orange);

        // Smite the fallen divine caster
        SmiteHeretic(player, creature, idol, deityName);
    }

    private bool HasMatchingDomain(NwCreature creature, NwPlaceable idol)
    {
        // Get the creature's domains
        int pcDomain1 = NWScript.GetDomain(creature);
        int pcDomain2 = NWScript.GetDomain(creature, 2);

        // Both domains must match the deity's domains
        bool domain1Matches = false;
        bool domain2Matches = false;

        // Collect all idol domains
        List<int> idolDomains = new();

        for (int i = 1; i <= 6; i++)
        {
            int idolDomain = NWScript.GetLocalInt(idol, $"dom_{i}");
            if (idolDomain > 0)
            {
                idolDomains.Add(idolDomain);
            }
            else if (idolDomain == 0 && i == 1)
            {
                // Check if any other domain is set to know if Air (ID 0) is intentionally set
                for (int j = 2; j <= 6; j++)
                {
                    if (NWScript.GetLocalInt(idol, $"dom_{j}") > 0)
                    {
                        idolDomains.Add(0); // Add Air domain
                        break;
                    }
                }
            }
        }

        // Check if each PC domain matches any idol domain
        foreach (int idolDomain in idolDomains)
        {
            if (pcDomain1 == idolDomain)
            {
                domain1Matches = true;
            }
            if (pcDomain2 == idolDomain)
            {
                domain2Matches = true;
            }
        }

        // Both domains must match
        return domain1Matches && domain2Matches;
    }

    private NwPlaceable? FindIdol(string godName)
    {
        // Idol tags use format: idol2_CapitalizedName (no spaces)
        // Each word is capitalized, spaces removed
        // Example: "Bahamut" -> "idol2_Bahamut"
        // Example: "Queen of Air and Darkness" -> "idol2_QueenofAirandDarkness"

        string formattedName = CapitalizeWords(godName);

        // Special case for Queen of Air and Darkness
        if (formattedName == "QueenOfAirAndDarkness")
            formattedName = "QueenofAirandDarkness";

        string idolTag = $"idol2_{formattedName}";

        // Search all areas for the idol
        foreach (NwArea area in NwModule.Instance.Areas)
        {
            NwPlaceable? idol = area.FindObjectsOfTypeInArea<NwPlaceable>()
                .FirstOrDefault(p => p.Tag.Equals(idolTag, StringComparison.OrdinalIgnoreCase));

            if (idol != null)
                return idol;
        }

        return null;
    }

    private string CapitalizeWords(string text)
    {
        // Remove spaces and capitalize first letter of each word
        string[] words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string result = "";

        foreach (string word in words)
        {
            if (word.Length > 0)
            {
                // Capitalize first letter, rest stays as-is
                result += char.ToUpper(word[0]) + word.Substring(1).ToLower();
            }
        }

        return result;
    }

    private bool MatchAlignment(NwCreature creature, NwPlaceable idol)
    {
        int lawChaos = NWScript.GetAlignmentLawChaos(creature);
        int goodEvil = NWScript.GetAlignmentGoodEvil(creature);

        string creatureAlignment = "";

        // Determine Law/Chaos component
        if (lawChaos == NWScript.ALIGNMENT_LAWFUL)
            creatureAlignment += "L";
        else if (lawChaos == NWScript.ALIGNMENT_CHAOTIC)
            creatureAlignment += "C";
        else
            creatureAlignment += "N";

        // Determine Good/Evil component
        if (goodEvil == NWScript.ALIGNMENT_GOOD)
            creatureAlignment += "G";
        else if (goodEvil == NWScript.ALIGNMENT_EVIL)
            creatureAlignment += "E";
        else
            creatureAlignment += "N";

        // Check if idol accepts this alignment via local int (e.g., al_LG, al_CG, etc.)
        string alignmentVar = $"al_{creatureAlignment}";
        int acceptsAlignment = NWScript.GetLocalInt(idol, sVarName: alignmentVar);

        return acceptsAlignment == 1;
    }

    private bool MatchAlignmentAxis(NwCreature creature, NwPlaceable idol)
    {
        // Get the deity's actual alignment from the idol (e.g., "LG", "NE", "CN")
        string deityAlignment = NWScript.GetLocalString(idol, "alignment");
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

    private bool IsOpposingGoodEvilAxis(NwCreature creature, NwPlaceable idol)
    {
        // Get the deity's actual alignment from the idol (e.g., "LG", "NE", "CN")
        string deityAlignment = NWScript.GetLocalString(idol, "alignment");
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

    private void SmiteHeretic(NwPlayer player, NwCreature creature, NwPlaceable idol, string deityName)
    {
        int creatureGoodEvil = NWScript.GetAlignmentGoodEvil(creature);

        // Get the deity's alignment
        string deityAlignment = NWScript.GetLocalString(idol, "alignment");
        bool deityIsGood = deityAlignment.Length >= 2 && deityAlignment[1] == 'G';
        bool deityIsEvil = deityAlignment.Length >= 2 && deityAlignment[1] == 'E';

        // Delay the smite for dramatic effect
        NwTask.Run(async () =>
        {
            await NwTask.Delay(TimeSpan.FromSeconds(3));

            // Calculate damage - half current HP
            int damage = creature.HP + 3;
            if (damage < 1) damage = 1;

            // Apply VFX based on deity alignment
            if (deityIsGood)
            {
                // Good deity smite VFX: 41, 74, 184
                creature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect((VfxType)41));
                creature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect((VfxType)74));
                creature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect((VfxType)184));
            }
            else if (deityIsEvil)
            {
                // Evil deity smite VFX: 673, 235, 246
                creature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect((VfxType)673));
                creature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect((VfxType)235));
                creature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect((VfxType)246));
                creature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect((VfxType)356));
                creature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect((VfxType)54));
            }
            else
            {
                // Neutral deity - use lightning
                creature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpLightningM));
            }

            // Apply damage
            Effect damageEffect = Effect.Damage(damage, DamageType.Divine);
            creature.ApplyEffect(EffectDuration.Instant, damageEffect);

            // Send appropriate message based on deity alignment
            if (deityIsGood && creatureGoodEvil == NWScript.ALIGNMENT_EVIL)
            {
                player.SendServerMessage($"{deityName} smites you for your wickedness!", ColorConstants.Red);
                player.SendServerMessage("The righteous fury of the heavens strikes you down!", ColorConstants.Orange);
            }
            else if (deityIsEvil && creatureGoodEvil == NWScript.ALIGNMENT_GOOD)
            {
                player.SendServerMessage($"{deityName} punishes your pathetic plea for mercy!", ColorConstants.Red);
                player.SendServerMessage("Dark powers lash out at your foolish piety!", ColorConstants.Orange);
            }
            else
            {
                player.SendServerMessage($"{deityName} rejects your prayer with violent displeasure!", ColorConstants.Red);
            }

            player.SendServerMessage($"[You took {damage} divine damage]", ColorConstants.Gray);
        });
    }

    private int MatchDomain(NwCreature creature, NwPlaceable idol, bool getSecondDomain)
    {
        // Get the PC's domain (1 = first domain, 2 = second domain)
        int pcDomain = NWScript.GetDomain(creature, getSecondDomain ? 2 : 1);

        // If PC has no domain in this slot, return -1
        if (pcDomain == -1 || pcDomain == 0)
            return -1;

        // Check if any of the idol's domains (dom_1 through dom_6) match the PC's domain
        for (int i = 1; i <= 6; i++)
        {
            int idolDomain = NWScript.GetLocalInt(idol, sVarName: $"dom_{i}");
            if (idolDomain == pcDomain)
            {
                return pcDomain; // Match found!
            }
        }

        return -1; // No match
    }

    private bool IsValidDruidGod(NwPlaceable idol)
    {
        // Druids can pray to gods with Animal (1), Plant (14), Moon (43), or Sun (17) domains
        // Or gods with the druid_deity flag set

        // Check for druid_deity flag first
        if (NWScript.GetLocalInt(idol, sVarName: "druid_deity") == 1)
            return true;

        // Check if any of the idol's domains are druid-friendly
        for (int i = 1; i <= 6; i++)
        {
            int idolDomain = NWScript.GetLocalInt(idol, sVarName: $"dom_{i}");

            // Domain constants: Animal = 1, Plant = 14, Moon = 43, Sun = 17
            if (idolDomain == 1 ||   // DOMAIN_ANIMAL
                idolDomain == 14 ||  // DOMAIN_PLANT
                idolDomain == 43 ||  // DOMAIN_MOON
                idolDomain == 17)    // DOMAIN_SUN
            {
                return true;
            }
        }

        return false;
    }

    private int GetSuccessRate(NwCreature creature)
    {
        // Calculate success rate for laypeople
        // Base: 40%
        // Bonus: +2% per Paladin/Ranger/Blackguard/Divine Champion level
        // Example: Level 30 Paladin = 40% + (30 × 2%) = 100%

        int baseRate = 40;

        // Get semi-divine class levels
        int paladinLevels = NWScript.GetLevelByClass(NWScript.CLASS_TYPE_PALADIN, creature);
        int rangerLevels = NWScript.GetLevelByClass(NWScript.CLASS_TYPE_RANGER, creature);
        int blackguardLevels = NWScript.GetLevelByClass(NWScript.CLASS_TYPE_BLACKGUARD, creature);
        int divineChampionLevels = NWScript.GetLevelByClass(NWScript.CLASS_TYPE_DIVINECHAMPION, creature);

        int totalSemiDivineLevels = paladinLevels + rangerLevels + blackguardLevels + divineChampionLevels;

        int rate = baseRate + (totalSemiDivineLevels * 2);

        if (rate > 100)
            rate = 100;


        return rate;
    }

    private void CastDeityEffect(NwCreature creature, string deityName, int divineLevel)
    {
        float duration = 300.0f + (divineLevel * 20.0f);
        NwPlayer? player = creature.ControllingPlayer;

        // Normalize deity name for comparison (case-insensitive)
        string deity = deityName.ToLowerInvariant().Trim();

        player?.SendServerMessage($"Adding {deityName} deity effects:", ColorConstants.Cyan);
        player?.SendServerMessage($" - Duration: {duration:F0} seconds", ColorConstants.Cyan);

        // Apply deity-specific effects
        ApplyDeitySpecificEffects(creature, deity, divineLevel, duration, player);
    }

    private void ApplyDeitySpecificEffects(NwCreature creature, string deity, int divineLevel, float duration, NwPlayer? player)
    {
        switch (deity)
        {
            case "aasterinian":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Bluff!, 10), divineLevel);
                player?.SendServerMessage(" - Bluff +10", ColorConstants.Cyan);
                // Add AC vs Lawful on boots (scales with level, caps at +6)
                ApplyACBonusVsAlignmentOnBoots(creature, player, divineLevel, IPAlignmentGroup.Lawful, "Lawful", 1);
                break;

            case "abbathor":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpEvilHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.Regenerate(2, TimeSpan.FromSeconds(duration)), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Appraise!, 5), divineLevel);
                player?.SendServerMessage(" - +1 Regeneration", ColorConstants.Cyan);
                player?.SendServerMessage(" - Appraise +10", ColorConstants.Cyan);
                break;

            case "aerdrie faenya":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadElectricity), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(3, DamageType.Electrical), divineLevel);
                player?.SendServerMessage(" - +3 Electrical Damage", ColorConstants.Cyan);
                // Set weather to rain if outdoors
                if (creature.Area != null && !creature.Area.IsInterior)
                {
                    NWScript.SetWeather(creature.Area, NWScript.WEATHER_RAIN);
                    player?.SendServerMessage(" - The skies open up and a gentle rain begins to fall!", ColorConstants.Cyan);
                }
                else
                {
                    player?.SendServerMessage(" - You must be outdoors to call upon the rain.", ColorConstants.Orange);
                }
                break;

            case "akadi":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadElectricity), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageImmunityIncrease(DamageType.Electrical, 5), divineLevel);
                player?.SendServerMessage(" - +10% Electrical Immunity", ColorConstants.Cyan);
                // Set wing slot to part 6 for the duration
                ApplyTemporaryWings(creature, player, duration, 6);
                break;

            case "angharradh":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 10), divineLevel);
                player?.SendServerMessage(" - Persuade +10", ColorConstants.Cyan);
                // +AB vs Drow (racial type 33), +1 per 4 divine levels, capped at +6
                int angharradhAbBonus = Math.Min(6, Math.Max(1, divineLevel / 4));
                ApplyAttackBonusVsRaceOnWeapon(creature, player, duration, 33, angharradhAbBonus, "Drow");
                break;

            case "anhur":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Intimidate!, 10), divineLevel);
                player?.SendServerMessage(" - Intimidate +10", ColorConstants.Cyan);
                // +AB vs Reptilian, +1 per 4 divine levels, capped at +6
                int anhurAbBonus = Math.Min(6, Math.Max(1, divineLevel / 4));
                ApplyAttackBonusVsRaceOnWeapon(creature, player, duration, NWScript.RACIAL_TYPE_HUMANOID_REPTILIAN, anhurAbBonus, "Reptilian");
                break;

            case "anubis":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadOdd), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Hide!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 2, SavingThrowType.Death), divineLevel);
                player?.SendServerMessage(" - Hide +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - +6 Saves vs. Death", ColorConstants.Cyan);
                break;

            case "arvoreen":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SpellResistanceIncrease(10), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Discipline!, 5), divineLevel);
                player?.SendServerMessage(" - +10 Spell Resistance", ColorConstants.Cyan);
                player?.SendServerMessage(" - Discipine +5", ColorConstants.Cyan);
                break;

            case "asmodeus":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpEvilHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.Will, 2), divineLevel);
                player?.SendServerMessage(" - Will Save +3", ColorConstants.Cyan);
                // Set wing slot to part 99 for the duration
                ApplyTemporaryWings(creature, player, duration, 99);
                break;

            case "astilabor":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Appraise!, 5), divineLevel);
                player?.SendServerMessage(" - Appraise +5", ColorConstants.Cyan);
                // Search bonus: +1 per 10 cleric levels + gold-based bonus, +15 max
                int clericLevels = NWScript.GetLevelByClass(NWScript.CLASS_TYPE_CLERIC, creature);
                int baseSearchBonus = clericLevels / 10;
                int astilaborGold = NWScript.GetGold(creature);
                int goldBonus = astilaborGold switch
                {
                    >= 10000000 => 12,
                    >= 1000000 => 9,
                    >= 100000 => 5,
                    >= 10000 => 3,
                    _ => 1
                };
                int totalSearchBonus = Math.Min(15, baseSearchBonus + goldBonus);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Search!, totalSearchBonus), divineLevel);
                player?.SendServerMessage($" - Search +{totalSearchBonus}", ColorConstants.Cyan);
                break;

            case "auril":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpFrostS), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Intimidate!, 10), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(3, DamageType.Cold), divineLevel);
                player?.SendServerMessage(" - Intimidate +10", ColorConstants.Cyan);
                player?.SendServerMessage(" - +3 Cold Damage", ColorConstants.Cyan);
                break;

            case "azuth":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Spellcraft!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Lore!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.BonusFeat(NwFeat.FromFeatId(7)!), divineLevel);
                player?.SendServerMessage(" - Spellcraft +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Lore +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Bonus Feat: Combat Casting", ColorConstants.Cyan);
                break;

            case "baervan wildwanderer":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHaste), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.MovementSpeedIncrease(10), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Bluff!, 10), divineLevel);
                player?.SendServerMessage(" - +10% Movement Speed", ColorConstants.Cyan);
                player?.SendServerMessage(" - Bluff +10", ColorConstants.Cyan);
                break;

            case "bahamut":
                float bahamutDuration = 300.0f + (divineLevel * 20.0f);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 6, SavingThrowType.MindSpells), divineLevel);
                ApplyTemporaryWings(creature, player, bahamutDuration, 72); // Silver dragon wings
                player?.SendServerMessage(" - +6 Saves vs. Mind Effects", ColorConstants.Cyan);
                break;

            case "bahgtru":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpSuperHeroism), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.AbilityIncrease(Ability.Strength, 2), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Intimidate!, 5), divineLevel);
                player?.SendServerMessage(" - Strength +2", ColorConstants.Cyan);
                player?.SendServerMessage(" - Intimidate +5", ColorConstants.Cyan);
                break;

            case "bane":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpEvilHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Intimidate!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 2), divineLevel);
                player?.SendServerMessage(" - Intimidate +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Universal Saves +2", ColorConstants.Cyan);
                break;

            case "baravar cloakshadow":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), divineLevel, fullDuration: false);
                // +1 Damage at night - check if it's night
                if (NWScript.GetIsNight() == 1)
                {
                    ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(3, DamageType.Divine), divineLevel);
                    player?.SendServerMessage(" - +3 Divine Damage (night bonus active)", ColorConstants.Cyan);
                }
                else
                {
                    player?.SendServerMessage(" - +3 Divine Damage at night (inactive - daytime)", ColorConstants.Yellow);
                }
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 10), divineLevel);
                player?.SendServerMessage(" - Persuade +13", ColorConstants.Cyan);
                break;

            case "berronar truesilver":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 6, SavingThrowType.Fear), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 10), divineLevel);
                player?.SendServerMessage(" - +6 Saves vs. Fear", ColorConstants.Cyan);
                player?.SendServerMessage(" - Persuade +10", ColorConstants.Cyan);
                break;

            case "beshaba":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadOdd), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 6, SavingThrowType.Trap), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Bluff!, 10), divineLevel);
                player?.SendServerMessage(" - +6 Saves vs. Traps", ColorConstants.Cyan);
                player?.SendServerMessage(" - Bluff +10", ColorConstants.Cyan);
                break;

            case "bhaal":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpDeath), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.BonusFeat(NwFeat.FromFeatId(31)!), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Intimidate!, 10), divineLevel);
                player?.SendServerMessage(" - Bonus Feat: Sap", ColorConstants.Cyan);
                player?.SendServerMessage(" - Intimidate +10", ColorConstants.Cyan);
                break;

            case "brandobaris":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 2, SavingThrowType.Chaos), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Bluff!, 5), divineLevel);
                player?.SendServerMessage(" - +3 Saves vs. Chaos", ColorConstants.Cyan);
                player?.SendServerMessage(" - +10 Bluff", ColorConstants.Cyan);
                break;

            case "callarduran smoothhands":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurMagicalSight), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.Ultravision(), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Spot!, 10), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Appraise!, 5), divineLevel);
                player?.SendServerMessage(" - True Seeing (As the Spell)", ColorConstants.Cyan);
                player?.SendServerMessage(" - +5 Appraise", ColorConstants.Cyan);
                break;

            case "chauntea":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadNature), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.Fortitude, 3), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 10), divineLevel);
                player?.SendServerMessage(" - Fortitude Save +3", ColorConstants.Cyan);
                player?.SendServerMessage(" - +10 Persuade", ColorConstants.Cyan);
                break;

            case "clangeddin silverbeard":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadHoly), divineLevel, fullDuration: false);
                ApplyVersusRacialTypeWeaponBonus(creature, player!, divineLevel, RacialType.Giant, 3, "Giants");
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Intimidate!, 10), divineLevel);
                player?.SendServerMessage(" - Intimidate +10", ColorConstants.Cyan);
                break;

            case "corellon larethian":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                ApplyVersusRacialTypeWeaponBonus(creature, player!, divineLevel, RacialType.HumanoidOrc, 3, "Orcs");
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 10), divineLevel);
                player?.SendServerMessage(" - Persuade +10", ColorConstants.Cyan);
                break;

            case "cyric":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpEvilHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Bluff!, 10), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 3, SavingThrowType.Law), divineLevel);
                player?.SendServerMessage(" - Bluff +10", ColorConstants.Cyan);
                player?.SendServerMessage(" - +3 Saves vs. Law", ColorConstants.Cyan);
                break;

            case "cyrrollalee":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 10), divineLevel);
                // +1 Saves vs. Chaos
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 3, SavingThrowType.Chaos), divineLevel);
                player?.SendServerMessage(" - Persuade +10", ColorConstants.Cyan);
                player?.SendServerMessage(" - +3 Saves vs. Chaos", ColorConstants.Cyan);
                break;

            case "dallah thaun":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadOdd), divineLevel, fullDuration: false);
                // Shadow Shield VFX for the duration
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurProtShadowArmor), divineLevel);
                // +3 Divine Damage vs multiple races
                ApplyVersusRacialTypeWeaponBonus(creature, player!, divineLevel, RacialType.Human, 3, "Humans");
                ApplyVersusRacialTypeWeaponBonus(creature, player!, divineLevel, RacialType.Dwarf, 3, "Dwarves");
                ApplyVersusRacialTypeWeaponBonus(creature, player!, divineLevel, RacialType.HalfOrc, 3, "Half-Orcs");
                ApplyVersusRacialTypeWeaponBonus(creature, player!, divineLevel, RacialType.Elf, 3, "Elves");
                ApplyVersusRacialTypeWeaponBonus(creature, player!, divineLevel, RacialType.HalfElf, 3, "Half-Elves");
                ApplyVersusRacialTypeWeaponBonus(creature, player!, divineLevel, RacialType.HumanoidOrc, 3, "Orcs");
                ApplyVersusRacialTypeWeaponBonus(creature, player!, divineLevel, RacialType.HumanoidReptilian, 3, "Trolls");
                ApplyVersusRacialTypeWeaponBonus(creature, player!, divineLevel, RacialType.HumanoidMonstrous, 3, "Monstrous Humanoids");
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Bluff!, 10), divineLevel);
                player?.SendServerMessage(" - Shadow Shield VFX", ColorConstants.Cyan);
                player?.SendServerMessage(" - Bluff +10", ColorConstants.Cyan);
                break;

            case "deep duerra":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpEvilHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 3, SavingThrowType.Good), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Discipline!, 5), divineLevel);
                player?.SendServerMessage(" - +3 Saves vs. Good", ColorConstants.Cyan);
                player?.SendServerMessage(" - +10 Lore", ColorConstants.Cyan);
                break;

            case "deep sashelas":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpFrostS), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageResistance(DamageType.Cold, 10), divineLevel);
                player?.SendServerMessage(" - 10/- Cold Resist", ColorConstants.Cyan);
                player?.SendServerMessage(" - +10 Lore", ColorConstants.Cyan);
                break;

            case "deneir":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Lore!, 15), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.BonusFeat(NwFeat.FromFeatId(378)!), divineLevel); // Artist feat
                player?.SendServerMessage(" - Lore +10", ColorConstants.Cyan);
                player?.SendServerMessage(" - Bonus Feat: Artist", ColorConstants.Cyan);
                break;

            case "dugmaren brightmantle":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Lore!, 10), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(3, DamageType.Magical), divineLevel);
                player?.SendServerMessage(" - Lore +10", ColorConstants.Cyan);
                player?.SendServerMessage(" - +3 Magic Damage", ColorConstants.Cyan);
                break;

            case "dumathoin":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadOdd), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 3, SavingThrowType.Death), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Lore!, 10), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.BonusFeat(NwFeat.FromFeatId(44)!), divineLevel); // Weapon Proficiency Exotic
                player?.SendServerMessage(" - +3 Saves vs. Death", ColorConstants.Cyan);
                player?.SendServerMessage(" - Lore +10", ColorConstants.Cyan);
                player?.SendServerMessage(" - Bonus Feat: Weapon Proficiency Exotic", ColorConstants.Cyan);
                break;

            case "eilistraee":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurBardSong), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Perform!, 5), divineLevel);
                // +AB vs Evil at night (starts at +2, scales with level, caps at +6)
                if (NWScript.GetIsNight() == 1)
                {
                    ApplyAttackBonusVsAlignmentOnWeapon(creature, player, divineLevel, IPAlignmentGroup.Evil, "Evil", 2);
                }
                else
                {
                    player?.SendServerMessage(" - AB vs Evil at night (inactive - daytime)", ColorConstants.Yellow);
                }
                player?.SendServerMessage(" - Bard Song VFX", ColorConstants.Cyan);
                player?.SendServerMessage(" - Perform +5", ColorConstants.Cyan);
                break;

            case "eldath":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadNature), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageResistance(DamageType.Bludgeoning, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageResistance(DamageType.Slashing, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 5), divineLevel);
                player?.SendServerMessage(" - 5/- Bludgeoning Resistance", ColorConstants.Cyan);
                player?.SendServerMessage(" - 5/- Slashing Resistance", ColorConstants.Cyan);
                player?.SendServerMessage(" - Persuade +5", ColorConstants.Cyan);
                break;

            case "erevan ilesere":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.BonusFeat(NwFeat.FromFeatId(9)!), divineLevel); // Evasion feat
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Bluff!, 5), divineLevel);
                player?.SendServerMessage(" - Bonus Feat: Evasion", ColorConstants.Cyan);
                player?.SendServerMessage(" - Bluff +5", ColorConstants.Cyan);
                break;

            case "faluzure":
                float faluzureDuration = 300.0f + (divineLevel * 20.0f);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpEvilHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 3, SavingThrowType.Death), divineLevel);
                ApplyTemporaryWings(creature, player, faluzureDuration, 96);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Intimidate!, 5), divineLevel);
                player?.SendServerMessage(" - +6 Saves vs. Death", ColorConstants.Cyan);
                player?.SendServerMessage(" - Intimidate +5", ColorConstants.Cyan);
                break;

            case "fenmarel mestarine":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurGhostlyVisageNoSound), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Lore!, 8), divineLevel);
                // +1 AB while outdoors
                NwArea? fenmarelArea = creature.Area;
                if (fenmarelArea != null && !fenmarelArea.IsInterior)
                {
                    ApplyPrayerEffectsToPCs(creature, Effect.AttackIncrease(1), divineLevel);
                    player?.SendServerMessage(" - +1 AB (outdoors bonus active)", ColorConstants.Cyan);
                }
                else
                {
                    player?.SendServerMessage(" - +1 AB outdoors (inactive - indoors)", ColorConstants.Yellow);
                }
                player?.SendServerMessage(" - Lore +8", ColorConstants.Cyan);
                break;

            case "finder wyvernspur":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurBardSong), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Perform!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Bluff!, 10), divineLevel);
                player?.SendServerMessage(" - Bard Song VFX", ColorConstants.Cyan);
                player?.SendServerMessage(" - Perform +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Bluff +10", ColorConstants.Cyan);
                break;

            case "flandal steelskin":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadFire), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.CraftArmor!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.CraftWeapon!, 5), divineLevel);
                player?.SendServerMessage(" - Craft Armor +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Craft Weapon +5", ColorConstants.Cyan);
                break;

            case "gaerdal ironhand":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                ApplyVersusRacialTypeWeaponBonus(creature, player!, divineLevel, RacialType.HumanoidGoblinoid, 3, "Goblinoids");
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 10), divineLevel);
                player?.SendServerMessage(" - Persuade +10", ColorConstants.Cyan);
                break;

            case "garagos":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadEvil), divineLevel, fullDuration: false);
                ApplyAttackBonusVsAlignmentOnWeapon(creature, player, divineLevel, IPAlignmentGroup.Good, "Good", 1);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Intimidate!, 10), divineLevel);
                player?.SendServerMessage(" - Intimidate +10", ColorConstants.Cyan);
                break;

            case "gargauth":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpEvilHelp), divineLevel, fullDuration: false);
                ApplyACBonusVsAlignmentOnBoots(creature, player, divineLevel, IPAlignmentGroup.Good, "Good", 1);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Intimidate!, 10), divineLevel);
                player?.SendServerMessage(" - Intimidate +10", ColorConstants.Cyan);
                break;

            case "garl glittergold":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), divineLevel, fullDuration: false);
                ApplyVersusAlignmentWeaponBonus(creature, player!, divineLevel, IPAlignmentGroup.Chaotic, 3, "Chaotic");
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.SetTrap!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Bluff!, 5), divineLevel);
                player?.SendServerMessage(" - Set Trap +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Bluff +5", ColorConstants.Cyan);
                break;

            case "garyx":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadFire), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(6, DamageType.Fire), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Intimidate!, 5), divineLevel);
                player?.SendServerMessage(" - +6 Fire Damage", ColorConstants.Cyan);
                player?.SendServerMessage(" - Intimidate +5", ColorConstants.Cyan);
                break;

            case "geb":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadAcid), divineLevel, fullDuration: false);
                CreateDeityHenchman(creature, player!, divineLevel, "pray_geb", 1, VfxType.FnfScreenShake, "Geb Servant");
                break;

            case "ghaunadaur":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpAcidS), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 6, SavingThrowType.Poison), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Intimidate!, 10), divineLevel);
                player?.SendServerMessage(" - +6 Saves vs. Poison", ColorConstants.Cyan);
                player?.SendServerMessage(" - Intimidate +10", ColorConstants.Cyan);

                break;

            case "gond":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadFire), divineLevel, fullDuration: false);
                CreateDeityHenchman(creature, player!, divineLevel, "pray_gond", 1, VfxType.FnfTimeStop, "Gond Construct");
                break;

            case "gorm gulthym":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(3, DamageType.Divine), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.SetTrap!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 5), divineLevel);
                player?.SendServerMessage(" - +3 Divine Damage", ColorConstants.Cyan);
                player?.SendServerMessage(" - Set Trap +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Persuade +5", ColorConstants.Cyan);
                break;

            case "grazzt":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpEvilHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 10), divineLevel);
                ApplyACBonusVsAlignmentOnBoots(creature, player, divineLevel, IPAlignmentGroup.Good, "Good", 1);
                player?.SendServerMessage(" - Persuade +10", ColorConstants.Cyan);
                break;

            case "grumbar":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadAcid), divineLevel, fullDuration: false);
                // Summon earth elemental henchmen based on divine level
                string grumbarResref;
                int grumbarCount;
                if (divineLevel <= 10)
                {
                    grumbarResref = "pray_grumbar1";
                    grumbarCount = 2;
                }
                else if (divineLevel <= 17)
                {
                    grumbarResref = "pray_grumbar2";
                    grumbarCount = 3;
                }
                else
                {
                    grumbarResref = "pray_grumbar3";
                    grumbarCount = 4;
                }
                CreateDeityHenchman(creature, player!, divineLevel, grumbarResref, grumbarCount, VfxType.FnfMeteorSwarm, "Earth Elemental");
                break;

            case "gruumsh":
                float gruumshDuration = 300.0f + (divineLevel * 20.0f);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadEvil), divineLevel, fullDuration: false);
                ApplyAttackBonusVsRaceOnWeapon(creature, player, gruumshDuration, (int)RacialType.Elf, 1, "Elves");
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Intimidate!, 10), divineLevel);
                player?.SendServerMessage(" - Intimidate +10", ColorConstants.Cyan);
                break;

            case "gwaeron windstrom":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadNature), divineLevel, fullDuration: false);
                ApplyVersusRacialTypeWeaponBonus(creature, player!, divineLevel, RacialType.Giant, 3, "Giants");
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 5), divineLevel);
                player?.SendServerMessage(" - Persuade +5", ColorConstants.Cyan);
                break;

            case "haela brightaxe":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 3, SavingThrowType.Evil), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 10), divineLevel);
                player?.SendServerMessage(" - Persuade +10", ColorConstants.Cyan);
                player?.SendServerMessage(" - +3 Saves vs. Evil", ColorConstants.Cyan);

                break;

            case "hanali celanil":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadFire), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(2, DamageType.Fire), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 10), divineLevel);
                player?.SendServerMessage(" - Persuade +10", ColorConstants.Cyan);
                player?.SendServerMessage(" - +3 Fire Damage", ColorConstants.Cyan);
                break;

            case "hathor":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadNature), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Heal!, 10), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 5), divineLevel);
                player?.SendServerMessage(" - Heal +10", ColorConstants.Cyan);
                player?.SendServerMessage(" - Persuade +5", ColorConstants.Cyan);
                break;

            case "helm":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageImmunityIncrease(DamageType.Bludgeoning, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Lore!, 5), divineLevel);
                player?.SendServerMessage(" - 5% Bludgeoning Damage Immunity", ColorConstants.Cyan);
                player?.SendServerMessage(" - Lore +5", ColorConstants.Cyan);
                break;

            case "hlal":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Bluff!, 10), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.BonusFeat(NwFeat.FromFeatId(423)!), divineLevel);
                player?.SendServerMessage(" - Bluff +10", ColorConstants.Cyan);
                player?.SendServerMessage(" - Bonus Feat: Extra Music", ColorConstants.Cyan);
                break;

            case "hoar":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurProtectionEvilMinor), divineLevel, fullDuration: false);
                // +2 Bludgeoning biteback - damage shield
                ApplyPrayerEffectsToPCs(creature, Effect.DamageShield(2, DamageBonus.Plus1, DamageType.Bludgeoning), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Intimidate!, 5), divineLevel);
                player?.SendServerMessage(" - +2 Bludgeoning Damage Shield (biteback)", ColorConstants.Cyan);
                player?.SendServerMessage(" - Intimidate +5", ColorConstants.Cyan);
                break;

            case "horus-re":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadHoly), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 3, SavingThrowType.Evil), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Lore!, 10), divineLevel);
                player?.SendServerMessage(" - +3 Saves vs. Evil", ColorConstants.Cyan);
                player?.SendServerMessage(" - Lore +10", ColorConstants.Cyan);
                break;

            case "ibrandul":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadOdd), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurAntiLight10), divineLevel);
                // +AB vs Law (indoor bonus only) as item property
                NwArea? ibrandulArea = creature.Area;
                if (ibrandulArea != null && ibrandulArea.IsInterior)
                {
                    ApplyAttackBonusVsAlignmentOnWeapon(creature, player, divineLevel, IPAlignmentGroup.Lawful, "Lawful", 1);
                }
                else
                {
                    player?.SendServerMessage(" - AB vs Lawful indoors (inactive - outdoors)", ColorConstants.Yellow);
                }
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Bluff!, 5), divineLevel);
                player?.SendServerMessage(" - Anti-Light Aura", ColorConstants.Cyan);
                player?.SendServerMessage(" - Bluff +5", ColorConstants.Cyan);
                break;

            case "ilmater":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHealingS), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.BonusFeat(NwFeat.FromFeatId(747)!), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 5), divineLevel);
                player?.SendServerMessage(" - Bonus Feat: Perfect Health", ColorConstants.Cyan);
                player?.SendServerMessage(" - Persuade +5", ColorConstants.Cyan);
                break;

            case "ilneval":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpEvilHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(3, DamageType.Divine), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Taunt!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Intimidate!, 5), divineLevel);
                player?.SendServerMessage(" - +3 Divine Damage", ColorConstants.Cyan);
                player?.SendServerMessage(" - Taunt +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Intimidate +5", ColorConstants.Cyan);
                break;

            case "io":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpUnsummon), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 2), divineLevel);
                player?.SendServerMessage(" - Universal Saves +2", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 5), divineLevel);
                player?.SendServerMessage(" - Persuade +5", ColorConstants.Cyan);
                break;

            case "isis":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Lore!, 10), divineLevel);
                ApplyVersusAlignmentWeaponBonus(creature, player!, divineLevel, IPAlignmentGroup.Evil, 3, "Evil");
                player?.SendServerMessage(" - Lore +10", ColorConstants.Cyan);
                break;

            case "istishia":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpFrostS), divineLevel, fullDuration: false);

                // First list: Combat blessings (5% chance of nothing)
                int combatRoll = Random.Shared.Next(1, 101); // 1-100
                if (combatRoll <= 5)
                {
                    player?.SendServerMessage(" - Istishia withholds his combat blessing...", ColorConstants.Yellow);
                }
                else
                {
                    int combatBlessing = Random.Shared.Next(1, 5); // 1-4
                    switch (combatBlessing)
                    {
                        case 1:
                            ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(3, DamageType.Cold), divineLevel);
                            player?.SendServerMessage(" - +3 Cold Damage", ColorConstants.Cyan);
                            break;
                        case 2:
                            ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 6, SavingThrowType.Fire), divineLevel);
                            player?.SendServerMessage(" - +6 Saves vs. Fire", ColorConstants.Cyan);
                            break;
                        case 3:
                            // +1 AC vs LG and CG
                            ApplyACBonusVsSpecificAlignmentOnBoots(creature, player, divineLevel, IPAlignment.LawfulGood, "Lawful Good", 1);
                            ApplyACBonusVsSpecificAlignmentOnBoots(creature, player, divineLevel, IPAlignment.ChaoticGood, "Chaotic Good", 1);
                            break;
                        case 4:
                            // +1 AB vs LE and CE
                            ApplyAttackBonusVsSpecificAlignmentOnWeapon(creature, player, divineLevel, IPAlignment.LawfulEvil, "Lawful Evil", 1);
                            ApplyAttackBonusVsSpecificAlignmentOnWeapon(creature, player, divineLevel, IPAlignment.ChaoticEvil, "Chaotic Evil", 1);
                            break;
                    }
                }

                // Second list: Skill blessings (5% chance of nothing)
                int skillRoll = Random.Shared.Next(1, 101); // 1-100
                if (skillRoll <= 5)
                {
                    player?.SendServerMessage(" - Istishia withholds his skill blessing...", ColorConstants.Yellow);
                }
                else
                {
                    int skillBlessing = Random.Shared.Next(1, 5); // 1-4
                    switch (skillBlessing)
                    {
                        case 1:
                            ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Bluff!, 15), divineLevel);
                            player?.SendServerMessage(" - Bluff +15", ColorConstants.Cyan);
                            break;
                        case 2:
                            ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 15), divineLevel);
                            player?.SendServerMessage(" - Persuade +15", ColorConstants.Cyan);
                            break;
                        case 3:
                            ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Lore!, 15), divineLevel);
                            player?.SendServerMessage(" - Lore +15", ColorConstants.Cyan);
                            break;
                        case 4:
                            ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Intimidate!, 15), divineLevel);
                            player?.SendServerMessage(" - Intimidate +15", ColorConstants.Cyan);
                            break;
                    }
                }
                break;

            case "jergal":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadOdd), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Lore!, 10), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 3, SavingThrowType.Death), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(2, DamageType.Divine), divineLevel);
                player?.SendServerMessage(" - Lore +10", ColorConstants.Cyan);
                player?.SendServerMessage(" - +3 Saves vs. Death", ColorConstants.Cyan);
                player?.SendServerMessage(" - +2 Divine Damage", ColorConstants.Cyan);
                break;

            case "kelemvor":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadOdd), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 6, SavingThrowType.Death), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(2, DamageType.Positive), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(2, DamageType.Cold), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 5), divineLevel);
                player?.SendServerMessage(" - +6 Saves vs. Death", ColorConstants.Cyan);
                player?.SendServerMessage(" - +2 Positive Damage", ColorConstants.Cyan);
                player?.SendServerMessage(" - +2 Cold Damage", ColorConstants.Cyan);
                player?.SendServerMessage(" - Persuade +5", ColorConstants.Cyan);
                break;

            case "kiaransalee":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpEvilHelp), divineLevel, fullDuration: false);
                ApplyACBonusVsAlignmentOnBoots(creature, player, divineLevel, IPAlignmentGroup.Good, "Good");
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 3, SavingThrowType.Death), divineLevel);

                // Apply race/gender-based spirit VFX
                int spiritVfx = GetKiaransaleeSpiritVfx(creature);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect((VfxType)spiritVfx), divineLevel);

                player?.SendServerMessage(" - +3 Saves vs. Death", ColorConstants.Cyan);
                player?.SendServerMessage(" - Spirit Friend", ColorConstants.Cyan);
                break;

            case "kossuth":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadFire), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 6, SavingThrowType.Fire), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(3, DamageType.Divine), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Intimidate!, 5), divineLevel);
                player?.SendServerMessage(" - +6 Saves vs. Fire", ColorConstants.Cyan);
                player?.SendServerMessage(" - +3 Divine Damage", ColorConstants.Cyan);
                player?.SendServerMessage(" - Intimidate +5", ColorConstants.Cyan);
                break;

            case "kurtulmak":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadOdd), divineLevel, fullDuration: false);
                ApplyVersusRacialTypeWeaponBonus(creature, player!, divineLevel, RacialType.Gnome, 3, "Gnomes");
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.SetTrap!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Intimidate!, 5), divineLevel);
                player?.SendServerMessage(" - Set Trap +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Intimidate +5", ColorConstants.Cyan);
                break;

            case "labelas enoreth":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Lore!, 5), divineLevel);
                // Apply OnHit: Slow (50%, 2 rounds) as temporary weapon property
                ApplyOnHitSlowWeaponProperty(creature, player, divineLevel);
                player?.SendServerMessage(" - Lore +5", ColorConstants.Cyan);
                break;

            case "laduguer":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpEvilHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Spellcraft!, 5), divineLevel);
                ApplyVersusAlignmentWeaponBonus(creature, player!, divineLevel, IPAlignmentGroup.Chaotic, 3, "Chaotic");
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Ride!, 5), divineLevel);
                player?.SendServerMessage(" - Spellcraft +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Ride +5", ColorConstants.Cyan);
                break;

            case "lathander":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadHoly), divineLevel, fullDuration: false);
                ApplyAttackBonusVsAlignmentOnWeapon(creature, player, divineLevel, IPAlignmentGroup.Evil, "Evil");
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Perform!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 5), divineLevel);
                player?.SendServerMessage(" - Perform +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Persuade +5", ColorConstants.Cyan);
                break;

            case "leira":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Bluff!, 10), divineLevel);
                // Apply +3 Vampiric Regeneration as temporary weapon property
                ApplyVampiricRegenerationWeaponProperty(creature, player, divineLevel, 3);
                player?.SendServerMessage(" - Bluff +10", ColorConstants.Cyan);
                break;

            case "lendys":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 6, SavingThrowType.Fear), divineLevel);
                ApplyVersusAlignmentWeaponBonus(creature, player!, divineLevel, IPAlignmentGroup.Chaotic, 3, "Chaotic");
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Lore!, 5), divineLevel);
                player?.SendServerMessage(" - +6 Saves vs. Fear", ColorConstants.Cyan);
                player?.SendServerMessage(" - Lore +5", ColorConstants.Cyan);
                break;

            case "lliira":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpCharm), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Perform!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.BonusFeat(NwFeat.FromFeatId(423)!), divineLevel);
                player?.SendServerMessage(" - Bonus Feat: Extra Music", ColorConstants.Cyan);
                player?.SendServerMessage(" - Perform +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Persuade 5", ColorConstants.Cyan);
                break;

            case "lolth":
                float lolthDuration = 300.0f + (divineLevel * 20.0f);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpEvilHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.Regenerate(1, TimeSpan.FromSeconds(6.0)), divineLevel);
                ApplyAttackBonusVsRaceOnWeapon(creature, player, lolthDuration, (int)RacialType.Elf, 1, "Elves");
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Bluff!, 5), divineLevel);
                player?.SendServerMessage(" - +1 Regeneration", ColorConstants.Cyan);
                player?.SendServerMessage(" - Bluff +5", ColorConstants.Cyan);
                break;

            case "loviatar":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpEvilHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageShield(2, DamageBonus.Plus1, DamageType.Slashing), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Intimidate!, 5), divineLevel);
                player?.SendServerMessage(" - +2 Slashing Damage Shield", ColorConstants.Cyan);
                player?.SendServerMessage(" - Intimidate +5", ColorConstants.Cyan);
                break;

            case "lurue":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadNature), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Ride!, 5), divineLevel);
                ApplyAttackBonusVsAlignmentOnWeapon(creature, player, divineLevel, IPAlignmentGroup.Evil, "Evil");
                ApplyPrayerEffectsToPCs(creature, Effect.BonusFeat(NwFeat.FromFeatId(421)!), divineLevel); // Arcane Defense: Necromancy
                player?.SendServerMessage(" - Ride +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Bonus Feat: Arcane Defense (Necromancy)", ColorConstants.Cyan);
                break;

            case "luthic":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadOdd), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Intimidate!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(1, DamageType.Divine), divineLevel);
                player?.SendServerMessage(" - Intimidate +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - +3 Divine Damage", ColorConstants.Cyan);
                break;

            case "maglubiyet":
                float maglubiyetDuration = 300.0f + (divineLevel * 20.0f);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpEvilHelp), divineLevel, fullDuration: false);
                ApplyACBonusVsAlignmentOnBoots(creature, player, divineLevel, IPAlignmentGroup.Good, "Good", 1);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(3, DamageType.Divine), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Bluff!, 5), divineLevel);
                ApplyTemporaryScale(creature, player, maglubiyetDuration, 2.0f);
                player?.SendServerMessage(" - +3 Divine Damage", ColorConstants.Cyan);
                player?.SendServerMessage(" - Bluff +5", ColorConstants.Cyan);
                break;

            case "malar":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadEvil), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(3, DamageType.Divine), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 6, SavingThrowType.Law), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Intimidate!, 5), divineLevel);
                player?.SendServerMessage(" - +3 Divine Damage", ColorConstants.Cyan);
                player?.SendServerMessage(" - +6 Saves vs. Law", ColorConstants.Cyan);
                player?.SendServerMessage(" - Intimidate +5", ColorConstants.Cyan);
                break;

            case "marthammor duin":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Search!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Bluff!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 6, SavingThrowType.Trap), divineLevel);
                player?.SendServerMessage(" - +6 Saves vs. Traps", ColorConstants.Cyan);
                player?.SendServerMessage(" - Search +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Bluff +5", ColorConstants.Cyan);
                break;

            case "mask":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurGhostlyVisageNoSound), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Hide!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Bluff!, 10), divineLevel);
                player?.SendServerMessage(" - Hide +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Bluff +10", ColorConstants.Cyan);
                break;

            case "mephistopheles":
                float mephistophelesDuration = 300.0f + (divineLevel * 20.0f);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadFire), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(3, DamageType.Fire), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageImmunityIncrease(DamageType.Fire, 10), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 10), divineLevel);
                ApplyTemporaryWings(creature, player, mephistophelesDuration, 93);
                player?.SendServerMessage(" - +3 Fire Damage", ColorConstants.Cyan);
                player?.SendServerMessage(" - +10% Fire Immunity", ColorConstants.Cyan);
                player?.SendServerMessage(" - Persuade +10", ColorConstants.Cyan);
                break;

            case "mielikki":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadNature), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.AnimalEmpathy!, 10), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 5), divineLevel);
                player?.SendServerMessage(" - Animal Empathy +10", ColorConstants.Cyan);
                player?.SendServerMessage(" - Persuade +5", ColorConstants.Cyan);
                break;

            case "milil":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpCharm), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Perform!, 5), divineLevel);
                ApplyACBonusVsAlignmentOnBoots(creature, player, divineLevel, IPAlignmentGroup.Evil, "Evil", 1);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Lore!, 5), divineLevel);
                player?.SendServerMessage(" - Perform +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Lore +5", ColorConstants.Cyan);
                break;

            case "moander":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpAcidS), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(2, DamageType.Acid), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Taunt!, 3), divineLevel);
                // VFX_DUR_TYRANTFOG (2542) for duration
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect((VfxType)2542), divineLevel);
                // Apply COM_CHUNK_YELLOW_MEDIUM at 30 second intervals
                ApplyRepeatingChunkVfx(creature, player, divineLevel);
                player?.SendServerMessage(" - +2 Acid Damage", ColorConstants.Cyan);
                player?.SendServerMessage(" - Taunt +3", ColorConstants.Cyan);
                player?.SendServerMessage(" - Tyrant Fog Aura", ColorConstants.Cyan);
                break;

            case "moradin":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.CraftArmor!, 5), divineLevel);
                ApplyACBonusVsRaceOnBoots(creature, player, divineLevel, RacialType.Giant, "Giants");
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.CraftWeapon!, 5), divineLevel);
                player?.SendServerMessage(" - Craft Armor +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Craft Weapon +5", ColorConstants.Cyan);
                break;

            case "myrkul":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadOdd), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 2, SavingThrowType.Death), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(3, DamageType.Negative), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Bluff!, 5), divineLevel);
                player?.SendServerMessage(" - +2 Saves vs. Death", ColorConstants.Cyan);
                player?.SendServerMessage(" - +3 Negative Damage", ColorConstants.Cyan);
                player?.SendServerMessage(" - Bluff +5", ColorConstants.Cyan);
                break;

            case "mystra":
                float mystraDuration = 300.0f + (divineLevel * 20.0f);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Spellcraft!, 10), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Lore!, 5), divineLevel);
                ApplyTemporaryPhenotype(creature, player, mystraDuration, 19);
                player?.SendServerMessage(" - Spellcraft +10", ColorConstants.Cyan);
                player?.SendServerMessage(" - Lore +5", ColorConstants.Cyan);
                break;

            case "nephthys":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadOdd), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Appraise!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 6, SavingThrowType.Death), divineLevel);
                // Give 1000 gold per divine level
                int nephthysGold = divineLevel * 1000;
                creature.GiveGold(nephthysGold);
                player?.SendServerMessage(" - Appraise +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - +6 Saves vs. Death", ColorConstants.Cyan);
                player?.SendServerMessage($" - You have pleased Nephthys! She showers you in {nephthysGold} gold pieces", ColorConstants.Cyan);
                break;

            case "nobanion":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 6, SavingThrowType.Fear), divineLevel);
                ApplyAttackBonusVsAlignmentOnWeapon(creature, player, divineLevel, IPAlignmentGroup.Evil, "Evil", 1);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 5), divineLevel);
                player?.SendServerMessage(" - +6 Saves vs. Fear", ColorConstants.Cyan);
                player?.SendServerMessage(" - Persuade +5", ColorConstants.Cyan);
                break;

            case "oberon":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadNature), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurPixiedust), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Lore!, 10), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(3, DamageType.Divine), divineLevel);
                player?.SendServerMessage(" - Pixie Dust Aura", ColorConstants.Cyan);
                player?.SendServerMessage(" - Lore +10", ColorConstants.Cyan);
                player?.SendServerMessage(" - +3 Divine Damage", ColorConstants.Cyan);
                break;

            case "oghma":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Lore!, 10), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 10, SavingThrowType.Fear), divineLevel);
                player?.SendServerMessage(" - Lore +10", ColorConstants.Cyan);
                player?.SendServerMessage(" - +6 Saves vs. Fear", ColorConstants.Cyan);
                break;

            case "orcus":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpEvilHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(3, DamageType.Divine), divineLevel);
                ApplyACBonusVsAlignmentOnBoots(creature, player, divineLevel, IPAlignmentGroup.Good, "Good", 1);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Intimidate!, 5), divineLevel);
                player?.SendServerMessage(" - +3 Divine Damage", ColorConstants.Cyan);
                player?.SendServerMessage(" - Intimidate +5", ColorConstants.Cyan);
                break;

            case "osiris":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadHoly), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 6, SavingThrowType.Death), divineLevel);
                ApplyVersusAlignmentWeaponBonus(creature, player!, divineLevel, IPAlignmentGroup.Evil, 3, "Evil");
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 5), divineLevel);
                player?.SendServerMessage(" - +6 Saves vs. Death", ColorConstants.Cyan);
                player?.SendServerMessage(" - Persuade +5", ColorConstants.Cyan);
                break;

            case "pazuzu":
                float pazuzuDuration = 300.0f + (divineLevel * 20.0f);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpEvilHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Intimidate!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 5), divineLevel);
                ApplyACBonusVsAlignmentOnBoots(creature, player, divineLevel, IPAlignmentGroup.Lawful, "Lawful", 1);
                ApplyTemporaryWings(creature, player, pazuzuDuration, 312);
                player?.SendServerMessage(" - Intimidate +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Persuade +5", ColorConstants.Cyan);
                break;

            case "queen of air and darkness":
            case "queenofairanddarkness":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadElectricity), divineLevel, fullDuration: false);
                ApplyVersusAlignmentWeaponBonus(creature, player!, divineLevel, IPAlignmentGroup.Lawful, 3, "Lawful");
                ApplyNaturalACBonusVsAlignmentOnAmulet(creature, player, divineLevel, IPAlignmentGroup.Good, "Good");
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Bluff!, 5), divineLevel);
                player?.SendServerMessage(" - Bluff +5", ColorConstants.Cyan);
                break;

            case "red knight":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Ride!, 10), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.Fortitude, 3), divineLevel);
                player?.SendServerMessage(" - Ride +10", ColorConstants.Cyan);
                player?.SendServerMessage(" - Fortitude Saves +3", ColorConstants.Cyan);
                break;

            case "rillifane rallathil":
                float rillifaneDuration = 300.0f + (divineLevel * 20.0f);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadNature), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Hide!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 3, SavingThrowType.Law), divineLevel);
                ApplyTemporaryAppearanceAndScale(creature, player, rillifaneDuration, 2000, 0.5f);
                player?.SendServerMessage(" - Hide +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - +3 Saves vs. Law", ColorConstants.Cyan);
                break;

            case "salandra":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHealingS), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Heal!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(3, DamageType.Divine), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 5), divineLevel);
                player?.SendServerMessage(" - Heal +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - +3 Divine Damage", ColorConstants.Cyan);
                player?.SendServerMessage(" - Persuade +5", ColorConstants.Cyan);
                break;

            case "savras":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), divineLevel, fullDuration: false);
                // Damage Reduction with progression: 1/+1 per 4 divine levels, capped at 5/+5
                int savrasDRAmount = Math.Min(5, 1 + (divineLevel / 4));
                ApplyPrayerEffectsToPCs(creature, Effect.DamageReduction(savrasDRAmount, DamagePower.Plus1 + (savrasDRAmount - 1)), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 3, SavingThrowType.Spell), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Lore!, 5), divineLevel);
                player?.SendServerMessage($" - Damage Reduction {savrasDRAmount}/+{savrasDRAmount}", ColorConstants.Cyan);
                player?.SendServerMessage(" - +3 Saves vs. Spells", ColorConstants.Cyan);
                player?.SendServerMessage(" - Lore +5", ColorConstants.Cyan);
                break;

            case "sebek":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpSuperHeroism), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.AbilityIncrease(Ability.Strength, 1), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 6, SavingThrowType.Poison), divineLevel);
                player?.SendServerMessage(" - Strength +1", ColorConstants.Cyan);
                player?.SendServerMessage(" - +6 Saves vs. Poison", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Bluff!, 5), divineLevel);
                player?.SendServerMessage(" - Bluff +5", ColorConstants.Cyan);
                break;

            case "segojan earthcaller":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpAcidS), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(3, DamageType.Acid), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Appraise!, 10), divineLevel);
                player?.SendServerMessage(" - +3 Acid Damage", ColorConstants.Cyan);
                player?.SendServerMessage(" - Appraise +10", ColorConstants.Cyan);
                break;

            case "sehanine moonbow":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpFrostS), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.Fortitude, 3), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 10), divineLevel);
                player?.SendServerMessage(" - Fortitude Save +3", ColorConstants.Cyan);
                player?.SendServerMessage(" - +10 Persuade", ColorConstants.Cyan);
                break;

            case "selune":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadHoly), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Heal!, 5), divineLevel);
                // +3 Divine Damage vs Evil at night
                if (NWScript.GetIsNight() == 1)
                {
                    ApplyVersusAlignmentWeaponBonus(creature, player!, divineLevel, IPAlignmentGroup.Evil, 3, "Evil");
                }
                else
                {
                    player?.SendServerMessage(" - +3 Divine Damage vs Evil at night (inactive - daytime)", ColorConstants.Yellow);
                }
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 5), divineLevel);
                player?.SendServerMessage(" - Heal +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Persuade +5", ColorConstants.Cyan);
                break;

            case "selvetarm":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpEvilHelp), divineLevel, fullDuration: false);
                ApplyAttackBonusVsAlignmentOnWeapon(creature, player, divineLevel, IPAlignmentGroup.Good, "Good", 1);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Intimidate!, 10), divineLevel);
                player?.SendServerMessage(" - Intimidate +10", ColorConstants.Cyan);
                break;

            case "set":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpEvilHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Bluff!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageResistance(DamageType.Piercing, 5), divineLevel);
                player?.SendServerMessage(" - Bluff +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - 5/- Piercing Resistance", ColorConstants.Cyan);
                break;

            case "shar":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadOdd), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 6, SavingThrowType.MindSpells), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Bluff!, 5), divineLevel);
                player?.SendServerMessage(" - +6 Saves vs. Mind Effects", ColorConstants.Cyan);
                player?.SendServerMessage(" - Bluff +10", ColorConstants.Cyan);
                break;

            case "sharess":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpCharm), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 2, SavingThrowType.MindSpells), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Perform!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 5), divineLevel);
                player?.SendServerMessage(" - +6 Saves vs. Mind Effects", ColorConstants.Cyan);
                player?.SendServerMessage(" - Perform +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Persuade +5", ColorConstants.Cyan);
                break;

            case "shargaas":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpEvilHelp), divineLevel, fullDuration: false);
                ApplyAttackBonusVsAlignmentOnWeapon(creature, player, divineLevel, IPAlignmentGroup.Good, "Good", 1);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 3, SavingThrowType.Good), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Bluff!, 5), divineLevel);
                player?.SendServerMessage(" - +3 Saves vs. Good", ColorConstants.Cyan);
                player?.SendServerMessage(" - Bluff +5", ColorConstants.Cyan);
                break;

            case "sharindlar":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHealingS), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Heal!, 5), divineLevel);
                ApplyVersusAlignmentWeaponBonus(creature, player!, divineLevel, IPAlignmentGroup.Evil, 3, "Evil");
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 5), divineLevel);
                player?.SendServerMessage(" - Heal +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Persuade +5", ColorConstants.Cyan);
                break;

            case "shaundakul":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurFreedomOfMovement), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.Immunity(ImmunityType.Entangle), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(2, DamageType.Divine), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Lore!, 5), divineLevel);
                player?.SendServerMessage(" - Immunity to Entangle", ColorConstants.Cyan);
                player?.SendServerMessage(" - +2 Divine Damage", ColorConstants.Cyan);
                player?.SendServerMessage(" - Lore +5", ColorConstants.Cyan);
                break;

            case "sheela peryroyl":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadNature), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 3, SavingThrowType.MindSpells), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 3, SavingThrowType.Negative), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 5), divineLevel);
                player?.SendServerMessage(" - +3 Saves vs. Mind Effects", ColorConstants.Cyan);
                player?.SendServerMessage(" - +3 Saves vs. Negative", ColorConstants.Cyan);
                player?.SendServerMessage(" - Persuade +5", ColorConstants.Cyan);
                break;

            case "shevarash":
                float shevarashDuration = 300.0f + (divineLevel * 20.0f);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                // Drow racial type is 33
                ApplyAttackBonusVsRaceOnWeapon(creature, player, shevarashDuration, 33, 1, "Drow");
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(3, DamageType.Divine), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Intimidate!, 5), divineLevel);
                player?.SendServerMessage(" - +3 Divine Damage", ColorConstants.Cyan);
                player?.SendServerMessage(" - Intimidate +5", ColorConstants.Cyan);
                break;

            case "shiallia":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadNature), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Concentration!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Lore!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(3, DamageType.Divine), divineLevel);
                player?.SendServerMessage(" - Concentration +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Lore +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - +3 Divine Damage", ColorConstants.Cyan);
                break;

            case "siamorphe":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpCharm), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.Will, 1), divineLevel);
                player?.SendServerMessage(" - Persuade +10", ColorConstants.Cyan);
                player?.SendServerMessage(" - Will Save +3", ColorConstants.Cyan);
                break;

            case "silvanus":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadNature), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.AnimalEmpathy!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.Fortitude, 3), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 5), divineLevel);
                player?.SendServerMessage(" - Fortitude Save +3", ColorConstants.Cyan);
                player?.SendServerMessage(" - Animal Empathy +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Persuade +5", ColorConstants.Cyan);
                break;

            case "solonor thelandira":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurAuraGreenLight), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.Immunity(ImmunityType.Blindness), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Lore!, 5), divineLevel);
                player?.SendServerMessage(" - Green Light Aura", ColorConstants.Cyan);
                player?.SendServerMessage(" - Immunity to Blindness", ColorConstants.Cyan);
                player?.SendServerMessage(" - Lore +5", ColorConstants.Cyan);
                break;

            case "sune":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpCharm), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 10), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.Will, 3), divineLevel);
                player?.SendServerMessage(" - Persuade +10", ColorConstants.Cyan);
                player?.SendServerMessage(" - Will Save +3", ColorConstants.Cyan);
                break;

            case "talona":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpAcidS), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 6, SavingThrowType.Poison), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.Regenerate(1, TimeSpan.FromSeconds(6)), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Bluff!, 5), divineLevel);
                player?.SendServerMessage(" - +6 Saves vs. Poison", ColorConstants.Cyan);
                player?.SendServerMessage(" - Regeneration +1", ColorConstants.Cyan);
                player?.SendServerMessage(" - Bluff +5", ColorConstants.Cyan);
                break;

            case "talos":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadElectricity), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(3, DamageType.Electrical), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.AbilityIncrease(Ability.Strength, 1), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Intimidate!, 5), divineLevel);
                player?.SendServerMessage(" - +3 Electrical Damage", ColorConstants.Cyan);
                player?.SendServerMessage(" - Strength +1", ColorConstants.Cyan);
                player?.SendServerMessage(" - Intimidate +5", ColorConstants.Cyan);
                break;

            case "tamara":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHealingS), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Heal!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.Will, 3), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 5), divineLevel);
                player?.SendServerMessage(" - Will Save +3", ColorConstants.Cyan);
                player?.SendServerMessage(" - Heal +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Persuade +5", ColorConstants.Cyan);
                break;

            case "tempus":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHolyAid), divineLevel, fullDuration: false);
                int tempusTempHP = Math.Min(30, 5 + divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.TemporaryHitpoints(tempusTempHP), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageResistance(DamageType.Slashing, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Intimidate!, 5), divineLevel);
                player?.SendServerMessage($" - +{tempusTempHP} Temporary HP", ColorConstants.Cyan);
                player?.SendServerMessage(" - 5/- Slashing Resistance", ColorConstants.Cyan);
                player?.SendServerMessage(" - Intimidate +5", ColorConstants.Cyan);
                break;

            case "thard harr":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpSuperHeroism), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.Fortitude, 3), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.AnimalEmpathy!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 5), divineLevel);
                player?.SendServerMessage(" - Fortitude Save +3", ColorConstants.Cyan);
                player?.SendServerMessage(" - Animal Empathy +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Persuade +5", ColorConstants.Cyan);
                break;

            case "thoth":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.Will, 3), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.CraftArmor!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.CraftWeapon!, 5), divineLevel);
                player?.SendServerMessage(" - Will Save +3", ColorConstants.Cyan);
                player?.SendServerMessage(" - Craft Armor +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Craft Weapon +5", ColorConstants.Cyan);
                break;

            case "tiamat":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpStarburstRed), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.TrueSeeing(), divineLevel);
                player?.SendServerMessage(" - True Seeing", ColorConstants.Cyan);
                break;

            case "titania":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpCharm), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurPixiedust), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Perform!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.Will, 3), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 5), divineLevel);
                player?.SendServerMessage(" - Fairy Dust Aura", ColorConstants.Cyan);
                player?.SendServerMessage(" - Perform +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Will Save +3", ColorConstants.Cyan);
                player?.SendServerMessage(" - Persuade +5", ColorConstants.Cyan);
                break;

            case "torm":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Discipline!, 5), divineLevel);
                ApplyAttackBonusVsAlignmentOnWeapon(creature, player, divineLevel, IPAlignmentGroup.Evil, "Evil");
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 5), divineLevel);
                player?.SendServerMessage(" - Discipline +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Persuade +5", ColorConstants.Cyan);
                break;

            case "tymora":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpSuperHeroism), divineLevel, fullDuration: false);

                // Random blessing - no chance of nothing
                int tymoraBlessing = Random.Shared.Next(1, 8); // 1-7
                switch (tymoraBlessing)
                {
                    case 1:
                        ApplyAttackBonusVsAlignmentOnWeapon(creature, player, divineLevel, IPAlignmentGroup.Lawful, "Lawful");
                        break;
                    case 2:
                        ApplyAttackBonusVsAlignmentOnWeapon(creature, player, divineLevel, IPAlignmentGroup.Chaotic, "Chaotic");
                        break;
                    case 3:
                        ApplyPrayerEffectsToPCs(creature, Effect.DamageResistance(DamageType.Bludgeoning, 5), divineLevel);
                        player?.SendServerMessage(" - 5/- Bludgeoning Resistance", ColorConstants.Cyan);
                        break;
                    case 4:
                        ApplyPrayerEffectsToPCs(creature, Effect.DamageResistance(DamageType.Piercing, 5), divineLevel);
                        player?.SendServerMessage(" - 5/- Piercing Resistance", ColorConstants.Cyan);
                        break;
                    case 5:
                        ApplyPrayerEffectsToPCs(creature, Effect.DamageResistance(DamageType.Slashing, 5), divineLevel);
                        player?.SendServerMessage(" - 5/- Slashing Resistance", ColorConstants.Cyan);
                        break;
                    case 6:
                        ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(3, DamageType.Magical), divineLevel);
                        player?.SendServerMessage(" - +3 Magic Damage", ColorConstants.Cyan);
                        break;
                    case 7:
                        ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(3, DamageType.Divine), divineLevel);
                        player?.SendServerMessage(" - +3 Divine Damage", ColorConstants.Cyan);
                        break;
                }

                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Appraise!, 10), divineLevel);
                player?.SendServerMessage(" - Appraise +10", ColorConstants.Cyan);
                break;

            case "tyr":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                ApplyAttackBonusVsAlignmentOnWeapon(creature, player, divineLevel, IPAlignmentGroup.Evil, "Evil", 1);
                // Damage Reduction with progression: 1/+1 per 4 divine levels, capped at 5/+5
                int tyrDRAmount = Math.Min(5, 1 + (divineLevel / 4));
                ApplyPrayerEffectsToPCs(creature, Effect.DamageReduction(tyrDRAmount, DamagePower.Plus1 + (tyrDRAmount - 1)), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Intimidate!, 5), divineLevel);
                player?.SendServerMessage($" - Damage Reduction {tyrDRAmount}/+{tyrDRAmount}", ColorConstants.Cyan);
                player?.SendServerMessage(" - Intimidate +5", ColorConstants.Cyan);
                break;

            case "ubtao":
                float ubtaoDuration = 300.0f + (divineLevel * 20.0f);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadNature), divineLevel, fullDuration: false);
                ApplyVersusRacialTypeWeaponBonus(creature, player!, divineLevel, RacialType.HumanoidMonstrous, 3, "Monstrous Humanoids");
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 6, SavingThrowType.Fear), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 5), divineLevel);
                player?.SendServerMessage(" - +6 Saves vs. Fear", ColorConstants.Cyan);
                player?.SendServerMessage(" - Persuade +5", ColorConstants.Cyan);
                break;

            case "ulutiu":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpFrostS), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageImmunityIncrease(DamageType.Cold, 10), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.Will, 3), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Lore!, 5), divineLevel);
                player?.SendServerMessage(" - 10% Cold Immunity", ColorConstants.Cyan);
                player?.SendServerMessage(" - Will Save +3", ColorConstants.Cyan);
                player?.SendServerMessage(" - Lore +5", ColorConstants.Cyan);
                break;

            case "umberlee":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadOdd), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 3, SavingThrowType.Law), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(2, DamageType.Divine), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(2, DamageType.Magical), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Bluff!, 5), divineLevel);
                player?.SendServerMessage(" - +3 Saves vs. Law", ColorConstants.Cyan);
                player?.SendServerMessage(" - +2 Divine Damage", ColorConstants.Cyan);
                player?.SendServerMessage(" - +2 Magical Damage", ColorConstants.Cyan);
                player?.SendServerMessage(" - Bluff +5", ColorConstants.Cyan);
                break;

            case "urdlen":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpEvilHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 3, SavingThrowType.Good), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(3, DamageType.Acid), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Intimidate!, 5), divineLevel);
                player?.SendServerMessage(" - +3 Saves vs. Good", ColorConstants.Cyan);
                player?.SendServerMessage(" - +3 Acid Damage", ColorConstants.Cyan);
                player?.SendServerMessage(" - Intimidate +5", ColorConstants.Cyan);
                break;

            case "urogalan":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadOdd), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 2, SavingThrowType.Death), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.MoveSilently!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Bluff!, 5), divineLevel);
                player?.SendServerMessage(" - +6 Saves vs. Death", ColorConstants.Cyan);
                player?.SendServerMessage(" - Move Silently +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Bluff +5", ColorConstants.Cyan);
                break;

            case "uthgar":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpSuperHeroism), divineLevel, fullDuration: false);
                ApplyAttackBonusVsAlignmentOnWeapon(creature, player, divineLevel, IPAlignmentGroup.Lawful, "Lawful");
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Intimidate!, 10), divineLevel);
                player?.SendServerMessage(" - Intimidate +10", ColorConstants.Cyan);
                break;

            case "valkur":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadElectricity), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageImmunityIncrease(DamageType.Electrical, 10), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(3, DamageType.Electrical), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Intimidate!, 5), divineLevel);
                player?.SendServerMessage(" - 10% Electric Immunity", ColorConstants.Cyan);
                player?.SendServerMessage(" - +3 Electrical Damage", ColorConstants.Cyan);
                player?.SendServerMessage(" - Intimidate +5", ColorConstants.Cyan);
                break;

            case "vandria gilmadrith":
                float vandriaDuration = 300.0f + (divineLevel * 20.0f);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 2, SavingThrowType.Evil), divineLevel);
                // Drow racial type is 33
                ApplyAttackBonusVsRaceOnWeapon(creature, player, vandriaDuration, 33, 1, "Drow");
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Lore!, 5), divineLevel);
                player?.SendServerMessage(" - +2 Saves vs. Evil", ColorConstants.Cyan);
                player?.SendServerMessage(" - Lore +5", ColorConstants.Cyan);
                break;

            case "velsharoon":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadOdd), divineLevel, fullDuration: false);
                // Summon undead henchmen based on divine level
                string velsharoonResref;
                int velsharoonCount;
                if (divineLevel <= 10)
                {
                    velsharoonResref = "pray_velsh1";
                    velsharoonCount = 2;
                }
                else if (divineLevel <= 17)
                {
                    velsharoonResref = "pray_velsh2";
                    velsharoonCount = 3;
                }
                else
                {
                    velsharoonResref = "pray_velsh3";
                    velsharoonCount = 4;
                }
                CreateDeityHenchman(creature, player!, divineLevel, velsharoonResref, velsharoonCount, VfxType.FnfSummonUndead, "Undead Servant");
                break;

            case "vergadain":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Appraise!, 10), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.Regenerate(1, TimeSpan.FromSeconds(6.0)), divineLevel);
                player?.SendServerMessage(" - Appraise +10", ColorConstants.Cyan);
                player?.SendServerMessage(" - +1 Regeneration", ColorConstants.Cyan);
                break;

            case "vhaeraun":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpEvilHelp), divineLevel, fullDuration: false);
                ApplyACBonusVsAlignmentOnBoots(creature, player, divineLevel, IPAlignmentGroup.Good, "Good");
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(2, DamageType.Divine), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.BonusFeat(NwFeat.FromFeatId(402)!), divineLevel); // Thug
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Intimidate!, 5), divineLevel);
                player?.SendServerMessage(" - +2 Divine Damage", ColorConstants.Cyan);
                player?.SendServerMessage(" - Bonus Feat: Thug", ColorConstants.Cyan);
                player?.SendServerMessage(" - Intimidate +5", ColorConstants.Cyan);
                break;

            case "waukeen":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Appraise!, 10), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 6, SavingThrowType.Fear), divineLevel);
                player?.SendServerMessage(" - Appraise +10", ColorConstants.Cyan);
                player?.SendServerMessage(" - +6 Saves vs. Fear", ColorConstants.Cyan);
                break;

            case "yondalla":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 6, SavingThrowType.Fear), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 10), divineLevel);
                player?.SendServerMessage(" - +6 Saves vs. Fear", ColorConstants.Cyan);
                player?.SendServerMessage(" - Persuade +10", ColorConstants.Cyan);
                break;

            case "yurtrus":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpEvilHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.Immunity(ImmunityType.Disease), divineLevel);
                ApplyAttackBonusVsAlignmentOnWeapon(creature, player, divineLevel, IPAlignmentGroup.Good, "Good");
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Bluff!, 5), divineLevel);
                player?.SendServerMessage(" - Immunity to Disease", ColorConstants.Cyan);
                player?.SendServerMessage(" - Bluff +5", ColorConstants.Cyan);
                break;

            default:
                // Fall back to alignment-based effects for unimplemented deities
                ApplyAlignmentFallback(creature, divineLevel, player);
                break;
        }
    }

    private void ApplyAlignmentFallback(NwCreature creature, int divineLevel, NwPlayer? player)
    {
        // Default fallback - apply a generic blessing visual
        ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHealingS), divineLevel, fullDuration: false);
        player?.SendServerMessage(" - (Deity effects not yet implemented - generic blessing applied)", ColorConstants.Yellow);
    }

    private void CastDeityEffectSelf(NwCreature creature, string deityName, int divineLevel)
    {
        float duration = 300.0f + (divineLevel * 20.0f);
        NwPlayer? player = creature.ControllingPlayer;

        // Normalize deity name for comparison (case-insensitive)
        string deity = deityName.ToLowerInvariant().Trim();

        player?.SendServerMessage($"Adding {deityName} deity effects (personal):", ColorConstants.Cyan);
        player?.SendServerMessage($" - Duration: {duration:F0} seconds", ColorConstants.Cyan);

        // Apply deity-specific effects to self only
        ApplyDeitySpecificEffectsSelf(creature, deity, divineLevel, duration, player);
    }

    private void ApplyDeitySpecificEffectsSelf(NwCreature creature, string deity, int divineLevel, float duration, NwPlayer? player)
    {
        // This method applies the same deity effects but to self only (for Rangers, Paladins, Blackguards, Divine Champions)
        // Uses ApplySelfEffect helper instead of ApplyPrayerEffectsToPCs

        switch (deity)
        {
            // For simplicity, we'll call the same deity logic but use a self-only application
            // The ApplyDeitySpecificEffects method uses ApplyPrayerEffectsToPCs which handles party-wide
            // For self, we need to apply directly to the creature
            default:
                // Apply the same effects but to self only by temporarily setting divineLevel to 0
                // which makes ApplyPrayerEffectsToPCs apply only to self
                ApplyDeitySpecificEffects(creature, deity, 0, duration, player);
                break;
        }
    }

    private void CastAlignmentEffect(NwCreature creature, NwPlaceable idol, int divineLevel)
    {
        // Get deity name directly from the creature's deity field
        string deityName = NWScript.GetDeity(creature);

        if (!string.IsNullOrEmpty(deityName))
        {
            CastDeityEffect(creature, deityName, divineLevel);
            return;
        }

        // Original alignment-based fallback (only if creature has no deity set)
        string alignment = NWScript.GetLocalString(idol, sVarName: "alignment");
        int vsGood = 0;
        int vsEvil = 0;
        int visual = 0;
        float duration = 300.0f + (divineLevel * 20.0f);
        int levelBonus = divineLevel > 9 ? 1 : 0;

        // Determine effects based on alignment
        if (alignment is "LG" or "NG" or "CG")
        {
            visual = NWScript.VFX_IMP_GOOD_HELP;
            vsEvil = 2 + levelBonus;
        }
        else if (alignment is "LN" or "NN" or "CN")
        {
            visual = NWScript.VFX_IMP_UNSUMMON;
            vsGood = 1 + levelBonus;
            vsEvil = 1 + levelBonus;
        }
        else if (alignment is "LE" or "NE" or "CE")
        {
            visual = NWScript.VFX_IMP_EVIL_HELP;
            vsGood = 2 + levelBonus;
        }

        if (divineLevel > -1)
        {
            NwPlayer? player = creature.ControllingPlayer;
            if (player != null)
            {
                player.SendServerMessage($"Adding {alignment} alignment effects:", ColorConstants.Cyan);
                player.SendServerMessage($" - Duration: {duration:F0} seconds", ColorConstants.Cyan);
            }

            // Apply visual effect
            Effect visualEffect = Effect.VisualEffect((VfxType)visual);
            ApplyPrayerEffectsToPCs(creature, visualEffect, divineLevel, fullDuration: false);

            // Apply AC bonuses
            if (vsGood > 0)
            {
                Effect eVsGood = Effect.ACIncrease(vsGood);
                eVsGood.SubType = EffectSubType.Supernatural;
                eVsGood.Tag = "PrayerVsGood";
                player?.SendServerMessage($" - Extra AC vs Good, {vsGood}", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, eVsGood, divineLevel, fullDuration: true);
            }

            if (vsEvil > 0)
            {
                Effect eVsEvil = Effect.ACIncrease(vsEvil);
                eVsEvil.SubType = EffectSubType.Supernatural;
                eVsEvil.Tag = "PrayerVsEvil";
                player?.SendServerMessage($" - Extra AC vs Evil, {vsEvil}", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, eVsEvil, divineLevel, fullDuration: true);
            }
        }
        else
        {
            // Just visuals for deity info display
            Effect visualEffect = Effect.VisualEffect((VfxType)visual);
            creature.ApplyEffect(EffectDuration.Temporary, visualEffect, TimeSpan.FromSeconds(3));
        }
    }

    private void CastAlignmentEffectSelf(NwCreature creature, NwPlaceable idol, int divineLevel)
    {
        // Get deity name directly from the creature's deity field
        string deityName = NWScript.GetDeity(creature);

        if (!string.IsNullOrEmpty(deityName))
        {
            CastDeityEffectSelf(creature, deityName, divineLevel);
            return;
        }

        // Original alignment-based fallback - same as CastAlignmentEffect but only applies to self
        string alignment = NWScript.GetLocalString(idol, sVarName: "alignment");
        int vsGood = 0;
        int vsEvil = 0;
        int visual = 0;
        float duration = 300.0f + (divineLevel * 20.0f);
        int levelBonus = divineLevel > 9 ? 1 : 0;

        // Determine effects based on alignment
        if (alignment is "LG" or "NG" or "CG")
        {
            visual = NWScript.VFX_IMP_GOOD_HELP;
            vsEvil = 2 + levelBonus;
        }
        else if (alignment is "LN" or "NN" or "CN")
        {
            visual = NWScript.VFX_IMP_UNSUMMON;
            vsGood = 1 + levelBonus;
            vsEvil = 1 + levelBonus;
        }
        else if (alignment is "LE" or "NE" or "CE")
        {
            visual = NWScript.VFX_IMP_EVIL_HELP;
            vsGood = 2 + levelBonus;
        }

        NwPlayer? player = creature.ControllingPlayer;
        if (player != null)
        {
            player.SendServerMessage($"Adding {alignment} alignment effects:", ColorConstants.Cyan);
            player.SendServerMessage($" - Duration: {duration:F0} seconds", ColorConstants.Cyan);
        }

        // Apply visual effect to self only
        Effect visualEffect = Effect.VisualEffect((VfxType)visual);
        creature.ApplyEffect(EffectDuration.Temporary, visualEffect, TimeSpan.FromSeconds(3));

        // Apply AC bonuses to self only
        if (vsGood > 0)
        {
            Effect eVsGood = Effect.ACIncrease(vsGood);
            eVsGood.SubType = EffectSubType.Supernatural;
            eVsGood.Tag = PRAYER_EFFECT_TAG;
            player?.SendServerMessage($" - Extra AC vs Good, {vsGood}", ColorConstants.Cyan);
            creature.ApplyEffect(EffectDuration.Temporary, eVsGood, TimeSpan.FromSeconds(duration));
        }

        if (vsEvil > 0)
        {
            Effect eVsEvil = Effect.ACIncrease(vsEvil);
            eVsEvil.SubType = EffectSubType.Supernatural;
            eVsEvil.Tag = PRAYER_EFFECT_TAG;
            player?.SendServerMessage($" - Extra AC vs Evil, {vsEvil}", ColorConstants.Cyan);
            creature.ApplyEffect(EffectDuration.Temporary, eVsEvil, TimeSpan.FromSeconds(duration));
        }
    }

    private void CastAlignmentEffectPartyWide(NwCreature creature, NwPlaceable idol, int divineLevel)
    {
        // Get deity name directly from the creature's deity field
        string deityName = NWScript.GetDeity(creature);

        if (!string.IsNullOrEmpty(deityName))
        {
            // For laypersons party-wide, we use the same deity effects
            CastDeityEffect(creature, deityName, divineLevel);
            return;
        }

        // Original alignment-based fallback - Party-wide alignment effect for laypersons
        string alignment = NWScript.GetLocalString(idol, sVarName: "alignment");
        int vsGood = 0;
        int vsEvil = 0;
        int visual = 0;
        float duration = 300.0f + (divineLevel * 20.0f);
        int levelBonus = divineLevel > 9 ? 1 : 0;

        // Determine effects based on alignment
        if (alignment is "LG" or "NG" or "CG")
        {
            visual = NWScript.VFX_IMP_GOOD_HELP;
            vsEvil = 2 + levelBonus;
        }
        else if (alignment is "LN" or "NN" or "CN")
        {
            visual = NWScript.VFX_IMP_UNSUMMON;
            vsGood = 1 + levelBonus;
            vsEvil = 1 + levelBonus;
        }
        else if (alignment is "LE" or "NE" or "CE")
        {
            visual = NWScript.VFX_IMP_EVIL_HELP;
            vsGood = 2 + levelBonus;
        }

        NwPlayer? player = creature.ControllingPlayer;
        if (player != null)
        {
            player.SendServerMessage($"Adding {alignment} alignment effects (Party-wide):", ColorConstants.Cyan);
            player.SendServerMessage($" - Duration: {duration:F0} seconds", ColorConstants.Cyan);
        }

        // Apply visual effect
        Effect visualEffect = Effect.VisualEffect((VfxType)visual);
        ApplyLaypersonEffectToParty(creature, visualEffect, duration, fullDuration: false);

        // Apply AC bonuses
        if (vsGood > 0)
        {
            Effect eVsGood = Effect.ACIncrease(vsGood);
            eVsGood.SubType = EffectSubType.Supernatural;
            eVsGood.Tag = PRAYER_EFFECT_TAG;
            player?.SendServerMessage($" - Extra AC vs Good, {vsGood}", ColorConstants.Cyan);
            ApplyLaypersonEffectToParty(creature, eVsGood, duration, fullDuration: true);
        }

        if (vsEvil > 0)
        {
            Effect eVsEvil = Effect.ACIncrease(vsEvil);
            eVsEvil.SubType = EffectSubType.Supernatural;
            eVsEvil.Tag = PRAYER_EFFECT_TAG;
            player?.SendServerMessage($" - Extra AC vs Evil, {vsEvil}", ColorConstants.Cyan);
            ApplyLaypersonEffectToParty(creature, eVsEvil, duration, fullDuration: true);
        }
    }

    private void ApplyLaypersonEffectToParty(NwCreature creature, Effect effect, float duration, bool fullDuration)
    {
        float effectDuration = fullDuration ? duration : 3.0f;

        // Tag all prayer effects so they can be removed later
        effect.Tag = PRAYER_EFFECT_TAG;

        // Apply to caster
        creature.ApplyEffect(EffectDuration.Temporary, effect, TimeSpan.FromSeconds(effectDuration));

        // Apply to party members in area
        NwPlayer? player = creature.ControllingPlayer;
        if (player?.LoginCreature != null)
        {
            foreach (NwPlayer partyMember in player.PartyMembers)
            {
                NwCreature? partyCreature = partyMember.ControlledCreature;
                if (partyCreature != null && partyCreature.Area == creature.Area && partyCreature != creature)
                {
                    partyCreature.ApplyEffect(EffectDuration.Temporary, effect, TimeSpan.FromSeconds(effectDuration));
                }
            }
        }
    }

    private void CastDomainEffect(NwPlayer player, NwCreature creature, int domain, int clericLevel)
    {
        int amount;

        switch (domain)
        {
            case 0: // DOMAIN_AIR
                amount = 20 + clericLevel;
                player.SendServerMessage("Adding Air domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadElectricity), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageImmunityIncrease(DamageType.Electrical, amount), clericLevel);
                player.SendServerMessage($" - Immunity vs Electrical damage, {amount}%", ColorConstants.Cyan);
                break;

            case 1: // DOMAIN_ANIMAL
                amount = 1 + (clericLevel / 7);
                player.SendServerMessage("Adding Animal domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadNature), clericLevel, fullDuration: false);
                // Apply damage bonus to Animal associates
                ApplyDamageBonusToAnimalAssociates(creature, player, amount, clericLevel);
                break;

            case 3: // DOMAIN_DEATH
                amount = 1 + (clericLevel / 7);
                player.SendServerMessage("Adding Death domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadOdd), clericLevel, fullDuration: false);
                // Apply damage bonus to Undead associates
                ApplyDamageBonusToUndeadAssociates(creature, player, amount, clericLevel);
                break;

            case 4: // DOMAIN_DESTRUCTION
                amount = 1 + (clericLevel / 5);
                player.SendServerMessage("Adding Destruction domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurElementalShield), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageShield(amount, DamageBonus.Plus1d6, DamageType.Fire), clericLevel);
                player.SendServerMessage($" - Damage shield, 1d6 + {amount} fire damage", ColorConstants.Cyan);
                break;

            case 5: // DOMAIN_EARTH
                amount = 20 + clericLevel;
                player.SendServerMessage("Adding Earth domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadAcid), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageImmunityIncrease(DamageType.Acid, amount), clericLevel);
                player.SendServerMessage($" - Immunity vs Acid damage, {amount}%", ColorConstants.Cyan);
                break;

            case 6: // DOMAIN_EVIL
                amount = 2 + (clericLevel / 7);
                player.SendServerMessage("Adding Evil domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurProtectionEvilMinor), clericLevel, fullDuration: false);
                // Apply damage vs Good as temporary item property on equipped weapon
                ApplyVersusAlignmentWeaponBonus(creature, player, clericLevel, IPAlignmentGroup.Good, amount, "Good");
                break;

            case 7: // DOMAIN_FIRE
                amount = 20 + clericLevel;
                player.SendServerMessage("Adding Fire domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadFire), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageImmunityIncrease(DamageType.Fire, amount), clericLevel);
                player.SendServerMessage($" - Immunity vs Fire damage, {amount}%", ColorConstants.Cyan);
                break;

            case 8: // DOMAIN_GOOD
                amount = 2 + (clericLevel / 7);
                player.SendServerMessage("Adding Good domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurProtectionGoodMinor), clericLevel, fullDuration: false);
                // Apply damage vs Evil as temporary item property on equipped weapon
                ApplyVersusAlignmentWeaponBonus(creature, player, clericLevel, IPAlignmentGroup.Evil, amount, "Evil");
                player.SendServerMessage($" - Extra damage, {amount} vs. Evil", ColorConstants.Cyan);
                break;

            case 9: // DOMAIN_HEALING
                amount = 2 + (2 * (clericLevel / 10));
                player.SendServerMessage("Adding Healing domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHealingS), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.Regenerate(amount, TimeSpan.FromSeconds(6.0)), clericLevel);
                player.SendServerMessage($" - Regeneration, +{amount}", ColorConstants.Cyan);
                break;

            case 10: // DOMAIN_KNOWLEDGE
                amount = 1 + (clericLevel / 5);
                player.SendServerMessage("Adding Knowledge domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.AllSkills!, amount), clericLevel);
                player.SendServerMessage($" - Skill boost, +{amount}", ColorConstants.Cyan);
                break;

            case 13: // DOMAIN_MAGIC
                amount = 11 + (clericLevel / 2);
                player.SendServerMessage("Adding Magic domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadSonic), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SpellResistanceIncrease(amount), clericLevel);
                player.SendServerMessage($" - Spell Resistance, +{amount}", ColorConstants.Cyan);
                break;

            case 14: // DOMAIN_PLANT
                amount = 1 + (clericLevel / 4);
                if (amount > 6)
                    amount = 6;
                player.SendServerMessage("Adding Plant domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurProtBarkskin), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.ACIncrease(amount, ACBonus.Natural), clericLevel);
                player.SendServerMessage($" - AC increase, +{amount} natural", ColorConstants.Cyan);
                break;

            case 15: // DOMAIN_PROTECTION
                amount = 20 + clericLevel;
                player.SendServerMessage("Adding Protection domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurEtherealVisage), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.Concealment(amount), clericLevel);
                player.SendServerMessage($" - Concealment, {amount}%", ColorConstants.Cyan);
                break;

            case 16: // DOMAIN_STRENGTH
                amount = 1 + (clericLevel / 7);
                player.SendServerMessage("Adding Strength domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadHeal), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.AbilityIncrease(Ability.Strength, amount), clericLevel);
                player.SendServerMessage($" - Extra Strength, {amount}", ColorConstants.Cyan);
                break;

            case 17: // DOMAIN_SUN
                amount = 2 + (clericLevel / 7);
                player.SendServerMessage("Adding Sun domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadHoly), clericLevel, fullDuration: false);
                // Apply damage vs Undead as temporary item property on equipped weapon
                ApplyVersusRacialTypeWeaponBonus(creature, player, clericLevel, RacialType.Undead, amount, "Undead");
                player.SendServerMessage($" - Extra damage, {amount} vs. Undead", ColorConstants.Cyan);
                break;

            case 18: // DOMAIN_TRAVEL
                player.SendServerMessage("Adding Travel domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHealingS), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.Immunity(ImmunityType.MovementSpeedDecrease), clericLevel);
                player.SendServerMessage(" - Immunity from movement decreases", ColorConstants.Cyan);
                break;

            case 19: // DOMAIN_TRICKERY
                player.SendServerMessage("Adding Trickery domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHealingS), clericLevel, fullDuration: false);
                ApplyRepeatingInvisibility(creature, player, clericLevel);
                player.SendServerMessage(" - Invisibility (reapplied every 30 seconds)", ColorConstants.Cyan);
                break;

            case 20: // DOMAIN_WAR
                amount = 1 + (clericLevel / 10);
                player.SendServerMessage("Adding War domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadEvil), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(amount, DamageType.BaseWeapon), clericLevel);
                player.SendServerMessage($" - Extra Damage, {amount}", ColorConstants.Cyan);
                break;

            case 21: // DOMAIN_WATER
                amount = 20 + clericLevel;
                player.SendServerMessage("Adding Water domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadCold), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageImmunityIncrease(DamageType.Cold, amount), clericLevel);
                player.SendServerMessage($" - Immunity vs Cold damage, {amount}%", ColorConstants.Cyan);
                break;

            case 22: // DOMAIN_BALANCE
                amount = 1 + (clericLevel / 7);
                player.SendServerMessage("Adding Balance domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.Will, amount), clericLevel);
                player.SendServerMessage($" - Increased Will Save, {amount}", ColorConstants.Cyan);
                break;

            case 23: // DOMAIN_CAVERN
                player.SendServerMessage("Adding Cavern domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurMagicalSight), clericLevel);
                // Apply ultravision and light blindness immunity
                ApplyPrayerEffectsToPCs(creature, Effect.Ultravision(), clericLevel);
                // Set lightSensitiveBlock on the player to block light blindness
                NWScript.SetLocalInt(creature, "LightSensitiveBlock", 1);
                player.SendServerMessage(" - Ultravision", ColorConstants.Cyan);
                player.SendServerMessage(" - Immunity to Light Sensitivity", ColorConstants.Cyan);
                break;

            case 24: // DOMAIN_CHAOS
                amount = 1 + (clericLevel / 7);
                player.SendServerMessage("Adding Chaos domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadOdd), clericLevel, fullDuration: false);
                // Apply damage vs Lawful as temporary item property on equipped weapon
                ApplyVersusAlignmentWeaponBonus(creature, player, clericLevel, IPAlignmentGroup.Lawful, amount, "Lawful");
                player.SendServerMessage($" - Extra damage, {amount} vs. Lawful", ColorConstants.Cyan);
                break;

            case 25: // DOMAIN_CHARM
                player.SendServerMessage("Adding Charm domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), clericLevel, fullDuration: false);
                // Immunity to mind-affecting spells
                ApplyPrayerEffectsToPCs(creature, Effect.Immunity(ImmunityType.MindSpells), clericLevel);
                player.SendServerMessage(" - Immunity to Mind-Affecting Spells", ColorConstants.Cyan);
                break;

            case 26: // DOMAIN_COLD
                amount = 1 + (clericLevel / 10);
                player.SendServerMessage("Adding Cold domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpFrostL), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(amount, DamageType.Cold), clericLevel);
                player.SendServerMessage($" - Bonus cold damage, {amount}", ColorConstants.Cyan);
                break;

            case 27: // DOMAIN_COMMUNITY
                // Base 10 HP + 10 HP per party member nearby (max 100)
                amount = 10;
                NwPlayer? playerRef = creature.ControllingPlayer;
                if (playerRef != null)
                {
                    foreach (NwPlayer partyMember in playerRef.PartyMembers)
                    {
                        NwCreature? partyCreature = partyMember.ControlledCreature;
                        if (partyCreature != null && partyCreature.Area == creature.Area &&
                            partyCreature != creature && creature.Distance(partyCreature) <= 30.0f)
                        {
                            amount += 10;
                            if (amount >= 100)
                            {
                                amount = 100;
                                break;
                            }
                        }
                    }
                }
                player.SendServerMessage("Adding Community domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHolyAid), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.TemporaryHitpoints(amount), clericLevel);
                player.SendServerMessage($" - Temporary Hitpoints, {amount}", ColorConstants.Cyan);
                break;

            case 28: // DOMAIN_COURAGE
                player.SendServerMessage("Adding Courage domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.Immunity(ImmunityType.Fear), clericLevel);
                player.SendServerMessage(" - Immunity to Fear", ColorConstants.Cyan);
                break;

            case 29: // DOMAIN_CRAFT
                amount = 1 + (clericLevel / 5);
                player.SendServerMessage("Adding Craft domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpReflexSaveThrowUse), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, amount, SavingThrowType.Trap), clericLevel);
                player.SendServerMessage($" - Increased saves vs traps, {amount}", ColorConstants.Cyan);
                break;

            case 30: // DOMAIN_DARKNESS
                amount = 1 + (2 * (clericLevel / 4));
                player.SendServerMessage("Adding Darkness domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurMagicalSight), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.Ultravision(), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Spot!, amount), clericLevel);
                player.SendServerMessage($" - Ultravision, Spot +{amount}", ColorConstants.Cyan);
                break;

            case 31: // DOMAIN_DRAGON
                // Alignment-dependent elemental immunity: 20% + 5% per 5 cleric levels
                amount = 20 + (5 * (clericLevel / 5));
                player.SendServerMessage("Adding Dragon domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpStarburstRed), clericLevel, fullDuration: false);

                // Determine element based on alignment (Good: Cold, Neutral: Electric, Evil: Fire)
                DamageType dragonElementType;
                string dragonElementName;
                int dragonGoodEvil = NWScript.GetAlignmentGoodEvil(creature);
                if (dragonGoodEvil == NWScript.ALIGNMENT_GOOD) // Good
                {
                    dragonElementType = DamageType.Cold;
                    dragonElementName = "Cold";
                }
                else if (dragonGoodEvil == NWScript.ALIGNMENT_EVIL) // Evil
                {
                    dragonElementType = DamageType.Fire;
                    dragonElementName = "Fire";
                }
                else // Neutral
                {
                    dragonElementType = DamageType.Electrical;
                    dragonElementName = "Electrical";
                }

                ApplyPrayerEffectsToPCs(creature, Effect.DamageImmunityIncrease(dragonElementType, amount), clericLevel);
                player.SendServerMessage($" - {dragonElementName} Immunity +{amount}%", ColorConstants.Cyan);
                break;

            case 32: // DOMAIN_DREAM
                player.SendServerMessage("Adding Dream domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpDazedS), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.Immunity(ImmunityType.Dazed), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.Immunity(ImmunityType.Stun), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.Immunity(ImmunityType.Sleep), clericLevel);
                player.SendServerMessage(" - Immunity to Sleep/Daze/Stun", ColorConstants.Cyan);
                break;

            case 33: // DOMAIN_DROW
                player.SendServerMessage("Adding Drow domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurFreedomOfMovement), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.Immunity(ImmunityType.Paralysis), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.Immunity(ImmunityType.Entangle), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.Immunity(ImmunityType.Slow), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.Immunity(ImmunityType.MovementSpeedDecrease), clericLevel);
                player.SendServerMessage(" - Freedom (Immunity to paralysis/entangle/slow)", ColorConstants.Cyan);
                break;

            case 34: // DOMAIN_DWARF
                amount = 1 + (clericLevel / 7);
                player.SendServerMessage("Adding Dwarf domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpSuperHeroism), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.AbilityIncrease(Ability.Constitution, amount), clericLevel);
                player.SendServerMessage($" - Constitution +{amount}", ColorConstants.Cyan);
                break;

            case 35: // DOMAIN_ELF
                amount = 1 + (clericLevel / 7);
                player.SendServerMessage("Adding Elf domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpSuperHeroism), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.AbilityIncrease(Ability.Dexterity, amount), clericLevel);
                player.SendServerMessage($" - Dexterity +{amount}", ColorConstants.Cyan);
                break;

            case 36: // DOMAIN_FATE
                amount = 1 + (clericLevel / 7);
                int fateRoll = Random.Shared.Next(1, 21); // d20
                SavingThrow saveType;
                string saveTypeName;

                if (fateRoll == 1)
                {
                    // Natural 1 = All saves
                    saveType = SavingThrow.All;
                    saveTypeName = "Universal Save";
                }
                else
                {
                    // Otherwise pick one of the three
                    int saveChoice = Random.Shared.Next(1, 4); // d3
                    saveType = saveChoice switch
                    {
                        1 => SavingThrow.Fortitude,
                        2 => SavingThrow.Reflex,
                        _ => SavingThrow.Will
                    };
                    saveTypeName = saveChoice switch
                    {
                        1 => "Fort Save",
                        2 => "Reflex Save",
                        _ => "Will Save"
                    };
                }

                player.SendServerMessage("Adding Fate domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpPdkOath), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(saveType, amount), clericLevel);
                player.SendServerMessage($" - {saveTypeName} +{amount}", ColorConstants.Cyan);
                break;

            case 37: // DOMAIN_GNOME
                amount = 1 + (clericLevel / 7);
                player.SendServerMessage("Adding Gnome domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpSuperHeroism), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.AbilityIncrease(Ability.Charisma, amount), clericLevel);
                player.SendServerMessage($" - Charisma +{amount}", ColorConstants.Cyan);
                break;

            case 38: // DOMAIN_HALFLING
                amount = 1 + (clericLevel / 7);
                player.SendServerMessage("Adding Halfling domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurGhostlyVisageNoSound), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Hide!, amount), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.MoveSilently!, amount), clericLevel);
                player.SendServerMessage($" - Hide/Move Silently +{amount}", ColorConstants.Cyan);
                break;

            case 39: // DOMAIN_HATRED
                amount = 1 + (clericLevel / 5);
                player.SendServerMessage("Adding Hatred domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurProtectionEvilMajor), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageShield(amount, DamageBonus.Plus1d4, DamageType.Negative), clericLevel);
                player.SendServerMessage($" - Negative Damage Shield, 1d4 + {amount}", ColorConstants.Cyan);
                break;

            case 40: // DOMAIN_ILLUSION
                player.SendServerMessage("Adding Illusion domain effects:", ColorConstants.Cyan);
                // Illusion domain is personal only - apply visual effect only to caster
                creature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpHealingS));

                // Set protection flag so other clerics' prayers don't remove this player's effects
                NWScript.SetLocalInt(creature, ILLUSION_PORTAL_SUMMON_VAR, 1);

                // Create illusory duplicate henchman
                CreateIllusionHenchman(creature, player, clericLevel);
                player.SendServerMessage(" - Illusory Duplicate Summoned", ColorConstants.Cyan);

                // Schedule clearing of protection flag when summon duration expires
                ScheduleIllusionPortalProtectionClear(creature, clericLevel);
                break;

            case 41: // DOMAIN_LAW
                amount = 1 + (clericLevel / 7);
                player.SendServerMessage("Adding Law domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadOdd), clericLevel, fullDuration: false);
                // Apply damage vs Chaotic as temporary item property on equipped weapon
                ApplyVersusAlignmentWeaponBonus(creature, player, clericLevel, IPAlignmentGroup.Chaotic, amount, "Chaotic");
                player.SendServerMessage($" - Extra damage, {amount} vs. Chaotic", ColorConstants.Cyan);
                break;

            case 42: // DOMAIN_LUCK
                int luckRoll = Random.Shared.Next(1, 7); // d6
                Ability luckyAbility;
                string abilityName;

                luckyAbility = luckRoll switch
                {
                    1 => Ability.Charisma,
                    2 => Ability.Constitution,
                    3 => Ability.Dexterity,
                    4 => Ability.Intelligence,
                    5 => Ability.Strength,
                    _ => Ability.Wisdom
                };

                abilityName = luckRoll switch
                {
                    1 => "Charisma",
                    2 => "Constitution",
                    3 => "Dexterity",
                    4 => "Intelligence",
                    5 => "Strength",
                    _ => "Wisdom"
                };

                player.SendServerMessage("Adding Luck domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpSuperHeroism), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.AbilityIncrease(luckyAbility, 3), clericLevel);
                player.SendServerMessage($" - {abilityName} +3", ColorConstants.Cyan);
                break;

            case 43: // DOMAIN_MOON
                amount = 1 + (clericLevel / 7);
                player.SendServerMessage("Adding Moon domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadHoly), clericLevel, fullDuration: false);
                // Apply damage vs Shapechangers as temporary item property on equipped weapon
                ApplyVersusRacialTypeWeaponBonus(creature, player, clericLevel, RacialType.ShapeChanger, amount, "Shapechangers");
                player.SendServerMessage($" - Extra damage, {amount} vs. Shapechangers", ColorConstants.Cyan);
                break;

            case 44: // DOMAIN_NOBILITY
                amount = 1 + (clericLevel / 5);
                player.SendServerMessage("Adding Nobility domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpCharm), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Intimidate!, amount), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, amount), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Bluff!, amount), clericLevel);
                player.SendServerMessage($" - Persuade/Intimidate/Bluff +{amount}", ColorConstants.Cyan);
                break;

            case 45: // DOMAIN_ORC
                amount = 1 + (clericLevel / 7);
                player.SendServerMessage("Adding Orc domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadEvil), clericLevel, fullDuration: false);
                // Apply damage vs Elves as temporary item property on equipped weapon
                ApplyVersusRacialTypeWeaponBonus(creature, player, clericLevel, RacialType.Elf, amount, "Elves");
                player.SendServerMessage($" - Extra damage, {amount} vs. Elves", ColorConstants.Cyan);
                break;

            case 46: // DOMAIN_PORTAL
                player.SendServerMessage("Adding Portal domain effects:", ColorConstants.Cyan);
                // Portal domain is personal only - apply visual effect only to caster
                creature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.FnfSummonMonster3));

                // Set protection flag so other clerics' prayers don't remove this player's effects
                NWScript.SetLocalInt(creature, ILLUSION_PORTAL_SUMMON_VAR, 1);

                // Create portal creature henchman
                CreatePortalHenchman(creature, player, clericLevel);
                player.SendServerMessage(" - Portal Creature Summoned", ColorConstants.Cyan);

                // Schedule clearing of protection flag when summon duration expires
                ScheduleIllusionPortalProtectionClear(creature, clericLevel);
                break;

            case 47: // DOMAIN_RENEWAL
                amount = 4 + (clericLevel / 5);
                player.SendServerMessage("Adding Renewal domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurProtectionElements), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageResistance(DamageType.Fire, amount), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageResistance(DamageType.Cold, amount), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageResistance(DamageType.Acid, amount), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageResistance(DamageType.Electrical, amount), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageResistance(DamageType.Sonic, amount), clericLevel);
                player.SendServerMessage($" - Elemental Resistance, {amount}/-", ColorConstants.Cyan);
                break;

            case 48: // DOMAIN_REPOSE
                amount = 1 + (clericLevel / 7);
                player.SendServerMessage("Adding Repose domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadHoly), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageImmunityIncrease(DamageType.Negative, amount), clericLevel);
                player.SendServerMessage($" - Immunity vs Negative damage, {amount}%", ColorConstants.Cyan);
                break;

            case 49: // DOMAIN_RETRIBUTION
                amount = 1 + (clericLevel / 5);
                player.SendServerMessage("Adding Retribution domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurGlowRed), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageShield(amount, DamageBonus.Plus1d4, DamageType.Divine), clericLevel);
                player.SendServerMessage($" - Divine Damage Shield, 1d4 + {amount}", ColorConstants.Cyan);
                break;

            case 50: // DOMAIN_RUNE
                amount = 10 + 10 * (clericLevel / 3);
                player.SendServerMessage("Adding Rune domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurProtStoneskin), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageReduction(amount, DamagePower.Plus5, amount), clericLevel);
                player.SendServerMessage($" - Damage Reduction ({amount}/+5)", ColorConstants.Cyan);
                break;

            case 51: // DOMAIN_SCALYKIND
                amount = 1 + (clericLevel / 7);
                player.SendServerMessage("Adding Scalykind domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpAcidL), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(amount, DamageType.Acid), clericLevel);
                player.SendServerMessage($" - Bonus acid damage, {amount}", ColorConstants.Cyan);
                break;

            case 52: // DOMAIN_SLIME
                amount = 20 + clericLevel;
                player.SendServerMessage("Adding Slime domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadAcid), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageImmunityIncrease(DamageType.Acid, amount), clericLevel);
                player.SendServerMessage($" - Immunity vs Acid damage, {amount}%", ColorConstants.Cyan);
                break;

            case 53: // DOMAIN_SPELL
                amount = 1 + (clericLevel / 5);
                player.SendServerMessage("Adding Spell domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurMagicResistance), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, amount, SavingThrowType.Spell), clericLevel);
                player.SendServerMessage($" - Bonus saves vs spells, {amount}", ColorConstants.Cyan);
                break;

            case 54: // DOMAIN_TIME
                amount = 5 + (clericLevel / 2);
                player.SendServerMessage("Adding Time domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHaste), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.MovementSpeedIncrease(amount), clericLevel);
                player.SendServerMessage($" - Movement speed +{amount}%", ColorConstants.Cyan);
                break;

            case 55: // DOMAIN_TRADE
                amount = 4 + (2 * (clericLevel / 3));
                player.SendServerMessage("Adding Trade domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurSanctuary), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Appraise!, amount), clericLevel);
                player.SendServerMessage($" - Appraise +{amount}", ColorConstants.Cyan);
                break;

            case 56: // DOMAIN_TYRANNY
                amount = 1 + (clericLevel / 5);
                player.SendServerMessage("Adding Tyranny domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadEvil), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageResistance(DamageType.Bludgeoning, amount), clericLevel);
                player.SendServerMessage($" - Bludgeoning resistance, {amount}/-", ColorConstants.Cyan);
                break;

            case 57: // DOMAIN_UNDEATH
                amount = 1 + (clericLevel / 7);
                player.SendServerMessage("Adding Undeath domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHarm), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(amount, DamageType.Negative), clericLevel);
                player.SendServerMessage($" - Bonus negative damage, {amount}", ColorConstants.Cyan);
                break;

            case 58: // DOMAIN_SUFFERING
                amount = 1 + (clericLevel / 5);
                player.SendServerMessage("Adding Suffering domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpDeath), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.Will, amount), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.Fortitude, amount), clericLevel);
                player.SendServerMessage($" - Bonus to Will and Fort saves, {amount}", ColorConstants.Cyan);
                break;

            default:
                player.SendServerMessage($"Domain {domain} - Not yet implemented", ColorConstants.Orange);
                break;
        }
    }

    /// <summary>
    /// Removes all prayer effects from a single creature.
    /// </summary>
    private void RemovePrayerEffects(NwCreature creature)
    {
        foreach (Effect effect in creature.ActiveEffects.ToList())
        {
            if (effect.Tag == PRAYER_EFFECT_TAG)
            {
                creature.RemoveEffect(effect);
            }
        }
    }

    /// <summary>
    /// Removes all prayer effects from the creature and all party members in the same area.
    /// Used when a cleric prays to override all existing prayers party-wide.
    /// Skips party members who have Illusion or Portal domain summons active to protect their summons.
    /// </summary>
    private void RemovePrayerEffectsFromParty(NwCreature creature)
    {
        // Remove from the caster first (caster always gets their effects replaced)
        RemovePrayerEffects(creature);

        // Remove from caster's associates
        foreach (NwCreature associate in creature.Associates)
        {
            if (associate.Area == creature.Area)
            {
                RemovePrayerEffects(associate);
            }
        }

        // Clear caster's Illusion/Portal protection since they're praying again
        NWScript.DeleteLocalInt(creature, ILLUSION_PORTAL_SUMMON_VAR);

        // Remove from all party members in the same area
        NwPlayer? player = creature.ControllingPlayer;
        if (player?.LoginCreature != null)
        {
            foreach (NwPlayer partyMember in player.PartyMembers)
            {
                NwCreature? partyCreature = partyMember.ControlledCreature;
                if (partyCreature != null && partyCreature.Area == creature.Area && partyCreature != creature)
                {
                    // Skip party members who have Illusion or Portal domain summons active
                    if (NWScript.GetLocalInt(partyCreature, ILLUSION_PORTAL_SUMMON_VAR) == 1)
                    {
                        continue;
                    }

                    RemovePrayerEffects(partyCreature);

                    // Remove from party member's associates
                    foreach (NwCreature associate in partyCreature.Associates)
                    {
                        if (associate.Area == creature.Area)
                        {
                            RemovePrayerEffects(associate);
                        }
                    }
                }
            }
        }
    }

    private void ApplyPrayerEffectsToPCs(NwCreature creature, Effect effect, int divineLevel, bool fullDuration = true)
    {
        float duration = fullDuration ? 300.0f + (divineLevel * 20.0f) : 3.0f;

        // Tag all prayer effects so they can be removed later
        effect.Tag = PRAYER_EFFECT_TAG;

        if (divineLevel > 0)
        {
            // Apply to party members in area
            NwPlayer? player = creature.ControllingPlayer;
            if (player?.LoginCreature != null)
            {
                // Apply to caster
                creature.ApplyEffect(EffectDuration.Temporary, effect, TimeSpan.FromSeconds(duration));

                // Apply to caster's associates
                foreach (NwCreature associate in creature.Associates)
                {
                    if (associate.Area == creature.Area)
                    {
                        associate.ApplyEffect(EffectDuration.Temporary, effect, TimeSpan.FromSeconds(duration));
                    }
                }

                // Apply to party
                foreach (NwPlayer partyMember in player.PartyMembers)
                {
                    NwCreature? partyCreature = partyMember.ControlledCreature;
                    if (partyCreature != null && partyCreature.Area == creature.Area && partyCreature != creature)
                    {
                        partyCreature.ApplyEffect(EffectDuration.Temporary, effect, TimeSpan.FromSeconds(duration));

                        // Apply to party member's associates
                        foreach (NwCreature associate in partyCreature.Associates)
                        {
                            if (associate.Area == creature.Area)
                            {
                                associate.ApplyEffect(EffectDuration.Temporary, effect, TimeSpan.FromSeconds(duration));
                            }
                        }
                    }
                }
            }
        }
        else
        {
            // Just apply to self (laypeople)
            creature.ApplyEffect(EffectDuration.Temporary, effect, TimeSpan.FromSeconds(duration));

            // Apply to self's associates (laypeople still get benefits on their associates)
            foreach (NwCreature associate in creature.Associates)
            {
                if (associate.Area == creature.Area)
                {
                    associate.ApplyEffect(EffectDuration.Temporary, effect, TimeSpan.FromSeconds(duration));
                }
            }
        }
    }

    private int GetPrayerCooldownRemaining(NwCreature creature)
    {
        // Prayer cooldown is 1 RL hour (60 real-world minutes)
        // Store timestamp as Unix time
        int lastPrayTime = NWScript.GetLocalInt(creature, sVarName: "PrayBlock");
        if (lastPrayTime == 0)
            return 0;

        // Get current Unix timestamp (seconds since epoch)
        int currentTime = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        int secondsElapsed = currentTime - lastPrayTime;
        int cooldownSeconds = PRAYER_COOLDOWN_MINUTES * 60; // 60 minutes in seconds

        int remainingSeconds = cooldownSeconds - secondsElapsed;

        return remainingSeconds > 0 ? remainingSeconds : 0;
    }

    private void SetPrayerCooldown(NwCreature creature)
    {
        // Store current Unix timestamp
        int currentTime = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        NWScript.SetLocalInt(creature, sVarName: "PrayBlock", currentTime);
    }

    /// <summary>
    /// Schedules the clearing of the Illusion/Portal domain protection flag when the summon duration expires.
    /// This allows other clerics' prayers to affect this creature again after the summon is gone.
    /// </summary>
    private void ScheduleIllusionPortalProtectionClear(NwCreature creature, int clericLevel)
    {
        float duration = 300.0f + (clericLevel * 20.0f);

        _ = NwTask.Run(async () =>
        {
            await NwTask.Delay(TimeSpan.FromSeconds(duration));
            if (creature.IsValid)
            {
                NWScript.DeleteLocalInt(creature, ILLUSION_PORTAL_SUMMON_VAR);
            }
        });
    }

    /// <summary>
    /// Creates an illusory duplicate henchman for the Illusion domain prayer.
    /// The duplicate copies the cleric's name, bio, portrait, level, and equipment appearance.
    /// </summary>
    private void CreateIllusionHenchman(NwCreature creature, NwPlayer player, int clericLevel)
    {
        // Calculate duration
        float duration = 300.0f + (clericLevel * 20.0f);

        // Spawn the base creature
        if (creature.Location == null)
        {
            player.SendServerMessage("Failed to create illusory duplicate - invalid location.", ColorConstants.Red);
            return;
        }

        NwCreature? illusion = NwCreature.Create("pray_illusion", creature.Location);
        if (illusion == null)
        {
            player.SendServerMessage("Failed to create illusory duplicate - creature template not found.", ColorConstants.Red);
            return;
        }

        // Copy cleric's appearance
        illusion.Name = creature.Name;
        illusion.Description = creature.Description;
        illusion.PortraitResRef = creature.PortraitResRef;

        // Copy creature appearance type
        NWScript.SetCreatureAppearanceType(illusion, NWScript.GetAppearanceType(creature));

        // Copy visual transform (scale)
        float scale = NWScript.GetObjectVisualTransform(creature, NWScript.OBJECT_VISUAL_TRANSFORM_SCALE);
        NWScript.SetObjectVisualTransform(illusion, NWScript.OBJECT_VISUAL_TRANSFORM_SCALE, scale);

        // Copy head model
        NWScript.SetCreatureBodyPart(NWScript.CREATURE_PART_HEAD, NWScript.GetCreatureBodyPart(NWScript.CREATURE_PART_HEAD, creature), illusion);

        // Copy colors
        NWScript.SetColor(illusion, NWScript.COLOR_CHANNEL_SKIN, NWScript.GetColor(creature, NWScript.COLOR_CHANNEL_SKIN));
        NWScript.SetColor(illusion, NWScript.COLOR_CHANNEL_HAIR, NWScript.GetColor(creature, NWScript.COLOR_CHANNEL_HAIR));
        NWScript.SetColor(illusion, NWScript.COLOR_CHANNEL_TATTOO_1, NWScript.GetColor(creature, NWScript.COLOR_CHANNEL_TATTOO_1));
        NWScript.SetColor(illusion, NWScript.COLOR_CHANNEL_TATTOO_2, NWScript.GetColor(creature, NWScript.COLOR_CHANNEL_TATTOO_2));

        // Copy soundset
        NWScript.SetSoundset(illusion, NWScript.GetSoundset(creature));

        // Copy wings if present
        int wings = NWScript.GetCreatureWingType(creature);
        if (wings != NWScript.CREATURE_WING_TYPE_NONE)
        {
            NWScript.SetCreatureWingType(wings, illusion);
        }

        // Copy tail if present
        int tail = NWScript.GetCreatureTailType(creature);
        if (tail != NWScript.CREATURE_TAIL_TYPE_NONE)
        {
            NWScript.SetCreatureTailType(tail, illusion);
        }

        // Copy body parts if they differ from default (1)
        int[] bodyParts = [
            NWScript.CREATURE_PART_LEFT_BICEP,
            NWScript.CREATURE_PART_LEFT_FOREARM,
            NWScript.CREATURE_PART_LEFT_HAND,
            NWScript.CREATURE_PART_RIGHT_BICEP,
            NWScript.CREATURE_PART_RIGHT_FOREARM,
            NWScript.CREATURE_PART_RIGHT_HAND,
            NWScript.CREATURE_PART_LEFT_THIGH,
            NWScript.CREATURE_PART_LEFT_SHIN,
            NWScript.CREATURE_PART_LEFT_FOOT,
            NWScript.CREATURE_PART_RIGHT_THIGH,
            NWScript.CREATURE_PART_RIGHT_SHIN,
            NWScript.CREATURE_PART_RIGHT_FOOT
        ];

        foreach (int part in bodyParts)
        {
            int partAppearance = NWScript.GetCreatureBodyPart(part, creature);
            if (partAppearance != 1)
            {
                NWScript.SetCreatureBodyPart(part, partAppearance, illusion);
            }
        }

        // Copy any permanent VFX from the cleric
        foreach (Effect effect in creature.ActiveEffects)
        {
            if (effect.EffectType == EffectType.VisualEffect && effect.DurationType == EffectDuration.Permanent)
            {
                illusion.ApplyEffect(EffectDuration.Permanent, effect);
            }
        }

        // Adjust summon's level to match caster's divine level
        AdjustSummonLevelToDivineLevel(illusion, clericLevel);

        // Copy equipment appearance (visual only)
        CopyEquipmentAppearance(creature, illusion);

        // Add as henchman
        NWScript.AddHenchman(creature, illusion);

        // Apply visual effect
        illusion.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpUnsummon));

        // Schedule destruction after prayer duration
        _ = NwTask.Run(async () =>
        {
            await NwTask.Delay(TimeSpan.FromSeconds(duration));
            if (illusion.IsValid)
            {
                illusion.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpUnsummon));
                await NwTask.Delay(TimeSpan.FromSeconds(1.0f));
                if (illusion.IsValid)
                {
                    illusion.Destroy();
                }
            }
        });
    }

    /// <summary>
    /// Creates a portal creature henchman for the Portal domain prayer.
    /// Appearance and VFX are based on the cleric's alignment.
    /// </summary>
    private void CreatePortalHenchman(NwCreature creature, NwPlayer player, int clericLevel)
    {
        // Calculate duration
        float duration = 300.0f + (clericLevel * 20.0f);

        // Spawn the base creature
        if (creature.Location == null)
        {
            player.SendServerMessage("Failed to create echo - invalid location.", ColorConstants.Red);
            return;
        }

        NwCreature? portalCreature = NwCreature.Create("pray_portal", creature.Location);
        if (portalCreature == null)
        {
            player.SendServerMessage("Failed to create echo - creature template not found.", ColorConstants.Red);
            return;
        }

        // Determine appearance and VFX based on alignment
        int appearanceId;
        int alignmentVfxId;
        string alignmentName;
        int? portraitId = null;
        int portalGoodEvil = NWScript.GetAlignmentGoodEvil(creature);

        if (portalGoodEvil == NWScript.ALIGNMENT_GOOD) // Good
        {
            appearanceId = 1982;
            alignmentVfxId = 565;
            alignmentName = "Good";
        }
        else if (portalGoodEvil == NWScript.ALIGNMENT_EVIL) // Evil
        {
            appearanceId = 1973;
            alignmentVfxId = 561;
            alignmentName = "Evil";
            portraitId = 1525;
        }
        else // Neutral
        {
            appearanceId = 1971;
            alignmentVfxId = 559;
            alignmentName = "Neutral";
            portraitId = 1522;
        }

        // Set appearance
        NWScript.SetCreatureAppearanceType(portalCreature, appearanceId);

        // Set portrait if specified
        if (portraitId.HasValue)
        {
            NWScript.SetPortraitId(portalCreature, portraitId.Value);
        }

        // Adjust alignment based on cleric's alignment
        if (portalGoodEvil == NWScript.ALIGNMENT_GOOD)
        {
            NWScript.AdjustAlignment(portalCreature, NWScript.ALIGNMENT_GOOD, 50, NWScript.FALSE);
        }
        else if (portalGoodEvil == NWScript.ALIGNMENT_EVIL)
        {
            NWScript.AdjustAlignment(portalCreature, NWScript.ALIGNMENT_EVIL, 50, NWScript.FALSE);
        }

        // Adjust summon's level to match caster's divine level
        AdjustSummonLevelToDivineLevel(portalCreature, clericLevel);

        // Add as henchman
        NWScript.AddHenchman(creature, portalCreature);

        // Apply permanent VFX: #465 on all, plus alignment-specific VFX
        Effect commonVfx = Effect.VisualEffect((VfxType)465);
        Effect alignVfx = Effect.VisualEffect((VfxType)alignmentVfxId);
        portalCreature.ApplyEffect(EffectDuration.Permanent, commonVfx);
        portalCreature.ApplyEffect(EffectDuration.Permanent, alignVfx);

        // Summon visual
        portalCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.FnfSummonMonster3));

        player.SendServerMessage($" - Portal Creature ({alignmentName})", ColorConstants.Cyan);

        // Schedule destruction after prayer duration
        _ = NwTask.Run(async () =>
        {
            await NwTask.Delay(TimeSpan.FromSeconds(duration));
            if (portalCreature.IsValid)
            {
                portalCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpUnsummon));
                await NwTask.Delay(TimeSpan.FromSeconds(1.0f));
                if (portalCreature.IsValid)
                {
                    portalCreature.Destroy();
                }
            }
        });
    }

    /// <summary>
    /// Copies the visual appearance of equipped items from source to target creature.
    /// </summary>
    private void CopyEquipmentAppearance(NwCreature source, NwCreature target)
    {
        // Copy appearance for each equipment slot
        InventorySlot[] slots = new[]
        {
            InventorySlot.Head,
            InventorySlot.Chest,
            InventorySlot.Cloak
        };

        foreach (InventorySlot slot in slots)
        {
            NwItem? sourceItem = source.GetItemInSlot(slot);
            if (sourceItem != null)
            {
                // Create a copy of the item and equip it
                NwItem copy = sourceItem.Clone(target);
                target.ActionEquipItem(copy, slot);
            }
        }

        // Copy creature appearance (body, hair, skin, etc.)
        target.Appearance = source.Appearance;
    }

    /// <summary>
    /// Applies damage bonus to all Animal-type associates of the creature and their party.
    /// </summary>
    private void ApplyDamageBonusToAnimalAssociates(NwCreature creature, NwPlayer player, int amount, int clericLevel)
    {
        float duration = 300.0f + (clericLevel * 20.0f);
        Effect damageBonus = Effect.DamageIncrease(amount, DamageType.BaseWeapon);
        damageBonus.Tag = PRAYER_EFFECT_TAG;
        int animalsBuffed = 0;

        // Check creature's own associates
        foreach (NwCreature associate in creature.Associates)
        {
            if (associate.Race.RacialType == RacialType.Animal)
            {
                associate.ApplyEffect(EffectDuration.Temporary, damageBonus, TimeSpan.FromSeconds(duration));
                associate.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpHeadNature));
                animalsBuffed++;
            }
        }

        // Check party members' associates
        if (player?.LoginCreature != null)
        {
            foreach (NwPlayer partyMember in player.PartyMembers)
            {
                NwCreature? partyCreature = partyMember.ControlledCreature;
                if (partyCreature != null && partyCreature.Area == creature.Area && partyCreature != creature)
                {
                    foreach (NwCreature associate in partyCreature.Associates)
                    {
                        if (associate.Race.RacialType == RacialType.Animal && associate.Area == creature.Area)
                        {
                            associate.ApplyEffect(EffectDuration.Temporary, damageBonus, TimeSpan.FromSeconds(duration));
                            associate.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpHeadNature));
                            animalsBuffed++;
                        }
                    }
                }
            }
        }

        if (animalsBuffed > 0)
        {
            player?.SendServerMessage($" - Boosted {animalsBuffed} Animal associate(s) with +{amount} damage", ColorConstants.Cyan);
        }
        else
        {
            player?.SendServerMessage(" - No Animal associates found to boost", ColorConstants.Orange);
        }
    }

    /// <summary>
    /// Applies damage bonus to all Undead-type associates of the creature and their party.
    /// </summary>
    private void ApplyDamageBonusToUndeadAssociates(NwCreature creature, NwPlayer player, int amount, int clericLevel)
    {
        float duration = 300.0f + (clericLevel * 20.0f);
        Effect damageBonus = Effect.DamageIncrease(amount, DamageType.BaseWeapon);
        damageBonus.Tag = PRAYER_EFFECT_TAG;
        int undeadBuffed = 0;

        // Check creature's own associates
        foreach (NwCreature associate in creature.Associates)
        {
            if (associate.Race.RacialType == RacialType.Undead)
            {
                associate.ApplyEffect(EffectDuration.Temporary, damageBonus, TimeSpan.FromSeconds(duration));
                associate.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpHeadOdd));
                undeadBuffed++;
            }
        }

        // Check party members' associates
        if (player?.LoginCreature != null)
        {
            foreach (NwPlayer partyMember in player.PartyMembers)
            {
                NwCreature? partyCreature = partyMember.ControlledCreature;
                if (partyCreature != null && partyCreature.Area == creature.Area && partyCreature != creature)
                {
                    foreach (NwCreature associate in partyCreature.Associates)
                    {
                        if (associate.Race.RacialType == RacialType.Undead && associate.Area == creature.Area)
                        {
                            associate.ApplyEffect(EffectDuration.Temporary, damageBonus, TimeSpan.FromSeconds(duration));
                            associate.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpHeadOdd));
                            undeadBuffed++;
                        }
                    }
                }
            }
        }

        if (undeadBuffed > 0)
        {
            player?.SendServerMessage($" - Boosted {undeadBuffed} Undead associate(s) with +{amount} damage", ColorConstants.Cyan);
        }
        else
        {
            player?.SendServerMessage(" - No Undead associates found to boost", ColorConstants.Orange);
        }
    }

    /// <summary>
    /// Applies a versus alignment damage bonus as a temporary item property on all equipped weapons and gloves.
    /// </summary>
    private void ApplyVersusAlignmentWeaponBonus(NwCreature creature, NwPlayer player, int clericLevel, IPAlignmentGroup alignmentGroup, int amount, string alignmentName)
    {
        float duration = 300.0f + (clericLevel * 20.0f);
        List<NwItem> items = GetEquippedWeaponsAndGloves(creature);

        if (items.Count == 0)
        {
            player.SendServerMessage($" - You need a weapon equipped to receive bonus damage vs {alignmentName}!", ColorConstants.Orange);
            return;
        }

        IPDamageBonus damageBonus = GetPrayerDamageBonus(amount);
        ItemProperty versusProperty = ItemProperty.DamageBonusVsAlign(alignmentGroup, IPDamageType.Divine, damageBonus);

        List<string> itemNames = new List<string>();
        foreach (NwItem item in items)
        {
            item.AddItemProperty(
                versusProperty,
                EffectDuration.Temporary,
                TimeSpan.FromSeconds(duration),
                AddPropPolicy.ReplaceExisting,
                ignoreSubType: false
            );
            itemNames.Add(item.Name);
        }

        player.SendServerMessage($" - {amount} Extra Damage vs {alignmentName} applied to {string.Join(", ", itemNames)}", ColorConstants.Cyan);
    }

    /// <summary>
    /// Applies a versus racial type damage bonus as a temporary item property on all equipped weapons and gloves.
    /// </summary>
    private void ApplyVersusRacialTypeWeaponBonus(NwCreature creature, NwPlayer player, int clericLevel, RacialType racialType, int amount, string raceName)
    {
        float duration = 300.0f + (clericLevel * 20.0f);
        List<NwItem> items = GetEquippedWeaponsAndGloves(creature);

        if (items.Count == 0)
        {
            player.SendServerMessage($" - You need a weapon equipped to receive bonus damage vs {raceName}!", ColorConstants.Orange);
            return;
        }

        IPDamageBonus damageBonus = GetPrayerDamageBonus(amount);
        ItemProperty versusProperty = ItemProperty.DamageBonusVsRace(NwRace.FromRacialType(racialType)!, IPDamageType.Divine, damageBonus);

        List<string> itemNames = new List<string>();
        foreach (NwItem item in items)
        {
            item.AddItemProperty(
                versusProperty,
                EffectDuration.Temporary,
                TimeSpan.FromSeconds(duration),
                AddPropPolicy.ReplaceExisting,
                ignoreSubType: false
            );
            itemNames.Add(item.Name);
        }

        player.SendServerMessage($" - +{amount} Extra Damage vs {raceName} applied to {string.Join(", ", itemNames)}", ColorConstants.Cyan);
    }

    /// <summary>
    /// Gets all equipped weapons from the creature's hands and gloves for applying bonuses.
    /// Returns main hand weapon, off-hand weapon (if not a shield), and gloves.
    /// </summary>
    private List<NwItem> GetEquippedWeaponsAndGloves(NwCreature creature)
    {
        List<NwItem> items = new List<NwItem>();

        // Try right hand (main hand)
        NwItem? rightHand = creature.GetItemInSlot(InventorySlot.RightHand);
        if (rightHand != null && IsWeapon(rightHand))
        {
            items.Add(rightHand);
        }

        // Try left hand (off-hand) - exclude shields
        NwItem? leftHand = creature.GetItemInSlot(InventorySlot.LeftHand);
        if (leftHand != null && IsWeapon(leftHand) && !IsShield(leftHand))
        {
            items.Add(leftHand);
        }

        // Always add gloves as fallback (for monks, unarmed, etc.)
        NwItem? gloves = creature.GetItemInSlot(InventorySlot.Arms);
        if (gloves != null && gloves.BaseItem.ItemType == BaseItemType.Gloves || gloves.BaseItem.ItemType == BaseItemType.Bracer )
        {
            items.Add(gloves);
        }

        // If no items found, try creature weapons
        if (items.Count == 0)
        {
            NwItem? creatureRight = creature.GetItemInSlot(InventorySlot.CreatureRightWeapon);
            if (creatureRight != null)
            {
                items.Add(creatureRight);
            }

            NwItem? creatureLeft = creature.GetItemInSlot(InventorySlot.CreatureLeftWeapon);
            if (creatureLeft != null)
            {
                items.Add(creatureLeft);
            }

            NwItem? creatureBite = creature.GetItemInSlot(InventorySlot.CreatureBiteWeapon);
            if (creatureBite != null)
            {
                items.Add(creatureBite);
            }
        }

        return items;
    }

    /// <summary>
    /// Checks if an item is a weapon that can be used in combat.
    /// Uses the NumDice column from baseitems.2da - if it's 1 or higher, it has a damage roll.
    /// </summary>
    private bool IsWeapon(NwItem item)
    {
        int numDice = NWScript.StringToInt(NWScript.Get2DAString("baseitems", "NumDice", (int)item.BaseItem.ItemType));
        return numDice >= 1;
    }

    /// <summary>
    /// Checks if an item is a shield.
    /// </summary>
    private bool IsShield(NwItem item)
    {
        BaseItemType itemType = item.BaseItem.ItemType;
        return itemType == BaseItemType.SmallShield ||
               itemType == BaseItemType.LargeShield ||
               itemType == BaseItemType.TowerShield;
    }

    /// <summary>
    /// Gets the appropriate spirit VFX for Kiaransalee based on creature's race and gender.
    /// </summary>
    private int GetKiaransaleeSpiritVfx(NwCreature creature)
    {
        bool isFemale = creature.Gender == Gender.Female;
        RacialType race = creature.Race.RacialType;

        return race switch
        {
            RacialType.Dwarf => isFemale ? 1051 : 1050,      // FFX_SPIRIT_DWARF
            RacialType.Elf => isFemale ? 1053 : 1052,        // FFX_SPIRIT_ELF
            RacialType.Gnome => isFemale ? 1055 : 1054,      // FFX_SPIRIT_GNOME
            RacialType.HalfElf => isFemale ? 1057 : 1056,    // FFX_SPIRIT_HALFELF
            RacialType.Halfling => isFemale ? 1059 : 1058,   // FFX_SPIRIT_HALFLING
            RacialType.HalfOrc => isFemale ? 1061 : 1060,    // FFX_SPIRIT_HALFORC
            _ => isFemale ? 1063 : 1062,                      // FFX_SPIRIT_HUMAN (default)
        };
    }

    /// <summary>
    /// Adjusts a summoned creature's effective level to match the caster's divine level.
    /// If the summon's level is higher than the divine level, applies negative levels to reduce it.
    /// </summary>
    private void AdjustSummonLevelToDivineLevel(NwCreature summon, int divineLevel)
    {
        int summonLevel = summon.Level;

        if (summonLevel > divineLevel)
        {
            int levelsToRemove = summonLevel - divineLevel;

            // Apply permanent negative levels to reduce the summon's effective level
            // Use Supernatural subtype to prevent removal by Restoration/Greater Restoration
            Effect negativeLevels = Effect.NegativeLevel(levelsToRemove);
            negativeLevels.SubType = EffectSubType.Supernatural;
            negativeLevels.Tag = "PrayerSummonLevelAdjust";
            summon.ApplyEffect(EffectDuration.Permanent, negativeLevels);
        }

        // Also set challenge rating to match divine level
        summon.ChallengeRating = divineLevel;
    }

    /// <summary>
    /// Applies OnHit: Slow (50% chance, 2 rounds) as a temporary item property on all equipped weapons and gloves.
    /// </summary>
    private void ApplyOnHitSlowWeaponProperty(NwCreature creature, NwPlayer? player, int divineLevel)
    {
        float duration = 300.0f + (divineLevel * 20.0f);
        List<NwItem> items = GetEquippedWeaponsAndGloves(creature);

        if (items.Count == 0)
        {
            player?.SendServerMessage(" - You need a weapon equipped to receive OnHit: Slow!", ColorConstants.Orange);
            return;
        }

        // OnHit: Slow, 50% chance, 2 rounds duration
        ItemProperty onHitSlow = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC22, HitEffect.Slow(IPOnHitDuration.Duration50Pct2Rounds));

        List<string> itemNames = new List<string>();
        foreach (NwItem item in items)
        {
            item.AddItemProperty(
                onHitSlow,
                EffectDuration.Temporary,
                TimeSpan.FromSeconds(duration),
                AddPropPolicy.ReplaceExisting,
                ignoreSubType: false
            );
            itemNames.Add(item.Name);
        }

        player?.SendServerMessage($" - OnHit: Slow (50%, 2 rounds) applied to {string.Join(", ", itemNames)}", ColorConstants.Cyan);
    }

    /// <summary>
    /// Applies Vampiric Regeneration as a temporary item property on all equipped weapons and gloves.
    /// </summary>
    private void ApplyVampiricRegenerationWeaponProperty(NwCreature creature, NwPlayer? player, int divineLevel, int amount)
    {
        float duration = 300.0f + (divineLevel * 20.0f);
        List<NwItem> items = GetEquippedWeaponsAndGloves(creature);

        if (items.Count == 0)
        {
            player?.SendServerMessage($" - You need a weapon equipped to receive +{amount} Vampiric Regeneration!", ColorConstants.Orange);
            return;
        }

        ItemProperty vampRegen = ItemProperty.VampiricRegeneration(amount);

        List<string> itemNames = new List<string>();
        foreach (NwItem item in items)
        {
            item.AddItemProperty(
                vampRegen,
                EffectDuration.Temporary,
                TimeSpan.FromSeconds(duration),
                AddPropPolicy.ReplaceExisting,
                ignoreSubType: false
            );
            itemNames.Add(item.Name);
        }

        player?.SendServerMessage($" - +{amount} Vampiric Regeneration applied to {string.Join(", ", itemNames)}", ColorConstants.Cyan);
    }

    /// <summary>
    /// Applies attack bonus vs alignment as a temporary item property on all equipped weapons and gloves.
    /// Uses level progression: 2 + (divineLevel / 7), capped at 6.
    /// </summary>
    private void ApplyAttackBonusVsAlignmentOnWeapon(NwCreature creature, NwPlayer? player, int divineLevel, IPAlignmentGroup alignmentGroup, string alignmentName, int baseBonus = 0)
    {
        float duration = 300.0f + (divineLevel * 20.0f);

        // Calculate bonus with level progression, capped at 6
        int amount = baseBonus > 0 ? baseBonus + (divineLevel / 4) : 1 + (divineLevel / 4);
        if (amount > 6) amount = 6;

        List<NwItem> items = GetEquippedWeaponsAndGloves(creature);

        if (items.Count == 0)
        {
            player?.SendServerMessage($" - You need a weapon equipped to receive +{amount} AB vs {alignmentName}!", ColorConstants.Orange);
            return;
        }

        ItemProperty abProperty = ItemProperty.AttackBonusVsAlign(alignmentGroup, amount);

        List<string> itemNames = new List<string>();
        foreach (NwItem item in items)
        {
            item.AddItemProperty(
                abProperty,
                EffectDuration.Temporary,
                TimeSpan.FromSeconds(duration),
                AddPropPolicy.ReplaceExisting,
                ignoreSubType: false
            );
            itemNames.Add(item.Name);
        }

        player?.SendServerMessage($" - +{amount} AB vs {alignmentName} applied to {string.Join(", ", itemNames)}", ColorConstants.Cyan);
    }

    /// <summary>
    /// Applies AC bonus vs alignment as a temporary item property on boots (Dodge AC).
    /// Uses level progression: 2 + (divineLevel / 7), capped at 6.
    /// </summary>
    private void ApplyACBonusVsAlignmentOnBoots(NwCreature creature, NwPlayer? player, int divineLevel, IPAlignmentGroup alignmentGroup, string alignmentName, int baseBonus = 0)
    {
        float duration = 300.0f + (divineLevel * 20.0f);

        // Calculate bonus with level progression, capped at 6
        int amount = baseBonus > 0 ? baseBonus + (divineLevel / 4) : 1 + (divineLevel / 4);
        if (amount > 6) amount = 6;

        NwItem? boots = creature.GetItemInSlot(InventorySlot.Boots);
        if (boots == null)
        {
            player?.SendServerMessage($" - You need boots equipped to receive +{amount} AC vs {alignmentName}!", ColorConstants.Orange);
            return;
        }

        // Dodge AC vs alignment
        ItemProperty acProperty = ItemProperty.ACBonusVsAlign(alignmentGroup, amount);

        boots.AddItemProperty(
            acProperty,
            EffectDuration.Temporary,
            TimeSpan.FromSeconds(duration),
            AddPropPolicy.ReplaceExisting,
            ignoreSubType: false
        );

        player?.SendServerMessage($" - +{amount} Dodge AC vs {alignmentName} applied to {boots.Name}", ColorConstants.Cyan);
    }

    /// <summary>
    /// Applies attack bonus vs a specific alignment (e.g., LG, CE) as a temporary item property on all equipped weapons and gloves.
    /// Uses level progression: baseBonus + (divineLevel / 4), capped at 6.
    /// </summary>
    private void ApplyAttackBonusVsSpecificAlignmentOnWeapon(NwCreature creature, NwPlayer? player, int divineLevel, IPAlignment alignment, string alignmentName, int baseBonus = 1)
    {
        float duration = 300.0f + (divineLevel * 20.0f);

        // Calculate bonus with level progression, capped at 6
        int amount = baseBonus + (divineLevel / 4);
        if (amount > 6) amount = 6;

        List<NwItem> items = GetEquippedWeaponsAndGloves(creature);

        if (items.Count == 0)
        {
            player?.SendServerMessage($" - You need a weapon equipped to receive +{amount} AB vs {alignmentName}!", ColorConstants.Orange);
            return;
        }

        ItemProperty abProperty = ItemProperty.AttackBonusVsSAlign(alignment, amount);

        List<string> itemNames = new List<string>();
        foreach (NwItem item in items)
        {
            item.AddItemProperty(
                abProperty,
                EffectDuration.Temporary,
                TimeSpan.FromSeconds(duration),
                AddPropPolicy.ReplaceExisting,
                ignoreSubType: false
            );
            itemNames.Add(item.Name);
        }

        player?.SendServerMessage($" - +{amount} AB vs {alignmentName} applied to {string.Join(", ", itemNames)}", ColorConstants.Cyan);
    }

    /// <summary>
    /// Applies AC bonus vs a specific alignment (e.g., LG, CE) as a temporary item property on boots (Dodge AC).
    /// Uses level progression: baseBonus + (divineLevel / 4), capped at 6.
    /// </summary>
    private void ApplyACBonusVsSpecificAlignmentOnBoots(NwCreature creature, NwPlayer? player, int divineLevel, IPAlignment alignment, string alignmentName, int baseBonus = 1)
    {
        float duration = 300.0f + (divineLevel * 20.0f);

        // Calculate bonus with level progression, capped at 6
        int amount = baseBonus + (divineLevel / 4);
        if (amount > 6) amount = 6;

        NwItem? boots = creature.GetItemInSlot(InventorySlot.Boots);
        if (boots == null)
        {
            player?.SendServerMessage($" - You need boots equipped to receive +{amount} AC vs {alignmentName}!", ColorConstants.Orange);
            return;
        }

        // Dodge AC vs specific alignment
        ItemProperty acProperty = ItemProperty.ACBonusVsSAlign(alignment, amount);

        boots.AddItemProperty(
            acProperty,
            EffectDuration.Temporary,
            TimeSpan.FromSeconds(duration),
            AddPropPolicy.ReplaceExisting,
            ignoreSubType: false
        );

        player?.SendServerMessage($" - +{amount} Dodge AC vs {alignmentName} applied to {boots.Name}", ColorConstants.Cyan);
    }

    /// <summary>
    /// Applies a temporary scale transform to the creature.
    /// Saves original scale to PC Key and restores it when prayer expires.
    /// </summary>
    private void ApplyTemporaryScale(NwCreature creature, NwPlayer? player, float duration, float newScale)
    {
        NwItem? pcKey = creature.FindItemWithTag("ds_pckey");
        if (pcKey == null)
        {
            player?.SendServerMessage(" - Unable to change scale: PC Key not found.", ColorConstants.Orange);
            return;
        }

        // Get and save original scale
        float originalScale = NWScript.GetObjectVisualTransform(creature, NWScript.OBJECT_VISUAL_TRANSFORM_SCALE);
        NWScript.SetLocalFloat(pcKey, "PrayerOriginalScale", originalScale);

        // Set new scale
        NWScript.SetObjectVisualTransform(creature, NWScript.OBJECT_VISUAL_TRANSFORM_SCALE, newScale);
        player?.SendServerMessage($" - Scale changed to {newScale}", ColorConstants.Cyan);

        // Schedule restoration
        _ = NwTask.Run(async () =>
        {
            await NwTask.Delay(TimeSpan.FromSeconds(duration));
            if (creature.IsValid && pcKey.IsValid)
            {
                float savedScale = NWScript.GetLocalFloat(pcKey, "PrayerOriginalScale");
                if (savedScale > 0)
                {
                    NWScript.SetObjectVisualTransform(creature, NWScript.OBJECT_VISUAL_TRANSFORM_SCALE, savedScale);
                    NWScript.DeleteLocalFloat(pcKey, "PrayerOriginalScale");
                }
                else
                {
                    NWScript.SetObjectVisualTransform(creature, NWScript.OBJECT_VISUAL_TRANSFORM_SCALE, 1.0f);
                }
            }
        });
    }

    /// <summary>
    /// Applies a temporary phenotype change to the creature.
    /// Saves original phenotype to PC Key and restores it when prayer expires.
    /// </summary>
    private void ApplyTemporaryPhenotype(NwCreature creature, NwPlayer? player, float duration, int newPhenotype)
    {
        NwItem? pcKey = creature.FindItemWithTag("ds_pckey");
        if (pcKey == null)
        {
            player?.SendServerMessage(" - Unable to change phenotype: PC Key not found.", ColorConstants.Orange);
            return;
        }

        // Get and save original phenotype
        int originalPhenotype = NWScript.GetPhenoType(creature);
        NWScript.SetLocalInt(pcKey, "PrayerOriginalPhenotype", originalPhenotype);

        // Set new phenotype
        NWScript.SetPhenoType(newPhenotype, creature);
        player?.SendServerMessage(" - Phenotype transformed", ColorConstants.Cyan);

        // Schedule restoration
        _ = NwTask.Run(async () =>
        {
            await NwTask.Delay(TimeSpan.FromSeconds(duration));
            if (creature.IsValid && pcKey.IsValid)
            {
                int savedPhenotype = NWScript.GetLocalInt(pcKey, "PrayerOriginalPhenotype");
                NWScript.SetPhenoType(savedPhenotype, creature);
                NWScript.DeleteLocalInt(pcKey, "PrayerOriginalPhenotype");
            }
        });
    }

    /// <summary>
    /// Applies a temporary appearance and scale change to the creature.
    /// Saves original appearance and scale to PC Key and restores them when prayer expires.
    /// </summary>
    private void ApplyTemporaryAppearanceAndScale(NwCreature creature, NwPlayer? player, float duration, int newAppearance, float newScale)
    {
        NwItem? pcKey = creature.FindItemWithTag("ds_pckey");
        if (pcKey == null)
        {
            player?.SendServerMessage(" - Unable to change appearance: PC Key not found.", ColorConstants.Orange);
            return;
        }

        // Get and save original appearance and scale
        int originalAppearance = NWScript.GetAppearanceType(creature);
        float originalScale = NWScript.GetObjectVisualTransform(creature, NWScript.OBJECT_VISUAL_TRANSFORM_SCALE);
        NWScript.SetLocalInt(pcKey, "PrayerOriginalAppearance", originalAppearance);
        NWScript.SetLocalFloat(pcKey, "PrayerOriginalScale", originalScale);

        // Set new appearance and scale
        NWScript.SetCreatureAppearanceType(creature, newAppearance);
        NWScript.SetObjectVisualTransform(creature, NWScript.OBJECT_VISUAL_TRANSFORM_SCALE, newScale);
        player?.SendServerMessage(" - Appearance transformed", ColorConstants.Cyan);

        // Schedule restoration
        _ = NwTask.Run(async () =>
        {
            await NwTask.Delay(TimeSpan.FromSeconds(duration));
            if (creature.IsValid && pcKey.IsValid)
            {
                int savedAppearance = NWScript.GetLocalInt(pcKey, "PrayerOriginalAppearance");
                float savedScale = NWScript.GetLocalFloat(pcKey, "PrayerOriginalScale");
                NWScript.SetCreatureAppearanceType(creature, savedAppearance);
                NWScript.SetObjectVisualTransform(creature, NWScript.OBJECT_VISUAL_TRANSFORM_SCALE, savedScale > 0 ? savedScale : 1.0f);
                NWScript.DeleteLocalInt(pcKey, "PrayerOriginalAppearance");
                NWScript.DeleteLocalFloat(pcKey, "PrayerOriginalScale");
            }
        });
    }

    /// <summary>
    /// Applies Natural AC bonus vs alignment as a temporary item property on amulet.
    /// Uses level progression: 1 + (divineLevel / 4), capped at 6.
    /// </summary>
    private void ApplyNaturalACBonusVsAlignmentOnAmulet(NwCreature creature, NwPlayer? player, int divineLevel, IPAlignmentGroup alignmentGroup, string alignmentName)
    {
        float duration = 300.0f + (divineLevel * 20.0f);

        // Calculate bonus with level progression, capped at 6
        int amount = 1 + (divineLevel / 4);
        if (amount > 6) amount = 6;

        NwItem? amulet = creature.GetItemInSlot(InventorySlot.Neck);
        if (amulet == null)
        {
            player?.SendServerMessage($" - You need an amulet equipped to receive +{amount} Natural AC vs {alignmentName}!", ColorConstants.Orange);
            return;
        }

        // Natural AC vs alignment - use AC bonus which applies as natural on amulets
        ItemProperty acProperty = ItemProperty.ACBonusVsAlign(alignmentGroup, amount);

        amulet.AddItemProperty(
            acProperty,
            EffectDuration.Temporary,
            TimeSpan.FromSeconds(duration),
            AddPropPolicy.ReplaceExisting,
            ignoreSubType: false
        );

        player?.SendServerMessage($" - +{amount} Natural AC vs {alignmentName} applied to {amulet.Name}", ColorConstants.Cyan);
    }

    /// <summary>
    /// Applies attack bonus vs a specific race as a temporary item property on all equipped weapons and gloves.
    /// </summary>
    private void ApplyAttackBonusVsRaceOnWeapon(NwCreature creature, NwPlayer? player, float duration, int racialTypeId, int amount, string raceName)
    {
        List<NwItem> items = GetEquippedWeaponsAndGloves(creature);

        if (items.Count == 0)
        {
            player?.SendServerMessage($" - You need a weapon equipped to receive +{amount} AB vs {raceName}!", ColorConstants.Orange);
            return;
        }

        ItemProperty abProperty = ItemProperty.AttackBonusVsRace(NwRace.FromRaceId(racialTypeId)!, amount);

        List<string> itemNames = new List<string>();
        foreach (NwItem item in items)
        {
            item.AddItemProperty(
                abProperty,
                EffectDuration.Temporary,
                TimeSpan.FromSeconds(duration),
                AddPropPolicy.ReplaceExisting,
                ignoreSubType: false
            );
            itemNames.Add(item.Name);
        }

        player?.SendServerMessage($" - +{amount} AB vs {raceName} applied to {string.Join(", ", itemNames)}", ColorConstants.Cyan);
    }

    /// <summary>
    /// Applies AC bonus vs a specific race as a temporary item property on boots.
    /// Uses level progression: 1 + (divineLevel / 4), capped at 6.
    /// </summary>
    private void ApplyACBonusVsRaceOnBoots(NwCreature creature, NwPlayer? player, int divineLevel, RacialType racialType, string raceName)
    {
        float duration = 300.0f + (divineLevel * 20.0f);

        // Calculate bonus with level progression, capped at 6
        int amount = 1 + (divineLevel / 4);
        if (amount > 6) amount = 6;

        NwItem? boots = creature.GetItemInSlot(InventorySlot.Boots);
        if (boots == null)
        {
            player?.SendServerMessage($" - You need boots equipped to receive +{amount} AC vs {raceName}!", ColorConstants.Orange);
            return;
        }

        ItemProperty acProperty = ItemProperty.ACBonusVsRace(NwRace.FromRacialType(racialType)!, amount);

        boots.AddItemProperty(
            acProperty,
            EffectDuration.Temporary,
            TimeSpan.FromSeconds(duration),
            AddPropPolicy.ReplaceExisting,
            ignoreSubType: false
        );

        player?.SendServerMessage($" - +{amount} Dodge AC vs {raceName} applied to {boots.Name}", ColorConstants.Cyan);
    }

    /// <summary>
    /// Applies COM_CHUNK_YELLOW_MEDIUM VFX every 30 seconds for the duration of the prayer.
    /// </summary>
    private void ApplyRepeatingChunkVfx(NwCreature creature, NwPlayer? player, int divineLevel)
    {
        float totalDuration = 300.0f + (divineLevel * 20.0f);
        float interval = 30.0f;

        // Apply initial chunk VFX
        creature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ComChunkYellowMedium));

        // Schedule reapplication every 30 seconds
        _ = NwTask.Run(async () =>
        {
            float elapsed = 0f;
            while (elapsed < totalDuration && creature.IsValid)
            {
                await NwTask.Delay(TimeSpan.FromSeconds(interval));
                elapsed += interval;

                if (elapsed >= totalDuration || !creature.IsValid)
                    break;

                creature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ComChunkYellowMedium));
            }
        });

        player?.SendServerMessage(" - Periodic chunk effects", ColorConstants.Cyan);
    }

    /// <summary>
    /// Applies invisibility every 30 seconds for the duration of the prayer.
    /// </summary>
    private void ApplyRepeatingInvisibility(NwCreature creature, NwPlayer? player, int clericLevel)
    {
        float totalDuration = 300.0f + (clericLevel * 20.0f);
        float interval = 30.0f;

        // Apply initial invisibility
        Effect invisibility = Effect.Invisibility(InvisibilityType.Normal);
        invisibility.SubType = EffectSubType.Supernatural;
        invisibility.Tag = "PrayerEffect";
        creature.ApplyEffect(EffectDuration.Temporary, invisibility, TimeSpan.FromSeconds(totalDuration));

        // Schedule reapplication every 30 seconds
        _ = NwTask.Run(async () =>
        {
            float elapsed = 0f;
            while (elapsed < totalDuration && creature.IsValid)
            {
                await NwTask.Delay(TimeSpan.FromSeconds(interval));
                elapsed += interval;

                if (elapsed >= totalDuration || !creature.IsValid)
                    break;

                // Reapply invisibility
                Effect reapplyInvis = Effect.Invisibility(InvisibilityType.Normal);
                reapplyInvis.SubType = EffectSubType.Supernatural;
                reapplyInvis.Tag = "PrayerEffect";
                float remainingDuration = totalDuration - elapsed;
                creature.ApplyEffect(EffectDuration.Temporary, reapplyInvis, TimeSpan.FromSeconds(remainingDuration));
                creature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpHeadOdd));
            }
        });
    }

    /// <summary>
    /// Converts a flat damage amount to an IPDamageBonus for item properties.
    /// </summary>
    private IPDamageBonus GetPrayerDamageBonus(int amount)
    {
        return amount switch
        {
            >= 10 => IPDamageBonus.Plus10,
            >= 9 => IPDamageBonus.Plus9,
            >= 8 => IPDamageBonus.Plus8,
            >= 7 => IPDamageBonus.Plus7,
            >= 6 => IPDamageBonus.Plus6,
            >= 5 => IPDamageBonus.Plus5,
            >= 4 => IPDamageBonus.Plus4,
            >= 3 => IPDamageBonus.Plus3,
            >= 2 => IPDamageBonus.Plus2,
            _ => IPDamageBonus.Plus1,
        };
    }

    /// <summary>
    /// Applies temporary wings to a creature for the specified duration.
    /// Stores the original wing type on the player's PC key to ensure proper restoration.
    /// </summary>
    private void ApplyTemporaryWings(NwCreature creature, NwPlayer? player, float duration, int wingType)
    {
        // Find the PC key to store the original wing type
        NwItem? pcKey = creature.FindItemWithTag("ds_pckey");
        if (pcKey == null)
        {
            player?.SendServerMessage(" - Unable to grant wings: PC Key not found.", ColorConstants.Orange);
            return;
        }

        // Get and store original wing type on the PC key
        int originalWings = NWScript.GetCreatureWingType(creature);
        NWScript.SetLocalInt(pcKey, "PrayerOriginalWings", originalWings);

        // Set the new wing type
        NWScript.SetCreatureWingType(wingType, creature);
        player?.SendServerMessage($" - Wings to honor your deity materialize painlessly!", ColorConstants.Cyan);

        // Schedule restoration of original wings when prayer expires
        _ = NwTask.Run(async () =>
        {
            await NwTask.Delay(TimeSpan.FromSeconds(duration));
            if (creature.IsValid)
            {
                // Find the PC key again in case it moved
                NwItem? pcKeyRestore = creature.FindItemWithTag("ds_pckey");
                if (pcKeyRestore != null)
                {
                    int storedOriginal = NWScript.GetLocalInt(pcKeyRestore, "PrayerOriginalWings");
                    NWScript.SetCreatureWingType(storedOriginal, creature);
                    NWScript.DeleteLocalInt(pcKeyRestore, "PrayerOriginalWings");
                }
                else
                {
                    // Fallback: just remove wings if we can't find the PC key
                    NWScript.SetCreatureWingType(NWScript.CREATURE_WING_TYPE_NONE, creature);
                }
            }
        });
    }


    /// <summary>
    /// Creates deity-specific henchmen for prayer effects.
    /// Sets the ILLUSION_PORTAL_SUMMON_VAR to prevent overwriting by other prayers.
    /// </summary>
    private void CreateDeityHenchman(NwCreature creature, NwPlayer player, int divineLevel, string resref, int count, VfxType summonVfx, string henchmanName)
    {
        float duration = 300.0f + (divineLevel * 20.0f);

        if (creature.Location == null)
        {
            player.SendServerMessage($"Failed to create {henchmanName} - invalid location.", ColorConstants.Red);
            return;
        }

        // Set the flag to prevent overwriting by other prayers
        NWScript.SetLocalInt(creature, ILLUSION_PORTAL_SUMMON_VAR, 1);

        int created = 0;
        for (int i = 0; i < count; i++)
        {
            NwCreature? henchman = NwCreature.Create(resref, creature.Location);
            if (henchman == null)
            {
                player.SendServerMessage($"Failed to create {henchmanName} - creature template '{resref}' not found.", ColorConstants.Red);
                continue;
            }

            // Adjust summon's level to match caster's divine level
            AdjustSummonLevelToDivineLevel(henchman, divineLevel);

            // Add as henchman
            NWScript.AddHenchman(creature, henchman);

            // Apply summon VFX
            henchman.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(summonVfx));

            created++;

            // Schedule unsummon when prayer expires
            _ = NwTask.Run(async () =>
            {
                await NwTask.Delay(TimeSpan.FromSeconds(duration));
                if (henchman.IsValid)
                {
                    henchman.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpUnsummon));
                    henchman.Destroy();
                }
            });
        }

        if (created > 0)
        {
            player.SendServerMessage($" - Summoned {created} {henchmanName}(s)", ColorConstants.Cyan);
        }

        // Schedule cleanup of the summon flag when prayer expires
        _ = NwTask.Run(async () =>
        {
            await NwTask.Delay(TimeSpan.FromSeconds(duration));
            if (creature.IsValid)
            {
                NWScript.DeleteLocalInt(creature, ILLUSION_PORTAL_SUMMON_VAR);
            }
        });
    }
}
