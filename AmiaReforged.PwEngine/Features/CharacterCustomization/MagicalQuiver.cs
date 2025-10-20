using AmiaReforged.Core.UserInterface;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using Microsoft.IdentityModel.Tokens;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.CharacterCustomization;

[ServiceBinding(typeof(MagicalQuiver))]
public class MagicalQuiver
{
    private readonly MagicQuiverMap _magicalQuiver;
    private const string QuiverTag = "magical_quiver";

    private const string PcKeyResRef = "ds_pckey";

    private const string MagicalQuiverPcKeyLocalObject = "magic_quiver";
    private const string IsSetUpLocalInt = "is_setup";
    private const string SelectedRaceLocalInt = "selected_race";

    private const string QuiverVfxTag = "quiver_vfx";
    private const string ArrowVfxTag = "arrow_vfx";

    private readonly Dictionary<int, string> _raceLabels = new()
    {
        { 0, "Dwarf Size" },
        { 1, "Elf Size" },
        { 2, "Gnome Size" },
        { 3, "Hin Size" },
        { 4, "Half-Elf Size" },
        { 5, "Half-Orc Size" },
        { 6, "Human Size" }
    };

    public MagicalQuiver(MagicQuiverMap magicalQuiver)
    {
        NwModule.Instance.OnItemUse += HandleMagicalQuiver;
        NwModule.Instance.OnClientEnter += ApplyVfx;

        _magicalQuiver = magicalQuiver;
    }

    private void ApplyVfx(ModuleEvents.OnClientEnter obj)
    {
        NwCreature? creature = obj.Player.LoginCreature;

        if (creature is null)
        {
            return;
        }

        Effect? existingQuiver = creature.ActiveEffects.FirstOrDefault(e => e.Tag == QuiverVfxTag);
        if (existingQuiver is not null)
        {
            creature.RemoveEffect(existingQuiver);
        }

        Effect? existingArrow = creature.ActiveEffects.FirstOrDefault(e => e.Tag == ArrowVfxTag);
        if (existingArrow is not null)
        {
            creature.RemoveEffect(existingArrow);
        }

        NwItem? pcKey = creature.Inventory.Items.FirstOrDefault(i => i.ResRef == PcKeyResRef);

        if (pcKey is null)
        {
            return;
        }

        int quiverVfx = NWScript.GetLocalInt(pcKey, QuiverVfxTag);
        int arrowVfx = NWScript.GetLocalInt(pcKey, ArrowVfxTag);

        if (arrowVfx != 0)
        {
            VisualEffectTableEntry arrows = NwGameTables.VisualEffectTable.GetRow(arrowVfx);
            Effect arrowEffect = Effect.VisualEffect(arrows);
            arrowEffect.Tag = ArrowVfxTag;

            creature.ApplyEffect(EffectDuration.Permanent, arrowEffect);
        }

        if (quiverVfx != 0)
        {
            VisualEffectTableEntry quiver = NwGameTables.VisualEffectTable.GetRow(quiverVfx);

            Effect quiverEffect = Effect.VisualEffect(quiver);
            quiverEffect.Tag = QuiverVfxTag;

            creature.ApplyEffect(EffectDuration.Permanent, quiverEffect);
        }
    }

    private void HandleMagicalQuiver(OnItemUse obj)
    {
        if (obj.Item.Tag != QuiverTag) return;

        if (!obj.UsedBy.IsPlayerControlled(out NwPlayer? player)) return;

        NwItem? pcKey = obj.UsedBy.Inventory.Items.FirstOrDefault(i => i.Tag == PcKeyResRef);
        if (pcKey is null) return;

        bool isSetUp = NWScript.GetLocalInt(obj.Item, IsSetUpLocalInt) == NWScript.TRUE;

        switch (obj.Item.Tag)
        {
            case QuiverTag:
                if (!isSetUp)
                {
                    NWScript.SetLocalObject(pcKey, MagicalQuiverPcKeyLocalObject, obj.Item);
                    player.ActionStartConversation(player.LoginCreature!, "quivracesel_conv", true, false);
                    return;
                }

                NWScript.SetLocalObject(pcKey, MagicalQuiverPcKeyLocalObject, obj.Item);
                player.ActionStartConversation(player.LoginCreature!, "quivchanger_conv", true, false);
                return;

            default:
                return;
        }
    }

    [ScriptHandler(scriptName: "quiv_race_select")]
    public void QuiverRaceSelection(CallInfo info)
    {
        uint player = info.ObjectSelf;
        int selectedRace = NWScript.GetLocalInt(player, "race");

        if (NWScript.GetIsObjectValid(player) != NWScript.TRUE)
        {
            return;
        }


        uint pcKey = NWScript.GetItemPossessedBy(player, PcKeyResRef);

        uint magicalQuiver = NWScript.GetLocalObject(pcKey, MagicalQuiverPcKeyLocalObject);
        NWScript.SetLocalInt(magicalQuiver, SelectedRaceLocalInt, selectedRace);
        NWScript.SetLocalInt(magicalQuiver, IsSetUpLocalInt, NWScript.TRUE);

        NWScript.SetName(magicalQuiver, $"<c~Îë>{_raceLabels[selectedRace]} Quiver</c>");
    }

    [ScriptHandler(scriptName: "quiver_select")]
    public void QuiverSelection(CallInfo info)
    {
        uint player = info.ObjectSelf;

        int selectedQuiver = NWScript.GetLocalInt(player, "quiver");
        int selectedArrow = NWScript.GetLocalInt(player, "arrow");

        int isQuiver = NWScript.GetLocalInt(player, "is_quiver");
        int isArrow = NWScript.GetLocalInt(player, "is_arrow");


        if (NWScript.GetIsObjectValid(player) != NWScript.TRUE)
        {
            return;
        }

        NwCreature? playerCreature = player.ToNwObjectSafe<NwCreature>();

        if (playerCreature is null)
        {
            return;
        }

        if (isQuiver == NWScript.TRUE)
        {
            HandleQuiver(playerCreature, selectedQuiver);
        }

        if (isArrow == NWScript.TRUE)
        {
            HandleArrows(playerCreature, selectedArrow);
        }
    }

    private void HandleQuiver(NwCreature playerCreature, int selectedQuiver)
    {
        WearableQuiver quiverEnum = (WearableQuiver)selectedQuiver;
        NwItem? pcKey = playerCreature.Inventory.Items.FirstOrDefault(i => i.Tag == PcKeyResRef);
        if (pcKey is null)
        {
            return;
        }


        Effect? existingQuiver = playerCreature.ActiveEffects.FirstOrDefault(e => e.Tag == QuiverVfxTag);
        if (existingQuiver is not null)
        {
            playerCreature.RemoveEffect(existingQuiver);
        }

        // The quiver was already removed, so return if their selection was "none"
        if (quiverEnum == WearableQuiver.None)
        {
            return;
        }

        uint magicalquiver = NWScript.GetLocalObject(pcKey, MagicalQuiverPcKeyLocalObject);
        int selectedRace = NWScript.GetLocalInt(magicalquiver, SelectedRaceLocalInt);
        int gender = NWScript.GetGender(playerCreature);

        int? vfx = gender == NWScript.GENDER_MALE
            ? _magicalQuiver.QuiversForRace[selectedRace].MaleQuivers.GetValueOrDefault(quiverEnum)
            : _magicalQuiver.QuiversForRace[selectedRace].FemaleQuivers.GetValueOrDefault(quiverEnum);


        if (vfx is null)
        {
            NWScript.SendMessageToPC(playerCreature,
                $"BUG REPORT: Could not select quiver from {selectedQuiver}. Screenshot this and send this to staff on Discord or on the Forums");
            return;
        }

        VisualEffectTableEntry quiverEntry = NwGameTables.VisualEffectTable.GetRow((int)vfx);
        Effect quiverVfx = Effect.VisualEffect(quiverEntry);
        quiverVfx.Tag = QuiverVfxTag;

        playerCreature.ApplyEffect(EffectDuration.Permanent, quiverVfx);
        NWScript.SetLocalInt(pcKey, QuiverVfxTag, (int)vfx);

    }

    private void HandleArrows(NwCreature playerCreature, int selectedArrow)
    {
        WearableArrow arrowEnum = (WearableArrow)selectedArrow;
        NwItem? pcKey = playerCreature.Inventory.Items.FirstOrDefault(i => i.Tag == PcKeyResRef);
        if (pcKey is null)
        {
            return;
        }


        Effect? existingArrows = playerCreature.ActiveEffects.FirstOrDefault(e => e.Tag == ArrowVfxTag);
        if (existingArrows is not null)
        {
            playerCreature.RemoveEffect(existingArrows);
        }

        // The quiver was already removed, so return if their selection was "none"
        if (arrowEnum == WearableArrow.None)
        {
            NWScript.SendMessageToPC(playerCreature, "Removing Arrows...");
            return;
        }

        uint magicalquiver = NWScript.GetLocalObject(pcKey, MagicalQuiverPcKeyLocalObject);
        int selectedRace = NWScript.GetLocalInt(magicalquiver, SelectedRaceLocalInt);
        int gender = NWScript.GetGender(playerCreature);

        int? vfx = gender == NWScript.GENDER_MALE
            ? _magicalQuiver.ArrowsForRace[selectedRace].MaleArrows.GetValueOrDefault(arrowEnum)
            : _magicalQuiver.ArrowsForRace[selectedRace].FemaleArrows.GetValueOrDefault(arrowEnum);


        if (vfx is null)
        {
            NWScript.SendMessageToPC(playerCreature,
                $"BUG REPORT: Could not select quiver from {selectedArrow}. Screenshot this and send this to staff on Discord or on the Forums");
            return;
        }

        VisualEffectTableEntry arrowEntry = NwGameTables.VisualEffectTable.GetRow((int)vfx);
        Effect arrowVfx = Effect.VisualEffect(arrowEntry);
        arrowVfx.Tag = ArrowVfxTag;

        playerCreature.ApplyEffect(EffectDuration.Permanent, arrowVfx);

        NWScript.SetLocalInt(pcKey, ArrowVfxTag, (int)vfx);
    }
}

[ServiceBinding(typeof(MagicQuiverMap))]
public sealed class MagicQuiverMap
{
    public Dictionary<int, RaceQuivers> QuiversForRace { get; }
    public Dictionary<int, RaceArrows> ArrowsForRace { get; }

    public MagicQuiverMap()
    {
        QuiversForRace = new Dictionary<int, RaceQuivers>
        {
            { 0, _dwarfQuivers },
            { 1, _elfQuivers },
            { 2, _hinQuivers },
            { 3, _hinQuivers },
            { 4, _humanQuivers },
            { 5, _halfOrcQuivers },
            { 6, _humanQuivers }
        };

        ArrowsForRace = new Dictionary<int, RaceArrows>
        {
            { 0, _dwarfArrows },
            { 1, _elfArrows },
            { 2, _hinArrows },
            { 3, _hinArrows },
            { 4, _humanArrows },
            { 5, _halfOrcArrows },
            { 6, _humanArrows },
        };
    }


    private readonly RaceQuivers _dwarfQuivers = new(new Dictionary<WearableQuiver, int>()
        {
            { WearableQuiver.QuiverBrown, 2282 },
            { WearableQuiver.QuiverBlack, 2294 },
            { WearableQuiver.QuiverBlue, 2426 },
            { WearableQuiver.QuiverWhite, 2438 },
            { WearableQuiver.QuiverGray, 2450 },
            { WearableQuiver.QuiverAqua, 2462 },
            { WearableQuiver.QuiverGreen, 2474 },
            { WearableQuiver.QuiverPurple, 2486 },
            { WearableQuiver.QuiverYellow, 2498 },
            { WearableQuiver.QuiverRed, 2510 }
        }, new Dictionary<WearableQuiver, int>()
        {
            { WearableQuiver.QuiverBrown, 2283 },
            { WearableQuiver.QuiverBlack, 2295 },
            { WearableQuiver.QuiverBlue, 2427 },
            { WearableQuiver.QuiverWhite, 2439 },
            { WearableQuiver.QuiverGray, 2451 },
            { WearableQuiver.QuiverAqua, 2463 },
            { WearableQuiver.QuiverGreen, 2475 },
            { WearableQuiver.QuiverPurple, 2487 },
            { WearableQuiver.QuiverYellow, 2499 },
            { WearableQuiver.QuiverRed, 2511 }
        }
    );

    private readonly RaceArrows _dwarfArrows = new RaceArrows(
        new Dictionary<WearableArrow, int>()
        {
            { WearableArrow.ArrowRed, 2306 },
            { WearableArrow.ArrowBlue, 2318 },
            { WearableArrow.ArrowGreen, 2330 },
            { WearableArrow.ArrowGray, 2342 },
            { WearableArrow.ArrowWhite, 2354 },
            { WearableArrow.ArrowBlack, 2366 },
            { WearableArrow.ArrowYellow, 2378 },
            { WearableArrow.ArrowOrange, 2390 },
            { WearableArrow.ArrowPurple, 2402 },
            { WearableArrow.ArrowAqua, 2414 }
        },
        new Dictionary<WearableArrow, int>()
        {
            { WearableArrow.ArrowRed, 2307 },
            { WearableArrow.ArrowBlue, 2319 },
            { WearableArrow.ArrowGreen, 2331 },
            { WearableArrow.ArrowGray, 2343 },
            { WearableArrow.ArrowWhite, 2355 },
            { WearableArrow.ArrowBlack, 2367 },
            { WearableArrow.ArrowYellow, 2379 },
            { WearableArrow.ArrowOrange, 2391 },
            { WearableArrow.ArrowPurple, 2403 },
            { WearableArrow.ArrowAqua, 2415 }
        }
    );

    private readonly RaceQuivers _humanQuivers = new RaceQuivers(new Dictionary<WearableQuiver, int>()
        {
            { WearableQuiver.QuiverBrown, 2274 },
            { WearableQuiver.QuiverBlack, 2286 },
            { WearableQuiver.QuiverBlue, 2418 },
            { WearableQuiver.QuiverWhite, 2430 },
            { WearableQuiver.QuiverGray, 2442 },
            { WearableQuiver.QuiverAqua, 2454 },
            { WearableQuiver.QuiverGreen, 2466 },
            { WearableQuiver.QuiverPurple, 2478 },
            { WearableQuiver.QuiverYellow, 2490 },
            { WearableQuiver.QuiverRed, 2502 }
        }, new Dictionary<WearableQuiver, int>()
        {
            { WearableQuiver.QuiverBrown, 2275 },
            { WearableQuiver.QuiverBlack, 2287 },
            { WearableQuiver.QuiverBlue, 2419 },
            { WearableQuiver.QuiverWhite, 2431 },
            { WearableQuiver.QuiverGray, 2443 },
            { WearableQuiver.QuiverAqua, 2455 },
            { WearableQuiver.QuiverGreen, 2467 },
            { WearableQuiver.QuiverPurple, 2479 },
            { WearableQuiver.QuiverYellow, 2491 },
            { WearableQuiver.QuiverRed, 2503 }
        }
    );

    private readonly RaceArrows _humanArrows = new RaceArrows(
        new Dictionary<WearableArrow, int>()
        {
            { WearableArrow.ArrowRed, 2298 },
            { WearableArrow.ArrowBlue, 2310 },
            { WearableArrow.ArrowGreen, 2322 },
            { WearableArrow.ArrowGray, 2334 },
            { WearableArrow.ArrowWhite, 2346 },
            { WearableArrow.ArrowBlack, 2358 },
            { WearableArrow.ArrowYellow, 2370 },
            { WearableArrow.ArrowOrange, 2382 },
            { WearableArrow.ArrowPurple, 2394 },
            { WearableArrow.ArrowAqua, 2406 }
        },
        new Dictionary<WearableArrow, int>()
        {
            { WearableArrow.ArrowRed, 2299 },
            { WearableArrow.ArrowBlue, 2311 },
            { WearableArrow.ArrowGreen, 2323 },
            { WearableArrow.ArrowGray, 2335 },
            { WearableArrow.ArrowWhite, 2347 },
            { WearableArrow.ArrowBlack, 2359 },
            { WearableArrow.ArrowYellow, 2371 },
            { WearableArrow.ArrowOrange, 2383 },
            { WearableArrow.ArrowPurple, 2395 },
            { WearableArrow.ArrowAqua, 2407 }
        }
    );

    private readonly RaceQuivers _elfQuivers = new RaceQuivers(new Dictionary<WearableQuiver, int>()
        {
            { WearableQuiver.QuiverBrown, 2280 },
            { WearableQuiver.QuiverBlack, 2292 },
            { WearableQuiver.QuiverBlue, 2424 },
            { WearableQuiver.QuiverWhite, 2436 },
            { WearableQuiver.QuiverGray, 2448 },
            { WearableQuiver.QuiverAqua, 2460 },
            { WearableQuiver.QuiverGreen, 2472 },
            { WearableQuiver.QuiverPurple, 2484 },
            { WearableQuiver.QuiverYellow, 2496 },
            { WearableQuiver.QuiverRed, 2508 }
        }, new Dictionary<WearableQuiver, int>()
        {
            { WearableQuiver.QuiverBrown, 2281 },
            { WearableQuiver.QuiverBlack, 2293 },
            { WearableQuiver.QuiverBlue, 2425 },
            { WearableQuiver.QuiverWhite, 2437 },
            { WearableQuiver.QuiverGray, 2449 },
            { WearableQuiver.QuiverAqua, 2461 },
            { WearableQuiver.QuiverGreen, 2473 },
            { WearableQuiver.QuiverPurple, 2485 },
            { WearableQuiver.QuiverYellow, 2497 },
            { WearableQuiver.QuiverRed, 2509 }
        }
    );

    private readonly RaceArrows _elfArrows = new RaceArrows(
        new Dictionary<WearableArrow, int>()
        {
            { WearableArrow.ArrowRed, 2304 },
            { WearableArrow.ArrowBlue, 2316 },
            { WearableArrow.ArrowGreen, 2328 },
            { WearableArrow.ArrowGray, 2340 },
            { WearableArrow.ArrowWhite, 2352 },
            { WearableArrow.ArrowBlack, 2364 },
            { WearableArrow.ArrowYellow, 2376 },
            { WearableArrow.ArrowOrange, 2388 },
            { WearableArrow.ArrowPurple, 2400 },
            { WearableArrow.ArrowAqua, 2412 }
        },
        new Dictionary<WearableArrow, int>()
        {
            { WearableArrow.ArrowRed, 2305 },
            { WearableArrow.ArrowBlue, 2317 },
            { WearableArrow.ArrowGreen, 2329 },
            { WearableArrow.ArrowGray, 2341 },
            { WearableArrow.ArrowWhite, 2353 },
            { WearableArrow.ArrowBlack, 2365 },
            { WearableArrow.ArrowYellow, 2377 },
            { WearableArrow.ArrowOrange, 2389 },
            { WearableArrow.ArrowPurple, 2401 },
            { WearableArrow.ArrowAqua, 2413 }
        }
    );

    private readonly RaceQuivers _hinQuivers = new RaceQuivers(new Dictionary<WearableQuiver, int>()
        {
            { WearableQuiver.QuiverBrown, 2276 },
            { WearableQuiver.QuiverBlack, 2288 },
            { WearableQuiver.QuiverBlue, 2420 },
            { WearableQuiver.QuiverWhite, 2432 },
            { WearableQuiver.QuiverGray, 2444 },
            { WearableQuiver.QuiverAqua, 2456 },
            { WearableQuiver.QuiverGreen, 2468 },
            { WearableQuiver.QuiverPurple, 2480 },
            { WearableQuiver.QuiverYellow, 2492 },
            { WearableQuiver.QuiverRed, 2504 }
        },
        new Dictionary<WearableQuiver, int>()
        {
            { WearableQuiver.QuiverBrown, 2277 },
            { WearableQuiver.QuiverBlack, 2289 },
            { WearableQuiver.QuiverBlue, 2421 },
            { WearableQuiver.QuiverWhite, 2433 },
            { WearableQuiver.QuiverGray, 2445 },
            { WearableQuiver.QuiverAqua, 2457 },
            { WearableQuiver.QuiverGreen, 2469 },
            { WearableQuiver.QuiverPurple, 2481 },
            { WearableQuiver.QuiverYellow, 2493 },
            { WearableQuiver.QuiverRed, 2505 }
        }
    );

    private readonly RaceArrows _hinArrows = new RaceArrows(
        new Dictionary<WearableArrow, int>()
        {
            { WearableArrow.ArrowRed, 2300 },
            { WearableArrow.ArrowBlue, 2312 },
            { WearableArrow.ArrowGreen, 2324 },
            { WearableArrow.ArrowGray, 2336 },
            { WearableArrow.ArrowWhite, 2348 },
            { WearableArrow.ArrowBlack, 2360 },
            { WearableArrow.ArrowYellow, 2372 },
            { WearableArrow.ArrowOrange, 2384 },
            { WearableArrow.ArrowPurple, 2396 },
            { WearableArrow.ArrowAqua, 2408 }
        },
        new Dictionary<WearableArrow, int>()
        {
            { WearableArrow.ArrowRed, 2301 },
            { WearableArrow.ArrowBlue, 2313 },
            { WearableArrow.ArrowGreen, 2325 },
            { WearableArrow.ArrowGray, 2337 },
            { WearableArrow.ArrowWhite, 2349 },
            { WearableArrow.ArrowBlack, 2361 },
            { WearableArrow.ArrowYellow, 2373 },
            { WearableArrow.ArrowOrange, 2385 },
            { WearableArrow.ArrowPurple, 2397 },
            { WearableArrow.ArrowAqua, 2409 }
        }
    );

    private readonly RaceQuivers _halfOrcQuivers = new RaceQuivers(new Dictionary<WearableQuiver, int>()
        {
            { WearableQuiver.QuiverBrown, 2284 },
            { WearableQuiver.QuiverBlack, 2296 },
            { WearableQuiver.QuiverBlue, 2428 },
            { WearableQuiver.QuiverWhite, 2440 },
            { WearableQuiver.QuiverGray, 2452 },
            { WearableQuiver.QuiverAqua, 2464 },
            { WearableQuiver.QuiverGreen, 2476 },
            { WearableQuiver.QuiverPurple, 2488 },
            { WearableQuiver.QuiverYellow, 2500 },
            { WearableQuiver.QuiverRed, 2512 }
        },
        new Dictionary<WearableQuiver, int>()
        {
            { WearableQuiver.QuiverBrown, 2285 },
            { WearableQuiver.QuiverBlack, 2297 },
            { WearableQuiver.QuiverBlue, 2429 },
            { WearableQuiver.QuiverWhite, 2441 },
            { WearableQuiver.QuiverGray, 2453 },
            { WearableQuiver.QuiverAqua, 2465 },
            { WearableQuiver.QuiverGreen, 2477 },
            { WearableQuiver.QuiverPurple, 2489 },
            { WearableQuiver.QuiverYellow, 2501 },
            { WearableQuiver.QuiverRed, 2513 }
        }
    );

    private readonly RaceArrows _halfOrcArrows = new RaceArrows(
        new Dictionary<WearableArrow, int>()
        {
            { WearableArrow.ArrowRed, 2308 },
            { WearableArrow.ArrowBlue, 2320 },
            { WearableArrow.ArrowGreen, 2332 },
            { WearableArrow.ArrowGray, 2344 },
            { WearableArrow.ArrowWhite, 2356 },
            { WearableArrow.ArrowBlack, 2368 },
            { WearableArrow.ArrowYellow, 2380 },
            { WearableArrow.ArrowOrange, 2392 },
            { WearableArrow.ArrowPurple, 2404 },
            { WearableArrow.ArrowAqua, 2416 }
        },
        new Dictionary<WearableArrow, int>()
        {
            { WearableArrow.ArrowRed, 2309 },
            { WearableArrow.ArrowBlue, 2321 },
            { WearableArrow.ArrowGreen, 2333 },
            { WearableArrow.ArrowGray, 2345 },
            { WearableArrow.ArrowWhite, 2357 },
            { WearableArrow.ArrowBlack, 2369 },
            { WearableArrow.ArrowYellow, 2381 },
            { WearableArrow.ArrowOrange, 2393 },
            { WearableArrow.ArrowPurple, 2405 },
            { WearableArrow.ArrowAqua, 2417 }
        }
    );
}

public record RaceQuivers(Dictionary<WearableQuiver, int> MaleQuivers, Dictionary<WearableQuiver, int> FemaleQuivers);

public record RaceArrows(Dictionary<WearableArrow, int> MaleArrows, Dictionary<WearableArrow, int> FemaleArrows);

public enum WearableQuiver
{
    None = 0,

    // Quivers
    QuiverBrown = 1,
    QuiverBlack = 2,
    QuiverBlue = 3,
    QuiverWhite = 4,
    QuiverGray = 5,
    QuiverAqua = 6,
    QuiverGreen = 7,
    QuiverPurple = 8,
    QuiverYellow = 9,
    QuiverRed = 10,
}

public enum WearableArrow
{
    None = 0,

    // Arrows
    ArrowRed = 11,
    ArrowBlue = 12,
    ArrowGreen = 13,
    ArrowGray = 14,
    ArrowWhite = 15,
    ArrowBlack = 16,
    ArrowYellow = 17,
    ArrowOrange = 18,
    ArrowPurple = 19,
    ArrowAqua = 20,
}
