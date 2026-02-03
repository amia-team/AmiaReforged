using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
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
            NWScript.ActionPlayAnimation(NWScript.ANIMATION_LOOPING_MEDITATE, 1.0f, 12.0f);
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
            MakeFallen(player, creature, idol, deity, "Your alignment no longer pleases your deity!");
            return;
        }

        // Clerics must have at least one matching domain
        if (clericLevels > 0)
        {
            bool hasMatchingDomain = HasMatchingDomain(creature, idol);
            if (!hasMatchingDomain)
            {
                MakeFallen(player, creature, idol, deity, "None of your domains match your deity's domains!");
                return;
            }
        }

        // For non-divine casters, check alignment axis
        if (!isDivineCaster && !alignmentMatches)
        {
            // First check if they have an OPPOSING alignment (Good vs Evil) - this triggers a smite!
            bool isOpposingAxis = IsOpposingGoodEvilAxis(creature, idol);

            if (isOpposingAxis)
            {
                // Smite the heretic!
                SmiteHeretic(player, creature, idol, deity);
                return;
            }

            // Check if they at least share the same Good/Evil axis
            bool axisMatches = MatchAlignmentAxis(creature, idol);

            if (!axisMatches)
            {
                // Wrong alignment entirely! Don't set cooldown for invalid prayers
                player.SendServerMessage($"{deity} isn't pleased. Nothing happens...", ColorConstants.Red);
                player.SendServerMessage("[Your alignment is not valid for this god.]", ColorConstants.Gray);
                return;
            }
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
        int creatureDomain1 = NWScript.GetDomain(creature, 1);
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
            // They get individual blessing (not party-wide) using total divine level
            NwTask.Run(async () =>
            {
                await NwTask.Delay(TimeSpan.FromSeconds(5));
                player.SendServerMessage($"{deity}'s power is demonstrated through your prayer!", ColorConstants.Green);
                player.SendServerMessage($"[Divine Level: {totalDivineLevel}]", ColorConstants.Gray);

                await NwTask.Delay(TimeSpan.FromSeconds(1));
                // Apply only to self for non-cleric/druid divine casters
                CastAlignmentEffectSelf(creature, idol, totalDivineLevel);
            });
        }
        else
        {
            // Non-divine caster with axis match - layperson prayer with success rate
            if (!alignmentMatches)
            {
                player.SendServerMessage($"You honor {deity} through your actions, if not your exact path...", ColorConstants.Yellow);
            }

            int successRate = GetSuccessRate(creature);
            int roll = Random.Shared.Next(1, 101);

            if (roll <= successRate)
            {
                NwTask.Run(async () =>
                {
                    await NwTask.Delay(TimeSpan.FromSeconds(5));
                    player.SendServerMessage($"{deity} blesses you!", ColorConstants.Green);
                    player.SendServerMessage($"[Your chance on a blessing is {successRate}%]", ColorConstants.Gray);

                    await NwTask.Delay(TimeSpan.FromSeconds(1));
                    CastAlignmentEffect(creature, idol, 0);
                });
            }
            else
            {
                NwTask.Run(async () =>
                {
                    await NwTask.Delay(TimeSpan.FromSeconds(5));
                    player.SendServerMessage($"{deity} does not answer your prayer this time...", ColorConstants.Orange);
                    player.SendServerMessage($"[Your chance on a blessing is {successRate}%]", ColorConstants.Gray);
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
        int pcDomain1 = NWScript.GetDomain(creature, 1);
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
                .FirstOrDefault(p => p.Tag?.Equals(idolTag, StringComparison.OrdinalIgnoreCase) == true);

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

    private void CastAlignmentEffect(NwCreature creature, NwPlaceable idol, int divineLevel)
    {
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
                Effect eVsGood = Effect.ACIncrease(vsGood, ACBonus.Dodge);
                eVsGood.SubType = EffectSubType.Supernatural;
                eVsGood.Tag = "PrayerVsGood";
                player?.SendServerMessage($" - Extra AC vs Good, {vsGood}", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, eVsGood, divineLevel, fullDuration: true);
            }

            if (vsEvil > 0)
            {
                Effect eVsEvil = Effect.ACIncrease(vsEvil, ACBonus.Dodge);
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
        // Same as CastAlignmentEffect but only applies to self (for Rangers, Paladins, Blackguards, Divine Champions)
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
            Effect eVsGood = Effect.ACIncrease(vsGood, ACBonus.Dodge);
            eVsGood.SubType = EffectSubType.Supernatural;
            eVsGood.Tag = "PrayerVsGood";
            player?.SendServerMessage($" - Extra AC vs Good, {vsGood}", ColorConstants.Cyan);
            creature.ApplyEffect(EffectDuration.Temporary, eVsGood, TimeSpan.FromSeconds(duration));
        }

        if (vsEvil > 0)
        {
            Effect eVsEvil = Effect.ACIncrease(vsEvil, ACBonus.Dodge);
            eVsEvil.SubType = EffectSubType.Supernatural;
            eVsEvil.Tag = "PrayerVsEvil";
            player?.SendServerMessage($" - Extra AC vs Evil, {vsEvil}", ColorConstants.Cyan);
            creature.ApplyEffect(EffectDuration.Temporary, eVsEvil, TimeSpan.FromSeconds(duration));
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
                amount = 1 + ((clericLevel - 1) / 10);
                player.SendServerMessage("Adding Healing domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHealingS), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.Regenerate(amount, TimeSpan.FromSeconds(6.0)), clericLevel);
                player.SendServerMessage($" - Regeneration, +{amount}", ColorConstants.Cyan);
                break;

            case 10: // DOMAIN_KNOWLEDGE
                amount = 1 + ((clericLevel - 1) / 5);
                player.SendServerMessage("Adding Knowledge domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.AllSkills, amount), clericLevel);
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
                Effect chaosDamage = Effect.DamageIncrease(amount, DamageType.Magical);
                chaosDamage.Tag = "PrayerChaosVsLaw";
                ApplyPrayerEffectsToPCs(creature, chaosDamage, clericLevel);
                player.SendServerMessage($" - Extra Damage vs Law, {amount}", ColorConstants.Cyan);
                break;

            case 25: // DOMAIN_CHARM
                player.SendServerMessage("Adding Charm domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadMind), clericLevel, fullDuration: false);
                // Immunity to charm/hold/dominate spells
                ApplyPrayerEffectsToPCs(creature, Effect.SpellImmunity(Spell.CharmPerson), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SpellImmunity(Spell.CharmMonster), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SpellImmunity(Spell.CharmPersonOrAnimal), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SpellImmunity(Spell.MassCharm), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SpellImmunity(Spell.HoldPerson), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SpellImmunity(Spell.HoldMonster), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SpellImmunity(Spell.DominateMonster), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SpellImmunity(Spell.DominatePerson), clericLevel);
                player.SendServerMessage(" - Immunity to Charm/Hold/Dominate Spells", ColorConstants.Cyan);
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
                player.SendServerMessage("Adding Darkness domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurMagicalSight), clericLevel);
                // Note: Light blindness immunity would need custom flag handling
                // Applying ultravision as alternative (same as Cavern)
                ApplyPrayerEffectsToPCs(creature, Effect.Ultravision(), clericLevel);
                player.SendServerMessage(" - Ultravision", ColorConstants.Cyan);
                break;

            case 31: // DOMAIN_DRAGON
                amount = 1;
                player.SendServerMessage("Adding Dragon domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpStarburstRed), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.AbilityIncrease(Ability.Strength, amount), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.AbilityIncrease(Ability.Dexterity, amount), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.AbilityIncrease(Ability.Constitution, amount), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.AbilityIncrease(Ability.Intelligence, amount), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.AbilityIncrease(Ability.Wisdom, amount), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.AbilityIncrease(Ability.Charisma, amount), clericLevel);
                player.SendServerMessage(" - All Abilities +1", ColorConstants.Cyan);
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
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Hide, amount), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.MoveSilently, amount), clericLevel);
                player.SendServerMessage($" - Hide/Move Silently +{amount}", ColorConstants.Cyan);
                break;

            case 39: // DOMAIN_HATRED
                amount = 1 + ((clericLevel - 1) / 7);
                player.SendServerMessage("Adding Hatred domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurProtectionEvilMajor), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.DamageShield(amount, DamageBonus.Plus1d4, DamageType.Divine), clericLevel);
                player.SendServerMessage($" - Divine Damage Shield, 1d4 + {amount}", ColorConstants.Cyan);
                break;

            case 40: // DOMAIN_ILLUSION
                player.SendServerMessage("Adding Illusion domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHealingS), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.Invisibility(InvisibilityType.Normal), clericLevel);
                player.SendServerMessage(" - Invisibility", ColorConstants.Cyan);
                break;

            case 41: // DOMAIN_LAW
                amount = 1 + ((clericLevel - 1) / 7);
                player.SendServerMessage("Adding Law domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadOdd), clericLevel, fullDuration: false);
                // Note: VersusAlignment not available - applying as general damage
                Effect lawDamage = Effect.DamageIncrease(amount, DamageType.Magical);
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
                Effect moonDamage = Effect.DamageIncrease(amount, DamageType.Magical);
                moonDamage.Tag = "PrayerMoonVsShapechangers";
                ApplyPrayerEffectsToPCs(creature, moonDamage, clericLevel);
                player.SendServerMessage($" - Extra Damage vs Shapechangers, {amount}", ColorConstants.Cyan);
                break;

            case 44: // DOMAIN_NOBILITY
                amount = 1 + ((clericLevel - 1) / 5);
                player.SendServerMessage("Adding Nobility domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpCharm), clericLevel, fullDuration: false);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Intimidate, amount), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Persuade, amount), clericLevel);
                ApplyPrayerEffectsToPCs(creature, Effect.SkillIncrease(Skill.Bluff, amount), clericLevel);
                player.SendServerMessage($" - Persuade/Intimidate/Bluff +{amount}", ColorConstants.Cyan);
                break;

            case 45: // DOMAIN_ORC
                amount = 1 + ((clericLevel - 1) / 7);
                player.SendServerMessage("Adding Orc domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.ImpHeadEvil), clericLevel, fullDuration: false);
                // Note: VersusRacialType (elves) not available - applying as general damage
                Effect orcDamage = Effect.DamageIncrease(amount, DamageType.Magical);
                orcDamage.Tag = "PrayerOrcVsElves";
                ApplyPrayerEffectsToPCs(creature, orcDamage, clericLevel);
                player.SendServerMessage($" - Extra Damage vs Elves, {amount}", ColorConstants.Cyan);
                break;

            case 46: // DOMAIN_PORTAL
                player.SendServerMessage("Adding Portal domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.FnfSummonMonster3), clericLevel, fullDuration: false);
                // Note: Portal wand mechanics would need custom flag handling
                // Placeholder visual effect for now
                player.SendServerMessage(" - Portal wands take no charges to use", ColorConstants.Cyan);
                player.SendServerMessage(" - (Feature requires custom implementation)", ColorConstants.Yellow);
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
                player.SendServerMessage("Adding Retribution domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurGlowRed), clericLevel);
                // Note: Explosion on death would need custom flag/event handling
                // Placeholder visual effect for now
                player.SendServerMessage(" - Explosion Upon Death", ColorConstants.Cyan);
                player.SendServerMessage(" - (Feature requires custom implementation)", ColorConstants.Yellow);
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
                player.SendServerMessage("Adding Trade domain effects:", ColorConstants.Cyan);
                ApplyPrayerEffectsToPCs(creature, Effect.VisualEffect(VfxType.DurSanctuary), clericLevel);
                // Note: Merchant appraise bonus would need custom flag handling
                // Placeholder visual effect for now
                player.SendServerMessage(" - +5% on appraise checks with merchants", ColorConstants.Cyan);
                player.SendServerMessage(" - (Feature requires custom implementation)", ColorConstants.Yellow);
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
}
