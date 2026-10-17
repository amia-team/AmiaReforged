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
        SteeringChecks.Run(Check);
        await HostileBoardingChecks.Run(Check);
        ChartCourseChecks.Run(Check);
        await TacticalChartChecks.Run(Check);
        ChartZoomChecks.Run(Check);
        await OverlayChecks.Run(Check);
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

static class ChartCourseChecks
{
    public static void Run(Action<bool, string> check)
    {
        bool Near(float a, float b) => MathF.Abs(a - b) < .001f;
        ShipState Ship(string id, float x = 60, float y = 25) => new()
        { ShipId = id, ShipName = id, DeckAreaResRef = "deck", AreaResRef = "ocean_01", X = x, Y = y, HelmsmanPCKey = id };
        foreach (float size in new[] { 416f, 608f, 640f, 800f })
        foreach (var point in new[] { new ChartPoint(0, 0), new(0, 640), new(640, 0), new(640, 640),
                     new(320, 320), new(0, 117.25f), new(640, 401.5f), new(219.3f, 0), new(15.75f, 640) })
        {
            var pixel = NauticalChartGeometry.Project(point.X, point.Y, size);
            check(NauticalChartGeometry.TryUnproject(pixel.X, pixel.Y, size, out var restored) &&
                Near(restored.X, point.X) && Near(restored.Y, point.Y), "Actual chart transform round trips corners, center and edges at every supported scale.");
            check(Near(pixel.X, point.X / 640 * size) && Near(pixel.Y, (640 - point.Y) / 640 * size),
                "Chart coordinates are north-up with no hidden visual offset.");
            var rect = NauticalChartGeometry.CenteredMarker(point.X, point.Y, 18, 18, size);
            check(Near(rect.X + 9, pixel.X) && Near(rect.Y + 9, pixel.Y), "Click and destination marker share the identical anchor at chart boundaries.");
        }
        foreach (var pixel in new[] { new ChartPoint(-.01f, 2), new(2, -.01f), new(640.01f, 2), new(2, 640.01f), new(float.NaN, 0), new(0, float.PositiveInfinity) })
            check(!NauticalChartGeometry.TryUnproject(pixel.X, pixel.Y, 640, out _), "Outside/non-finite chart clicks are rejected.");
        check(!NauticalChartGeometry.TryUnproject(0, 0, 0, out _) && !NauticalChartGeometry.TryUnproject(0, 0, float.NaN, out _),
            "Invalid chart size cannot produce world coordinates.");
        var payload = Anvil.API.JsonUtility.FromJson<ChartMousePayload>("{\"mouse_btn\":0,\"mouse_pos\":{\"x\":73.5,\"y\":408.25}}");
        check(payload is { Button: 0, Position: { X: 73.5f, Y: 408.25f } } &&
            Anvil.API.JsonUtility.FromJson<ChartMousePayload>("{}") is { Button: null, Position: null },
            "Installed Anvil deserializer reads exact mouse coordinates and does not invent missing coordinates/buttons.");

        var areas = new SailingAreaService(); var obstacles = new ShipObstacleService();
        var planner = new ShipRoutePlannerService(areas); var nav = new ShipNavigationService(obstacles, areas);
        var ship = Ship("captain"); var session = new ChartCourseSession("captain", ship, planner, nav, obstacles);
        bool Select(ChartCourseSession s, ShipState vessel, float x, float y, bool present = true)
        { var point = NauticalChartGeometry.Project(x, y, 640); return s.Select(vessel, present, point.X, point.Y, 640, out _); }
        var before = ShipSnapshotCodec.Capture(ship, nav.GetRoute(ship));
        using var discovery = new ChartDiscoveryService(); discovery.RevealAroundShip("captain", ship);
        string fog = discovery.CaptureCells("captain", "ocean_01");
        check(Select(session, ship, 150, 25) && session.Preview?.Route.Waypoints.Count >= 3,
            "A valid ocean click previews the actual planner's obstacle-avoiding route around configured land.");
        var first = session.Preview;
        check(!ship.IsNavigating && !ship.Underway && nav.GetRoute(ship) == null &&
            ShipSnapshotCodec.Capture(ship).SnapshotJson == before.SnapshotJson,
            "Preview does not install a route, move the ship or change its persistence snapshot.");
        check(Select(session, ship, 180, 40) && !ReferenceEquals(session.Preview, first) && session.Preview!.Destination == new ChartPoint(180, 40),
            "A second click replaces the previous proposal.");
        check(!Select(session, ship, 100, 25) && session.Preview == null, "Configured land is rejected and cannot leave an older course ready to confirm.");
        check(!Select(session, ship, 150, 25, false), "Missing physical presence/command authorization rejects passengers.");
        ship.HelmsmanPCKey = "other";
        check(!Select(session, ship, 150, 25), "A passenger cannot plot a course using another helmsman's ship.");
        ship.HelmsmanPCKey = "captain";
        foreach (string state in new[] { "stored", "disabled", "initializing", "docking", "port", "busy", "area" })
        {
            ship.IsStored = state == "stored"; ship.Hull = state == "disabled" ? 0 : 100;
            ship.IsInitializing = state == "initializing"; ship.IsDocking = state == "docking";
            ship.IsInPort = state == "port"; ship.IsEconomyBusy = state == "busy";
            ship.AreaResRef = state == "area" ? "ocean_002" : "ocean_01";
            check(!Select(session, ship, 150, 25), $"Chart selection rejects {state} ship state.");
        }
        ship.IsStored = ship.IsInitializing = ship.IsDocking = ship.IsInPort = ship.IsEconomyBusy = false;
        ship.Hull = 100; ship.AreaResRef = "ocean_01";
        check(!session.Select(Ship("captain"), true, 150, 615, 640, out _), "A replacement object with the same ship ID cannot reuse a stale chart session.");
        check(Select(session, ship, 150, 25), "Selection recovers after invalid context.");
        check(!session.Select(ship, true, -1, 100, 640, out _) && session.Preview == null,
            "The selection service itself rejects an outside-chart click and clears its proposal.");
        Select(session, ship, 150, 25);
        ship.HelmsmanPCKey = "other";
        check(!session.Confirm(ship, true, out _) && session.Preview == null && !ship.IsNavigating, "Confirm rechecks helm ownership and clears unauthorized proposals.");
        ship.HelmsmanPCKey = "captain";
        Select(session, ship, 150, 25);
        ship.X = 65;
        check(session.Confirm(ship, true, out _) && ship.IsNavigating && ship.Underway && session.Preview == null &&
            nav.GetRoute(ship) is { IsChartCourse: true } && nav.GetRoute(ship)!.Waypoints[^1].X == 150,
            "Confirmation replans from the current position and installs one active route in the actual navigation service.");
        session.Close();
        var reopened = new ChartCourseSession("captain", ship, planner, nav, obstacles);
        check(nav.IsNavigating(ship) && !Select(session, ship, 200, 50) && reopened.Preview == null,
            "Closing/reopening chart sessions preserves autopilot and rejects stale window selections.");
        var active = nav.GetRoute(ship);
        Select(reopened, ship, 200, 40); reopened.CancelPreview();
        check(ReferenceEquals(nav.GetRoute(ship), active) && ship.Underway, "Cancelling a preview leaves the active route untouched.");
        check(discovery.CaptureCells("captain", "ocean_01") == fog, "All course actions preserve the exact 16 by 16 fog mask.");
        var frame = NauticalChartProjection.Capture(ship, nav, [], [], [], [], [], (_, _) => false);
        check(frame.ChartCourse && frame.Mode == "Mode: AUTOPILOT", "Reopened presentation reports the actual chart autopilot mode.");
        var previewFrame = frame with { Preview = [new("ocean_01", 200, 40, true)] };
        var renderer = new NauticalChartRenderer(); var render = renderer.Prepare(previewFrame, 640);
        var destinationRect = (ChartRect)render.Changes.Single(c => c.Key == "chart_p0_rect").Value;
        var end = (ChartPoint)render.Changes.Single(c => c.Key == "chart_p0_b").Value;
        check(Near(destinationRect.X + 9, end.X) && Near(destinationRect.Y + 9, end.Y) && end == NauticalChartGeometry.Project(200, 40, 640),
            "Preview marker and route endpoint align exactly with the original click.");
        check(render.Layout!.Children.Single() is Anvil.API.NuiImage { Id: SailingNuiService.ChartSurfaceId, Width: 640, Height: 640, Margin: 0, Padding: 0 } &&
            render.Layout.DrawList!.All(d => d.Order == Anvil.API.NuiDrawListItemOrder.After),
            "One image receives chart input without obscuring the drawing overlays.");
        check(render.Changes.Count(c => c.Key.StartsWith("chart_f") && c.Value is bool) == 256 && previewFrame.Discovery == frame.Discovery &&
            previewFrame.Features.Length == 0 && previewFrame.Markers.All(m => m.Kind == ChartContactKind.OwnShip),
            "Preview rendering retains every fog mask and cannot reveal hidden ships, ports or landmarks.");
        check(renderer.Prepare(previewFrame, 640).Changes.Count == 0 &&
            renderer.Prepare(frame, 640).Layout != null && ReferenceEquals(nav.GetRoute(ship), active),
            "Unchanged preview is cached; removing it rebuilds only presentation and preserves active-route identity.");
        check(render.Changes.Any(c => c.Key.StartsWith("chart_w") && c.Key.EndsWith("_final") && c.Value is true),
            "The helmsman's active destination remains visible as an annotation over undiscovered water.");
        var saved = ShipSnapshotCodec.Capture(ship, active); var restoredShip = Ship("captain");
        check(System.Text.Json.JsonSerializer.Deserialize<ShipSnapshotPayload>(saved.SnapshotJson!)?.Route?.IsChartCourse == true &&
            ShipSnapshotCodec.TryRestore(restoredShip, saved, out var restoredRoute) && restoredRoute == null && !restoredShip.Underway,
            "Route JSON preserves chart metadata while the existing player-restart policy still clears autopilot and ownership.");
        check(!System.Text.Json.JsonSerializer.Serialize(new ShipNavigationRoute { ShipName = "legacy" }).Contains("IsChartCourse"),
            "Existing non-chart route JSON does not acquire an unnecessary default field.");
        nav.CancelNavigation(ship);
        Select(reopened, ship, 250, 100);
        obstacles.RegisterObstacle(new() { Name = "changed destination", AreaResRef = "ocean_01", MinX = 249, MaxX = 251, MinY = 99, MaxY = 101 });
        check(!reopened.Confirm(ship, true, out _) && nav.GetRoute(ship) == null && !ship.Underway, "New destination obstacles are revalidated on confirm.");
        var blockedObstacles = new ShipObstacleService();
        blockedObstacles.RegisterObstacle(new() { Name = "barrier", AreaResRef = "ocean_01", MinX = 200, MaxX = 210, MinY = 0, MaxY = 640 });
        check(planner.BuildChartRoute(ship, new(250, 100), blockedObstacles) == null, "A full-height configured land barrier rejects unreachable destinations without retry loops.");
        check(!blockedObstacles.IsSegmentClear("ocean_01", 190, 100, 220, 100) &&
            blockedObstacles.IsSegmentClear("ocean_01", 190, 100, 190, 200), "Swept collision detects land crossed between two clear endpoints.");
        ProductionTicks(check);
        Console.WriteLine("PASS: chart course transforms, selection, preview, confirmation, safety and production navigation ticks.");
    }

    static void ProductionTicks(Action<bool, string> check)
    {
        var services = new Dictionary<Type, object>
        {
            [typeof(Anvil.Services.EventService)] = new Anvil.Services.EventService([]),
            [typeof(DatabaseContextFactory)] = new DatabaseContextFactory(),
            [typeof(ShipPersistenceOptions)] = new ShipPersistenceOptions { Backend = SailingPersistenceBackend.Disabled }
        };
        var resolve = StartupChecks.CreateResolver(services);
        var helm = (HelmService)resolve(typeof(HelmService)); var nav = (ShipNavigationService)resolve(typeof(ShipNavigationService));
        var registry = (ShipRegistryService)resolve(typeof(ShipRegistryService)); var planner = (ShipRoutePlannerService)resolve(typeof(ShipRoutePlannerService));
        var obstacles = (ShipObstacleService)resolve(typeof(ShipObstacleService));
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var tick = typeof(HelmService).GetMethod("AdvanceShipMovement", flags)!;
        var command = typeof(HelmService).GetMethod("HandleNavigationCommand", flags)!;
        ShipState Vessel(string id, float x, float y) => new()
        { ShipId = id, ShipName = id, DeckAreaResRef = "deck", AreaResRef = "ocean_01", X = x, Y = y, HelmsmanPCKey = id };
        void Course(ShipState vessel, ChartPoint target)
        {
            var session = new ChartCourseSession(vessel.HelmsmanPCKey!, vessel, planner, nav, obstacles);
            var p = NauticalChartGeometry.Project(target.X, target.Y, 640);
            check(session.Select(vessel, true, p.X, p.Y, 640, out _) && session.Confirm(vessel, true, out _), "Production service graph engages a chart course.");
        }
        try
        {
            var a = Vessel("chart-A", 60.25f, 25.5f); var b = Vessel("chart-B", 150.25f, 28.5f);
            registry.Register(a); registry.Register(b);
            Course(a, new(150.25f, 25.5f)); Course(b, new(60.25f, 28.5f));
            int progressed = 0, turns = 0;
            for (int i = 0; i < 400 && (a.IsNavigating || b.IsNavigating); i++)
            foreach (var vessel in new[] { a, b })
            {
                if (!vessel.IsNavigating) continue;
                float x = vessel.X, y = vessel.Y; var heading = vessel.Heading;
                var route = nav.GetRoute(vessel)!; int index = route.CurrentWaypointIndex;
                tick.Invoke(helm, [vessel]);
                check(obstacles.IsSegmentClear("ocean_01", x, y, vessel.X, vessel.Y) && vessel.AreaResRef == "ocean_01",
                    "Actual fleet tick never enters or crosses configured land and stays inside the selected ocean.");
                if (route.CurrentWaypointIndex > index) progressed++;
                if (vessel.IsNavigating && heading != vessel.Heading)
                {
                    turns++;
                    check(vessel.Underway && (vessel.X != x || vessel.Y != y), "Automatic heading changes preserve continuous propulsion on the same production tick.");
                }
            }
            check(progressed >= 4 && turns >= 4 && !a.IsNavigating && !b.IsNavigating && !a.Underway && !b.Underway &&
                nav.GetRoute(a) == null && nav.GetRoute(b) == null && MathF.Abs(a.X - 150.25f) <= 1 && MathF.Abs(b.X - 60.25f) <= 1,
                "Two independent ships advance intermediate waypoints, turn around land and stop/clear routes upon arrival.");
            Course(a, new(170, 50)); var otherRoute = nav.GetRoute(a);
            Course(b, new(55, 50));
            command.Invoke(helm, [b, "left_button"]);
            check(b.Underway && !b.IsNavigating && nav.GetRoute(b) == null && ReferenceEquals(nav.GetRoute(a), otherRoute),
                "Manual override cancels only its vessel's route while preserving propulsion.");
            command.Invoke(helm, [a, "stop_button"]);
            float stoppedX = a.X, stoppedY = a.Y; tick.Invoke(helm, [a]);
            check(!a.Underway && !a.IsNavigating && nav.GetRoute(a) == null && a.X == stoppedX && a.Y == stoppedY,
                "Production STOP cancels the chart route and prevents future tick movement.");
            Course(a, new(180, 50));
            var waypoint = nav.GetCurrentWaypoint(a)!;
            obstacles.RegisterObstacle(new() { Name = "blocked leg", AreaResRef = "ocean_01", MinX = waypoint.X - .01f, MaxX = waypoint.X + .01f,
                MinY = waypoint.Y - .01f, MaxY = waypoint.Y + .01f });
            tick.Invoke(helm, [a]);
            check(!a.IsNavigating && !a.Underway && a.X == stoppedX && a.Y == stoppedY && nav.GetCourseFailure(a) != null,
                "A newly blocked active leg stops safely on the production tick with visible feedback, without recalculation or teleportation.");
            var loop = (ShipNavigationLoopService)resolve(typeof(ShipNavigationLoopService));
            check(loop.Completion.IsCompleted, "Chart sessions do not start an additional navigation tick loop.");
        }
        finally { foreach (IDisposable service in services.Values.OfType<IDisposable>().Reverse()) service.Dispose(); }
    }
}

// Invoke the production NUI command dispatcher and the per-ship fleet tick;
// native window events and placeable transforms still require a live NWN server.
static class SteeringChecks
{
    public static void Run(Action<bool, string> check)
    {
        Dictionary<Type, object> services = new()
        {
            [typeof(Anvil.Services.EventService)] = new Anvil.Services.EventService([]),
            [typeof(DatabaseContextFactory)] = new DatabaseContextFactory(),
            [typeof(ShipPersistenceOptions)] = new ShipPersistenceOptions { Backend = SailingPersistenceBackend.Disabled }
        };
        var resolve = StartupChecks.CreateResolver(services);
        var helm = (HelmService)resolve(typeof(HelmService));
        var registry = (ShipRegistryService)resolve(typeof(ShipRegistryService));
        var navigation = (ShipNavigationService)resolve(typeof(ShipNavigationService));
        var effects = (ShipSpellEffectStateService)resolve(typeof(ShipSpellEffectStateService));
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var command = typeof(HelmService).GetMethod("HandleNavigationCommand", flags)!;
        var tick = typeof(HelmService).GetMethod("AdvanceShipMovement", flags)!;
        void Command(ShipState ship, string button) =>
            check((bool)command.Invoke(helm, [ship, button])!, $"Production dispatcher handles {button}.");
        void Tick(ShipState ship) => tick.Invoke(helm, [ship]);
        ShipState Ship(string id) => new()
        {
            ShipId = id, ShipName = id, DeckAreaResRef = "deck", AreaResRef = "ocean_01",
            X = 40, Y = 40, HelmsmanPCKey = "captain", Heading = Heading.East
        };
        void Moved(ShipState ship, float x, float y, float distance, string message)
        {
            var delta = ship.Heading switch
            {
                Heading.North => (0, 1), Heading.NorthEast => (1, 1), Heading.East => (1, 0),
                Heading.SouthEast => (1, -1), Heading.South => (0, -1), Heading.SouthWest => (-1, -1),
                Heading.West => (-1, 0), Heading.NorthWest => (-1, 1), _ => throw new Exception("Invalid heading")
            };
            check(MathF.Abs(ship.X - x - delta.Item1 * distance) < 0.0001f &&
                MathF.Abs(ship.Y - y - delta.Item2 * distance) < 0.0001f, message);
        }
        ShipNavigationRoute Route(ShipState ship, bool loop = false) => new()
        {
            ShipName = ship.ShipName, Loop = loop,
            Waypoints = [new() { AreaResRef = "ocean_01", X = 50, Y = 40 },
                new() { AreaResRef = "ocean_01", X = 50, Y = 50 }]
        };
        try
        {
            foreach (string turn in new[] { "left_button", "right_button" })
            foreach (float multiplier in new[] { 0.5f, 1f, 1.5f })
            {
                ShipState ship = Ship($"steer-{turn}-{multiplier}"); registry.Register(ship);
                effects.ApplySpeedBoost(ship, "steering-test", (multiplier - 1) * 100, TimeSpan.FromMinutes(5));
                Command(ship, "ahead_button");
                Tick(ship);
                check(ship.X == 40 + multiplier && ship.Y == 40 && ship.Underway,
                    "AHEAD starts forward movement on the next navigation tick.");
                // Sixteen consecutive turns cover every heading, wrapping twice.
                for (int i = 0; i < 16; i++)
                {
                    float x = ship.X, y = ship.Y;
                    Command(ship, turn);
                    Heading expected = (Heading)((2 + (turn == "left_button" ? -1 : 1) * (i + 1) % 8 + 8) % 8);
                    check(ship.Heading == expected, "Steering retains the established 45-degree turn increment.");
                    check(ship.Underway && ship.HelmsmanPCKey == "captain" && effects.GetMovementMultiplier(ship) == multiplier,
                        "Steering preserves propulsion, helm ownership and speed modifiers.");
                    check(ship.X == x && ship.Y == y, "Steering waits for the navigation tick to move.");
                    Tick(ship);
                    Moved(ship, x, y, multiplier, "The navigation tick advances along the newly selected heading.");
                }
                Command(ship, "stop_button");
                float stoppedX = ship.X, stoppedY = ship.Y;
                Command(ship, turn);
                Tick(ship); Tick(ship);
                check(!ship.Underway && ship.X == stoppedX && ship.Y == stoppedY,
                    "STOP followed by either turn remains stopped across navigation ticks.");
                registry.Remove(ship);
            }

            ShipState manual = Ship("manual-route"), npc = Ship("merchant-route");
            npc.ShipType = ShipType.Merchant; npc.HelmsmanPCKey = null;
            registry.Register(manual); registry.Register(npc);
            ShipNavigationRoute merchantRoute = Route(npc, true);
            navigation.SetRoute(npc, merchantRoute);
            foreach (string turn in new[] { "left_button", "right_button" })
            {
                navigation.SetRoute(manual, Route(manual));
                Command(manual, turn);
                check(manual.Underway && !manual.IsNavigating && manual.DestinationAreaResRef == null &&
                    manual.DestinationX == 0 && manual.DestinationY == 0 && manual.DestinationZ == 0 && navigation.GetRoute(manual) == null,
                    "Manual steering cancels the autonomous course while keeping propulsion active.");
                float x = manual.X, y = manual.Y;
                Tick(manual);
                Moved(manual, x, y, 1, "Manual steering takes over the next tick from the cancelled route.");
                float npcX = npc.X, npcY = npc.Y;
                Tick(npc);
                check(npc.X == npcX + 1 && npc.Y == npcY && npc.IsNavigating && npc.Underway &&
                    npc.HelmsmanPCKey == null && ReferenceEquals(navigation.GetRoute(npc), merchantRoute) && merchantRoute.CurrentWaypointIndex == 0,
                    "Player steering leaves the NPC merchant's route and automatic movement intact.");
            }
            navigation.SetRoute(manual, Route(manual));
            Command(manual, "stop_button");
            check(!manual.Underway && !manual.IsNavigating && manual.DestinationAreaResRef == null && navigation.GetRoute(manual) == null,
                "STOP still cancels both propulsion and autonomous navigation.");
            float finalX = manual.X, finalY = manual.Y;
            Tick(manual); Tick(manual);
            check(manual.X == finalX && manual.Y == finalY && manual.HelmsmanPCKey == "captain",
                "STOP reliably prevents later movement without releasing the helm.");

            // ASTERN is explicitly a stop command: it must never move the ship backwards.
            foreach (string turn in new[] { "left_button", "right_button" })
            foreach (bool routed in new[] { false, true })
            {
                Command(manual, "ahead_button");
                if (routed) navigation.SetRoute(manual, Route(manual));
                effects.ApplySpeedBoost(manual, "astern-test", 50, TimeSpan.FromMinutes(5));
                float x = manual.X, y = manual.Y;
                Heading heading = manual.Heading;
                Command(manual, "astern_button");
                check(!manual.Underway && !manual.IsNavigating && navigation.GetRoute(manual) == null &&
                    manual.DestinationAreaResRef == null && manual.Heading == heading && manual.X == x && manual.Y == y,
                    "ASTERN stops manual or autonomous propulsion without a reverse step or heading change.");
                Command(manual, turn);
                Tick(manual); Tick(manual);
                check(!manual.Underway && manual.X == x && manual.Y == y && manual.HelmsmanPCKey == "captain" &&
                    effects.GetMovementMultiplier(manual) == 1.5f,
                    "ASTERN followed by either turn remains stopped, preserving helm ownership and speed modifiers.");
                Command(manual, "ahead_button");
                Tick(manual);
                Moved(manual, x, y, 1.5f, "AHEAD restarts forward propulsion after ASTERN using the selected heading and speed.");
                Command(manual, "stop_button");
            }
            Console.WriteLine("PASS: steering command and navigation tick checks.");
        }
        finally
        {
            foreach (IDisposable service in services.Values.OfType<IDisposable>().Reverse()) service.Dispose();
        }
    }
}

static class TacticalChartChecks
{
    private static ShipState Ship(string id, float x = 320, float y = 320) => new()
    { ShipId = id, ShipName = id, DeckAreaResRef = "deck_" + id, AreaResRef = "ocean_01", X = x, Y = y };

    private static NauticalChartFrame Frame(ShipState own, ShipNavigationService nav, ShipState[] fleet, string? selected = null,
        Func<int, int, bool>? discovered = null) => NauticalChartProjection.Capture(own, nav,
        new ShipVisibilityService().GetVisibleShips(own, fleet), [], [], [], [], discovered ?? ((_, _) => true), selected);

    public static async Task Run(Action<bool, string> check)
    {
        var areas = new SailingAreaService(); var obstacles = new ShipObstacleService();
        var navigation = new ShipNavigationService(obstacles, areas);
        var registry = new ShipRegistryService(); var encounters = new ShipEncounterService();
        using var persistence = new ShipStatePersistenceService(null!);
        var combat = new ShipCombatService(registry, encounters, persistence, null!);
        var own = Ship("own"); var target = Ship("target", 320, 325); target.ShipType = ShipType.Pirate;
        var other = Ship("merchant", 320, 321); other.ShipType = ShipType.Merchant;
        ShipState[] fleet = [own, target, other]; foreach (var ship in fleet) registry.Register(ship);
        encounters.CheckEncounters(fleet);
        var selections = new ShipTargetSelection(); var combatSelections = new CombatTargetSelectionService().Selection;
        selections.Refresh("viewer", own, [target, other]); combatSelections.Refresh("viewer", own, [target, other]);
        selections.Select("viewer", own, 0, [target, other]); combatSelections.Select("viewer", own, 1, [target, other]);
        NauticalChartFrame Capture(bool authorized = true)
        {
            var selected = selections.GetSelected("viewer", own, encounters.GetNearbyShips(own));
            var f = Frame(own, navigation, fleet, selected?.ShipId);
            return f with { Tactical = TacticalChartProjection.Capture(f, own, selected, combat, authorized) };
        }
        string before = System.Text.Json.JsonSerializer.Serialize(fleet.Select(s => ShipSnapshotCodec.Capture(s)));
        var cache = new NauticalChartWindowCache(); var renderer = cache.Open("viewer");
        var exploration = renderer.Prepare(Capture(), 640);
        check(renderer.Mode == SailingChartMode.Exploration && exploration.Layout != null &&
            exploration.Changes.Any(c => c.Key == "chart_m2_image" && c.Value is ChartRect { Width: 34, Height: 34 }),
            "Exploration defaults to the original 40px own marker and 34px illustrated sprite.");
        string originalLayout = Anvil.API.JsonUtility.ToJson(exploration.Layout!);
        renderer.ToggleMode(); var tactical = renderer.Prepare(Capture(), 640);
        check(renderer.Mode == SailingChartMode.Tactical && tactical.Layout != null &&
            !tactical.Changes.Any(c => c.Key.EndsWith("_sprite")) && tactical.Changes.Any(c => c.Key == "tac_m0_anchor"),
            "Tactical activation replaces illustrated sprites with compact symbols.");
        check(Capture().Tactical is { TargetId: "target", InRange: true, InArc: true, AttackStatus: "READY" } &&
            combatSelections.GetSelected("viewer", own, [target, other]) == other,
            "Tactical highlighting and readiness use the independent Sailing selection.");
        check(tactical.Changes.Count(c => c.Key.StartsWith("tac_m") && c.Key.EndsWith("_selected") && Equals(c.Value, true)) == 1,
            "Exactly the selected Sailing target receives a tactical halo.");
        check(renderer.Prepare(Capture(), 640) is { Layout: null, Changes.Count: 0 }, "Unchanged tactical frames produce no bind writes or layout rebuild.");
        int builds = renderer.LayoutBuilds;
        own.Heading = Heading.North; target.Y += .1f;
        check(renderer.Prepare(Capture(), 640).Layout == null && renderer.LayoutBuilds == builds,
            "Movement, heading and arc geometry update dynamic binds without rebuilding artwork.");
        own.Heading = Heading.East; target.Y = 325;
        renderer.InvalidateLayout(); check(renderer.Mode == SailingChartMode.Tactical && renderer.Prepare(Capture(), 416).Layout != null,
            "Resize invalidates geometry while retaining the open window's mode.");
        renderer.ToggleMode(); var returned = renderer.Prepare(Capture(), 640);
        check(Anvil.API.JsonUtility.ToJson(returned.Layout!) == originalLayout,
            "Switching back restores the exact exploration drawing tree.");
        check(before == System.Text.Json.JsonSerializer.Serialize(fleet.Select(s => ShipSnapshotCodec.Capture(s))) &&
            selections.GetSelected("viewer", own, [target, other]) == target && combatSelections.GetSelected("viewer", own, [target, other]) == other,
            "Chart switches do not mutate either target selection, positions, equipment or persisted gameplay state.");
        renderer.ToggleMode(); cache.Open("other");
        check(cache.TryGet("other", out var otherWindow) && otherWindow.Mode == SailingChartMode.Exploration, "Chart mode is per viewer.");
        cache.Close("viewer"); renderer = cache.Open("viewer");
        check(renderer.Mode == SailingChartMode.Exploration && renderer.Prepare(Capture(), 640).Layout != null,
            "Close/reopen starts a fresh Exploration window without stale mode or drawing bindings.");

        foreach (float size in new[] { 416f, 640f, 800f })
        {
            var frame = Capture(); var positions = TacticalChartProjection.PlaceMarkers(frame, size);
            foreach (var marker in positions)
                check(marker.Anchor == NauticalChartGeometry.Project(marker.Ship.X, marker.Ship.Y, size) &&
                    TacticalChartProjection.DistanceSquared(marker.Anchor, marker.Center) <= 36 * 36 + .001f,
                    "Tactical anchors use the original world transform and displacement is strictly bounded.");
            check(positions[^1].Ship.Kind == ChartContactKind.OwnShip && positions[^1].Center == positions[^1].Anchor && positions[^2].Ship.Selected,
                "Own ship remains exactly anchored and draws last; selection draws above ordinary contacts.");
            for (int i = 0; i < positions.Length; i++) for (int j = i + 1; j < positions.Length; j++)
                check(TacticalChartProjection.DistanceSquared(positions[i].Center, positions[j].Center) >= 18 * 18 - .01f,
                    "Nearby markers get minimum screen-space separation.");
            check(positions.SequenceEqual(TacticalChartProjection.PlaceMarkers(frame with { Markers = Enumerable.Reverse(frame.Markers).ToArray() }, size)),
                "Marker placement and layering are deterministic independent of enumeration order.");
            var values = new Dictionary<string, object>(); TacticalChartRenderer.Values(frame, size, values);
            for (int i = 0; i < positions.Length; i++) if (positions[i].Center != positions[i].Anchor)
                check(Equals(values[$"tac_m{i}_leader_on"], true) && Equals(values[$"tac_m{i}_leader_a"], positions[i].Anchor),
                    "Each displaced symbol has a thin leader beginning at its true anchor.");
            var lines = TacticalChartProjection.WeaponLines(frame, size);
            var origin = NauticalChartGeometry.Project(own.X, own.Y, size);
            check(Math.Abs(Math.Sqrt(TacticalChartProjection.DistanceSquared(lines[0].From, origin)) - 10 * size / 640) < .001,
                "Maximum weapon range is scaled from the real Cannon's 10 world units.");
            check(lines[^1].From == origin && lines[^1].To == NauticalChartGeometry.Project(target.X, target.Y, size),
                "Selected bearing line terminates at true positions, never displaced symbols.");
            renderer = new(); renderer.ToggleMode(); var drawn = renderer.Prepare(frame, size);
            for (int x = 0; x < 16; x++) for (int y = 0; y < 16; y++)
                check(Equals(drawn.Changes.Single(c => c.Key == $"chart_f{x}_{y}").Value, frame.Discovery[x * 16 + y] != '1'),
                    "Tactical mode preserves every fog cell binding and its column-major alignment.");
        }

        foreach (Heading heading in Enum.GetValues<Heading>())
        {
            own.Heading = heading;
            var vector = TacticalChartProjection.HeadingVector(heading);
            var expected = NauticalChartGeometry.HeadingVector(heading);
            check(Math.Abs(vector.X - expected.X) < .00001 && Math.Abs(vector.Y - expected.Y) < .00001,
                "All eight tactical arrows agree with exploration and the domain heading orientation.");
            foreach (WeaponArc arc in Enum.GetValues<WeaponArc>())
            {
                var weapon = new ShipWeapon { Damage = 1, MaxRange = 10, Arc = arc };
                foreach (double relative in Enumerable.Range(0, 360).Select(i => (double)i)
                             .Concat(new[] { 44.9999, 45.0001, 89.9999, 90.0001, 134.9999, 135.0001, 179.9999, 180.0001, 224.9999, 225.0001, 269.9999, 270.0001, 314.9999, 315.0001 }))
                {
                    double radians = (ShipActionRules.HeadingDegrees(heading) + relative) * Math.PI / 180;
                    var probe = Ship("probe", own.X + 10 * (float)Math.Cos(radians), own.Y + 10 * (float)Math.Sin(radians));
                    double angle = (Math.Atan2(probe.Y - own.Y, probe.X - own.X) * 180 / Math.PI - ShipActionRules.HeadingDegrees(heading) + 360) % 360;
                    const double epsilon = .00001;
                    bool oldRule = arc switch
                    {
                        WeaponArc.Forward => angle <= 45 + epsilon || angle >= 315 - epsilon,
                        WeaponArc.Aft => angle >= 135 - epsilon && angle <= 225 + epsilon,
                        WeaponArc.Port => angle >= 90 - epsilon && angle <= 180 + epsilon,
                        WeaponArc.Starboard => angle >= 180 - epsilon && angle <= 270 + epsilon,
                        WeaponArc.Broadside => angle >= 45 - epsilon && angle <= 135 + epsilon || angle >= 225 - epsilon && angle <= 315 + epsilon,
                        _ => false
                    };
                    check(ShipActionRules.InFiringArc(own, probe, weapon) == oldRule, $"Legacy combat predicate: {heading}/{arc}, relative {relative}, raw {angle}.");
                    double normalized = ShipActionRules.RelativeBearingDegrees(own, probe);
                    bool drawn = ShipActionRules.FiringSectors(arc, heading).Any(s =>
                        normalized >= s.Start && normalized <= s.End || s.End > 360 && normalized + 360 >= s.Start && normalized + 360 <= s.End);
                    // Exact endpoints use the original epsilon and Atan2 seam; compare interiors here.
                    if (relative % 45 > .01 && relative % 45 < 44.99)
                        check(drawn == oldRule, $"Drawn sector interior agrees with real firing rule: {heading}/{arc}/{relative}.");
                }
                var f = Capture(); f = f with { Tactical = f.Tactical! with { Weapon = weapon } };
                var lines = TacticalChartProjection.WeaponLines(f, 640); int slot = TacticalChartProjection.RangeSegments;
                foreach (var sector in ShipActionRules.FiringSectors(arc, heading))
                {
                    foreach (double boundary in new[] { sector.Start, sector.End })
                    {
                        double radians = (ShipActionRules.HeadingDegrees(heading) + boundary) * Math.PI / 180;
                        var world = new ChartPoint(own.X + 10 * (float)Math.Cos(radians), own.Y + 10 * (float)Math.Sin(radians));
                        check(lines[slot++].To == NauticalChartGeometry.Project(world.X, world.Y, 640),
                            "Port, starboard, bow and stern boundary lines match the domain sectors at every heading.");
                    }
                    slot += TacticalChartProjection.SectorSegments;
                }
            }
        }
        own.Heading = Heading.East;
        foreach (var weapon in combat.GetAvailableWeapons())
        foreach (float size in new[] { 416f, 640f, 800f })
        {
            own.WeaponResRef = weapon.ResRef;
            var f = Capture(); var lines = TacticalChartProjection.WeaponLines(f, size);
            var origin = NauticalChartGeometry.Project(own.X, own.Y, size);
            check(f.Tactical!.Weapon == weapon && Math.Abs(Math.Sqrt(TacticalChartProjection.DistanceSquared(origin, lines[0].From)) - weapon.MaxRange * size / 640) < .001,
                "Each actual equipped weapon supplies its own range, with no presentation substitute.");
        }
        own.WeaponResRef = "ship_cannon";
        check(TacticalChartProjection.Bearing(90, 5) == "PORT 90 deg" && TacticalChartProjection.Bearing(270, 5) == "STBD 90 deg" &&
            TacticalChartProjection.Bearing(0, 5) == "BOW 0 deg" && TacticalChartProjection.Bearing(180, 5) == "STERN 180 deg" &&
            TacticalChartProjection.Bearing(90, 0) == "COINCIDENT", "Relative bearing distinguishes both sides, bow, stern and coincident positions.");
        target.X = 325; target.Y = 320;
        check(Capture().Tactical is { InRange: true, InArc: false, AttackStatus: "OUT OF ARC" }, "Bow target is in Cannon range but outside its broadside.");
        target.X = 320; target.Y = 331;
        check(Capture().Tactical is { InRange: false, InArc: true, AttackStatus: "OUT OF RANGE" }, "Range and arc statuses remain independent.");
        target.Y = 330; check(Capture().Tactical is { InRange: true, AttackStatus: "READY" }, "Exact maximum range is inclusive.");
        check(Capture(false).Tactical!.AttackStatus == "NOT AUTHORIZED", "Tactical readiness honors denied weapon authorization.");
        own.Hull = 0; check(Capture().Tactical!.AttackStatus == "HULL DISABLED", "Disabled hull blocks attack readiness."); own.Hull = 100;
        target.Hull = 0; check(Capture().Tactical!.AttackStatus == "TARGET DISABLED", "Disabled target blocks attack readiness."); target.Hull = 100;
        own.ShipType = ShipType.Pirate;
        for (int i = 0; i < 10; i++) check(Capture().Tactical!.AttackStatus == "READY" && target.Hull == 100 && combat.GetCooldownRemaining(own) == TimeSpan.Zero,
            "Repeated readiness previews never consume cooldown or damage hull.");
        var hit = await combat.TryNpcAttack(own, target);
        check(hit.Success && hit.Damage == 10 && hit.RemainingHull == 90 && Capture().Tactical!.AttackStatus == "RELOADING" &&
            (await combat.TryNpcAttack(own, target)).IsCooldown, "Real attacks retain damage and cooldown; chart reflects reloading without bypassing it.");
        foreach (var cutter in new[] { new ShipState { ShipId = "cut1", ShipName = "Cutter", DeckAreaResRef = "cutter1", AreaResRef = "ocean_01", X = 320, Y = 320, AllowsWeapons = false },
                     new ShipState { ShipId = "cut2", ShipName = "Rental", DeckAreaResRef = "cutter2", AreaResRef = "ocean_01", X = 320, Y = 320, DefinitionId = CutterRentalFleet.DefinitionId } })
        {
            registry.Register(cutter); var f = Frame(cutter, navigation, [cutter, target], target.ShipId);
            f = f with { Tactical = TacticalChartProjection.Capture(f, cutter, target, combat, true) };
            check(f.Tactical is { Weapon.Damage: 0, AttackStatus: "UNARMED", InRange: false, InArc: false } &&
                !TacticalChartProjection.WeaponLines(f, 640).Take(TacticalChartProjection.OverlayLines - 1).Any(l => l.Visible) &&
                TacticalChartProjection.WeaponReadout(f.Tactical).Contains("UNARMED"), "Cutter definition and capability both suppress all weapon arcs and advertise UNARMED.");
        }

        foreach (string hidden in new[] { "fog", "range", "area", "stored", "initializing", "docking", "removed" })
        {
            var probe = Ship("hidden", 320, 325);
            if (hidden == "range") probe.Y = 391;
            if (hidden == "area") probe.AreaResRef = "elsewhere";
            if (hidden == "stored") probe.IsStored = true;
            if (hidden == "initializing") probe.IsInitializing = true;
            if (hidden == "docking") probe.IsDocking = true;
            var f = Frame(own, navigation, hidden == "removed" ? [own] : [own, probe], probe.ShipId, (_, _) => hidden != "fog");
            f = f with { Tactical = TacticalChartProjection.Capture(f, own, probe, combat, true) };
            check(f.Markers.All(m => m.Id != probe.ShipId) && f.Tactical is { TargetId: null, Distance: null, RelativeBearing: null } &&
                !TacticalChartProjection.WeaponLines(f, 640)[^1].Visible, "Hidden contacts produce no marker, target readout or bearing line: " + hidden);
        }
        foreach (float size in new[] { 416f, 640f, 800f })
        foreach (var corner in new[] { new ChartPoint(0, 0), new(640, 0), new(0, 640), new(640, 640), new(320, 320) })
        {
            own.X = corner.X; own.Y = corner.Y;
            ShipState[] crowd = Enumerable.Range(0, 20).Select(i => Ship("crowd" + i, own.X, own.Y)).Append(own).ToArray();
            var f = Frame(own, navigation, crowd, crowd[0].ShipId);
            f = f with { Tactical = TacticalChartProjection.Capture(f, own, crowd[0], combat, true), Route = [new("ocean_01", -20, 700, true)] };
            var markers = TacticalChartProjection.PlaceMarkers(f, size);
            check(markers.Length == 21 && markers.All(m => TacticalChartProjection.DistanceSquared(m.Anchor, m.Center) <= 1296.001f),
                "Dense clusters retain every contact and cap displacement, including chart corners.");
            check(markers.Any(m => m.Crowded) && !markers.Single(m => m.Ship.Selected).Crowded,
                "Unavoidable dense overlap is reported while selected-target placement has priority.");
            bool Inside(ChartPoint p) => p.X >= .999f && p.Y >= .999f && p.X <= size - .999f && p.Y <= size - .999f;
            check(TacticalChartProjection.WeaponLines(f, size).Where(l => l.Visible).All(l => Inside(l.From) && Inside(l.To)) &&
                NauticalChartProjection.Segments(f, size).All(l => Inside(l.From) && Inside(l.To)), "All instrument and route lines clip to the chart boundary.");
            var values = new Dictionary<string, object>(); TacticalChartRenderer.Values(f, size, values);
            foreach (var on in values.Where(p => p.Key.EndsWith("_on") && Equals(p.Value, true)))
            {
                string key = on.Key[..^3];
                check(Inside((ChartPoint)values[key + "_a"]) && Inside((ChartPoint)values[key + "_b"]), "Enabled marker strokes and leaders stay inside the chart.");
            }
        }
        own.X = own.Y = 320; own.ShipType = ShipType.Player;
        var route = new ShipNavigationRoute { ShipName = own.ShipName, IsChartCourse = true, Waypoints = [new() { AreaResRef = "ocean_01", X = 340, Y = 320 }] };
        navigation.SetRoute(own, route); var snapshot = System.Text.Json.JsonSerializer.Serialize(ShipSnapshotCodec.Capture(own, route));
        renderer = new(); int encounterEvents = 0; encounters.EncounterStarted += _ => encounterEvents++; encounters.EncounterEnded += _ => encounterEvents++;
        for (int i = 0; i < 25; i++) { renderer.ToggleMode(); renderer.Prepare(Capture(), 640); }
        check(ReferenceEquals(navigation.GetRoute(own), route) && snapshot == System.Text.Json.JsonSerializer.Serialize(ShipSnapshotCodec.Capture(own, route)) && encounterEvents == 0,
            "Repeated chart refresh/switching preserves autopilot and never reconciles encounters.");
        var movement = new ShipMovementService(areas, obstacles);
        own.Heading = navigation.GetNavigationHeading(own);
        check(movement.TryMove(own, 1) && own.X == 321 && own.Y == 320 && navigation.IsNavigating(own), "Navigation progresses normally after Tactical mode switching.");
        LayoutAndScanContracts(check);
        Console.WriteLine("PASS: tactical chart geometry, visibility, combat parity, caching and mode lifecycle checks.");
    }

    private static void LayoutAndScanContracts(Action<bool, string> check)
    {
        Dictionary<Type, object> services = new()
        {
            [typeof(Anvil.Services.EventService)] = new Anvil.Services.EventService([]),
            [typeof(ShipPersistenceOptions)] = new ShipPersistenceOptions { Enabled = false, Backend = SailingPersistenceBackend.Disabled },
            [typeof(SailingPersistenceContextFactory)] = SailingPersistenceContextFactory.ForOptions(
                new DbContextOptionsBuilder<SailingPersistenceDbContext>().UseNpgsql("Host=127.0.0.1;Port=1;Database=unused;Username=unused;Password=unused").Options)
        };
        try
        {
            var service = (SailingNuiService)StartupChecks.CreateResolver(services)(typeof(SailingNuiService));
            foreach (float size in new[] { 416f, 640f, 800f })
            {
                var root = (Anvil.API.NuiColumn)typeof(SailingNuiService).GetMethod("CreateLayout",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(service, [Ship("layout"), size])!;
                var header = (Anvil.API.NuiRow)root.Children[0];
                var toggle = header.Children.OfType<Anvil.API.NuiButton>().Single();
                check(toggle.Id == "sailing_chart_toggle" && toggle.Width == 304 && toggle.Height == 28 &&
                    header.Height == 28 && header.Children.Sum(c => c.Width ?? 0) <= header.Width &&
                    toggle.Label is Anvil.API.NuiBind<string> { Key: "chart_toggle" },
                    "The visible mode toggle fits the existing header at every supported chart size.");
                check(NauticalChartRecoveryChecks.Elements(root).OfType<Anvil.API.NuiGroup>().Count(g => g.Scrollbars != Anvil.API.NuiScrollbars.None) == 1,
                    "The toggle and tactical readouts add no scrollable groups.");
            }
        }
        finally { foreach (var disposable in services.Values.OfType<IDisposable>().Reverse()) disposable.Dispose(); }

        var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly;
        foreach (var type in new[] { typeof(TacticalChartProjection), typeof(TacticalChartRenderer), typeof(NauticalChartRenderer) })
        foreach (var method in type.GetMethods(flags).Concat(type.GetNestedTypes(flags).SelectMany(t => t.GetMethods(flags))))
            check(!Calls(method).Any(m => m.DeclaringType == typeof(ShipEncounterService) ||
                m.DeclaringType == typeof(ShipRegistryService) && m.Name == "GetShips"),
                "Tactical projection/rendering cannot enumerate the global fleet or scan encounters.");
        var updateCalls = Calls(typeof(SailingNuiService).GetMethod("Update")!).ToArray();
        check(updateCalls.Count(m => m.DeclaringType == typeof(ShipRegistryService) && m.Name == "GetShips") == 1 &&
            updateCalls.Count(m => m.DeclaringType == typeof(ShipEncounterService) && m.Name == "GetNearbyShips") == 1 &&
            !updateCalls.Any(m => m.Name is "GetActiveEncounters" or "CheckEncounters"),
            "Sailing Update retains its single existing optional fleet snapshot and local adjacency lookup, with no global encounter scan.");
        var authorization = typeof(ShipCombatService).GetMethod("CanOperateWeapons", [typeof(ShipState), typeof(Anvil.API.NwPlayer)])!;
        check(!Calls(authorization).Any(m => m.DeclaringType == typeof(ShipRegistryService) && m.Name == "GetShip"),
            "Tactical authorization uses canonical identity without a global name lookup.");
    }

    private static IEnumerable<System.Reflection.MethodBase> Calls(System.Reflection.MethodInfo method)
    {
        var bytes = method.GetMethodBody()?.GetILAsByteArray();
        if (bytes == null) yield break;
        var codes = typeof(System.Reflection.Emit.OpCodes).GetFields().Where(f => f.FieldType == typeof(System.Reflection.Emit.OpCode))
            .Select(f => (System.Reflection.Emit.OpCode)f.GetValue(null)!).ToDictionary(c => unchecked((ushort)c.Value));
        for (int i = 0; i < bytes.Length;)
        {
            ushort value = bytes[i++]; if (value == 0xfe) value = (ushort)(0xfe00 | bytes[i++]);
            var operand = codes[value].OperandType;
            if (operand == System.Reflection.Emit.OperandType.InlineMethod && method.Module.ResolveMethod(BitConverter.ToInt32(bytes, i)) is { } called)
                yield return called;
            i += operand switch
            {
                System.Reflection.Emit.OperandType.InlineNone => 0,
                System.Reflection.Emit.OperandType.ShortInlineBrTarget or System.Reflection.Emit.OperandType.ShortInlineI or System.Reflection.Emit.OperandType.ShortInlineVar => 1,
                System.Reflection.Emit.OperandType.InlineVar => 2,
                System.Reflection.Emit.OperandType.InlineI8 or System.Reflection.Emit.OperandType.InlineR => 8,
                System.Reflection.Emit.OperandType.InlineSwitch => 4 + 4 * BitConverter.ToInt32(bytes, i),
                _ => 4
            };
        }
    }
}

static class ChartZoomChecks
{
    private static ShipState Ship(string id, float x = 320, float y = 320) => new()
    { ShipId = id, ShipName = id, DeckAreaResRef = "deck", AreaResRef = "ocean_01", X = x, Y = y, HelmsmanPCKey = "captain" };
    private static bool Near(float a, float b) => Math.Abs(a - b) < .002f;
    private static bool Near(ChartPoint a, ChartPoint b) => Near(a.X, b.X) && Near(a.Y, b.Y);
    private static ChartPoint Center(ChartRect r) => new(r.X + r.Width / 2, r.Y + r.Height / 2);
    private static void ZoomTo(NauticalChartRenderer renderer, int zoom)
    { renderer.HandleZoomControl("chart_zoom_reset"); while (renderer.Zoom < zoom) renderer.HandleZoomControl("chart_zoom_in"); }

    public static void Run(Action<bool, string> check)
    {
        var areas = new SailingAreaService(); var obstacles = new ShipObstacleService();
        var navigation = new ShipNavigationService(obstacles, areas); var planner = new ShipRoutePlannerService(areas);
        var own = Ship("own"); var target = Ship("target", 320, 325); target.ShipType = ShipType.Pirate;
        var merchant = Ship("merchant", 320.5f, 320.5f); merchant.ShipType = ShipType.Merchant;
        var registry = new ShipRegistryService(); foreach (var s in new[] { own, target, merchant }) registry.Register(s);
        var combat = new ShipCombatService(registry, new ShipEncounterService(), new ShipStatePersistenceService(null!), null!);
        var landmarks = new[] { new ChartLandmark { AreaResRef = "ocean_01", X = 330, Y = 330, Width = 80, Height = 40, Sprite = "sailing_map" } };
        var ports = new[] { new IslandLocation { Id = "test", Name = "Port", OceanArea = "ocean_01", OceanX = 310, OceanY = 310 } };
        NauticalChartFrame Frame(string? fog = null)
        {
            var f = NauticalChartProjection.Capture(own, navigation, new ShipVisibilityService().GetVisibleShips(own, [own, target, merchant]),
                [], landmarks, [], ports, (x, y) => fog == null || fog[x * 16 + y] == '1', target.ShipId);
            return f with { Tactical = TacticalChartProjection.Capture(f, own, target, combat, true) };
        }
        string originalState = System.Text.Json.JsonSerializer.Serialize(new[] { own, target, merchant }.Select(s => ShipSnapshotCodec.Capture(s)));
        foreach (int zoom in new[] { 1, 2, 4, 8 })
        foreach (float size in new[] { 416f, 640f, 800f })
        foreach (var shipPoint in new[] { new ChartPoint(0, 0), new(640, 0), new(0, 640), new(640, 640), new(320, 320), new(321.125f, 317.375f) })
        {
            var viewport = ChartViewport.Create(size, zoom, shipPoint.X, shipPoint.Y);
            check(viewport.Span == 640f / zoom && viewport.Left >= 0 && viewport.Bottom >= 0 &&
                viewport.Left + viewport.Span <= 640 && viewport.Bottom + viewport.Span <= 640 && viewport.Contains(shipPoint.X, shipPoint.Y),
                "All zoom cameras clamp to real ocean bounds and retain the player at every corner.");
            if (shipPoint.X >= viewport.Span / 2 && shipPoint.X <= 640 - viewport.Span / 2 &&
                shipPoint.Y >= viewport.Span / 2 && shipPoint.Y <= 640 - viewport.Span / 2)
                check(Near(viewport.Project(shipPoint.X, shipPoint.Y), new ChartPoint(size / 2, size / 2)), "Local camera centres on the ship where ocean bounds allow.");
            if (zoom == 1) check(viewport.Left == 0 && viewport.Bottom == 0 && viewport.Background == new ChartRect(0, 0, size, size),
                "1x always retains the entire chart independent of the ship's position.");
            var background = viewport.Background;
            check(background.Width == size * zoom && background.Height == background.Width && background.X <= 0 && background.Y <= 0 &&
                background.X + background.Width >= size && background.Y + background.Height >= size, "Enlarged artwork covers the viewport uniformly with no invalid ocean space.");
            foreach (var pixel in new[] { new ChartPoint(0, 0), new(size, 0), new(0, size), new(size, size), new(size / 2, size / 2), new(.125f, size - .125f) })
            {
                check(viewport.TryUnproject(pixel.X, pixel.Y, out var world) && viewport.Contains(world.X, world.Y) &&
                    Near(viewport.Project(world.X, world.Y), pixel), "World/screen inversion round trips corners, centre and edge clicks at every zoom.");
                var artPoint = new ChartPoint(background.X + world.X / 640 * background.Width, background.Y + (1 - world.Y / 640) * background.Height);
                check(Near(artPoint, pixel), "Chart artwork uses exactly the same coordinates as every overlay.");
                var cell = NauticalChartGeometry.Cell(world.X, world.Y); var fogRect = viewport.FogCell(cell.X, cell.Y);
                check(pixel.X >= fogRect.X - .002f && pixel.Y >= fogRect.Y - .002f && pixel.X <= fogRect.X + fogRect.Width + .002f &&
                    pixel.Y <= fogRect.Y + fogRect.Height + .002f, "Fog and inverse coordinates agree at ocean and viewport edges.");
            }
            check(!viewport.TryUnproject(-.01f, 0, out _) && !viewport.TryUnproject(0, size + .01f, out _) &&
                !viewport.TryUnproject(float.NaN, 0, out _), "Viewport rejects outside and nonfinite clicks.");
            for (int x = 0; x < 16; x++) for (int y = 0; y < 16; y++)
            {
                var r = viewport.FogCell(x, y);
                check(r.X >= 0 && r.Y >= 0 && r.Width >= 0 && r.Height >= 0 && r.X + r.Width <= size + .001 && r.Y + r.Height <= size + .001,
                    "Every fog rectangle clips inside the canvas.");
                if (x < 15 && r.Width > 0 && r.Height > 0 && viewport.FogCell(x + 1, y) is { Width: > 0 } next)
                    check(r.X + r.Width >= next.X - .001, "Adjacent fog columns leave no seam under fractional follow positions.");
                if (y < 15 && r.Width > 0 && r.Height > 0 && viewport.FogCell(x, y + 1) is { Height: > 0 } below)
                    check(r.Y + r.Height >= below.Y - .001, "Adjacent fog rows leave no seam under fractional follow positions.");
            }
        }

        foreach (int zoom in new[] { 1, 2, 4, 8 })
        foreach (var mode in Enum.GetValues<SailingChartMode>())
        {
            var renderer = new NauticalChartRenderer(); ZoomTo(renderer, zoom);
            if (mode == SailingChartMode.Tactical) renderer.ToggleMode();
            var frame = Frame() with { Route = [new("ocean_01", 330, 325, true)], ChartCourse = true,
                Preview = [new("ocean_01", 315, 315, true)] };
            var update = renderer.Prepare(frame, 640); var values = update.Changes.ToDictionary(c => c.Key, c => c.Value);
            var viewport = renderer.Viewport!.Value;
            var transformed = frame with { Viewport = viewport, ChartMode = mode };
            check(renderer.Zoom == zoom && Equals(values["chart_zoom_label"], $"ZOOM {zoom}x") &&
                Equals(values["chart_zoom_in_on"], zoom < 8) && Equals(values["chart_zoom_out_on"], zoom > 1), "All zoom levels publish accurate indicators and endpoint control availability in both modes.");
            check(Near((ChartPoint)values["chart_r0_a"], viewport.Project(own.X, own.Y)) &&
                Near((ChartPoint)values["chart_r0_b"], viewport.Project(330, 325)) && Near(Center((ChartRect)values["chart_w0_rect"]), viewport.Project(330, 325)) &&
                Near(Center((ChartRect)values["chart_p0_rect"]), viewport.Project(315, 315)), "Actual route/preview lines and destination circles all use the camera transform.");
            if (mode == SailingChartMode.Exploration)
            {
                for (int i = 0; i < frame.Markers.Length; i++)
                    check(Near(Center((ChartRect)values[$"chart_m{i}_ring"]), viewport.Project(frame.Markers[i].X, frame.Markers[i].Y)),
                        "Exploration ship, merchant and pirate sprites retain true zoomed anchors.");
            }
            else
            {
                var markers = TacticalChartProjection.PlaceMarkers(transformed, 640);
                for (int i = 0; i < markers.Length; i++)
                {
                    var m = markers[i];
                    check(Near(Center((ChartRect)values[$"tac_m{i}_anchor"]), viewport.Project(m.Ship.X, m.Ship.Y)) &&
                        TacticalChartProjection.DistanceSquared(m.Center, m.Anchor) <= 1296.01f, "Tactical anchors and bounded separation use the current zoom.");
                    if (m.Center != m.Anchor) check(Equals(values[$"tac_m{i}_leader_on"], true) && Near((ChartPoint)values[$"tac_m{i}_leader_a"], m.Anchor),
                        "Zoomed tactical connectors start at the true transformed anchor.");
                }
                check(markers[^1].Ship.Kind == ChartContactKind.OwnShip && markers[^2].Ship.Selected &&
                    Equals(values[$"tac_m{markers.Length - 2}_selected"], true), "Zoom preserves selected-target highlighting and own-ship foreground priority.");
                var lines = TacticalChartProjection.WeaponLines(transformed, 640);
                check(Near(lines[^1].From, viewport.Project(own.X, own.Y)) && Near(lines[^1].To, viewport.Project(target.X, target.Y)) &&
                    Near((float)Math.Sqrt(TacticalChartProjection.DistanceSquared(lines[0].From, lines[^1].From)), 10 * zoom),
                    "Tactical maximum range and selected bearing share the camera scale.");
                check(Near(lines[TacticalChartProjection.RangeSegments].To, viewport.Project(own.X + 10 / MathF.Sqrt(2), own.Y + 10 / MathF.Sqrt(2))),
                    "Cannon port-sector boundary scales from its unchanged combat heading and range.");
            }
            if (zoom > 1)
            {
                var background = update.Layout!.DrawList!.OfType<Anvil.API.NuiDrawListImage>().First();
                check(background.Rect is Anvil.API.NuiBind<Anvil.API.NuiRect> { Key: "chart_background_rect" } &&
                    Equals(values["chart_background_rect"], viewport.Background), "Actual background binds enlarge and translate the original artwork.");
                var landmark = (ChartRect)values["chart_g0_art"];
                check(Near(Center(landmark), viewport.Project(330, 330)) && Near(landmark.Width, 80 * 640 * zoom / 800f) &&
                    Near(landmark.Height, landmark.Width / 2), "Landmark artwork scales uniformly around its geographic centre.");
                check(Near(Center(((ChartPolygon)values["chart_g1_points"]).Bounds), viewport.Project(310, 310)), "Discovered port diamond follows the same viewport.");
                check(update.Layout.DrawList!.OfType<ChartBoundPolyLine>().Count(p => p.Points.Key.StartsWith("chart_f")) == 256,
                    "Every discovery cell uses a native bound polygon in local zoom.");
            }
            check(renderer.Prepare(frame, 640) is { Layout: null, Changes.Count: 0 }, "Unchanged zoomed charts publish no redundant bindings.");
            int builds = renderer.LayoutBuilds;
            own.X += 1.125f; own.Y += .375f;
            var moved = renderer.Prepare(Frame() with { Route = frame.Route, Preview = frame.Preview, ChartCourse = true }, 640);
            check(moved.Layout == null && renderer.LayoutBuilds == builds, "Ship-follow updates dynamic geometry without rebuilding static artwork or controls.");
            if (zoom > 1) check(renderer.Viewport != viewport && moved.Changes.Any(c => c.Key == "chart_background_rect") &&
                moved.Changes.Any(c => c.Key.EndsWith("_points") && c.Key.StartsWith("chart_f")), "Background and fog follow the ship together.");
            own.X = own.Y = 320;
            renderer.ToggleMode(); renderer.Prepare(frame, 640);
            check(renderer.Zoom == zoom, "Switching chart mode retains the selected zoom.");
            renderer.InvalidateLayout(); renderer.Prepare(frame, 416);
            check(renderer.Zoom == zoom && renderer.Viewport!.Value.Size == 416, "Resize retains zoom and recomputes its pixel transform.");
        }

        // Verify opaque native wire format, including the adapter for Anvil's static-only Points wrapper.
        var bound = new ChartBoundPolyLine(new Anvil.API.Color(38, 54, 65), true, 0, "fog_points");
        var native = new Anvil.API.NuiDrawListPolyLine(new Anvil.API.Color(38, 54, 65), true, 0, [0, 0, 10, 0, 10, 10, 0, 10, 0, 0]);
        var boundJson = Newtonsoft.Json.Linq.JObject.Parse(Anvil.API.JsonUtility.ToJson(bound));
        var nativeJson = Newtonsoft.Json.Linq.JObject.Parse(Anvil.API.JsonUtility.ToJson(native));
        check(boundJson["points"]!.ToString().Contains("fog_points"), "The fog adapter serializes an actual NUI points binding.");
        boundJson.Remove("points"); nativeJson.Remove("points");
        check(Newtonsoft.Json.Linq.JToken.DeepEquals(boundJson, nativeJson), "Bound fog has the exact installed Anvil native polygon schema, opacity, fill and zero stroke.");

        using var discovery = new ChartDiscoveryService(); discovery.RevealAroundShip("captain", own);
        string cells = discovery.CaptureCells("captain", "ocean_01");
        foreach (int zoom in new[] { 1, 2, 4, 8 })
        {
            var renderer = new NauticalChartRenderer(); ZoomTo(renderer, zoom);
            var hiddenFrame = Frame(new string('0', 256)); renderer.ToggleMode();
            var hidden = renderer.Prepare(hiddenFrame, 640);
            check(hiddenFrame.Markers.Length == 1 && hiddenFrame.Features.Length == 0 && hiddenFrame.Tactical!.TargetId == null &&
                !hidden.Changes.Any(c => c.Key.StartsWith("tac_m") && c.Key.EndsWith("_selected") && Equals(c.Value, true)),
                "Zoom cannot reveal undiscovered contacts, target data or landmarks.");
            var f = Frame(cells); var values = new NauticalChartRenderer(); ZoomTo(values, zoom);
            var drawn = values.Prepare(f, 640).Changes.ToDictionary(c => c.Key, c => c.Value);
            for (int x = 0; x < 16; x++) for (int y = 0; y < 16; y++)
            {
                var rect = values.Viewport!.Value.FogCell(x, y);
                check(Equals(drawn[$"chart_f{x}_{y}"], cells[x * 16 + y] != '1' && (zoom == 1 || rect.Width > 0 && rect.Height > 0)),
                    "Fog binding retains discovery and only suppresses completely offscreen cells.");
            }
            check(discovery.CaptureCells("captain", "ocean_01") == cells && f.Discovery == cells, "Zoom never alters persistent 16x16 discovery.");

            var viewport = ChartViewport.Create(640, zoom, own.X, own.Y);
            var session = new ChartCourseSession("captain", own, planner, navigation, obstacles);
            var destination = new ChartPoint(335, 325); var click = viewport.Project(destination.X, destination.Y);
            check(session.Select(own, true, click.X, click.Y, viewport, out _) && session.Preview is { } preview && Near(preview.Destination, destination),
                "Actual click-to-preview converts the shared viewport at every zoom.");
            var proposed = session.Preview!;
            var previewCamera = new NauticalChartRenderer(); ZoomTo(previewCamera, zoom);
            foreach (int changedZoom in new[] { 8, 4, 2, 1 })
            {
                ZoomTo(previewCamera, changedZoom); previewCamera.ToggleMode(); previewCamera.Prepare(Frame(), 640);
                check(ReferenceEquals(proposed, session.Preview) && Near(session.Preview!.Destination, destination),
                    "Zoom and mode changes retain the existing world-space course preview.");
            }
            var routeFrame = Frame() with { Viewport = viewport, Preview = proposed.Route.Waypoints.Select((w, i) => new ChartRoutePoint(w.AreaResRef, w.X, w.Y, i == proposed.Route.Waypoints.Count - 1)).ToArray() };
            check(Near(NauticalChartProjection.Segments(routeFrame with { Route = routeFrame.Preview }, 640)[^1].To, click),
                "The displayed route preview ends at the exact clicked chart point.");
            check(session.Confirm(own, true, out _) && navigation.GetRoute(own) is { IsChartCourse: true } route &&
                Near(new ChartPoint(route.Waypoints[^1].X, route.Waypoints[^1].Y), destination), "Confirm uses actual world destination and existing autopilot route planning at every zoom.");
            navigation.CancelNavigation(own);

            var blocked = new ShipObstacleService(); blocked.RegisterObstacle(new() { Name = "test island", AreaResRef = "ocean_01", MinX = 334, MaxX = 336, MinY = 324, MaxY = 326 });
            var blockedSession = new ChartCourseSession("captain", own, planner, new ShipNavigationService(blocked, areas), blocked);
            check(!blockedSession.Select(own, true, click.X, click.Y, viewport, out _) && blockedSession.Preview == null,
                "Zoomed click navigation preserves blocked-terrain rejection.");
            foreach (var edge in new[] { new ChartPoint(0, 0), new(640, 0), new(0, 640), new(640, 640) })
            {
                var vessel = Ship("edge", edge.X == 0 ? 2 : 638, edge.Y == 0 ? 2 : 638);
                var camera = ChartViewport.Create(640, zoom, vessel.X, vessel.Y);
                var edgeSession = new ChartCourseSession("captain", vessel, planner, navigation, obstacles);
                var point = camera.Project(edge.X, edge.Y);
                check(edgeSession.Select(vessel, true, point.X, point.Y, camera, out _) && Near(edgeSession.Preview!.Destination, edge),
                    "Actual course selection at every ocean corner remains accurate in a clamped viewport.");
                foreach (var pixel in new[] { new ChartPoint(1, 639), new(639, 1) })
                {
                    camera.TryUnproject(pixel.X, pixel.Y, out var expected);
                    check(edgeSession.Select(vessel, true, pixel.X, pixel.Y, camera, out _) && Near(edgeSession.Preview!.Destination, expected),
                        "Actual course selection near viewport edges uses the clamped camera inverse.");
                }
            }
        }
        var cache = new NauticalChartWindowCache(); var a = cache.Open("a"); var b = cache.Open("b");
        ZoomTo(a, 8); ZoomTo(b, 2); a.ToggleMode(); a.Prepare(Frame(), 640); b.Prepare(Frame(), 640);
        check(a.Zoom == 8 && b.Zoom == 2 && a.Viewport!.Value.Span == 80 && b.Viewport!.Value.Span == 320, "Independent players hold independent camera states.");
        check(!a.HandleZoomControl("chart_zoom_in") && !a.HandleZoomControl("unrelated"), "Zoom commands stop at supported bounds and ignore unrelated actions.");
        a.HandleZoomControl("chart_zoom_out"); check(a.Zoom == 4, "Zoom out steps from 8x to 4x.");
        a.HandleZoomControl("chart_zoom_reset"); check(a.Zoom == 1 && a.Mode == SailingChartMode.Tactical, "Reset changes only zoom, preserving the current mode.");
        cache.Close("a"); var reopened = cache.Open("a");
        check(reopened.Zoom == 1 && reopened.Mode == SailingChartMode.Exploration && reopened.Viewport == null && b.Zoom == 2,
            "Close/reopen resets only that viewer's camera and leaves other windows intact.");
        var outsideFrame = Frame() with { Viewport = ChartViewport.Create(640, 8), Markers =
            [new("offscreen", "Offscreen", 370, 320, Heading.West, "sloop_w", ChartContactKind.Player), Frame().Markers[^1]] };
        check(TacticalChartProjection.PlaceMarkers(outsideFrame, 640).All(m => m.Ship.Id != "offscreen"),
            "An offscreen contact is clipped, never displaced onto a false viewport-edge position.");
        check(originalState == System.Text.Json.JsonSerializer.Serialize(new[] { own, target, merchant }.Select(s => ShipSnapshotCodec.Capture(s))),
            "Zoom and rendering leave ship, combat and navigation state unchanged after normal course cancellation.");
        var activeRoute = new ShipNavigationRoute { ShipName = own.ShipName, IsChartCourse = true,
            Waypoints = [new() { AreaResRef = "ocean_01", X = 340, Y = 320 }] };
        navigation.SetRoute(own, activeRoute);
        string underwayState = System.Text.Json.JsonSerializer.Serialize(ShipSnapshotCodec.Capture(own, activeRoute));
        var underwayCamera = new NauticalChartRenderer();
        foreach (int zoom in new[] { 1, 2, 4, 8, 4, 2, 1 })
        {
            ZoomTo(underwayCamera, zoom); underwayCamera.ToggleMode(); underwayCamera.Prepare(Frame(), 640);
            check(ReferenceEquals(activeRoute, navigation.GetRoute(own)) && underwayState ==
                System.Text.Json.JsonSerializer.Serialize(ShipSnapshotCodec.Capture(own, activeRoute)),
                "Camera changes leave the active autopilot route, destination, ship and combat state intact.");
        }
        own.Heading = navigation.GetNavigationHeading(own);
        check(new ShipMovementService(areas, obstacles).TryMove(own, 1) && own.X == 321 && own.Y == 320 && navigation.IsNavigating(own),
            "The production navigation heading and movement services advance autopilot normally after zoom changes.");
        navigation.CancelNavigation(own); own.X = 320;
        ClippingAndHeadings(Frame(), check);
        Layout(check);
        Console.WriteLine("PASS: universal chart zoom, viewport/fog/artwork alignment, real course selection, tactical integration and window lifecycle.");
    }

    private static void ClippingAndHeadings(NauticalChartFrame frame, Action<bool, string> check)
    {
        foreach (int zoom in new[] { 1, 2, 4, 8 })
        foreach (var heading in Enum.GetValues<Heading>())
        {
            var renderer = new NauticalChartRenderer(); ZoomTo(renderer, zoom); renderer.ToggleMode();
            var rotated = frame with { Markers = frame.Markers.Select(m => m.Kind == ChartContactKind.OwnShip ? m with { Heading = heading } : m).ToArray() };
            var values = renderer.Prepare(rotated, 640).Changes.ToDictionary(c => c.Key, c => c.Value);
            int player = TacticalChartProjection.PlaceMarkers(rotated with { Viewport = renderer.Viewport }, 640).Length - 1;
            var from = (ChartPoint)values[$"tac_m{player}_heading0_a"]; var tip = (ChartPoint)values[$"tac_m{player}_heading0_b"];
            var direction = NauticalChartGeometry.HeadingVector(heading);
            check(Near(new ChartPoint((tip.X - from.X) / 8, (tip.Y - from.Y) / 8), direction),
                "All eight tactical heading arrows preserve direction at all four zoom levels.");
        }
        foreach (var mode in Enum.GetValues<SailingChartMode>())
        {
            var renderer = new NauticalChartRenderer(); ZoomTo(renderer, 8);
            if (mode == SailingChartMode.Tactical) renderer.ToggleMode();
            var routeFrame = frame with { ChartCourse = true, Route =
                [new("ocean_01", 380, 320, false), new("ocean_01", 380, 380, false), new("ocean_01", 320, 320, true)] };
            var values = renderer.Prepare(routeFrame, 640).Changes.ToDictionary(c => c.Key, c => c.Value);
            check(Equals(values["chart_r0_on"], true) && Equals(values["chart_r1_on"], false) && Equals(values["chart_r2_on"], true) &&
                Equals(values["chart_w0_on"], false) && Equals(values["chart_w1_on"], false),
                "Routes retain stable segment slots, clip offscreen legs and hide offscreen destinations in both modes.");
            foreach (int i in new[] { 0, 2 })
            foreach (string end in new[] { "a", "b" })
            {
                var p = (ChartPoint)values[$"chart_r{i}_{end}"];
                check(p.X >= .999f && p.X <= 639.001f && p.Y >= .999f && p.Y <= 639.001f,
                    "Zoomed route lines stop inside the chart scissor boundary.");
            }
            int builds = renderer.LayoutBuilds;
            renderer.HandleZoomControl("chart_zoom_out");
            check(renderer.Prepare(routeFrame, 640).Layout == null && renderer.LayoutBuilds == builds,
                "Changing between local zoom levels reuses artwork and fixed route drawing slots.");
        }
    }

    private static void Layout(Action<bool, string> check)
    {
        Dictionary<Type, object> services = new()
        {
            [typeof(Anvil.Services.EventService)] = new Anvil.Services.EventService([]),
            [typeof(ShipPersistenceOptions)] = new ShipPersistenceOptions { Enabled = false, Backend = SailingPersistenceBackend.Disabled },
            [typeof(SailingPersistenceContextFactory)] = SailingPersistenceContextFactory.ForOptions(
                new DbContextOptionsBuilder<SailingPersistenceDbContext>().UseNpgsql("Host=127.0.0.1;Port=1;Database=unused;Username=unused;Password=unused").Options)
        };
        try
        {
            var service = (SailingNuiService)StartupChecks.CreateResolver(services)(typeof(SailingNuiService));
            foreach (float size in new[] { 416f, 640f, 800f })
            {
                var root = (Anvil.API.NuiColumn)typeof(SailingNuiService).GetMethod("CreateLayout",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(service, [Ship("layout"), size])!;
                var header = (Anvil.API.NuiRow)root.Children[0]; var controls = (Anvil.API.NuiRow)header.Children[0];
                check(controls.Children.Sum(c => c.Width ?? 0) <= controls.Width && controls.Height == 28 &&
                    controls.Children.OfType<Anvil.API.NuiButton>().Select(b => b.Id).SequenceEqual(["chart_zoom_out", "chart_zoom_in", "chart_zoom_reset"]),
                    "Visible zoom controls and indicator fit the existing fixed header at minimum and maximum sizes.");
                check(NauticalChartRecoveryChecks.Elements(root).OfType<Anvil.API.NuiGroup>().Count(g => g.Scrollbars != Anvil.API.NuiScrollbars.None) == 1,
                    "Universal zoom adds no nested or horizontal scrolling surface.");
            }
        }
        finally { foreach (var d in services.Values.OfType<IDisposable>().Reverse()) d.Dispose(); }
    }
}

static class OverlayChecks
{
    private static bool Near(float a, float b) => Math.Abs(a - b) < .003f;
    private static bool OnLeg(ChartPoint p, ChartRouteSegment leg)
    {
        double dx = leg.To.X - leg.From.X, dy = leg.To.Y - leg.From.Y;
        double length = Math.Sqrt(dx * dx + dy * dy);
        if (length < .0001) return Near(p.X, leg.From.X) && Near(p.Y, leg.From.Y);
        double t = ((p.X - leg.From.X) * dx + (p.Y - leg.From.Y) * dy) / (length * length);
        return t >= -.0001 && t <= 1.0001 && Math.Abs((p.X - leg.From.X) * dy - (p.Y - leg.From.Y) * dx) / length < .003;
    }
    private static ShipState Ship(string id, float x = 320, float y = 320) => new()
    { ShipId = id, ShipName = id, AreaResRef = "ocean_01", DeckAreaResRef = "deck", X = x, Y = y, HelmsmanPCKey = "captain" };

    public static async Task Run(Action<bool, string> check)
    {
        var areas = new SailingAreaService(); var obstacles = new ShipObstacleService();
        var navigation = new ShipNavigationService(obstacles, areas); var registry = new ShipRegistryService();
        var encounters = new ShipEncounterService(); using var persistence = new ShipStatePersistenceService(null!);
        var combat = new ShipCombatService(registry, encounters, persistence, null!);
        var own = Ship("overlay-own"); var target = Ship("overlay-target", 320, 325); target.ShipType = ShipType.Pirate;
        registry.Register(own); registry.Register(target); encounters.CheckEncounters([own, target]);
        NauticalChartFrame Capture(bool authorized = true, bool selected = true, bool known = true)
        {
            var f = NauticalChartProjection.Capture(own, navigation, new ShipVisibilityService().GetVisibleShips(own, [own, target]),
                [], [], [], [], (_, _) => known, selected ? target.ShipId : null);
            return f with { Tactical = TacticalChartProjection.Capture(f, own, selected ? target : null, combat, authorized) };
        }
        Dictionary<string, object> Values(NauticalChartFrame f)
        { var v = new Dictionary<string, object>(); TacticalChartRenderer.Values(f, 640, v); return v; }
        bool Orange(Dictionary<string, object> v, int i) => Equals(v[$"tac_sector{i}_color"], ChartOverlayDrawing.Target);
        bool Slate(Dictionary<string, object> v) => Equals(v["tac_range_color"], ChartOverlayDrawing.Unavailable) &&
            Equals(v["tac_sector0_color"], ChartOverlayDrawing.Unavailable) && Equals(v["tac_sector1_color"], ChartOverlayDrawing.Unavailable);

        check(Capture().Tactical is { WeaponsAvailable: true, AttackStatus: "READY" } && Orange(Values(Capture()), 0) && !Orange(Values(Capture()), 1),
            "A domain-valid selected port target highlights only its actual sector orange-red.");
        check(Equals(Values(Capture(selected: false))["tac_sector0_color"], ChartOverlayDrawing.Available) &&
            !Orange(Values(Capture(selected: false)), 0), "Available untargeted sectors are ice blue, without changing target acquisition.");
        target.Y = 315; check(Orange(Values(Capture()), 1) && !Orange(Values(Capture()), 0), "A valid starboard target highlights the opposite sector.");
        target.X = 325; target.Y = 320;
        check(Capture().Tactical is { InArc: false } && !Orange(Values(Capture()), 0) && !Orange(Values(Capture()), 1), "Out-of-arc targets cannot create a valid targeting tint.");
        target.X = 320; target.Y = 331;
        check(Capture().Tactical is { InRange: false } && !Orange(Values(Capture()), 0), "Out-of-range targets cannot create a valid targeting tint.");
        target.Y = 325; target.Hull = 0;
        check(Capture().Tactical!.AttackStatus == "TARGET DISABLED" && !Orange(Values(Capture()), 0), "Disabled targets never look attackable.");
        target.Hull = 100;
        check(Slate(Values(Capture(authorized: false))), "Denied weapon authorization renders sectors slate.");
        own.Hull = 0; check(Slate(Values(Capture())), "Disabled attacker hull renders sectors slate."); own.Hull = 100;
        check(Capture(known: false).Tactical!.TargetId == null && !Orange(Values(Capture(known: false)), 0), "Fog-hidden selected ships cannot leak orange target sectors.");

        foreach (Heading heading in Enum.GetValues<Heading>())
        foreach (WeaponArc arc in Enum.GetValues<WeaponArc>())
        foreach (int zoom in new[] { 1, 2, 4, 8 })
        {
            own.Heading = heading;
            var f = Capture(); var camera = ChartViewport.Create(640, zoom, own.X, own.Y);
            f = f with { Viewport = camera, Tactical = f.Tactical! with { Weapon = new ShipWeapon { Damage = 1, MaxRange = 10, Arc = arc } } };
            var sectors = ShipActionRules.FiringSectors(arc, heading); var lines = TacticalChartProjection.WeaponLines(f, 640);
            for (int i = 0; i < 2; i++)
            {
                var points = TacticalChartProjection.SectorPoints(f, 640, i);
                if (i >= sectors.Length || sectors[i].End == sectors[i].Start)
                { check(points.Count == 0, "Absent/degenerate sectors do not invent filled areas."); continue; }
                int slot = TacticalChartProjection.RangeSegments + i * (TacticalChartProjection.SectorSegments + 2);
                check(points.Count == (TacticalChartProjection.SectorSegments + 3) * 2 &&
                    Near(points[0], camera.Project(own.X, own.Y).X) && Near(points[1], camera.Project(own.X, own.Y).Y) &&
                    Near(points[2], lines[slot].To.X) && Near(points[3], lines[slot].To.Y) &&
                    Near(points[^4], lines[slot + 1].To.X) && Near(points[^3], lines[slot + 1].To.Y),
                    "All headings, firing arcs and zoom levels fill exactly the existing boundary geometry.");
            }
        }
        own.Heading = Heading.East;
        foreach (var p in new[] { new ChartPoint(0, 0), new(640, 0), new(0, 640), new(640, 640) })
        {
            own.X = p.X; own.Y = p.Y;
            var f = Capture() with { Viewport = ChartViewport.Create(640, 8, p.X, p.Y) };
            for (int i = 0; i < 2; i++)
            {
                var points = TacticalChartProjection.SectorPoints(f, 640, i);
                check(points.All(v => v >= .999f && v <= 639.001f), "Filled sectors clip inside every ocean corner.");
            }
        }
        own.X = own.Y = 320;

        var cadence = NauticalChartRenderer.CourseDashes(new(0, 100), [new("ocean_01", 32, 100, true)], "ocean_01");
        check(cadence.Length == 4 && cadence.Select(d => d.To.X).SequenceEqual([8f, 16f, 24f, 32f]) &&
            cadence.All(d => d.To.X - d.From.X == NauticalChartRenderer.CourseDashLength), "Navigation dashes use fixed world-space spacing and length.");
        check(cadence.SequenceEqual(NauticalChartRenderer.CourseDashes(new(1, 100), [new("ocean_01", 32, 100, true)], "ocean_01")),
            "Waypoint-anchored dash positions do not slide when the ship advances along a leg.");
        var corner = NauticalChartRenderer.CourseDashes(new(0, 0), [new("ocean_01", 7, 0, false), new("ocean_01", 7, 1, true)], "ocean_01");
        check(corner.Length == 2 && corner[0].To == new ChartPoint(7, 0) && corner[1].From == corner[0].To &&
            corner[1].To == new ChartPoint(7, 1), "A dash spanning a waypoint bends along both actual legs instead of cutting the corner.");

        obstacles.RegisterObstacle(new() { Name = "overlay island", AreaResRef = "ocean_01", MinX = 333, MaxX = 337, MinY = 317, MaxY = 323 });
        var planner = new ShipRoutePlannerService(areas); var planned = planner.BuildChartRoute(own, new(500, 320), obstacles)!;
        check(planned != null && planned.Waypoints.Count > 1, "The actual chart planner supplies a multi-leg detour around blocked terrain.");
        navigation.SetRoute(own, planned!);
        var routeFrame = Capture(); var origin = new ChartPoint(own.X, own.Y); var legs = new List<ChartRouteSegment>();
        foreach (var point in routeFrame.Route) { var next = new ChartPoint(point.X, point.Y); legs.Add(new(origin, next)); origin = next; }
        var actual = NauticalChartRenderer.CourseDashes(new(own.X, own.Y), routeFrame.Route, own.AreaResRef);
        check(actual.Length > 0 && actual.All(d => legs.Any(l => OnLeg(d.From, l) && OnLeg(d.To, l))) &&
            actual.All(d => obstacles.IsSegmentClear(own.AreaResRef, d.From.X, d.From.Y, d.To.X, d.To.Y)),
            "Every visible course dash follows an actual production waypoint leg and avoids the planner's island detour.");
        string snapshot = System.Text.Json.JsonSerializer.Serialize(ShipSnapshotCodec.Capture(own, planned));
        foreach (int zoom in new[] { 1, 2, 4, 8 })
        foreach (var mode in Enum.GetValues<SailingChartMode>())
        {
            var renderer = new NauticalChartRenderer();
            while (renderer.Zoom < zoom) renderer.HandleZoomControl("chart_zoom_in");
            if (mode == SailingChartMode.Tactical) renderer.ToggleMode();
            var update = renderer.Prepare(routeFrame, 640); var v = update.Changes.ToDictionary(c => c.Key, c => c.Value);
            var drawings = update.Layout!.DrawList!;
            var strokes = drawings.OfType<Anvil.API.NuiDrawListLine>().Where(l => l.PointA is Anvil.API.NuiBind<Anvil.API.NuiVector> b && b.Key.StartsWith("chart_route")).ToArray();
            check(strokes.Length > 0 && strokes.Length % 2 == 0 && strokes.Any(l => l.Color is Anvil.API.NuiValue<Anvil.API.Color> c && c.Value.Equals(ChartOverlayDrawing.Course)) &&
                strokes.Any(l => l.Color is Anvil.API.NuiValue<Anvil.API.Color> c && c.Value.Equals(ChartOverlayDrawing.Backing)), "Both modes render saturated red course strokes with dark backing at every zoom.");
            foreach (var on in v.Where(p => p.Key.StartsWith("chart_route") && p.Key.EndsWith("_on") && Equals(p.Value, true)))
            {
                string key = on.Key[..^3]; var a = (ChartPoint)v[key + "_a"]; var b = (ChartPoint)v[key + "_b"];
                check(renderer.Viewport!.Value.TryUnproject(a.X, a.Y, out var wa) && renderer.Viewport.Value.TryUnproject(b.X, b.Y, out var wb) &&
                    legs.Any(l => OnLeg(wa, l) && OnLeg(wb, l)), "Native dash endpoint bindings use the same viewport as ships and actual waypoint legs.");
            }
            if (mode == SailingChartMode.Tactical)
            {
                int fill = drawings.FindIndex(d => d is ChartBoundPolyLine p && p.Points.Key.StartsWith("tac_sector"));
                int course = drawings.FindIndex(d => d is Anvil.API.NuiDrawListLine l && l.PointA is Anvil.API.NuiBind<Anvil.API.NuiVector> a && a.Key.StartsWith("chart_route"));
                int ownMarker = drawings.FindLastIndex(d => d is Anvil.API.NuiDrawListCircle c && c.Rect is Anvil.API.NuiBind<Anvil.API.NuiRect> r && r.Key.StartsWith("tac_m"));
                check(fill >= 0 && fill < course && course < ownMarker && drawings.All(d => d.Order == Anvil.API.NuiDrawListItemOrder.After),
                    "Subtle combat fills stay below the chart course and foreground ships/selection indicators.");
                check(drawings.OfType<Anvil.API.NuiDrawListLine>().Any(l => l.Color is Anvil.API.NuiBind<Anvil.API.Color> c && c.Key.StartsWith("tac_sector") &&
                    l.LineThickness is Anvil.API.NuiValue<float> { Value: > 2 }), "Combat boundaries have thicker state-bound strokes over shared dark backing.");
            }
            int geometry = renderer.CourseGeometryBuilds, builds = renderer.LayoutBuilds;
            check(renderer.Prepare(routeFrame, 640) is { Layout: null, Changes.Count: 0 } && renderer.CourseGeometryBuilds == geometry,
                "Identical frames reuse course geometry and every overlay binding.");
            var recolored = renderer.Prepare(routeFrame with { Tactical = routeFrame.Tactical! with { WeaponsAvailable = false } }, 640);
            check(recolored.Layout == null && renderer.LayoutBuilds == builds && renderer.CourseGeometryBuilds == geometry,
                "Combat availability changes do not rebuild navigation geometry or static chart artwork.");
        }
        check(snapshot == System.Text.Json.JsonSerializer.Serialize(ShipSnapshotCodec.Capture(own, planned)) && ReferenceEquals(planned, navigation.GetRoute(own)),
            "Both presentation overlays preserve actual navigation, weapon, hull and persistence state.");

        var lifecycle = new NauticalChartRenderer(); lifecycle.ToggleMode(); lifecycle.Prepare(Capture(), 640);
        navigation.SetRoute(own, planned!);
        check(lifecycle.Prepare(Capture(), 640) is { Layout: null, Changes.Count: 0 }, "Repeated activation does not duplicate course or combat drawings.");
        own.X = planned!.Waypoints[0].X; own.Y = planned.Waypoints[0].Y; navigation.AdvanceWaypoint(own);
        var progressed = Capture();
        check(progressed.Route.Length == planned.Waypoints.Count - 1 && lifecycle.Prepare(progressed, 640).Layout != null,
            "Advancing the actual waypoint removes completed route segments.");
        while (!planned.IsComplete) navigation.AdvanceWaypoint(own);
        var completed = lifecycle.Prepare(Capture(), 640);
        check(completed.Layout != null && !completed.Layout.DrawList!.OfType<Anvil.API.NuiDrawListLine>().Any(l => l.PointA is Anvil.API.NuiBind<Anvil.API.NuiVector> b && b.Key.StartsWith("chart_route")),
            "Completed navigation removes native course drawings.");
        navigation.ClearRoute(own);
        navigation.SetDestination(own, "ocean_01", own.X + 10, own.Y, 0);
        check(Capture().Route.Length == 1, "Legacy direct navigation retains its real destination-vector fallback.");
        lifecycle.Prepare(Capture(), 640); navigation.CancelNavigation(own);
        check(Capture().Route.Length == 0 && lifecycle.Prepare(Capture(), 640).Layout != null && Capture().Tactical!.Weapon.Damage > 0,
            "Cancelling navigation clears its overlay without disabling the independent combat overlay.");
        foreach (string inactive in new[] { "destroyed", "stored", "docking", "despawned" })
        {
            own.Hull = 100; own.IsStored = own.IsDocking = own.IsInitializing = false;
            navigation.SetDestination(own, "ocean_01", own.X + 10, own.Y, 0); lifecycle.Prepare(Capture(), 640);
            if (inactive == "destroyed") own.Hull = 0;
            if (inactive == "stored") own.IsStored = true;
            if (inactive == "docking") own.IsDocking = true;
            if (inactive == "despawned") own.IsInitializing = true;
            var cleared = lifecycle.Prepare(Capture(), 640);
            check(Capture().Route.Length == 0 && cleared.Layout != null && !cleared.Changes.Any(c => c.Key.StartsWith("chart_route") && c.Key.EndsWith("_on") && Equals(c.Value, true)),
                "Inactive source state clears the course snapshot and geometry: " + inactive);
        }
        own.Hull = 100; own.IsInitializing = own.IsStored = own.IsDocking = false; navigation.CancelNavigation(own);
        own.X = own.Y = 320; target.X = 320; target.Y = 325;
        var cutter = new ShipState { ShipId = "overlay-cutter", ShipName = "Cutter", AreaResRef = "ocean_01", DeckAreaResRef = "cutter", X = 320, Y = 320, AllowsWeapons = false };
        registry.Register(cutter);
        var unarmed = NauticalChartProjection.Capture(cutter, navigation, [], [], [], [], [], (_, _) => true);
        unarmed = unarmed with { Tactical = TacticalChartProjection.Capture(unarmed, cutter, null, combat, true) };
        check(!unarmed.Tactical!.WeaponsAvailable && TacticalChartProjection.SectorPoints(unarmed, 640, 0).Count == 0 &&
            !TacticalChartProjection.WeaponLines(unarmed, 640).Any(l => l.Visible), "Unarmed cutters acquire neither filled nor outlined weapon sectors.");
        own.ShipType = ShipType.Pirate; own.HelmsmanPCKey = null;
        navigation.SetDestination(own, "ocean_01", 420, 320, 0);
        check((await combat.TryNpcAttack(own, target)).Success && Capture().Tactical!.AttackStatus == "RELOADING" && Slate(Values(Capture())) &&
            Slate(Values(Capture(selected: false))), "Real combat cooldown makes sectors slate even when no target is selected.");
        var hiddenTarget = Capture(known: false);
        check(hiddenTarget.Tactical!.TargetId == null && Slate(Values(hiddenTarget)), "Cooldown state is preserved without revealing a hidden target.");
        var timer = System.Diagnostics.Stopwatch.StartNew(); var cached = new NauticalChartRenderer(); cached.ToggleMode();
        cached.Prepare(Capture(), 640); int beforeGeometry = cached.CourseGeometryBuilds, beforeWrites = cached.BindWrites;
        var fixedFrame = Capture();
        for (int i = 0; i < 1000; i++) cached.Prepare(fixedFrame, 640);
        timer.Stop();
        check(cached.CourseGeometryBuilds == beforeGeometry && cached.BindWrites == beforeWrites, "One thousand unchanged overlay refreshes create no geometry or native bind churn.");
        Console.WriteLine($"PASS: overlay states, sector fills, real route dashes, cleanup and coexistence; 1,000 cached refreshes: {timer.ElapsedMilliseconds} ms.");
    }
}

#endif
