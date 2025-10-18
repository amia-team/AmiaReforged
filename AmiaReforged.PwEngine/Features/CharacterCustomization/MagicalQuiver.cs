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
        _magicalQuiver = magicalQuiver;
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

        if (NWScript.GetIsObjectValid(player) != NWScript.TRUE)
        {
            return;
        }

        NwCreature? playerCreature = player.ToNwObjectSafe<NwCreature>();

        if (playerCreature is null)
        {
            return;
        }


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
        int gender = NWScript.GetGender(player);

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
            { WearableQuiver.QuiverRed, 2510 },
            { WearableQuiver.ArrowRed, 2306 },
            { WearableQuiver.ArrowBlue, 2318 },
            { WearableQuiver.ArrowGreen, 2330 },
            { WearableQuiver.ArrowGray, 2342 },
            { WearableQuiver.ArrowWhite, 2354 },
            { WearableQuiver.ArrowBlack, 2366 },
            { WearableQuiver.ArrowYellow, 2378 },
            { WearableQuiver.ArrowOrange, 2390 },
            { WearableQuiver.ArrowPurple, 2402 },
            { WearableQuiver.ArrowAqua, 2414 }
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
            { WearableQuiver.QuiverRed, 2511 },
            { WearableQuiver.ArrowRed, 2307 },
            { WearableQuiver.ArrowBlue, 2319 },
            { WearableQuiver.ArrowGreen, 2331 },
            { WearableQuiver.ArrowGray, 2343 },
            { WearableQuiver.ArrowWhite, 2355 },
            { WearableQuiver.ArrowBlack, 2367 },
            { WearableQuiver.ArrowYellow, 2379 },
            { WearableQuiver.ArrowOrange, 2391 },
            { WearableQuiver.ArrowPurple, 2403 },
            { WearableQuiver.ArrowAqua, 2415 }
        }
    );

    private readonly RaceArrows _dwarfArrows = new RaceArrows(
        new Dictionary<WearableQuiver, int>()
        {
        },
        new Dictionary<WearableQuiver, int>()
        {
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
            { WearableQuiver.QuiverRed, 2502 },
            { WearableQuiver.ArrowRed, 2298 },
            { WearableQuiver.ArrowBlue, 2310 },
            { WearableQuiver.ArrowGreen, 2322 },
            { WearableQuiver.ArrowGray, 2334 },
            { WearableQuiver.ArrowWhite, 2346 },
            { WearableQuiver.ArrowBlack, 2358 },
            { WearableQuiver.ArrowYellow, 2370 },
            { WearableQuiver.ArrowOrange, 2382 },
            { WearableQuiver.ArrowPurple, 2394 },
            { WearableQuiver.ArrowAqua, 2406 }
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
            { WearableQuiver.QuiverRed, 2503 },
            { WearableQuiver.ArrowRed, 2299 },
            { WearableQuiver.ArrowBlue, 2311 },
            { WearableQuiver.ArrowGreen, 2323 },
            { WearableQuiver.ArrowGray, 2335 },
            { WearableQuiver.ArrowWhite, 2347 },
            { WearableQuiver.ArrowBlack, 2359 },
            { WearableQuiver.ArrowYellow, 2371 },
            { WearableQuiver.ArrowOrange, 2383 },
            { WearableQuiver.ArrowPurple, 2395 },
            { WearableQuiver.ArrowAqua, 2407 }
        }
    );

    private readonly RaceArrows _humanArrows = new RaceArrows(
        new Dictionary<WearableQuiver, int>()
        {
        },
        new Dictionary<WearableQuiver, int>()
        {
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
            { WearableQuiver.QuiverRed, 2508 },
            { WearableQuiver.ArrowRed, 2304 },
            { WearableQuiver.ArrowBlue, 2316 },
            { WearableQuiver.ArrowGreen, 2328 },
            { WearableQuiver.ArrowGray, 2340 },
            { WearableQuiver.ArrowWhite, 2352 },
            { WearableQuiver.ArrowBlack, 2364 },
            { WearableQuiver.ArrowYellow, 2376 },
            { WearableQuiver.ArrowOrange, 2388 },
            { WearableQuiver.ArrowPurple, 2400 },
            { WearableQuiver.ArrowAqua, 2412 }
        }, new Dictionary<WearableQuiver, int>()
        {
            { WearableQuiver.QuiverBrown, 2281 },
            { WearableQuiver.QuiverBlack, 2293 },
            { WearableQuiver.QuiverBlue, 2425 },
            { WearableQuiver.QuiverWhite, 2437 },
            { WearableQuiver.QuiverGray, 2449 },
            { WearableQuiver.QuiverAqua, 2461 },
            { WearableQuiver.QuiverGreen, 2743 },
            { WearableQuiver.QuiverPurple, 2485 },
            { WearableQuiver.QuiverYellow, 2497 },
            { WearableQuiver.QuiverRed, 2509 },
            { WearableQuiver.ArrowRed, 2305 },
            { WearableQuiver.ArrowBlue, 2317 },
            { WearableQuiver.ArrowGreen, 2329 },
            { WearableQuiver.ArrowGray, 2341 },
            { WearableQuiver.ArrowWhite, 2353 },
            { WearableQuiver.ArrowBlack, 2365 },
            { WearableQuiver.ArrowYellow, 2377 },
            { WearableQuiver.ArrowOrange, 2389 },
            { WearableQuiver.ArrowPurple, 2401 },
            { WearableQuiver.ArrowAqua, 2413 }
        }
    );

    private readonly RaceArrows _elfArrows = new RaceArrows(
        new Dictionary<WearableQuiver, int>()
        {
        },
        new Dictionary<WearableQuiver, int>()
        {
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
            { WearableQuiver.QuiverRed, 2504 },
            { WearableQuiver.ArrowRed, 2300 },
            { WearableQuiver.ArrowBlue, 2312 },
            { WearableQuiver.ArrowGreen, 2324 },
            { WearableQuiver.ArrowGray, 2336 },
            { WearableQuiver.ArrowWhite, 2348 },
            { WearableQuiver.ArrowBlack, 2360 },
            { WearableQuiver.ArrowYellow, 2372 },
            { WearableQuiver.ArrowOrange, 2384 },
            { WearableQuiver.ArrowPurple, 2396 },
            { WearableQuiver.ArrowAqua, 2408 }
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
            { WearableQuiver.QuiverRed, 2505 },
            { WearableQuiver.ArrowRed, 2301 },
            { WearableQuiver.ArrowBlue, 2313 },
            { WearableQuiver.ArrowGreen, 2325 },
            { WearableQuiver.ArrowGray, 2337 },
            { WearableQuiver.ArrowWhite, 2349 },
            { WearableQuiver.ArrowBlack, 2361 },
            { WearableQuiver.ArrowYellow, 2373 },
            { WearableQuiver.ArrowOrange, 2385 },
            { WearableQuiver.ArrowPurple, 2397 },
            { WearableQuiver.ArrowAqua, 2409 }
        }
    );
    private readonly RaceArrows _hinArrows = new RaceArrows(
        new Dictionary<WearableQuiver, int>()
        {
        },
        new Dictionary<WearableQuiver, int>()
        {
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
            { WearableQuiver.QuiverRed, 2512 },
            { WearableQuiver.ArrowRed, 2308 },
            { WearableQuiver.ArrowBlue, 2320 },
            { WearableQuiver.ArrowGreen, 2332 },
            { WearableQuiver.ArrowGray, 2344 },
            { WearableQuiver.ArrowWhite, 2356 },
            { WearableQuiver.ArrowBlack, 2368 },
            { WearableQuiver.ArrowYellow, 2380 },
            { WearableQuiver.ArrowOrange, 2392 },
            { WearableQuiver.ArrowPurple, 2404 },
            { WearableQuiver.ArrowAqua, 2416 }
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
            { WearableQuiver.QuiverRed, 2513 },
            { WearableQuiver.ArrowRed, 2309 },
            { WearableQuiver.ArrowBlue, 2321 },
            { WearableQuiver.ArrowGreen, 2333 },
            { WearableQuiver.ArrowGray, 2345 },
            { WearableQuiver.ArrowWhite, 2357 },
            { WearableQuiver.ArrowBlack, 2369 },
            { WearableQuiver.ArrowYellow, 2381 },
            { WearableQuiver.ArrowOrange, 2393 },
            { WearableQuiver.ArrowPurple, 2405 },
            { WearableQuiver.ArrowAqua, 2417 }
        }
    );

    private readonly RaceArrows _halfOrcArrows = new RaceArrows(
        new Dictionary<WearableQuiver, int>()
        {
        },
        new Dictionary<WearableQuiver, int>()
        {
        }
    );
}

public record RaceQuivers(Dictionary<WearableQuiver, int> MaleQuivers, Dictionary<WearableQuiver, int> FemaleQuivers);

public record RaceArrows(Dictionary<WearableQuiver, int> MaleArrows, Dictionary<WearableQuiver, int> FemaleArrows);

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
