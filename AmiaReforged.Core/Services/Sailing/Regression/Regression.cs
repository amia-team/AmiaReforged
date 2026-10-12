#if SAILING_REGRESSION
using AmiaReforged.Core.Models.Sailing;
using Microsoft.EntityFrameworkCore;
using AmiaReforged.Core.Services;
using AmiaReforged.Core.Services.Sailing;

static class Regression
{
    private static int _checks;
    static void Check(bool valid, string message)
    {
        _checks++;
        if (!valid) throw new Exception(message);
    }
    static ShipState Ship(string id, float x = 0, float y = 0, int hull = 100) => new()
    { ShipId = id, ShipName = id, DeckAreaResRef = "deck", AreaResRef = "ocean_01", X = x, Y = y,
        MaximumHull = hull, Hull = hull };

    public static async Task Main()
    {
        try { await RunAsync(); }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FAIL after {_checks} regression assertions.");
            Console.Error.WriteLine($"Exception type: {ex.GetType().FullName}\nMessage: {ex.Message}");
            Console.Error.WriteLine($"Inner exception: {ex.InnerException?.ToString() ?? "(none)"}\n{ex}");
            Environment.ExitCode = 1;
        }
    }

    private static async Task RunAsync()
    {
        Console.WriteLine($"Runtime: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}; target: net8.0.");
        if (Environment.GetCommandLineArgs().Contains("--api"))
        {
            foreach (Type type in typeof(Anvil.API.NwPlayer).Assembly.GetTypes().Where(t => t.Name == "OnSpellCast" || t.Name == "MemorizedSpellSlot"))
                Console.WriteLine(type.FullName + " " + string.Join(", ", type.GetProperties().Select(p => p.PropertyType.Name + " " + p.Name)));
            return;
        }
        ShipState large = Ship("large", hull: 140);
        Check(large.ApplyHullDamage(10) == 10 && large.Hull == 130, "Damage must not truncate a 140-hull ship.");
        Check(large.RepairHull(100) == 10 && large.Hull == 140, "Repair respects definition maximum.");
        large.IsNavigating = large.Underway = true;
        large.DestinationAreaResRef = "ocean_01";
        Check(large.ApplyHullDamage(200) == 140 && large.IsDisabled && !large.IsNavigating && !large.Underway,
            "Lethal damage disables navigation and reports actual damage.");
        Check(large.RepairHull(25) == 25 && !large.Underway, "Disabled repair does not restart movement.");
        Check(large.ApplyHullDamage(-20) == 0, "Negative damage must not repair.");
        ShipState small = Ship("small", hull: 90); small.RepairHull(int.MaxValue);
        Check(small.Hull == 90, "Small ships cannot repair above their maximum.");

        foreach (Heading heading in Enum.GetValues<Heading>())
        {
            ShipState source = Ship("arc-source"); source.Heading = heading;
            double radians = ShipActionRules.HeadingDegrees(heading) * Math.PI / 180;
            ShipState front = Ship("front", (float)Math.Cos(radians) * 5, (float)Math.Sin(radians) * 5);
            ShipState side = Ship("side", (float)-Math.Sin(radians) * 5, (float)Math.Cos(radians) * 5);
            Check(ShipActionRules.InFiringArc(source, front, new() { Arc = WeaponArc.Forward }), $"Forward arc {heading}.");
            Check(!ShipActionRules.InFiringArc(source, front, new() { Arc = WeaponArc.Broadside }), $"Broadside excludes bow {heading}.");
            Check(ShipActionRules.InFiringArc(source, side, new() { Arc = WeaponArc.Broadside }), $"Broadside {heading}.");
        }

        ShipState a = Ship("A", 10, 10), b = Ship("B", 10, 15), c = Ship("C", 15, 10), d = Ship("D", 15, 15);
        ShipEncounterService encounters = new();
        int started = 0, ended = 0;
        encounters.EncounterStarted += _ => started++;
        encounters.EncounterEnded += _ => ended++;
        encounters.CheckEncounters([a, b, c, d]);
        Check(started == 6 && encounters.GetNearbyShips(a).Count() == 3, "Four ships produce six independent pairs.");
        encounters.CheckEncounters([a, b, c, d]);
        Check(started == 6 && ended == 0, "Reconciliation is idempotent.");
        b.IsStored = true;
        Check(!encounters.IsInEncounter(a, b) && encounters.GetActiveEncounters().Count == 3,
            "Storage invalidates pair and global active queries before reconciliation.");
        encounters.CheckEncounters([a, b, c, d]);
        Check(ended == 3 && encounters.IsInEncounter(c, d), "Removing B preserves unrelated pairs.");
        b.IsStored = false;
        encounters.CheckEncounters([a, b, c, d]);
        Check(started == 9, "Retrieval restores independent encounters.");
        encounters.CheckEncounters([a, c, d]);
        Check(!encounters.GetNearbyShips(a).Contains(b), "Removed ships leave no stale adjacency.");

        ShipTargetSelection combat = new(), sailing = new();
        combat.Refresh("p", a, [b, c]); sailing.Refresh("p", a, [b, c]);
        Check(combat.Select("p", a, 0, [b, c]) && sailing.Select("p", a, 1, [b, c]), "Explicit row selection.");
        Check(combat.GetSelected("p", a, [b, c]) == b && sailing.GetSelected("p", a, [b, c]) == c,
            "Combat and Sailing selections are independent.");
        Check(!combat.Select("p", a, 0, [c, d]), "A removed displayed row cannot select a different ship.");
        Check(combat.GetSelected("p", a, [c, d]) == null, "Invalid selection clears without fallback.");
        sailing.Refresh("other", a, [b, c]); sailing.Select("other", a, 0, [b, c]);
        Check(sailing.GetSelected("p", a, [b, c]) == c && sailing.GetSelected("other", a, [b, c]) == b,
            "Players sharing a ship have independent selection.");
        Check(sailing.GetSelected("p", d, [b, c]) == null, "Ship changes invalidate old selection.");

        combat.Refresh("p", a, [c, d]);
        Check(!combat.Select("p", a, 0, [c, d]), "Delayed row index remains bound to departed B after refresh.");
        Check(combat.Select("p", a, 1, [c, d]) && combat.GetSelected("p", a, [c, d]) == c,
            "Surviving row identities remain stable across list refresh.");

        DateTime now = DateTime.UtcNow;
        BoardingRequestBook book = new();
        ShipBoardingRequest request = new() { RequestingShip = a, TargetShip = b, RequestingPlayerName = "p",
            TargetPlayerName = "q", RequestedAt = now };
        Check(book.Add(request, now), "Boarding consent request creation.");
        Check(!book.Add(request, now), "Duplicate pending request rejected.");
        book.Cleanup(now.AddSeconds(60));
        Check(book.Requests.Count == 0 && book.Add(request, now.AddSeconds(60)), "Expired requests free future attempts.");
        book.CancelPlayer("q"); Check(book.Requests.Count == 0, "Target departure cancels consent.");

        int uses = 1;
        Check(SpellCastTransaction.Execute(() => uses--, () => uses++, () => true) && uses == 0,
            "Successful cast consumes one resource.");
        uses = 1;
        Check(!SpellCastTransaction.Execute(() => uses--, () => uses++, () => false) && uses == 1,
            "Rejected effects refund reservation.");

        uses = 1;
        bool castThrew = false;
        try { SpellCastTransaction.Execute(() => uses--, () => uses++, () => throw new InvalidOperationException("partial effect")); }
        catch (InvalidOperationException) { castThrew = true; }
        Check(castThrew && uses == 0, "A possibly applied spell is not refunded after an exception.");
        int firstPosition = 0, secondPosition = 0, membershipCommits = 0, transferErrors = 0;
        static Task RunMove(Action move) { move(); return Task.CompletedTask; }
        Check(!await ShipTransferTransaction.ExecuteAsync([
            (() => RunMove(() => firstPosition = 1), () => RunMove(() => firstPosition = 0), () => { }),
            (() => RunMove(() => { secondPosition = 1; throw new InvalidOperationException("second teleport failed"); }),
             () => RunMove(() => secondPosition = 0), () => { })],
            () => membershipCommits++, _ => transferErrors++), "Failed group transfer reports failure.");
        Check(firstPosition == 0 && secondPosition == 0 && membershipCommits == 0 && transferErrors == 1,
            "Group failure restores every native position before any membership is published.");
        Check(await ShipTransferTransaction.ExecuteAsync([
            (() => RunMove(() => firstPosition = 1), () => RunMove(() => firstPosition = 0), () => { }),
            (() => RunMove(() => secondPosition = 1), () => RunMove(() => secondPosition = 0), () => { })],
            () => { Check(firstPosition == 1 && secondPosition == 1, "All teleports precede membership commit."); membershipCommits++; }),
            "Successful group transfer commits exactly once.");
        Check(membershipCommits == 1, "Group membership changes are committed once.");
        List<int> rollbackOrder = new();
        Check(!await ShipTransferTransaction.ExecuteAsync([
            (() => RunMove(() => firstPosition = 2), () => RunMove(() => { rollbackOrder.Add(1); firstPosition = 1; }), () => { }),
            (() => RunMove(() => secondPosition = 2),
             () => RunMove(() => { rollbackOrder.Add(2); secondPosition = 1; throw new InvalidOperationException("rollback failure"); }), () => { })],
            () => throw new InvalidOperationException("commit failed"), _ => transferErrors++),
            "Transfer commit failures initiate rollback.");
        Check(rollbackOrder.SequenceEqual(new[] { 2, 1 }) && firstPosition == 1,
            "One native rollback failure does not prevent the remaining rollbacks.");
        book.Add(request, now); book.Cleanup(now, _ => false);
        Check(book.Requests.Count == 0, "Invalid boarding membership cancels stale consent.");

        SailingAreaService areas = new(); ShipObstacleService obstacles = new();
        ShipNavigationService navigation = new(obstacles, areas);
        ShipMovementService movement = new(areas, obstacles);
        ShipState routeShip = Ship("route", 20, 20);
        ShipNavigationRoute route = new() { ShipName = routeShip.ShipName, Waypoints =
            [new() { AreaResRef = "ocean_01", X = 20, Y = 20 }, new() { AreaResRef = "ocean_01", X = 30, Y = 20 }] };
        navigation.SetRoute(routeShip, route);
        Check(navigation.IsCurrentWaypointReached(routeShip) && !navigation.AdvanceWaypoint(routeShip) &&
            navigation.IsNavigating(routeShip) && routeShip.DestinationX == 30, "Intermediate waypoint keeps navigation active.");
        routeShip.X = 30;
        Check(navigation.AdvanceWaypoint(routeShip), "Final nonlooping waypoint completes.");
        navigation.CancelNavigation(routeShip);
        Check(!routeShip.IsNavigating && navigation.GetRoute(routeShip) == null, "STOP clears route and destination.");
        route.Loop = true; route.CurrentWaypointIndex = 1;
        navigation.SetRoute(routeShip, route);
        Check(!navigation.AdvanceWaypoint(routeShip) && route.CurrentWaypointIndex == 0, "Merchant routes wrap.");
        routeShip.Heading = Heading.NorthEast; routeShip.X = routeShip.Y = 20;
        Check(movement.TryMove(routeShip, 1) && routeShip.X == 21 && routeShip.Y == 21, "Existing diagonal policy is preserved.");
        routeShip.IsStored = true;
        Check(!movement.TryMove(routeShip, 1) && !navigation.IsNavigating(routeShip), "Stored ships cannot navigate.");
        bool rejected = false;
        try { obstacles.RegisterObstacle(new() { Name = "invalid", AreaResRef = "ocean_01", MinX = 10, MaxX = 5 }); }
        catch (ArgumentException) { rejected = true; }
        Check(rejected, "Invalid obstacle bounds are rejected.");
        ShipState avoid = Ship("avoid", 89, 20);
        navigation.SetDestination(avoid, "ocean_01", 130, 20, 0);
        Heading detour = navigation.GetNavigationHeading(avoid);
        Check(!navigation.IsHeadingBlocked(avoid, detour, 1), "Avoidance chooses a clear candidate.");

        ShipState fractional = Ship("fractional", 20, 20); fractional.Heading = Heading.West;
        navigation.SetDestination(fractional, "ocean_01", 20.8f, 20.8f, 0);
        Check(!navigation.IsDestinationReached(fractional) && navigation.GetDesiredHeading(fractional) == Heading.NorthEast,
            "Small diagonal destination offsets cannot leave navigation following the previous heading.");
        fractional.Heading = navigation.GetDesiredHeading(fractional); movement.TryMove(fractional, 1);
        Check(navigation.IsDestinationReached(fractional), "Fractional diagonal destinations reach the arrival radius.");
        fractional.X = fractional.Y = 20;
        fractional.Heading = navigation.GetDesiredHeading(fractional);
        float shortened = navigation.GetMovementDistance(fractional, fractional.Heading, 1.5f);
        Check(MathF.Abs(shortened - 0.8f) < 0.001f && movement.TryMove(fractional, shortened) && navigation.IsDestinationReached(fractional),
            "Gust-assisted navigation shortens its last step instead of oscillating across a fractional target.");
        OceanContactService contactBook = new(); contactBook.RegisterContact(new() { Id = "registered", Name = "Registered", Type = EncounterType.Pirate, AreaResRef = "ocean_01" });
        OceanContact[] contactSnapshot = contactBook.GetContacts("ocean_01").ToArray();
        contactBook.RemoveContact(contactSnapshot[0]);
        Check(contactSnapshot.Length == 1 && !contactBook.GetContacts("ocean_01").Any(), "Contact removal cannot invalidate an existing enumeration snapshot.");
        ShipDefinition abstractPirate = new("abstract", "brig", "", "", "", "", "", "ocean_01", 10, 10, 0, Heading.East, ShipType.Pirate, 100);
        abstractPirate.Validate();
        bool missingPlayerMappings = false;
        try { (abstractPirate with { ShipType = ShipType.Player }).Validate(); } catch (ArgumentException) { missingPlayerMappings = true; }
        Check(missingPlayerMappings, "Abstract NPC definitions are valid while player ships require physical mappings.");
        ShipState oldRouteOwner = Ship("replacement-route"), newRouteOwner = Ship("replacement-route");
        navigation.SetRoute(oldRouteOwner, new() { ShipName = oldRouteOwner.ShipName, Waypoints = [new() { AreaResRef = "ocean_01", X = 10, Y = 10 }] });
        Check(navigation.GetRoute(newRouteOwner) == null, "A replacement instance does not inherit a stale runtime route.");
        navigation.PruneRemovedShips([newRouteOwner]);
        Check(navigation.GetRoute(oldRouteOwner) == null, "Fleet pruning removes obsolete navigation references.");
        ShipEncounterService isolatedEvents = new(); int notified = 0;
        isolatedEvents.EncounterStarted += _ => throw new InvalidOperationException("bad subscriber");
        isolatedEvents.EncounterStarted += _ => notified++;
        isolatedEvents.CheckEncounters([a, b, c]);
        Check(notified == 3 && isolatedEvents.GetActiveEncounters().Count == 3,
            "A failing encounter subscriber cannot abort other subscribers or ship pairs.");
        OceanContact pirateContact = new() { Id = "pirate-contact", Name = "Pirate Cutter", Type = EncounterType.Pirate,
            AreaResRef = "ocean_01", X = 20, Y = 30, Z = 2 };
        ShipState converted = OceanContactShipFactory.Create(pirateContact)!;
        Check(converted.ShipType == ShipType.Pirate && converted.ShipId == pirateContact.Id && converted.Z == 2 &&
            converted.Hull == 100 && converted.WeaponResRef == "ship_cannon", "Pirate conversion preserves stable identity, position and configured weapon defaults.");
        Check(OceanContactShipFactory.Create(new() { Id = "merchant-contact", Name = "Merchant Cog", Type = EncounterType.Merchant,
            AreaResRef = "ocean_01" })?.ShipType == ShipType.Merchant, "Merchant contact conversion preserves ship type.");
        Check(OceanContactShipFactory.Create(new() { Id = "bad", Type = EncounterType.Pirate }) == null,
            "Unconfigured ship contacts cannot become active ships.");

        ShipRegistryService registry = new(); registry.Register(a); registry.Register(b);
        Check(registry.IsCurrent(a) && registry.GetShip(a.ShipId) == a, "Canonical identity lookup.");
        ShipState stale = Ship("A");
        Check(!registry.IsCurrent(stale), "Same ID on a stale instance does not confer current identity.");
        registry.Remove(a); Check(!registry.IsCurrent(a), "Registry removal invalidates references.");
        ShipRegistryService combatRegistry = new();
        ShipState pirate = Ship("pirate", 10, 10), playerShip = Ship("player", 10, 15, 140);
        pirate.ShipType = ShipType.Pirate;
        playerShip.HelmsmanPCKey = "captain";
        combatRegistry.Register(pirate); combatRegistry.Register(playerShip);
        ShipCombatService weapons = new(combatRegistry, encounters,
            new ShipStatePersistenceService(new SailingPersistenceContextFactory(new DatabaseContextFactory())), null!);
        var hit = await weapons.TryNpcAttack(pirate, playerShip);
        Check(hit.Success && hit.Damage == 10 && hit.RemainingHull == 130, "NPC attacks share hull-aware weapon rules.");
        Check((await weapons.TryNpcAttack(pirate, playerShip)).IsCooldown, "NPC cooldown cannot be bypassed.");
        Check((await weapons.TryNpcAttack(pirate, null)).NoTargetFound, "Missing explicit target never falls back.");
        playerShip.IsStored = true;
        Check((await weapons.TryNpcAttack(pirate, playerShip)).NoTargetFound, "Stored target cannot be attacked.");
        playerShip.IsStored = false;
        ShipState secondPirate = Ship("pirate-2", 10, 10); secondPirate.ShipType = ShipType.Pirate;
        combatRegistry.Register(secondPirate);
        playerShip.Hull = 5;
        var lethal = await weapons.TryNpcAttack(secondPirate, playerShip);
        Check(lethal.Success && lethal.TargetDisabled && lethal.Damage == 5, "Lethal hits remain successful and report actual damage.");
        ShipState failurePirate = Ship("failure-pirate", 10, 10); failurePirate.ShipType = ShipType.Pirate;
        combatRegistry.Register(failurePirate); playerShip.Hull = 140; playerShip.X = 40; playerShip.Y = 10;
        Check((await weapons.TryNpcAttack(failurePirate, playerShip)).IsOutOfRange && weapons.GetCooldownRemaining(failurePirate) == TimeSpan.Zero,
            "Range rejection does not consume cooldown.");
        playerShip.X = 15;
        Check((await weapons.TryNpcAttack(failurePirate, playerShip)).IsOutOfArc && weapons.GetCooldownRemaining(failurePirate) == TimeSpan.Zero,
            "Arc rejection does not consume cooldown.");
        playerShip.X = 10; playerShip.Y = 15; playerShip.Hull = 0;
        Check((await weapons.TryNpcAttack(failurePirate, playerShip)).TargetDisabled, "Disabled targets are distinguished from missing targets.");
        playerShip.Hull = 140; failurePirate.Hull = 0;
        Check((await weapons.TryNpcAttack(failurePirate, playerShip)).AttackerDisabled, "Disabled attackers cannot fire.");
        failurePirate.Hull = 100; failurePirate.ShipType = ShipType.Merchant;
        Check((await weapons.TryNpcAttack(failurePirate, playerShip)).IsNotAuthorized, "NPC authorization does not grant merchant weapon control.");
        failurePirate.ShipType = ShipType.Pirate; combatRegistry.Remove(failurePirate);
        Check((await weapons.TryNpcAttack(failurePirate, playerShip)).InvalidAttacker, "Removed attacker references cannot act.");
        ShipState positioned = Ship("positioning-pirate", 20, 20); positioned.ShipType = ShipType.Pirate; positioned.Heading = Heading.North;
        ShipState aiVictim = Ship("ai-victim", 20, 25); aiVictim.HelmsmanPCKey = "victim";
        combatRegistry.Register(positioned); combatRegistry.Register(aiVictim);
        PirateAiService positionAi = new(navigation, weapons);
        await positionAi.UpdatePirates([positioned, aiVictim]);
        Check(positionAi.GetState(positioned) == PirateCombatState.Position && ShipActionRules.InFiringArc(positioned, aiVictim, weapons.GetWeapon(positioned.WeaponResRef)),
            "Pirates rotate into a validated firing arc before attacking.");
        await positionAi.UpdatePirates([positioned, aiVictim]);
        Check(aiVictim.Hull == 90 && positionAi.GetState(positioned) == PirateCombatState.Attack, "Pirate positioning leads to a shared validated attack.");
        await positionAi.UpdatePirates([positioned, aiVictim]);
        Check(aiVictim.Hull == 90, "AI cannot bypass weapon cooldown on the next tick.");
        aiVictim.X = 200;
        await positionAi.UpdatePirates([positioned, aiVictim]);
        Check(positionAi.GetTarget(positioned) == null && !positioned.Underway, "Pirates lose targets outside detection range.");

        playerShip.Hull = 140; playerShip.X = 50; playerShip.Y = 10;
        PirateAiService ai = new(navigation, weapons);
        await ai.UpdatePirates([pirate, secondPirate, playerShip]);
        Check(ai.GetTarget(pirate) == playerShip && ai.GetTarget(secondPirate) == playerShip &&
            ai.GetState(pirate) == PirateCombatState.Pursue, "Multiple pirates pursue one eligible player ship.");
        float priorDestination = pirate.DestinationX;
        playerShip.X += 1;
        await ai.UpdatePirates([pirate, secondPirate, playerShip]);
        Check(pirate.DestinationX == priorDestination, "Small target movement does not reset pursuit every tick.");
        playerShip.X += 10;
        await ai.UpdatePirates([pirate, secondPirate, playerShip]);
        Check(pirate.DestinationX == playerShip.X, "Moving target eventually updates pursuit.");
        ShipState replacement = Ship("replacement", 20, 10); replacement.HelmsmanPCKey = "other captain";
        combatRegistry.Register(replacement);
        playerShip.IsStored = true;
        await ai.UpdatePirates([pirate, secondPirate, playerShip, replacement]);
        Check(ai.GetTarget(pirate) == replacement, "Lost targets are replaced by an explicit eligible AI target.");
        replacement.Hull = 0;
        await ai.UpdatePirates([pirate, secondPirate, playerShip, replacement]);
        Check(ai.GetTarget(pirate) == null && !pirate.IsNavigating, "Pirates disengage from disabled or stored targets.");
        pirate.Hull = 0;
        await ai.UpdatePirates([pirate, secondPirate]);
        Check(ai.GetState(pirate) == PirateCombatState.Disengage, "Disabled pirates stop combat.");
        foreach (var crossing in new[] { (Heading.East, "ocean_002", 640f, 80f, "West"),
            (Heading.West, "ocean_003", 0f, 80f, "East"), (Heading.North, "ocean_005", 80f, 640f, "South"),
            (Heading.South, "ocean_004", 80f, 0f, "North") })
        {
            ShipState crossingShip = Ship("crossing", crossing.Item3, crossing.Item4); crossingShip.Heading = crossing.Item1;
            SailingLocation? entry = areas.GetEntryLocation(crossing.Item2, crossing.Item5);
            Check(movement.TryMove(crossingShip, 1) && crossingShip.AreaResRef == crossing.Item2 &&
                crossingShip.X == entry!.X && crossingShip.Y == entry.Y, $"Destination-side entry for {crossing.Item1}.");
        }
        ShipState boundary = Ship("boundary", 639, 20); boundary.Heading = Heading.East;
        var planned = new ShipRoutePlannerService(areas).BuildRoute(boundary.ShipName, boundary.AreaResRef,
            boundary.X, boundary.Y, 0, "ocean_002", 30, 80, 0)!;
        navigation.SetRoute(boundary, planned);
        for (int tick = 0; tick < 800 && navigation.IsNavigating(boundary); tick++)
        {
            if (navigation.IsCurrentWaypointReached(boundary) && navigation.AdvanceWaypoint(boundary))
            { navigation.CancelNavigation(boundary); break; }
            boundary.Heading = navigation.GetNavigationHeading(boundary);
            movement.TryMove(boundary, 1);
        }
        Check(boundary.AreaResRef == "ocean_002" && !boundary.IsNavigating && MathF.Abs(boundary.X - 30) <= 1,
            "A planned multi-area route crosses the boundary and completes.");
        ShipState renamed = Ship("stable-id", 20, 20); renamed.ShipName = "Display name";
        navigation.SetRoute(renamed, new() { ShipName = renamed.ShipName, Waypoints = [new() { AreaResRef = "ocean_01", X = 25, Y = 20 }] });
        navigation.CancelNavigation(renamed);
        Check(navigation.GetRoute(renamed) == null, "Route cancellation uses stable identity, independent of display name.");
        ShipState merchant = Ship("merchant", 12, 10); merchant.ShipType = ShipType.Merchant;
        merchant.IsInPort = true; merchant.CurrentTradePortId = "southport";
        a.X = a.Y = 10;
        ShipTradeSession trading = new(a, merchant, "southport");
        Check(ShipTradeNuiService.IsEligible(trading), "Captured merchant market is eligible.");
        merchant.CurrentTradePortId = "driftwood";
        Check(!ShipTradeNuiService.IsEligible(trading) && trading.Merchant == merchant, "Market changes invalidate the session without selecting another merchant.");
        merchant.CurrentTradePortId = "southport"; merchant.IsStored = true;
        Check(!ShipActionRules.CanTrade(a, merchant, "southport"), "Stored merchants cannot transact.");
        merchant.IsStored = false; merchant.X = 100;
        Check(!ShipActionRules.CanTrade(a, merchant, "southport"), "Moving outside encounter range invalidates trade.");
        ShipState savedMerchant = Ship("durable-merchant", 120, 90, 140);
        savedMerchant.ShipType = ShipType.Merchant; savedMerchant.CargoCapacity = 120;
        savedMerchant.Cargo.Add(new() { ItemId = "timber", Quantity = 50 });
        savedMerchant.MerchantGold = 900; savedMerchant.IsInPort = true; savedMerchant.CurrentTradePortId = "driftwood";
        savedMerchant.PortDepartureTime = DateTime.UtcNow.AddMinutes(1);
        ShipNavigationRoute savedRoute = new() { ShipName = savedMerchant.ShipName, Loop = true, Waypoints =
            [new() { AreaResRef = "ocean_01", X = 120, Y = 90, PortId = "driftwood" },
             new() { AreaResRef = "ocean_01", X = 90, Y = 100, PortId = "southport" }] };
        navigation.SetRoute(savedMerchant, savedRoute);
        SavedShipState snapshot = ShipSnapshotCodec.Capture(savedMerchant, savedRoute);
        savedMerchant.Cargo[0].Quantity = 1; savedRoute.CurrentWaypointIndex = 1;
        ShipState restoredMerchant = Ship("durable-merchant");
        Check(ShipSnapshotCodec.TryRestore(restoredMerchant, snapshot, out var restoredRoute) &&
            restoredMerchant.Hull == 140 && restoredMerchant.MaximumHull == 140 && restoredMerchant.Cargo[0].Quantity == 50 &&
            restoredRoute!.CurrentWaypointIndex == 0 && restoredMerchant.IsInPort && restoredMerchant.MerchantGold == 900,
            "Coherent merchant snapshots preserve hull, cargo, gold, route, and an existing port stay without executing another trade.");
        ShipState stored = Ship("durable-stored", hull: 90); stored.IsStored = true; stored.StoredPortId = "southport";
        ShipState restoredStored = Ship("durable-stored");
        Check(ShipSnapshotCodec.TryRestore(restoredStored, ShipSnapshotCodec.Capture(stored), out _) &&
            restoredStored.IsStored && restoredStored.StoredPortId == "southport" && !restoredStored.IsActiveWorld,
            "Stored ship round trip restores port identity and world exclusion.");
        ShipState underway = Ship("underway"); underway.HelmsmanPCKey = "captain"; underway.Underway = underway.IsNavigating = true;
        underway.DestinationAreaResRef = "ocean_01";
        ShipState restoredPlayer = Ship("underway");
        Check(ShipSnapshotCodec.TryRestore(restoredPlayer, ShipSnapshotCodec.Capture(underway), out _) &&
            restoredPlayer.HelmsmanPCKey == null && !restoredPlayer.Underway && !restoredPlayer.IsNavigating,
            "Restart never restores transient helm ownership or unattended player navigation.");
        string serialized = snapshot.SnapshotJson!;
        Check(!serialized.Contains("Helmsman") && !serialized.Contains("Nui") && !serialized.Contains("Requesting"),
            "Native objects, window tokens, helmsmen, and boarding consent are excluded from persisted payloads.");
        SavedShipState corrupt = ShipSnapshotCodec.Capture(restoredMerchant); corrupt.SnapshotJson = "{invalid";
        float originalX = restoredMerchant.X;
        corrupt.X = 300;
        Check(!ShipSnapshotCodec.TryRestore(restoredMerchant, corrupt, out _) && restoredMerchant.X == originalX,
            "Corrupt snapshots are rejected before mutating live state.");
        Check(!ShipSnapshotCodec.TryRestore(Ship("wrong-identity"), snapshot, out _), "Restoration cannot replace another ship identity.");

        corrupt.SnapshotJson = "null";
        Check(!ShipSnapshotCodec.TryRestore(restoredMerchant, corrupt, out _), "JSON null is not treated as a valid legacy row.");
        restoredMerchant.IsInPort = false; restoredMerchant.CurrentTradePortId = null;
        ShipNavigationRoute completedRoute = new() { ShipName = restoredMerchant.ShipName, CurrentWaypointIndex = 1,
            Waypoints = [new() { AreaResRef = "ocean_01", X = 10, Y = 10 }] };
        Check(ShipSnapshotCodec.TryRestore(Ship("durable-merchant"), ShipSnapshotCodec.Capture(restoredMerchant, completedRoute), out var noCompletedRoute) && noCompletedRoute == null,
            "Completed routes are excluded from persistence instead of creating an unrestorable snapshot.");
        ShipState disabledSaved = Ship("disabled-saved"); disabledSaved.Hull = 0;
        ShipState disabledRestored = Ship("disabled-saved");
        Check(ShipSnapshotCodec.TryRestore(disabledRestored, ShipSnapshotCodec.Capture(disabledSaved), out _) && disabledRestored.IsDisabled && disabledRestored.IsActiveWorld,
            "Disabled ships survive restoration in the world without sinking or restarting movement.");

        CoalescingSaveQueue<string, SavedShipState> queue = new();
        TaskCompletionSource<bool> releaseSave = new(TaskCreationOptions.RunContinuationsAsynchronously);
        List<float> writes = new(); int simultaneous = 0, peak = 0;
        async Task<bool> Writer(SavedShipState data)
        {
            peak = Math.Max(peak, ++simultaneous); writes.Add(data.X);
            if (writes.Count == 1) await releaseSave.Task;
            simultaneous--; return true;
        }
        Task<bool> firstSave = queue.Enqueue("ship", new() { ShipName = "ship", AreaResRef = "ocean_01", X = 1 }, Writer);
        _ = queue.Enqueue("ship", new() { ShipName = "ship", AreaResRef = "ocean_01", X = 2 }, Writer);
        Task<bool> lastSave = queue.Enqueue("ship", new() { ShipName = "ship", AreaResRef = "ocean_01", X = 3 }, Writer);
        releaseSave.SetResult(true);
        Check(await firstSave && await lastSave && writes.SequenceEqual(new[] { 1f, 3f }) && peak == 1,
            "Overlapping saves serialize and coalesce, so older snapshots cannot overwrite newer state.");
        Check(!await queue.Enqueue("failure", new() { ShipName = "failed", AreaResRef = "ocean_01" }, _ => Task.FromResult(false)),
            "Failed saves are reported explicitly.");
        SailingPersistenceContextFactory db = SailingPersistenceContextFactory.ForOptions(
            new DbContextOptionsBuilder<SailingPersistenceDbContext>()
                .UseInMemoryDatabase("sailing-integration-" + Guid.NewGuid()).Options);
        ShipStatePersistenceService persistence = new(db, navigation, new() { Enabled = true, Backend = SailingPersistenceBackend.PostgreSql });
        Check(await persistence.TrySaveState(stored), "EF InMemory integration saves an isolated stored ship.");
        SavedShipState? loaded = await persistence.LoadState(stored.ShipId);
        Check(loaded != null && loaded.ShipId == stored.ShipId && loaded.SnapshotJson != null,
            "EF integration loads by stable ship identity.");
        stored.X = 25; await persistence.SaveState(stored); stored.X = 30; await persistence.SaveState(stored);
        Check((await persistence.LoadState(stored.ShipId))!.X == 30, "Repeated database saves retain latest state.");
        await using (SailingPersistenceDbContext legacyContext = db.CreateDbContext())
        {
            legacyContext.SavedShipStates.Add(new() { ShipName = stored.ShipName + "-old", AreaResRef = "ocean_01", X = 5, Hull = 90 });
            await legacyContext.SaveChangesAsync();
        }
        Check((await persistence.LoadState(stored.ShipId, stored.ShipName + "-old"))!.X == 30,
            "Stable-ID restoration takes precedence over a distinct legacy name row.");
        stored.X = 35; Check(await persistence.TrySaveState(stored), "Stable-ID saves remain valid with legacy rows present.");
        Check((await persistence.LoadState(stored.ShipId))!.X == 35, "Saving updates the current stable-ID row rather than the legacy-name row.");
        ShipState legacyShip = Ship("legacy-id"); legacyShip.ShipName = "Legacy vessel"; legacyShip.Hull = 60;
        await using (SailingPersistenceDbContext legacyContext = db.CreateDbContext())
        {
            legacyContext.SavedShipStates.Add(new() { ShipName = legacyShip.ShipName, AreaResRef = "ocean_01", Hull = 60 });
            await legacyContext.SaveChangesAsync();
        }
        SavedShipState? legacyLoaded = await persistence.LoadState(legacyShip.ShipId, legacyShip.ShipName);
        Check(legacyLoaded is { ShipId: null, Hull: 60 }, "Existing name-only database rows remain readable.");
        Check(await persistence.TrySaveState(legacyShip), "Saving a legacy ship promotes its stable identity.");
        Check((await persistence.LoadState(legacyShip.ShipId)) is { ShipId: "legacy-id", Hull: 60 },
            "Legacy promotion preserves hull and enables stable-ID reads.");
        await using (SailingPersistenceDbContext legacyContext = db.CreateDbContext())
            Check(await legacyContext.SavedShipStates.CountAsync(s => s.ShipName == legacyShip.ShipName) == 1,
                "Legacy promotion updates the existing row without violating unique ship names.");
        Check(!new ShipStatePersistenceService(db).IsEnabled, "Constructing persistence without injected options performs no I/O.");
        ChartDiscoveryService charts = new(); ShipState explored = Ship("explorer", 320, 320);
        Check(charts.RevealAroundShip("account-a", explored), "Actual aboard position reveals chart cells.");
        SavedChartDiscovery[] chartSnapshots = charts.Capture("account-a").ToArray();
        Check(chartSnapshots[0].Cells.Count(c => c == '1') == 25, "Chart discovery preserves the 16x16 grid and radius-two policy.");
        ChartDiscoveryService chartRestore = new(); chartRestore.Restore("account-a", chartSnapshots);
        Check(chartRestore.IsDiscovered("account-a", "ocean_01", 8, 7) &&
            !chartRestore.IsDiscovered("account-a", "ocean_01", 0, 0) && !chartRestore.IsDiscovered("account-b", "ocean_01", 8, 7),
            "Discovery restoration preserves fog and account separation.");
        chartRestore.Restore("account-a", [new() { AccountKey = "account-a", AreaResRef = "secret", Cells = "bad" }]);
        Check(!chartRestore.IsDiscovered("account-a", "secret", 0, 0) && !chartRestore.IsDiscovered("account-a", "ocean_01", -1, 0),
            "Corrupt discovery grids and invalid coordinates cannot reveal cells.");
        ShipState recentExploration = Ship("recent", 40, 40);
        chartRestore.RevealAroundShip("account-a", recentExploration);
        chartRestore.Restore("account-a", chartSnapshots);
        Check(chartRestore.IsDiscovered("account-a", "ocean_01", 1, 14) && chartRestore.IsDiscovered("account-a", "ocean_01", 8, 7),
            "Restoration merges discovery made during loading rather than erasing it.");

        ChartDiscoveryPersistenceService chartDb = new(db, new() { Enabled = true, Backend = SailingPersistenceBackend.PostgreSql });
        Check(await chartDb.Save(chartSnapshots[0]), "Isolated EF chart discovery save.");
        Check((await chartDb.Load("account-a")).Single().Cells == chartSnapshots[0].Cells, "Chart database round trip preserves exact fog bits.");
        Check(await chartDb.Save(new() { AccountKey = "account-a", AreaResRef = "OCEAN_01", Cells = chartSnapshots[0].Cells }),
            "Chart persistence accepts case-insensitive NWN area identities.");
        Check((await chartDb.Load("account-a")).Count == 1,
            "Area resref case changes update the existing discovery row rather than creating duplicates.");
        SailingMapView mapBefore = new("viewer", "ocean_01", 10, 10, Heading.East, "sloop", 1,
            [new("nearby", 12, 10, Heading.West, "cog")]);
        Check(mapBefore.Matches(new("viewer", "ocean_01", 10, 10, Heading.East, "sloop", 1,
            [new("nearby", 12, 10, Heading.West, "cog")])), "Unrelated fleet movement does not change the viewer's map snapshot.");
        Check(!mapBefore.Matches(mapBefore with { DiscoveryRevision = 2 }), "New chart discovery invalidates the map cache.");
        var serviceTypes = typeof(HelmService).Assembly.GetTypes().Where(t => t.GetCustomAttributes(false)
            .Any(a => a.GetType().Name == "ServiceBindingAttribute")).ToHashSet();
        HashSet<Type> visited = new(), visiting = new();
        void Visit(Type service)
        {
            if (visited.Contains(service)) return;
            Check(visiting.Add(service), $"Circular service dependency at {service.Name}.");
            foreach (Type dependency in service.GetConstructors().SelectMany(c => c.GetParameters())
                .Select(p => p.ParameterType).Where(serviceTypes.Contains)) Visit(dependency);
            visiting.Remove(service); visited.Add(service);
        }
        foreach (Type service in serviceTypes.Where(t => t.Namespace?.StartsWith(
            "AmiaReforged.Core.Services.Sailing", StringComparison.Ordinal) == true)) Visit(service);
        RepositoryIntegration.Verify(Check);
        await MaritimeChecks.Run(Check);
        CutterActivationChecks.Run(Check);
        await CutterRentalChecks.Run(Check);
        DockyardNuiChecks.Run(Check);
        DockyardLayoutRecoveryChecks.Run(Check);
        await PartyAndTradeChecks.Run(Check);
        NauticalChartChecks.Run(Check);
        NauticalChartRecoveryChecks.Run(Check);
        StartupChecks.Verify(Check);
        await CompletionChecks.Run(Check);
        await SeaSpriteStorageChecks.Run(Check);
        await DeferredTransferChecks.Run(Check);
        await ShipExitChecks.Run(Check);

        var timer = System.Diagnostics.Stopwatch.StartNew();
        ShipState[] scaleFleet = Enumerable.Range(0, 100).Select(n => Ship($"scale-{n}", n * 5, 50)).ToArray();
        encounters.CheckEncounters(scaleFleet);
        int scalePairs = encounters.GetActiveEncounters().Count;
        for (int query = 0; query < 1000; query++) _ = encounters.GetNearbyShips(scaleFleet[50]).Count();
        timer.Stop();
        Console.WriteLine($"100-ship reconciliation + 1,000 adjacency queries: {timer.ElapsedMilliseconds} ms; {scalePairs} pairs.");
        Console.WriteLine($"PASS: {_checks} regression assertions.");
    }
}

#endif
