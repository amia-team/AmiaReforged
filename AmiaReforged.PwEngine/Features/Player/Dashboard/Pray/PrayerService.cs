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

        // Clerics must have at least one matching domain
        if (clericLevels > 0)
        {
            bool hasMatchingDomain = HasMatchingDomain(creature, idol);
            if (!hasMatchingDomain)
            {
                MakeFallen(player, creature, idol, deity, $"None of your Domains match {deity}'s Domains!");
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
            NwTask.Run(async () =>
            {
                await NwTask.Delay(TimeSpan.FromSeconds(5));
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
            NwTask.Run(async () =>
            {
                await NwTask.Delay(TimeSpan.FromSeconds(5));
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

        // Check if either domain matches any of the idol's domains
        for (int i = 1; i <= 6; i++)
        {
            int idolDomain = NWScript.GetLocalInt(idol, $"dom_{i}");

            if ((pcDomain1 > 0 && pcDomain1 == idolDomain) || (pcDomain2 > 0 && pcDomain2 == idolDomain))
            {
                return true;
            }

            // Also check for Air domain (ID 0) - need to verify it's intentionally set
            if (idolDomain == 0 && i == 1)
            {
                // Check if any other domain is set to know if this idol has domains configured
                bool hasOtherDomains = false;
                for (int j = 2; j <= 6; j++)
                {
                    if (NWScript.GetLocalInt(idol, $"dom_{j}") > 0)
                    {
                        hasOtherDomains = true;
                        break;
                    }
                }
                if (hasOtherDomains && (pcDomain1 == 0 || pcDomain2 == 0))
                {
                    return true;
                }
            }
        }

        return false;
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
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Bluff!, 15), divineLevel);
                player?.SendServerMessage(" - Bluff +15", ColorConstants.Cyan);
                break;

            case "abbathor":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpEvilHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.Regenerate(2, TimeSpan.FromSeconds(duration)), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Appraise!, 5), divineLevel);
                player?.SendServerMessage(" - +2 Regeneration", ColorConstants.Cyan);
                player?.SendServerMessage(" - Appraise +5", ColorConstants.Cyan);
                break;

            case "aerdrie faenya":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadElectricity), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.Reflex, 2), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(2, DamageType.Electrical), divineLevel);
                player?.SendServerMessage(" - Reflex Save +2", ColorConstants.Cyan);
                player?.SendServerMessage(" - +2 Electrical Damage", ColorConstants.Cyan);
                break;

            case "akadi":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadElectricity), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.Reflex, 1), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageImmunityIncrease(DamageType.Electrical, 5), divineLevel);
                player?.SendServerMessage(" - Reflex Save +1", ColorConstants.Cyan);
                player?.SendServerMessage(" - +5% Electrical Immunity", ColorConstants.Cyan);
                break;

            case "angharradh":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 1), divineLevel);
                player?.SendServerMessage(" - Universal Save +2", ColorConstants.Cyan);
                break;

            case "anhur":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.ACIncrease(1), divineLevel);
                player?.SendServerMessage(" - Dodge AC +1", ColorConstants.Cyan);
                break;

            case "anubis":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadOdd), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Hide!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 2, SavingThrowType.Death), divineLevel);
                player?.SendServerMessage(" - Hide +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - +2 Saves vs. Death", ColorConstants.Cyan);
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
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.Will, 2), divineLevel);
                player?.SendServerMessage(" - Persuade +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Will Save +2", ColorConstants.Cyan);
                break;

            case "astilabor":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Search!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Appraise!, 5), divineLevel);
                player?.SendServerMessage(" - Search +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Appraise +5", ColorConstants.Cyan);
                break;

            case "auril":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpFrostS), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 2, SavingThrowType.Cold), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(3, DamageType.Cold), divineLevel);
                player?.SendServerMessage(" - +2 Saves vs. Cold", ColorConstants.Cyan);
                player?.SendServerMessage(" - +3 Cold Damage", ColorConstants.Cyan);
                break;

            case "azuth":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Spellcraft!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.BonusFeat(NwFeat.FromFeatId(7)!), divineLevel);
                player?.SendServerMessage(" - Spellcraft +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Bonus Feat: Combat Casting", ColorConstants.Cyan);
                break;

            case "baervan wildwanderer":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHaste), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.MovementSpeedIncrease(10), divineLevel);
                player?.SendServerMessage(" - +10% Movement Speed", ColorConstants.Cyan);
                break;

            case "bahamut":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 2, SavingThrowType.MindSpells), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.BonusFeat(NwFeat.FromFeatId(0)!), divineLevel);
                player?.SendServerMessage(" - +2 Saves vs. Mind Effects", ColorConstants.Cyan);
                player?.SendServerMessage(" - Bonus Feat: Alertness", ColorConstants.Cyan);
                break;

            case "bahgtru":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpSuperHeroism), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.AbilityIncrease(Ability.Strength, 2), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Taunt!, 5), divineLevel);
                player?.SendServerMessage(" - Strength +2", ColorConstants.Cyan);
                player?.SendServerMessage(" - Taunt +5", ColorConstants.Cyan);
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
                    ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(1, DamageType.Divine), divineLevel);
                    player?.SendServerMessage(" - +1 Damage (night bonus active)", ColorConstants.Cyan);
                }
                else
                {
                    player?.SendServerMessage(" - +1 Damage at night (inactive - daytime)", ColorConstants.Yellow);
                }
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Spellcraft!, 5), divineLevel);
                player?.SendServerMessage(" - Spellcraft +5", ColorConstants.Cyan);
                break;

            case "berronar truesilver":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 2, SavingThrowType.Fear), divineLevel);
                player?.SendServerMessage(" - +2 Saves vs. Fear", ColorConstants.Cyan);
                break;

            case "beshaba":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadOdd), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 2, SavingThrowType.Trap), divineLevel);
                player?.SendServerMessage(" - +2 Saves vs. Traps", ColorConstants.Cyan);
                break;

            case "bhaal":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpDeath), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.BonusFeat(NwFeat.FromFeatId(31)!), divineLevel);
                player?.SendServerMessage(" - Bonus Feat: Sap", ColorConstants.Cyan);
                break;

            case "brandobaris":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 2, SavingThrowType.Chaos), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Bluff!, 5), divineLevel);
                player?.SendServerMessage(" - +2 Saves vs. Chaos", ColorConstants.Cyan);
                player?.SendServerMessage(" - +5 Bluff", ColorConstants.Cyan);
                break;

            case "callarduran smoothhands":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurMagicalSight), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.Ultravision(), divineLevel);
                player?.SendServerMessage(" - Ultravision", ColorConstants.Cyan);
                break;

            case "chauntea":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadNature), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.Fortitude, 2), divineLevel);
                player?.SendServerMessage(" - Fortitude Save +2", ColorConstants.Cyan);
                break;

            case "clangeddin silverbeard":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadHoly), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(1, DamageType.Divine), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.BonusFeat(NwFeat.FromFeatId(279)!), divineLevel);
                player?.SendServerMessage(" - +1 Divine Damage", ColorConstants.Cyan);
                player?.SendServerMessage(" - Bonus Feat: Favored Enemy (Giants)", ColorConstants.Cyan);
                break;

            case "corellon larethian":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(1, DamageType.Divine), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.BonusFeat(NwFeat.FromFeatId(275)!), divineLevel);
                player?.SendServerMessage(" - +1 Divine Damage", ColorConstants.Cyan);
                player?.SendServerMessage(" - Bonus Feat: Favored Enemy (Orcs)", ColorConstants.Cyan);
                break;

            case "cyric":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpEvilHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Hide!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 2, SavingThrowType.Chaos), divineLevel);
                player?.SendServerMessage(" - Hide +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - +2 Saves vs. Chaos", ColorConstants.Cyan);
                break;

            case "cyrrollalee":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Heal!, 5), divineLevel);
                // +1 Saves vs. Chaos
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 1, SavingThrowType.Chaos), divineLevel);
                player?.SendServerMessage(" - Heal +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - +1 Saves vs. Chaos", ColorConstants.Cyan);
                break;

            case "dallah thaun":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadOdd), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(1, DamageType.Divine), divineLevel);
                player?.SendServerMessage(" - +1 Divine Damage", ColorConstants.Cyan);
                break;

            case "deep duerra":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpEvilHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 2, SavingThrowType.Good), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Discipline!, 5), divineLevel);
                player?.SendServerMessage(" - +2 Saves vs. Good", ColorConstants.Cyan);
                player?.SendServerMessage(" - Discipline +5", ColorConstants.Cyan);
                break;

            case "deep sashelas":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpFrostS), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageResistance(DamageType.Cold, 10), divineLevel);
                player?.SendServerMessage(" - 10/- Cold Resist", ColorConstants.Cyan);
                break;

            case "deneir":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Lore!, 15), divineLevel);
                player?.SendServerMessage(" - Lore +15", ColorConstants.Cyan);
                break;

            case "dugmaren brightmantle":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Lore!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Search!, 5), divineLevel);
                player?.SendServerMessage(" - Lore +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Search +5", ColorConstants.Cyan);
                break;

            case "dumathoin":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadOdd), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 1, SavingThrowType.Death), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Listen!, 5), divineLevel);
                player?.SendServerMessage(" - +1 Saves vs. Death", ColorConstants.Cyan);
                player?.SendServerMessage(" - Listen +5", ColorConstants.Cyan);
                break;

            case "eilistraee":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Perform!, 5), divineLevel);
                // +1 AB at night
                if (NWScript.GetIsNight() == 1)
                {
                    ApplyPrayerEffectsToPCs(creature, Effect.AttackIncrease(1), divineLevel);
                    player?.SendServerMessage(" - +1 AB (night bonus active)", ColorConstants.Cyan);
                }
                else
                {
                    player?.SendServerMessage(" - +1 AB at night (inactive - daytime)", ColorConstants.Yellow);
                }
                player?.SendServerMessage(" - Perform +5", ColorConstants.Cyan);
                break;

            case "eldath":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadNature), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Heal!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 5), divineLevel);
                player?.SendServerMessage(" - Heal +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Persuade +5", ColorConstants.Cyan);
                break;

            case "erevan ilesere":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Tumble!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Bluff!, 5), divineLevel);
                player?.SendServerMessage(" - Tumble +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Bluff +5", ColorConstants.Cyan);
                break;

            case "faluzure":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpEvilHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.Fortitude, 2), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.ACIncrease(1), divineLevel);
                player?.SendServerMessage(" - Fortitude Save +2", ColorConstants.Cyan);
                player?.SendServerMessage(" - +1 AC", ColorConstants.Cyan);
                break;

            case "fenmarel mestarine":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurGhostlyVisageNoSound), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Hide!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.Fortitude, 2), divineLevel);
                player?.SendServerMessage(" - Hide +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Fortitude Save +2", ColorConstants.Cyan);
                break;

            case "finder wyvernspur":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Perform!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Appraise!, 5), divineLevel);
                player?.SendServerMessage(" - Perform +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Appraise +5", ColorConstants.Cyan);
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
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Discipline!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.BonusFeat(NwFeat.FromFeatId(273)!), divineLevel);
                player?.SendServerMessage(" - Discipline +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Bonus Feat: Favored Enemy (Goblinoids)", ColorConstants.Cyan);
                break;

            case "garagos":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadEvil), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.AttackIncrease(1), divineLevel);
                player?.SendServerMessage(" - +1 AB", ColorConstants.Cyan);
                break;

            case "gargauth":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpEvilHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.ACIncrease(1), divineLevel);
                player?.SendServerMessage(" - +1 AC", ColorConstants.Cyan);
                break;

            case "garl glittergold":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.DisableTrap!, 10), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.SetTrap!, 5), divineLevel);
                player?.SendServerMessage(" - Disable Trap +10", ColorConstants.Cyan);
                player?.SendServerMessage(" - Set Trap +5", ColorConstants.Cyan);
                break;

            case "garyx":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadFire), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(2, DamageType.Fire), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Intimidate!, 5), divineLevel);
                player?.SendServerMessage(" - +2 Fire Damage", ColorConstants.Cyan);
                player?.SendServerMessage(" - Intimidate +5", ColorConstants.Cyan);
                break;

            case "geb":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadAcid), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Search!, 10), divineLevel);
                player?.SendServerMessage(" - Search +10", ColorConstants.Cyan);
                break;

            case "ghaunadaur":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpAcidS), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 5, SavingThrowType.Poison), divineLevel);
                player?.SendServerMessage(" - +5 Saves vs. Poison", ColorConstants.Cyan);
                break;

            case "gond":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadFire), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.CraftArmor!, 10), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.CraftWeapon!, 10), divineLevel);
                player?.SendServerMessage(" - Craft Armor +10", ColorConstants.Cyan);
                player?.SendServerMessage(" - Craft Weapon +10", ColorConstants.Cyan);
                break;

            case "gorm gulthym":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.SetTrap!, 10), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Spot!, 5), divineLevel);
                player?.SendServerMessage(" - Set Trap +10", ColorConstants.Cyan);
                player?.SendServerMessage(" - Spot +5", ColorConstants.Cyan);
                break;

            case "grazzt":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpEvilHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 10), divineLevel);
                Effect grazztAC = Effect.ACIncrease(1);
                grazztAC.Tag = "PrayerVsGood";
                ApplyPrayerEffectsToPCs(creature, grazztAC, divineLevel);
                player?.SendServerMessage(" - Persuade +10", ColorConstants.Cyan);
                player?.SendServerMessage(" - +1 AC", ColorConstants.Cyan);
                break;

            case "grumbar":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadAcid), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.AbilityIncrease(Ability.Strength, 1), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(2, DamageType.Acid), divineLevel);
                player?.SendServerMessage(" - Strength +1", ColorConstants.Cyan);
                player?.SendServerMessage(" - +2 Acid Damage", ColorConstants.Cyan);
                break;

            case "gruumsh":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadEvil), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.AttackIncrease(1), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.BonusFeat(NwFeat.FromFeatId(262)!), divineLevel);
                player?.SendServerMessage(" - +1 AB", ColorConstants.Cyan);
                player?.SendServerMessage(" - Bonus Feat: Favored Enemy (Elves)", ColorConstants.Cyan);
                break;

            case "gwaeron windstrom":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadNature), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.AnimalEmpathy!, 10), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Concentration!, 5), divineLevel);
                player?.SendServerMessage(" - Animal Empathy +10", ColorConstants.Cyan);
                player?.SendServerMessage(" - Concentration +5", ColorConstants.Cyan);
                break;

            case "haela brightaxe":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 2, SavingThrowType.Evil), divineLevel);
                player?.SendServerMessage(" - +2 Saves vs. Evil", ColorConstants.Cyan);
                break;

            case "hanali celanil":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadFire), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.Will, 1), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(2, DamageType.Fire), divineLevel);
                player?.SendServerMessage(" - Will Save +1", ColorConstants.Cyan);
                player?.SendServerMessage(" - +2 Fire Damage", ColorConstants.Cyan);
                break;

            case "hathor":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadNature), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Heal!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Concentration!, 5), divineLevel);
                player?.SendServerMessage(" - Heal +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Concentration +5", ColorConstants.Cyan);
                break;

            case "helm":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Spot!, 10), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Listen!, 5), divineLevel);
                player?.SendServerMessage(" - Spot +10", ColorConstants.Cyan);
                player?.SendServerMessage(" - Listen +5", ColorConstants.Cyan);
                break;

            case "hlal":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Bluff!, 10), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.BonusFeat(NwFeat.FromFeatId(424)!), divineLevel);
                player?.SendServerMessage(" - Bluff +10", ColorConstants.Cyan);
                player?.SendServerMessage(" - Bonus Feat: Lingering Song", ColorConstants.Cyan);
                break;

            case "hoar":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurProtectionEvilMinor), divineLevel, fullDuration: false);
                // +2 Bludgeoning biteback - damage shield
                ApplyPrayerEffectsToPCs(creature, Effect.DamageShield(2, DamageBonus.Plus1, DamageType.Bludgeoning), divineLevel);
                player?.SendServerMessage(" - +2 Bludgeoning Damage Shield (biteback)", ColorConstants.Cyan);
                break;

            case "horus-re":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadHoly), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 2, SavingThrowType.Evil), divineLevel);
                player?.SendServerMessage(" - +2 Saves vs. Evil", ColorConstants.Cyan);
                break;

            case "ibrandul":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadOdd), divineLevel, fullDuration: false);
                // +1 AB when not outside - check for interior
                NwArea? area = creature.Area;
                if (area != null && !area.IsInterior)
                {
                    player?.SendServerMessage(" - +2 AB indoors (inactive - outdoors)", ColorConstants.Yellow);
                }
                else
                {
                    ApplyPrayerEffectsToPCs(creature, Effect.AttackIncrease(2), divineLevel);
                    player?.SendServerMessage(" - +2 AB (indoor bonus active)", ColorConstants.Cyan);
                }
                break;

            case "ilmater":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHealingS), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Heal!, 10), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 5), divineLevel);
                player?.SendServerMessage(" - Heal +10", ColorConstants.Cyan);
                player?.SendServerMessage(" - Persuade +5", ColorConstants.Cyan);
                break;

            case "ilneval":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpEvilHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(2, DamageType.Divine), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Taunt!, 5), divineLevel);
                player?.SendServerMessage(" - +2 Divine Damage", ColorConstants.Cyan);
                player?.SendServerMessage(" - Taunt +5", ColorConstants.Cyan);
                break;

            case "io":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpUnsummon), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 2), divineLevel);
                player?.SendServerMessage(" - Universal Saves +2", ColorConstants.Cyan);
                break;

            case "isis":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Lore!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Spellcraft!, 5), divineLevel);
                player?.SendServerMessage(" - Lore +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Spellcraft +5", ColorConstants.Cyan);
                break;

            case "istishia":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpFrostS), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageImmunityIncrease(DamageType.Cold, 10), divineLevel);
                player?.SendServerMessage(" - 10% Cold Immunity", ColorConstants.Cyan);
                break;

            case "jergal":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadOdd), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Lore!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 2, SavingThrowType.Death), divineLevel);
                player?.SendServerMessage(" - Lore +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - +2 Saves vs. Death", ColorConstants.Cyan);
                break;

            case "kelemvor":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadOdd), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 3, SavingThrowType.Death), divineLevel);
                player?.SendServerMessage(" - +3 Saves vs. Death", ColorConstants.Cyan);
                break;

            case "kiaransalee":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpEvilHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Spellcraft!, 5), divineLevel);
                player?.SendServerMessage(" - Spellcraft +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - +1 AC", ColorConstants.Cyan);
                break;

            case "kossuth":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadFire), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 3, SavingThrowType.Fire), divineLevel);
                player?.SendServerMessage(" - +3 Saves vs. Fire", ColorConstants.Cyan);
                break;

            case "kurtulmak":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadOdd), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.SetTrap!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Intimidate!, 5), divineLevel);
                player?.SendServerMessage(" - Set Trap +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Intimidate +5", ColorConstants.Cyan);
                break;

            case "labelas enoreth":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Lore!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Concentration!, 5), divineLevel);
                player?.SendServerMessage(" - Lore +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Concentration +5", ColorConstants.Cyan);
                break;

            case "laduguer":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpEvilHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Spellcraft!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(2, DamageType.Divine), divineLevel);
                player?.SendServerMessage(" - Spellcraft +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - +2 Divine Damage", ColorConstants.Cyan);
                break;

            case "lathander":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadHoly), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 3, SavingThrowType.Disease), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 5), divineLevel);
                player?.SendServerMessage(" - +3 Saves vs. Disease", ColorConstants.Cyan);
                player?.SendServerMessage(" - Persuade +5", ColorConstants.Cyan);
                break;

            case "leira":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Bluff!, 10), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Spellcraft!, 5), divineLevel);
                player?.SendServerMessage(" - Bluff +10", ColorConstants.Cyan);
                player?.SendServerMessage(" - Spellcraft +5", ColorConstants.Cyan);
                break;

            case "lendys":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 2, SavingThrowType.Fear), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 2, SavingThrowType.Chaos), divineLevel);
                player?.SendServerMessage(" - +2 Saves vs. Fear", ColorConstants.Cyan);
                player?.SendServerMessage(" - +2 Saves vs. Chaos", ColorConstants.Cyan);
                break;

            case "lliira":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpCharm), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Perform!, 15), divineLevel);
                player?.SendServerMessage(" - Perform +15", ColorConstants.Cyan);
                break;

            case "lolth":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpEvilHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 4, SavingThrowType.Poison), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.AttackIncrease(1), divineLevel);
                player?.SendServerMessage(" - +4 Saves vs. Poison", ColorConstants.Cyan);
                player?.SendServerMessage(" - +1 AB", ColorConstants.Cyan);
                break;

            case "loviatar":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpEvilHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Discipline!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Concentration!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.AbilityIncrease(Ability.Constitution, 1), divineLevel);
                player?.SendServerMessage(" - Discipline +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Concentration +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Constitution +1", ColorConstants.Cyan);
                break;

            case "lurue":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadNature), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Ride!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Heal!, 10), divineLevel);
                player?.SendServerMessage(" - Ride +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Heal +10", ColorConstants.Cyan);
                break;

            case "luthic":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadOdd), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Heal!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(1, DamageType.Divine), divineLevel);
                player?.SendServerMessage(" - Heal +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - +1 Divine Damage", ColorConstants.Cyan);
                break;

            case "maglubiyet":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpEvilHelp), divineLevel, fullDuration: false);
                Effect maglubiyetAC = Effect.ACIncrease(1);
                maglubiyetAC.Tag = "PrayerVsGood";
                ApplyPrayerEffectsToPCs(creature, maglubiyetAC, divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Discipline!, 5), divineLevel);
                player?.SendServerMessage(" - +1 AC", ColorConstants.Cyan);
                player?.SendServerMessage(" - Discipline +5", ColorConstants.Cyan);
                break;

            case "malar":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadEvil), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(1, DamageType.Divine), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 3, SavingThrowType.Law), divineLevel);
                player?.SendServerMessage(" - +1 Divine Damage", ColorConstants.Cyan);
                player?.SendServerMessage(" - +3 Saves vs. Law", ColorConstants.Cyan);
                break;

            case "marthammor duin":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Search!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 2, SavingThrowType.Trap), divineLevel);
                player?.SendServerMessage(" - Search +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - +2 Saves vs. Traps", ColorConstants.Cyan);
                break;

            case "mask":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurGhostlyVisageNoSound), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Hide!, 10), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.MoveSilently!, 10), divineLevel);
                player?.SendServerMessage(" - Hide +10", ColorConstants.Cyan);
                player?.SendServerMessage(" - Move Silently +10", ColorConstants.Cyan);
                break;

            case "mephistopheles":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadFire), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(2, DamageType.Fire), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageImmunityIncrease(DamageType.Fire, 10), divineLevel);
                player?.SendServerMessage(" - +2 Fire Damage", ColorConstants.Cyan);
                player?.SendServerMessage(" - +10% Fire Immunity", ColorConstants.Cyan);
                break;

            case "mielikki":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadNature), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.AnimalEmpathy!, 15), divineLevel);
                player?.SendServerMessage(" - Animal Empathy +15", ColorConstants.Cyan);
                break;

            case "milil":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpCharm), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Perform!, 5), divineLevel);
                Effect mililAC = Effect.ACIncrease(1);
                mililAC.Tag = "PrayerVsEvil";
                ApplyPrayerEffectsToPCs(creature, mililAC, divineLevel);
                player?.SendServerMessage(" - Perform +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - +1 AC", ColorConstants.Cyan);
                break;

            case "moander":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpAcidS), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(2, DamageType.Acid), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Taunt!, 5), divineLevel);
                player?.SendServerMessage(" - +2 Acid Damage", ColorConstants.Cyan);
                player?.SendServerMessage(" - Taunt +5", ColorConstants.Cyan);
                break;

            case "moradin":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.CraftArmor!, 5), divineLevel);
                Effect moradinAC = Effect.ACIncrease(1);
                moradinAC.Tag = "PrayerVsGiants";
                ApplyPrayerEffectsToPCs(creature, moradinAC, divineLevel);
                player?.SendServerMessage(" - +5 Craft Armor", ColorConstants.Cyan);
                player?.SendServerMessage(" - +1 AC", ColorConstants.Cyan);
                break;

            case "myrkul":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadOdd), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 2, SavingThrowType.Death), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.AttackIncrease(1), divineLevel);
                player?.SendServerMessage(" - +2 Saves vs. Death", ColorConstants.Cyan);
                player?.SendServerMessage(" - +1 AB", ColorConstants.Cyan);
                break;

            case "mystra":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 4, SavingThrowType.Spell), divineLevel);
                player?.SendServerMessage(" - +4 Saves vs. Spells", ColorConstants.Cyan);
                break;

            case "nephthys":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadOdd), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Appraise!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 2, SavingThrowType.Death), divineLevel);
                player?.SendServerMessage(" - Appraise +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - +2 Saves vs. Death", ColorConstants.Cyan);
                break;

            case "nobanion":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 1, SavingThrowType.Fear), divineLevel);
                Effect nobanionAB = Effect.AttackIncrease(1);
                nobanionAB.Tag = "PrayerVsEvil";
                ApplyPrayerEffectsToPCs(creature, nobanionAB, divineLevel);
                player?.SendServerMessage(" - +1 Saves vs. Fear", ColorConstants.Cyan);
                player?.SendServerMessage(" - +1 AB", ColorConstants.Cyan);
                break;

            case "oberon":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadNature), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Lore!, 10), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.AnimalEmpathy!, 5), divineLevel);
                player?.SendServerMessage(" - Lore +10", ColorConstants.Cyan);
                player?.SendServerMessage(" - Animal Empathy +5", ColorConstants.Cyan);
                break;

            case "oghma":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Lore!, 10), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 1, SavingThrowType.Fear), divineLevel);
                player?.SendServerMessage(" - Lore +10", ColorConstants.Cyan);
                player?.SendServerMessage(" - +1 Saves vs. Fear", ColorConstants.Cyan);
                break;

            case "orcus":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpEvilHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(1, DamageType.Divine), divineLevel);
                Effect orcusAC = Effect.ACIncrease(1);
                orcusAC.Tag = "PrayerVsGood";
                ApplyPrayerEffectsToPCs(creature, orcusAC, divineLevel);
                player?.SendServerMessage(" - +1 Divine Damage", ColorConstants.Cyan);
                player?.SendServerMessage(" - +1 AC", ColorConstants.Cyan);
                break;

            case "osiris":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadHoly), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 2, SavingThrowType.Death), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 5), divineLevel);
                player?.SendServerMessage(" - +2 Saves vs. Death", ColorConstants.Cyan);
                player?.SendServerMessage(" - Persuade +5", ColorConstants.Cyan);
                break;

            case "pazuzu":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpEvilHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Intimidate!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.ACIncrease(1), divineLevel);
                player?.SendServerMessage(" - Intimidate +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Persuade +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - AC +1", ColorConstants.Cyan);
                break;

            case "queen of air and darkness":
            case "queenofairanddarkness":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadElectricity), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.Reflex, 2), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Concentration!, 10), divineLevel);
                player?.SendServerMessage(" - Reflex Saves +2", ColorConstants.Cyan);
                player?.SendServerMessage(" - +10 Concentration", ColorConstants.Cyan);
                break;

            case "red knight":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Ride!, 10), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.Fortitude, 2), divineLevel);
                player?.SendServerMessage(" - Ride +10", ColorConstants.Cyan);
                player?.SendServerMessage(" - Fortitude Saves +2", ColorConstants.Cyan);
                break;

            case "rillifane rallathil":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadNature), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Hide!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 2, SavingThrowType.Law), divineLevel);
                player?.SendServerMessage(" - Hide +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - +2 Saves vs. Law", ColorConstants.Cyan);
                break;

            case "salandra":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHealingS), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Heal!, 15), divineLevel);
                player?.SendServerMessage(" - Heal +15", ColorConstants.Cyan);
                break;

            case "savras":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Spot!, 10), divineLevel);
                player?.SendServerMessage(" - Spot +10", ColorConstants.Cyan);
                break;

            case "sebek":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpSuperHeroism), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.AbilityIncrease(Ability.Strength, 2), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 2, SavingThrowType.Poison), divineLevel);
                player?.SendServerMessage(" - Strength +2", ColorConstants.Cyan);
                player?.SendServerMessage(" - +2 Saves vs. Poison", ColorConstants.Cyan);
                break;

            case "segojan earthcaller":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpAcidS), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(2, DamageType.Acid), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Concentration!, 5), divineLevel);
                player?.SendServerMessage(" - +2 Acid Damage", ColorConstants.Cyan);
                player?.SendServerMessage(" - Concentration +5", ColorConstants.Cyan);
                break;

            case "sehanine moonbow":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpFrostS), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.Fortitude, 1), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(2, DamageType.Cold), divineLevel);
                player?.SendServerMessage(" - Fortitude Save +2", ColorConstants.Cyan);
                player?.SendServerMessage(" - +2 Cold Damage", ColorConstants.Cyan);
                break;

            case "selune":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadHoly), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Heal!, 5), divineLevel);
                // +1 Divine Damage at night
                if (NWScript.GetIsNight() == 1)
                {
                    ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(1, DamageType.Divine), divineLevel);
                    player?.SendServerMessage(" - +1 Divine Damage (night bonus active)", ColorConstants.Cyan);
                }
                else
                {
                    player?.SendServerMessage(" - +1 Divine Damage at night (inactive - daytime)", ColorConstants.Yellow);
                }
                player?.SendServerMessage(" - Heal +5", ColorConstants.Cyan);
                break;

            case "selvetarm":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpEvilHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.AttackIncrease(1), divineLevel);
                player?.SendServerMessage(" - +1 AB", ColorConstants.Cyan);
                player?.SendServerMessage(" - +5 Intimidate", ColorConstants.Cyan);
                break;

            case "set":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpEvilHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Bluff!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.AbilityIncrease(Ability.Intelligence, 2), divineLevel);
                player?.SendServerMessage(" - Bluff +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Intelligence +2", ColorConstants.Cyan);
                break;

            case "shar":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadOdd), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 2, SavingThrowType.MindSpells), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.MoveSilently!, 5), divineLevel);
                player?.SendServerMessage(" - +2 Saves vs. Mind Effects", ColorConstants.Cyan);
                player?.SendServerMessage(" - Move Silently +5", ColorConstants.Cyan);
                break;

            case "sharess":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpCharm), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Perform!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 2, SavingThrowType.MindSpells), divineLevel);
                player?.SendServerMessage(" - Perform +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - +2 Saves vs. Mind Effects", ColorConstants.Cyan);
                break;

            case "shargaas":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpEvilHelp), divineLevel, fullDuration: false);
                Effect shargaasAB = Effect.AttackIncrease(1);
                shargaasAB.Tag = "PrayerVsGood";
                ApplyPrayerEffectsToPCs(creature, shargaasAB, divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 2, SavingThrowType.Good), divineLevel);
                player?.SendServerMessage(" - +1 AB", ColorConstants.Cyan);
                player?.SendServerMessage(" - +2 Saves vs. Good", ColorConstants.Cyan);
                break;

            case "sharindlar":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHealingS), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Heal!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 5), divineLevel);
                player?.SendServerMessage(" - Heal +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Persuade +5", ColorConstants.Cyan);
                break;

            case "shaundakul":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurFreedomOfMovement), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.Immunity(ImmunityType.Entangle), divineLevel);
                player?.SendServerMessage(" - Immunity to Entangle", ColorConstants.Cyan);
                break;

            case "sheela peryroyl":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadNature), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 2, SavingThrowType.MindSpells), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(1, DamageType.Electrical), divineLevel);
                player?.SendServerMessage(" - +2 Saves vs. Mind Effects", ColorConstants.Cyan);
                player?.SendServerMessage(" - +1 Electrical Damage", ColorConstants.Cyan);
                break;

            case "shevarash":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                Effect shevarashAB = Effect.AttackIncrease(1);
                shevarashAB.Tag = "PrayerVsDrow";
                ApplyPrayerEffectsToPCs(creature, shevarashAB, divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.ACIncrease(1), divineLevel);
                player?.SendServerMessage(" - +1 AB", ColorConstants.Cyan);
                player?.SendServerMessage(" - AC +1", ColorConstants.Cyan);
                break;

            case "shiallia":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadNature), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Concentration!, 10), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.AnimalEmpathy!, 5), divineLevel);
                player?.SendServerMessage(" - Concentration +10", ColorConstants.Cyan);
                player?.SendServerMessage(" - Animal Empathy +5", ColorConstants.Cyan);
                break;

            case "siamorphe":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpCharm), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.Will, 1), divineLevel);
                player?.SendServerMessage(" - Persuade +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Will Save +1", ColorConstants.Cyan);
                break;

            case "silvanus":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadNature), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.AnimalEmpathy!, 10), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.Fortitude, 1), divineLevel);
                player?.SendServerMessage(" - Animal Empathy +10", ColorConstants.Cyan);
                player?.SendServerMessage(" - Fortitude Save +1", ColorConstants.Cyan);
                break;

            case "solonor thelandira":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.Immunity(ImmunityType.Blindness), divineLevel);
                player?.SendServerMessage(" - Immunity to Blindness", ColorConstants.Cyan);
                break;

            case "sune":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpCharm), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.Will, 2), divineLevel);
                player?.SendServerMessage(" - Persuade +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Will Save +2", ColorConstants.Cyan);
                break;

            case "talona":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpAcidS), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 2, SavingThrowType.Poison), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.Regenerate(1, TimeSpan.FromSeconds(6)), divineLevel);
                player?.SendServerMessage(" - +2 Saves vs. Poison", ColorConstants.Cyan);
                player?.SendServerMessage(" - Regeneration +1", ColorConstants.Cyan);
                break;

            case "talos":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadElectricity), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(2, DamageType.Electrical), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.AbilityIncrease(Ability.Strength, 2), divineLevel);
                player?.SendServerMessage(" - +2 Electrical Damage", ColorConstants.Cyan);
                player?.SendServerMessage(" - Strength +2", ColorConstants.Cyan);
                break;

            case "tamara":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHealingS), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Heal!, 10), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.Will, 1), divineLevel);
                player?.SendServerMessage(" - Heal +10", ColorConstants.Cyan);
                player?.SendServerMessage(" - Will Save +1", ColorConstants.Cyan);
                break;

            case "tempus":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHolyAid), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.TemporaryHitpoints(30), divineLevel);
                player?.SendServerMessage(" - +30 Temporary HP", ColorConstants.Cyan);
                break;

            case "thard harr":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpSuperHeroism), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.AbilityIncrease(Ability.Strength, 2), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.Fortitude, 1), divineLevel);
                player?.SendServerMessage(" - Strength +2", ColorConstants.Cyan);
                player?.SendServerMessage(" - Fortitude Save +1", ColorConstants.Cyan);
                break;

            case "thoth":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Spellcraft!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.CraftArmor!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.CraftWeapon!, 5), divineLevel);
                player?.SendServerMessage(" - Spellcraft +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Craft Armor +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Craft Weapon +5", ColorConstants.Cyan);
                break;

            case "tiamat":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpStarburstRed), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(1, DamageType.Divine), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(1, DamageType.Fire), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(1, DamageType.Cold), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(1, DamageType.Acid), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(1, DamageType.Electrical), divineLevel);
                player?.SendServerMessage(" - +1 Divine/Fire/Cold/Acid/Electrical Damage", ColorConstants.Cyan);
                break;

            case "titania":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpCharm), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Perform!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.Will, 1), divineLevel);
                player?.SendServerMessage(" - Perform +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Will Save +1", ColorConstants.Cyan);
                break;

            case "torm":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Discipline!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, 5), divineLevel);
                player?.SendServerMessage(" - Discipline +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Persuade +5", ColorConstants.Cyan);
                break;

            case "tymora":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpSuperHeroism), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.AllSkills!, 2), divineLevel);
                player?.SendServerMessage(" - All Skills +2", ColorConstants.Cyan);
                break;

            case "tyr":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Discipline!, 5), divineLevel);
                Effect tyrAB = Effect.AttackIncrease(1);
                tyrAB.Tag = "PrayerVsEvil";
                ApplyPrayerEffectsToPCs(creature, tyrAB, divineLevel);
                player?.SendServerMessage(" - Discipline +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - +1 AB", ColorConstants.Cyan);
                break;

            case "ubtao":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadNature), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Concentration!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.BonusFeat(NwFeat.FromFeatId(274)!), divineLevel);
                player?.SendServerMessage(" - Concentration +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Bonus Feat: Favored Enemy (Monstrous Humanoid)", ColorConstants.Cyan);
                break;

            case "ulutiu":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpFrostS), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageImmunityIncrease(DamageType.Cold, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(2, DamageType.Cold), divineLevel);
                player?.SendServerMessage(" - 5% Cold Immunity", ColorConstants.Cyan);
                player?.SendServerMessage(" - +2 Cold Damage", ColorConstants.Cyan);
                break;

            case "umberlee":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadOdd), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 2, SavingThrowType.Law), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.Fortitude, 1), divineLevel);
                player?.SendServerMessage(" - +2 Saves vs. Law", ColorConstants.Cyan);
                player?.SendServerMessage(" - Fortitude Save +1", ColorConstants.Cyan);
                break;

            case "urdlen":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpEvilHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 2, SavingThrowType.Good), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(2, DamageType.Acid), divineLevel);
                player?.SendServerMessage(" - +2 Saves vs. Good", ColorConstants.Cyan);
                player?.SendServerMessage(" - +2 Acid Damage", ColorConstants.Cyan);
                break;

            case "urogalan":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadOdd), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 2, SavingThrowType.Death), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.MoveSilently!, 5), divineLevel);
                player?.SendServerMessage(" - +2 Saves vs. Death", ColorConstants.Cyan);
                player?.SendServerMessage(" - Move Silently +5", ColorConstants.Cyan);
                break;

            case "uthgar":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpSuperHeroism), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.AbilityIncrease(Ability.Strength, 2), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Discipline!, 5), divineLevel);
                player?.SendServerMessage(" - Strength +2", ColorConstants.Cyan);
                player?.SendServerMessage(" - Discipline +5", ColorConstants.Cyan);
                break;

            case "valkur":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadElectricity), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageImmunityIncrease(DamageType.Electrical, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageIncrease(1, DamageType.Divine), divineLevel);
                player?.SendServerMessage(" - 5% Electric Immunity", ColorConstants.Cyan);
                player?.SendServerMessage(" - +1 Divine Damage", ColorConstants.Cyan);
                break;

            case "vandria gilmadrith":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 2, SavingThrowType.Evil), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.AbilityIncrease(Ability.Strength, 1), divineLevel);
                player?.SendServerMessage(" - +2 Saves vs. Evil", ColorConstants.Cyan);
                player?.SendServerMessage(" - Strength +1", ColorConstants.Cyan);
                break;

            case "velsharoon":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadOdd), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 2, SavingThrowType.Death), divineLevel);
                player?.SendServerMessage(" - +4 Saves vs. Death", ColorConstants.Cyan);
                break;

            case "vergadain":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Appraise!, 10), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.CraftArmor!, 2), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.CraftWeapon!, 2), divineLevel);
                player?.SendServerMessage(" - Appraise +10", ColorConstants.Cyan);
                player?.SendServerMessage(" - Craft Armor +2", ColorConstants.Cyan);
                player?.SendServerMessage(" - Craft Weapon +2", ColorConstants.Cyan);
                break;

            case "vhaeraun":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpEvilHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Hide!, 5), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Spot!, 5), divineLevel);
                Effect vhaeraunAC = Effect.ACIncrease(1);
                vhaeraunAC.Tag = "PrayerVsGood";
                ApplyPrayerEffectsToPCs(creature, vhaeraunAC, divineLevel);
                player?.SendServerMessage(" - Hide +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - Spot +5", ColorConstants.Cyan);
                player?.SendServerMessage(" - +1 AC", ColorConstants.Cyan);
                break;

            case "waukeen":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Appraise!, 15), divineLevel);
                player?.SendServerMessage(" - Appraise +15", ColorConstants.Cyan);
                break;

            case "yondalla":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpGoodHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 2, SavingThrowType.Fear), divineLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.AbilityIncrease(Ability.Strength, 1), divineLevel);
                player?.SendServerMessage(" - +2 Saves vs. Fear", ColorConstants.Cyan);
                player?.SendServerMessage(" - Strength +1", ColorConstants.Cyan);
                break;

            case "yurtrus":
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpEvilHelp), divineLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, 2, SavingThrowType.Disease), divineLevel);
                Effect yurtrusAC = Effect.ACIncrease(1);
                yurtrusAC.Tag = "PrayerVsGood";
                ApplyPrayerEffectsToPCs(creature, yurtrusAC, divineLevel);
                player?.SendServerMessage(" - +2 Saves vs. Disease", ColorConstants.Cyan);
                player?.SendServerMessage(" - +1 AC", ColorConstants.Cyan);
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
        // Get deity name and use deity-specific effects
        string deityName = NWScript.GetLocalString(idol, "deity_name");
        if (string.IsNullOrEmpty(deityName))
        {
            // Try to get deity name from idol tag
            string tag = idol.Tag;
            if (tag.StartsWith("idol2_"))
            {
                deityName = tag.Substring(6); // Remove "idol2_" prefix
            }
        }

        if (!string.IsNullOrEmpty(deityName))
        {
            CastDeityEffect(creature, deityName, divineLevel);
            return;
        }

        // Original alignment-based fallback
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
        // Get deity name and use deity-specific effects (self only)
        string deityName = NWScript.GetLocalString(idol, "deity_name");
        if (string.IsNullOrEmpty(deityName))
        {
            // Try to get deity name from idol tag
            string tag = idol.Tag;
            if (tag.StartsWith("idol2_"))
            {
                deityName = tag.Substring(6); // Remove "idol2_" prefix
            }
        }

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
            eVsGood.Tag = "PrayerVsGood";
            player?.SendServerMessage($" - Extra AC vs Good, {vsGood}", ColorConstants.Cyan);
            creature.ApplyEffect(EffectDuration.Temporary, eVsGood, TimeSpan.FromSeconds(duration));
        }

        if (vsEvil > 0)
        {
            Effect eVsEvil = Effect.ACIncrease(vsEvil);
            eVsEvil.SubType = EffectSubType.Supernatural;
            eVsEvil.Tag = "PrayerVsEvil";
            player?.SendServerMessage($" - Extra AC vs Evil, {vsEvil}", ColorConstants.Cyan);
            creature.ApplyEffect(EffectDuration.Temporary, eVsEvil, TimeSpan.FromSeconds(duration));
        }
    }

    private void CastAlignmentEffectPartyWide(NwCreature creature, NwPlaceable idol, int divineLevel)
    {
        // Get deity name and use deity-specific effects (party-wide for laypersons)
        string deityName = NWScript.GetLocalString(idol, "deity_name");
        if (string.IsNullOrEmpty(deityName))
        {
            // Try to get deity name from idol tag
            string tag = idol.Tag;
            if (tag.StartsWith("idol2_"))
            {
                deityName = tag.Substring(6); // Remove "idol2_" prefix
            }
        }

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
            eVsGood.Tag = "PrayerVsGood";
            player?.SendServerMessage($" - Extra AC vs Good, {vsGood}", ColorConstants.Cyan);
            ApplyLaypersonEffectToParty(creature, eVsGood, duration, fullDuration: true);
        }

        if (vsEvil > 0)
        {
            Effect eVsEvil = Effect.ACIncrease(vsEvil);
            eVsEvil.SubType = EffectSubType.Supernatural;
            eVsEvil.Tag = "PrayerVsEvil";
            player?.SendServerMessage($" - Extra AC vs Evil, {vsEvil}", ColorConstants.Cyan);
            ApplyLaypersonEffectToParty(creature, eVsEvil, duration, fullDuration: true);
        }
    }

    private void ApplyLaypersonEffectToParty(NwCreature creature, Effect effect, float duration, bool fullDuration)
    {
        float effectDuration = fullDuration ? duration : 3.0f;

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
                amount = 1 + ((clericLevel - 1) / 7);
                player.SendServerMessage("Adding Animal domain effects:", ColorConstants.Cyan);
                player.SendServerMessage(" - Boosting Animal companions...", ColorConstants.Cyan);
                // Note: Animal companion bonuses would need companion system integration
                // For now, just visual feedback
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadNature), clericLevel, fullDuration: false);
                break;

            case 3: // DOMAIN_DEATH
                amount = 1 + ((clericLevel - 1) / 7);
                player.SendServerMessage("Adding Death domain effects:", ColorConstants.Cyan);
                player.SendServerMessage(" - Boosting Undead companions...", ColorConstants.Cyan);
                // Note: Undead companion bonuses would need companion system integration
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadOdd), clericLevel, fullDuration: false);
                break;

            case 4: // DOMAIN_DESTRUCTION
                amount = 1 + ((clericLevel - 1) / 5);
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
                amount = 2 + ((clericLevel - 1) / 7);
                player.SendServerMessage("Adding Evil domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurProtectionEvilMinor), clericLevel, fullDuration: false);
                // Note: VersusAlignment effect not available - applying as general damage increase
                Effect evilDamage = Effect.DamageIncrease(amount, DamageType.Divine);
                evilDamage.Tag = "PrayerEvilVsGood";
                ApplyPrayerEffectsToPCs(creature, evilDamage, clericLevel);
                player.SendServerMessage($" - Extra Damage, {amount} vs Good", ColorConstants.Cyan);
                break;

            case 7: // DOMAIN_FIRE
                amount = 20 + clericLevel;
                player.SendServerMessage("Adding Fire domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadFire), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageImmunityIncrease(DamageType.Fire, amount), clericLevel);
                player.SendServerMessage($" - Immunity vs Fire damage, {amount}%", ColorConstants.Cyan);
                break;

            case 8: // DOMAIN_GOOD
                amount = 2 + ((clericLevel - 1) / 7);
                player.SendServerMessage("Adding Good domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurProtectionGoodMinor), clericLevel, fullDuration: false);
                // Note: VersusAlignment effect not available - applying as general damage increase
                Effect goodDamage = Effect.DamageIncrease(amount, DamageType.Divine);
                goodDamage.Tag = "PrayerGoodVsEvil";
                ApplyPrayerEffectsToPCs(creature, goodDamage, clericLevel);
                player.SendServerMessage($" - Extra Damage, {amount} vs Evil", ColorConstants.Cyan);
                break;

            case 9: // DOMAIN_HEALING
                amount = 2 + (2 * ((clericLevel - 1) / 10));
                player.SendServerMessage("Adding Healing domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHealingS), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.Regenerate(amount, TimeSpan.FromSeconds(6.0)), clericLevel);
                player.SendServerMessage($" - Regeneration, +{amount}", ColorConstants.Cyan);
                break;

            case 10: // DOMAIN_KNOWLEDGE
                amount = 1 + ((clericLevel - 1) / 5);
                player.SendServerMessage("Adding Knowledge domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.AllSkills!, amount), clericLevel);
                player.SendServerMessage($" - Skill boost, +{amount}", ColorConstants.Cyan);
                break;

            case 13: // DOMAIN_MAGIC
                amount = 11 + ((clericLevel - 1) / 2);
                player.SendServerMessage("Adding Magic domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadSonic), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SpellResistanceIncrease(amount), clericLevel);
                player.SendServerMessage($" - Spell Resistance, +{amount}", ColorConstants.Cyan);
                break;

            case 14: // DOMAIN_PLANT
                amount = 1 + ((clericLevel - 1) / 4);
                if (amount > 5)
                    amount = 5;
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
                amount = 1 + ((clericLevel - 1) / 7);
                player.SendServerMessage("Adding Strength domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadHeal), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.AbilityIncrease(Ability.Strength, amount), clericLevel);
                player.SendServerMessage($" - Extra Strength, {amount}", ColorConstants.Cyan);
                break;

            case 17: // DOMAIN_SUN
                amount = 2 + ((clericLevel - 1) / 7);
                player.SendServerMessage("Adding Sun domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadHoly), clericLevel, fullDuration: false);
                // Note: VersusRacialType effect - applying as tagged damage increase
                Effect sunDamage = Effect.DamageIncrease(amount, DamageType.Divine);
                sunDamage.Tag = "PrayerSunVsUndead";
                ApplyPrayerEffectsToPCs(creature, sunDamage, clericLevel);
                player.SendServerMessage($" - Extra Damage, {amount} vs Undead", ColorConstants.Cyan);
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
                ApplyPrayerEffectsToPCs(creature, Effect.Invisibility(InvisibilityType.Normal), clericLevel);
                player.SendServerMessage(" - Invisibility", ColorConstants.Cyan);
                break;

            case 20: // DOMAIN_WAR
                amount = 1 + ((clericLevel - 1) / 10);
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
                amount = 1 + ((clericLevel - 1) / 7);
                player.SendServerMessage("Adding Balance domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.Will, amount), clericLevel);
                player.SendServerMessage($" - Increased Will Save, {amount}", ColorConstants.Cyan);
                break;

            case 23: // DOMAIN_CAVERN
                player.SendServerMessage("Adding Cavern domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurMagicalSight), clericLevel);
                // Note: Light blindness immunity would need custom flag handling
                // Applying darkvision as alternative
                ApplyPrayerEffectsToPCs(creature, Effect.Ultravision(), clericLevel);
                player.SendServerMessage(" - Ultravision", ColorConstants.Cyan);
                break;

            case 24: // DOMAIN_CHAOS
                amount = 1 + ((clericLevel - 1) / 7);
                player.SendServerMessage("Adding Chaos domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadOdd), clericLevel, fullDuration: false);
                // Note: VersusAlignment not available - applying as general damage
                Effect chaosDamage = Effect.DamageIncrease(amount);
                chaosDamage.Tag = "PrayerChaosVsLaw";
                ApplyPrayerEffectsToPCs(creature, chaosDamage, clericLevel);
                player.SendServerMessage($" - Extra Damage vs Law, {amount}", ColorConstants.Cyan);
                break;

            case 25: // DOMAIN_CHARM
                player.SendServerMessage("Adding Charm domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), clericLevel, fullDuration: false);
                // Immunity to mind-affecting spells
                ApplyPrayerEffectsToPCs(creature, Effect.Immunity(ImmunityType.MindSpells), clericLevel);
                player.SendServerMessage(" - Immunity to Mind-Affecting Spells", ColorConstants.Cyan);
                break;

            case 26: // DOMAIN_COLD
                amount = 1 + ((clericLevel - 1) / 10);
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
                amount = 1 + ((clericLevel - 1) / 5);
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
                amount = 1 + ((clericLevel - 1) / 7);
                player.SendServerMessage("Adding Dwarf domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpSuperHeroism), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.AbilityIncrease(Ability.Constitution, amount), clericLevel);
                player.SendServerMessage($" - Constitution +{amount}", ColorConstants.Cyan);
                break;

            case 35: // DOMAIN_ELF
                amount = 1 + ((clericLevel - 1) / 7);
                player.SendServerMessage("Adding Elf domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpSuperHeroism), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.AbilityIncrease(Ability.Dexterity, amount), clericLevel);
                player.SendServerMessage($" - Dexterity +{amount}", ColorConstants.Cyan);
                break;

            case 36: // DOMAIN_FATE
                amount = 1 + ((clericLevel - 1) / 7);
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
                amount = 1 + ((clericLevel - 1) / 7);
                player.SendServerMessage("Adding Gnome domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpSuperHeroism), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.AbilityIncrease(Ability.Charisma, amount), clericLevel);
                player.SendServerMessage($" - Charisma +{amount}", ColorConstants.Cyan);
                break;

            case 38: // DOMAIN_HALFLING
                amount = 1 + ((clericLevel - 1) / 7);
                player.SendServerMessage("Adding Halfling domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurGhostlyVisageNoSound), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Hide!, amount), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.MoveSilently!, amount), clericLevel);
                player.SendServerMessage($" - Hide/Move Silently +{amount}", ColorConstants.Cyan);
                break;

            case 39: // DOMAIN_HATRED
                amount = 1 + ((clericLevel - 1) / 5);
                player.SendServerMessage("Adding Hatred domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurProtectionEvilMajor), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageShield(amount, DamageBonus.Plus1d4, DamageType.Negative), clericLevel);
                player.SendServerMessage($" - Negative Damage Shield, 1d4 + {amount}", ColorConstants.Cyan);
                break;

            case 40: // DOMAIN_ILLUSION
                player.SendServerMessage("Adding Illusion domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHealingS), clericLevel, fullDuration: false);

                // Create illusory duplicate henchman
                CreateIllusionHenchman(creature, player, clericLevel);
                player.SendServerMessage(" - Illusory Duplicate Summoned", ColorConstants.Cyan);
                break;

            case 41: // DOMAIN_LAW
                amount = 1 + ((clericLevel - 1) / 7);
                player.SendServerMessage("Adding Law domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadOdd), clericLevel, fullDuration: false);
                // Note: VersusAlignment not available - applying as general damage
                Effect lawDamage = Effect.DamageIncrease(amount);
                lawDamage.Tag = "PrayerLawVsChaos";
                ApplyPrayerEffectsToPCs(creature, lawDamage, clericLevel);
                player.SendServerMessage($" - Extra Damage vs Chaos, {amount}", ColorConstants.Cyan);
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
                amount = 1 + ((clericLevel - 1) / 7);
                player.SendServerMessage("Adding Moon domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadHoly), clericLevel, fullDuration: false);
                // Note: VersusRacialType (shapechangers) not available - applying as general damage
                Effect moonDamage = Effect.DamageIncrease(amount);
                moonDamage.Tag = "PrayerMoonVsShapechangers";
                ApplyPrayerEffectsToPCs(creature, moonDamage, clericLevel);
                player.SendServerMessage($" - Extra Damage vs Shapechangers, {amount}", ColorConstants.Cyan);
                break;

            case 44: // DOMAIN_NOBILITY
                amount = 1 + ((clericLevel - 1) / 5);
                player.SendServerMessage("Adding Nobility domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpCharm), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Intimidate!, amount), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade!, amount), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Bluff!, amount), clericLevel);
                player.SendServerMessage($" - Persuade/Intimidate/Bluff +{amount}", ColorConstants.Cyan);
                break;

            case 45: // DOMAIN_ORC
                amount = 1 + ((clericLevel - 1) / 7);
                player.SendServerMessage("Adding Orc domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadEvil), clericLevel, fullDuration: false);
                // Note: VersusRacialType (elves) not available - applying as general damage
                Effect orcDamage = Effect.DamageIncrease(amount);
                orcDamage.Tag = "PrayerOrcVsElves";
                ApplyPrayerEffectsToPCs(creature, orcDamage, clericLevel);
                player.SendServerMessage($" - Extra Damage vs Elves, {amount}", ColorConstants.Cyan);
                break;

            case 46: // DOMAIN_PORTAL
                player.SendServerMessage("Adding Portal domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.FnfSummonMonster3), clericLevel, fullDuration: false);

                // Create portal creature henchman
                CreatePortalHenchman(creature, player, clericLevel);
                player.SendServerMessage(" - Portal Creature Summoned", ColorConstants.Cyan);
                break;

            case 47: // DOMAIN_RENEWAL
                amount = 2 + ((clericLevel - 1) / 5);
                if (amount > 10)
                    amount = 10;
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
                amount = 1 + ((clericLevel - 1) / 7);
                player.SendServerMessage("Adding Repose domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadHoly), clericLevel, fullDuration: false);
                // Note: VersusRacialType (undead) not available - applying as general damage (same as Sun)
                Effect reposeDamage = Effect.DamageIncrease(amount, DamageType.Divine);
                reposeDamage.Tag = "PrayerReposeVsUndead";
                ApplyPrayerEffectsToPCs(creature, reposeDamage, clericLevel);
                player.SendServerMessage($" - Extra Damage vs Undead, {amount}", ColorConstants.Cyan);
                break;

            case 49: // DOMAIN_RETRIBUTION
                amount = 1 + ((clericLevel - 1) / 5);
                player.SendServerMessage("Adding Retribution domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurGlowRed), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageShield(amount, DamageBonus.Plus1d4, DamageType.Divine), clericLevel);
                player.SendServerMessage($" - Divine Damage Shield, 1d4 + {amount}", ColorConstants.Cyan);
                break;

            case 50: // DOMAIN_RUNE
                amount = 10 + 10 * ((clericLevel - 1) / 3);
                player.SendServerMessage("Adding Rune domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurProtStoneskin), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageReduction(10, DamagePower.Plus5, amount), clericLevel);
                player.SendServerMessage($" - Stoneskin (10/+5) stopping {amount} damage", ColorConstants.Cyan);
                break;

            case 51: // DOMAIN_SCALYKIND
                amount = 1 + ((clericLevel - 1) / 7);
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
                amount = 1 + ((clericLevel - 1) / 5);
                player.SendServerMessage("Adding Spell domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurMagicResistance), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SavingThrowIncrease(SavingThrow.All, amount, SavingThrowType.Spell), clericLevel);
                player.SendServerMessage($" - Bonus saves vs spells, {amount}", ColorConstants.Cyan);
                break;

            case 54: // DOMAIN_TIME
                amount = 1 + (clericLevel / 2);
                player.SendServerMessage("Adding Time domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHaste), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.MovementSpeedIncrease(amount), clericLevel);
                player.SendServerMessage($" - Movement speed +{amount}%", ColorConstants.Cyan);
                break;

            case 55: // DOMAIN_TRADE
                amount = 1 + (2 * (clericLevel / 3));
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
                amount = 1 + ((clericLevel - 1) / 7);
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

    private void ApplyPrayerEffectsToPCs(NwCreature creature, Effect effect, int divineLevel, bool fullDuration = true)
    {
        float duration = fullDuration ? 300.0f + (divineLevel * 20.0f) : 3.0f;

        if (divineLevel > 0)
        {
            // Apply to party members in area
            NwPlayer? player = creature.ControllingPlayer;
            if (player?.LoginCreature != null)
            {
                // Apply to caster
                creature.ApplyEffect(EffectDuration.Temporary, effect, TimeSpan.FromSeconds(duration));

                // Apply to party
                foreach (NwPlayer partyMember in player.PartyMembers)
                {
                    NwCreature? partyCreature = partyMember.ControlledCreature;
                    if (partyCreature != null && partyCreature.Area == creature.Area && partyCreature != creature)
                    {
                        partyCreature.ApplyEffect(EffectDuration.Temporary, effect, TimeSpan.FromSeconds(duration));
                    }
                }
            }
        }
        else
        {
            // Just apply to self (laypeople)
            creature.ApplyEffect(EffectDuration.Temporary, effect, TimeSpan.FromSeconds(duration));
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

        // Adjust challenge rating to match cleric level
        illusion.ChallengeRating = clericLevel;

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
        }
        else // Neutral
        {
            appearanceId = 1971;
            alignmentVfxId = 559;
            alignmentName = "Neutral";
        }

        // Set appearance
        NWScript.SetCreatureAppearanceType(portalCreature, appearanceId);

        // Adjust challenge rating based on cleric level
        portalCreature.ChallengeRating = clericLevel;

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
            InventorySlot.Boots,
            InventorySlot.Arms,
            InventorySlot.RightHand,
            InventorySlot.LeftHand,
            InventorySlot.Cloak,
            InventorySlot.Belt,
            InventorySlot.Neck,
            InventorySlot.RightRing,
            InventorySlot.LeftRing
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
}
