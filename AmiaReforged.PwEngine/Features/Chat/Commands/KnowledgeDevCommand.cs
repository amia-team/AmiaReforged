using System.Text;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Services;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using Anvil.API;
using Anvil.Services;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.Chat.Commands;

/// <summary>
/// Temporary dev/test command for granting knowledge and managing industry membership.
/// Disabled on the live server via <c>SERVER_MODE</c> environment variable.
/// <list type="bullet">
///   <item><c>./knowledge list</c> — lists all knowledge tags across all industries</item>
///   <item><c>./knowledge mine</c> — lists all knowledge the character currently has</item>
///   <item><c>./knowledge grant &lt;tag&gt;</c> — grants knowledge to the character (bypasses preconditions)</item>
///   <item><c>./knowledge join &lt;industryTag&gt;</c> — joins an industry at Novice rank</item>
///   <item><c>./knowledge leave &lt;industryTag&gt;</c> — leaves an industry</item>
///   <item><c>./knowledge points &lt;amount&gt;</c> — sets knowledge points to the given value</item>
///   <item><c>./knowledge status</c> — shows industry memberships and knowledge points</item>
/// </list>
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class KnowledgeDevCommand : IChatCommand
{
    private readonly IIndustryRepository _industries;
    private readonly ICharacterKnowledgeRepository _characterKnowledge;
    private readonly IIndustryMembershipRepository _memberships;
    private readonly ICharacterStatService _statService;
    private readonly RuntimeCharacterService _characters;
    private readonly bool _isEnabled;

    public KnowledgeDevCommand(
        IIndustryRepository industries,
        ICharacterKnowledgeRepository characterKnowledge,
        IIndustryMembershipRepository memberships,
        ICharacterStatService statService,
        RuntimeCharacterService characters)
    {
        _industries = industries;
        _characterKnowledge = characterKnowledge;
        _memberships = memberships;
        _statService = statService;
        _characters = characters;
        _isEnabled = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE") != "live";
    }

    public string Command => "./knowledge";
    public string Description => "Dev tool — list or grant knowledge (disabled on live)";
    public string AllowedRoles => "All";

    public Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (!_isEnabled)
        {
            caller.SendServerMessage("This command is disabled on the live server.", ColorConstants.Red);
            return Task.CompletedTask;
        }

        if (args.Length == 0)
        {
            SendUsage(caller);
            return Task.CompletedTask;
        }

        string subCommand = args[0].ToLowerInvariant();

        switch (subCommand)
        {
            case "list":
                ListAllKnowledge(caller);
                break;
            case "mine":
                ListMyKnowledge(caller);
                break;
            case "grant" when args.Length >= 2:
                GrantKnowledge(caller, args[1]);
                break;
            case "join" when args.Length >= 2:
                JoinIndustry(caller, args[1]);
                break;
            case "leave" when args.Length >= 2:
                LeaveIndustry(caller, args[1]);
                break;
            case "points" when args.Length >= 2:
                SetKnowledgePoints(caller, args[1]);
                break;
            case "status":
                ShowStatus(caller);
                break;
            default:
                SendUsage(caller);
                break;
        }

        return Task.CompletedTask;
    }

    private void ListAllKnowledge(NwPlayer caller)
    {
        List<Industry> industries = _industries.All();

        if (industries.Count == 0)
        {
            caller.SendServerMessage("No industries are configured.", ColorConstants.Orange);
            return;
        }

        StringBuilder sb = new();
        int totalCount = 0;

        foreach (Industry industry in industries)
        {
            if (industry.Knowledge.Count == 0) continue;

            sb.AppendLine($"[{industry.Name}] ({industry.Tag}):");
            foreach (Knowledge k in industry.Knowledge)
            {
                string effects = k.Effects.Count > 0
                    ? $" → {string.Join(", ", k.Effects.Select(e => $"{e.EffectType}:{e.TargetTag}"))}"
                    : "";
                sb.AppendLine($"  • {k.Tag} — {k.Name} (Lv{(int)k.Level}, {k.PointCost}pts){effects}");
                totalCount++;
            }
        }

        if (totalCount == 0)
        {
            caller.SendServerMessage("No knowledge entries found in any industry.", ColorConstants.Orange);
            return;
        }

        sb.Insert(0, $"All knowledge entries ({totalCount}):\n");
        caller.SendServerMessage(sb.ToString(), ColorConstants.Lime);
    }

    private void ListMyKnowledge(NwPlayer caller)
    {
        if (!_characters.TryGetPlayerKey(caller, out Guid playerKey))
        {
            caller.SendServerMessage("Could not resolve your character ID.", ColorConstants.Red);
            return;
        }

        List<Knowledge> known = _characterKnowledge.GetAllKnowledge(playerKey);

        if (known.Count == 0)
        {
            caller.SendServerMessage("Your character has no knowledge entries.", ColorConstants.Orange);
            return;
        }

        StringBuilder sb = new();
        sb.AppendLine($"Your knowledge ({known.Count}):");
        foreach (Knowledge k in known)
        {
            sb.AppendLine($"  • {k.Tag} — {k.Name}");
        }

        caller.SendServerMessage(sb.ToString(), ColorConstants.Cyan);
    }

    private void GrantKnowledge(NwPlayer caller, string knowledgeTag)
    {
        if (!_characters.TryGetPlayerKey(caller, out Guid playerKey))
        {
            caller.SendServerMessage("Could not resolve your character ID.", ColorConstants.Red);
            return;
        }

        // Find the knowledge definition across all industries
        Industry? foundIndustry = null;
        Knowledge? foundKnowledge = null;

        foreach (Industry industry in _industries.All())
        {
            Knowledge? k = industry.Knowledge.FirstOrDefault(
                k => string.Equals(k.Tag, knowledgeTag, StringComparison.OrdinalIgnoreCase));
            if (k != null)
            {
                foundIndustry = industry;
                foundKnowledge = k;
                break;
            }
        }

        if (foundIndustry == null || foundKnowledge == null)
        {
            caller.SendServerMessage($"Knowledge '{knowledgeTag}' not found in any industry.", ColorConstants.Red);
            return;
        }

        // Check if already known
        if (_characterKnowledge.AlreadyKnows(playerKey, foundKnowledge))
        {
            caller.SendServerMessage($"You already know '{foundKnowledge.Name}'.", ColorConstants.Orange);
            return;
        }

        // Bypass all preconditions — directly grant
        CharacterKnowledge ck = new()
        {
            Id = Guid.NewGuid(),
            IndustryTag = foundIndustry.Tag,
            Definition = foundKnowledge,
            CharacterId = playerKey
        };

        _characterKnowledge.Add(ck);
        _characterKnowledge.SaveChanges();

        StringBuilder sb = new();
        sb.AppendLine($"Granted knowledge: {foundKnowledge.Name} ({foundKnowledge.Tag})");

        if (foundKnowledge.Effects.Count > 0)
        {
            sb.AppendLine("  Effects:");
            foreach (KnowledgeEffect effect in foundKnowledge.Effects)
            {
                sb.AppendLine($"    • {effect.EffectType}: {effect.TargetTag}");
            }
        }

        caller.SendServerMessage(sb.ToString(), ColorConstants.Lime);
    }

    private void JoinIndustry(NwPlayer caller, string industryTag)
    {
        if (!_characters.TryGetPlayerKey(caller, out Guid playerKey))
        {
            caller.SendServerMessage("Could not resolve your character ID.", ColorConstants.Red);
            return;
        }

        Industry? industry = _industries.Get(industryTag);
        if (industry == null)
        {
            caller.SendServerMessage($"Industry '{industryTag}' not found.", ColorConstants.Red);
            return;
        }

        // Check if already a member
        List<IndustryMembership> existing = _memberships.All(playerKey);
        if (existing.Any(m => m.IndustryTag == industryTag))
        {
            caller.SendServerMessage($"You are already a member of '{industry.Name}'.", ColorConstants.Orange);
            return;
        }

        IndustryMembership membership = new()
        {
            Id = Guid.NewGuid(),
            CharacterId = CharacterId.From(playerKey),
            IndustryTag = new IndustryTag(industryTag),
            Level = ProficiencyLevel.Novice,
            CharacterKnowledge = []
        };

        _memberships.Add(membership);
        _memberships.SaveChanges();

        caller.SendServerMessage($"Joined industry '{industry.Name}' ({industryTag}) at Novice rank.", ColorConstants.Lime);
    }

    private void LeaveIndustry(NwPlayer caller, string industryTag)
    {
        if (!_characters.TryGetPlayerKey(caller, out Guid playerKey))
        {
            caller.SendServerMessage("Could not resolve your character ID.", ColorConstants.Red);
            return;
        }

        List<IndustryMembership> memberships = _memberships.All(playerKey);
        IndustryMembership? membership = memberships.FirstOrDefault(
            m => string.Equals(m.IndustryTag, industryTag, StringComparison.OrdinalIgnoreCase));

        if (membership == null)
        {
            caller.SendServerMessage($"You are not a member of industry '{industryTag}'.", ColorConstants.Orange);
            return;
        }

        _memberships.Delete(membership.Id);
        _memberships.SaveChanges();

        caller.SendServerMessage($"Left industry '{industryTag}'.", ColorConstants.Lime);
    }

    private void SetKnowledgePoints(NwPlayer caller, string amountStr)
    {
        if (!_characters.TryGetPlayerKey(caller, out Guid playerKey))
        {
            caller.SendServerMessage("Could not resolve your character ID.", ColorConstants.Red);
            return;
        }

        if (!int.TryParse(amountStr, out int amount) || amount < 0)
        {
            caller.SendServerMessage("Invalid amount. Usage: ./knowledge points <number>", ColorConstants.Red);
            return;
        }

        _statService.UpdateKnowledgePoints(playerKey, amount);
        caller.SendServerMessage($"Knowledge points set to {amount}.", ColorConstants.Lime);
    }

    private void ShowStatus(NwPlayer caller)
    {
        if (!_characters.TryGetPlayerKey(caller, out Guid playerKey))
        {
            caller.SendServerMessage("Could not resolve your character ID.", ColorConstants.Red);
            return;
        }

        int knowledgePoints = _statService.GetKnowledgePoints(playerKey);
        List<IndustryMembership> memberships = _memberships.All(playerKey);
        List<Knowledge> known = _characterKnowledge.GetAllKnowledge(playerKey);

        StringBuilder sb = new();
        sb.AppendLine($"Knowledge Points: {knowledgePoints}");
        sb.AppendLine($"Knowledge Learned: {known.Count}");

        if (memberships.Count == 0)
        {
            sb.AppendLine("Industry Memberships: none");
        }
        else
        {
            sb.AppendLine($"Industry Memberships ({memberships.Count}):");
            foreach (IndustryMembership m in memberships)
            {
                sb.AppendLine($"  • {m.IndustryTag} — {m.Level}");
            }
        }

        caller.SendServerMessage(sb.ToString(), ColorConstants.Cyan);
    }

    private static void SendUsage(NwPlayer caller)
    {
        caller.SendServerMessage(
            "Usage: ./knowledge list | mine | status | grant <tag> | join <industry> | leave <industry> | points <amount>",
            ColorConstants.White);
    }
}
