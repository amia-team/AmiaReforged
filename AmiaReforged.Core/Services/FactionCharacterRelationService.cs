﻿using AmiaReforged.Core.Helpers;
using AmiaReforged.Core.Models;
using AmiaReforged.Core.Models.Faction;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services;

// [ServiceBinding(typeof(FactionCharacterRelationService))]
public class FactionCharacterRelationService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly AmiaDbContext _ctx;
    private readonly NwTaskHelper _nwTaskHelper;
    private readonly CharacterService _characterService;

    public FactionCharacterRelationService(AmiaDbContext ctx, NwTaskHelper nwTaskHelper,
        CharacterService characterService)
    {
        _ctx = ctx;
        _nwTaskHelper = nwTaskHelper;
        _characterService = characterService;
    }

    /// <summary>
    /// Adds a new faction character relation to the database if and only if the character actually exists.
    /// </summary>
    /// <param name="factionCharacterRelation">the new relation service to be added.</param>
    public async Task AddFactionCharacterRelation(FactionCharacterRelation factionCharacterRelation)
    {
        try
        {
            bool characterExists = await _characterService.CharacterExists(factionCharacterRelation.CharacterId);

            if (characterExists) await _ctx.AddAsync(factionCharacterRelation);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error adding faction character relation.");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
    }
}