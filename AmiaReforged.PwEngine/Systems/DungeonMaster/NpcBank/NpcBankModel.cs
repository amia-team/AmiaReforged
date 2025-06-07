using AmiaReforged.Core.Models;
using AmiaReforged.Core.Services;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry.GenericWindows;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using Microsoft.IdentityModel.Tokens;
using NWN.Core;
using Action = System.Action;

namespace AmiaReforged.PwEngine.Systems.DungeonMaster.NpcBank;

public class NpcBankModel(NwPlayer player)
{
    [Inject] private Lazy<NpcBankService> BankService { get; init; } = null!;
    [Inject] private Lazy<WindowDirector> WindowDirector { get; init; } = null!;
    private string _searchTerm = string.Empty;

    public delegate void NpcUpdateEventHandler(NpcBankModel sender, EventArgs e);

    public event NpcUpdateEventHandler? NpcUpdate;

    public IEnumerable<Npc> VisibleNpcs { get; private set; } = [];
    public IEnumerable<Npc> Npcs { get; set; } = [];

    public Npc? SelectedNpc { get; set; }

    public void LoadNpcs()
    {
        Npcs = BankService.Value.GetNpcs(player.CDKey).OrderBy(n => n.Id);


        RefreshNpcList();
    }

    private IEnumerable<Npc> GetVisibleNpcs()
    {
        if (_searchTerm.IsNullOrEmpty()) return Npcs;

        return Npcs.ToList().FindAll(n => n.Name.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase))
            .OrderBy(n => n.Id);
    }


    public void RefreshNpcList()
    {
        VisibleNpcs = GetVisibleNpcs();
    }

    public void AddNpc(NwCreature creature, bool publicToAll)
    {
        byte[]? maybeSerialized = creature.Serialize();

        if (maybeSerialized == null)
        {
            player.SendServerMessage("Something went horribly wrong. Could not serialize NPC.");
            return;
        }

        Npc npc = new()
        {
            Name = creature.Name,
            DmCdKey = player.CDKey,
            Serialized = maybeSerialized,
            Public = publicToAll
        };

        BankService.Value.AddNpc(npc);
    }

    public void DeleteNpc(long id)
    {
        BankService.Value.DeleteNpcAsync(id);
    }

    public void SetSearchTerm(string search)
    {
        _searchTerm = search;
    }

    public void PromptSpawn(int eventDataArrayIndex)
    {
        SelectedNpc = VisibleNpcs.ToArray()[eventDataArrayIndex];

        player.EnterTargetMode(ValidateAndSpawn, new()
        {
            CursorType = MouseCursor.Action,
            ValidTargets = ObjectTypes.Tile
        });
    }

    private void ValidateAndSpawn(ModuleEvents.OnPlayerTarget obj)
    {
        if (SelectedNpc == null) return;

        NwCreature? creature = NwCreature.Deserialize(SelectedNpc.Serialized);

        if (creature == null)
        {
            player.SendServerMessage("Failed to spawn NPC.");
            return;
        }

        Location spawnLocation = Location.Create(player.LoginCreature.Area, obj.TargetPosition, 0);

        creature.Clone(spawnLocation);
        creature.Destroy();
    }

    public void PromptAdd()
    {
        player.EnterTargetMode(ValidateAndAdd, new()
        {
            CursorType = MouseCursor.Create,
            ValidTargets = ObjectTypes.Creature
        });

        OnNpcUpdate();
    }

    private void ValidateAndAdd(ModuleEvents.OnPlayerTarget obj)
    {
        if (obj.TargetObject.IsPlayerControlled(out NwPlayer? nwPlayer))
        {
            player.SendServerMessage("Please don't try to clone players to the database", ColorConstants.Red);
            return;
        }

        if (obj.TargetObject is not NwCreature creature)
        {
            player.SendServerMessage("Target must be creature", ColorConstants.Red);
            return;
        }

        AddNpc(creature, false);
        OnNpcUpdate();
    }

    public void PromptForDelete(int eventDataArrayIndex)
    {
        Npc npc = VisibleNpcs.ToArray()[eventDataArrayIndex];

        WindowDirector.Value.OpenPopupWithReaction(
            player,
            "Really delete NPC?",
            "If you delete this NPC, the action is permanent", () =>
            {
                BankService.Value.DeleteNpcAsync(npc.Id);
                OnNpcUpdate();
            }
        );
    }

    protected virtual void OnNpcUpdate()
    {
        NpcUpdate?.Invoke(this, EventArgs.Empty);
    }

    public void TogglePublicSetting(int eventDataArrayIndex)
    {
        Npc npc = VisibleNpcs.ToArray()[eventDataArrayIndex];

        bool isPublic = npc.Public;

        if (isPublic)
        {
            WindowDirector.Value.OpenPopupWithReaction(
                player,
                "This will hide the NPC from other DMs",
                "If you press OK, other DMs can no longer see your NPC", () =>
                {
                    BankService.Value.SetPublic(npc.Id, false);
                    OnNpcUpdate();
                }
            );
        }
        else
        {
            WindowDirector.Value.OpenPopupWithReaction(
                player,
                "This will allow other DMs to see your NPC",
                "If you press OK, other DMs can see your NPC", () =>
                {
                    BankService.Value.SetPublic(npc.Id, true);
                    OnNpcUpdate();
                }
            );
        }
    }
}