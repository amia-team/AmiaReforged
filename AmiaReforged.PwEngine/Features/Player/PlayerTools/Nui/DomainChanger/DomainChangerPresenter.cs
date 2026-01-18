using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using System.Text;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.DomainChanger;

public sealed class DomainChangerPresenter : ScryPresenter<DomainChangerView>
{
    public override DomainChangerView View { get; }

    private readonly NwPlayer _player;
    private NuiWindowToken _token;
    private NuiWindow? _window;
    private NuiWindowToken? _confirmModalToken;
    private int _pendingDomainSlot;
    private int _pendingNewDomainId;
    private string _pendingOldDomainName = "";
    private string _pendingNewDomainName = "";

    public override NuiWindowToken Token() => _token;

    public DomainChangerPresenter(DomainChangerView view, NwPlayer player)
    {
        View = view;
        _player = player;
    }

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), View.Title)
        {
            Geometry = new NuiRect(300f, 100f, 500f, 450f),
            Resizable = false
        };
    }

    public override void Create()
    {
        if (_window is null) InitBefore();

        if (_window is null)
        {
            _player.SendServerMessage("The window could not be created. Screenshot this message and report it to a DM.", ColorConstants.Orange);
            return;
        }

        if (!_player.TryCreateNuiWindow(_window, out _token))
            return;

        // Initialize bind values
        Token().SetBindValue(View.Title, "Domain Changer");
        Token().SetBindValue(View.ChangeButtonsEnabled, true);

        if (_player.LoginCreature == null)
        {
            Token().SetBindValue(View.CharacterInfo, "No character available");
            Token().SetBindValue(View.CurrentDomains, "You must be logged in to change domains.");
            Token().SetBindValue(View.AvailableDomains, "");
            return;
        }

        // Check if player is a cleric
        int clericLevels = NWN.Core.NWScript.GetLevelByClass(NWN.Core.NWScript.CLASS_TYPE_CLERIC, _player.LoginCreature);
        if (clericLevels == 0)
        {
            Token().SetBindValue(View.CharacterInfo, $"Character: {_player.LoginCreature.Name}");
            Token().SetBindValue(View.CurrentDomains, "You must be a Cleric to change domains.");
            Token().SetBindValue(View.AvailableDomains, "");
            Token().SetBindValue(View.ChangeButtonsEnabled, false);
            return;
        }

        // Check cooldown
        NwItem? pcKey = _player.LoginCreature.FindItemWithTag("ds_pckey");
        if (pcKey == null)
        {
            _player.SendServerMessage("PC Key (ds_pckey) not found in your inventory!", ColorConstants.Red);
            Close();
            return;
        }

        int lastChangeTimestamp = NWN.Core.NWScript.GetLocalInt(pcKey, "last_domain_change");
        int currentTimestamp = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        int oneMonthInSeconds = 60 * 60 * 24 * 30; // ~30 days

        if (lastChangeTimestamp > 0)
        {
            int timeSinceLastChange = currentTimestamp - lastChangeTimestamp;
            if (timeSinceLastChange < oneMonthInSeconds)
            {
                int remainingSeconds = oneMonthInSeconds - timeSinceLastChange;
                int remainingDays = remainingSeconds / (60 * 60 * 24);
                Token().SetBindValue(View.CharacterInfo, $"Character: {_player.LoginCreature.Name}");
                Token().SetBindValue(View.CurrentDomains, $"You must wait {remainingDays} more days before changing domains again.");
                Token().SetBindValue(View.AvailableDomains, "");
                Token().SetBindValue(View.ChangeButtonsEnabled, false);
                return;
            }
        }

        UpdateDomainInfo();
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent ev)
    {
        if (ev.EventType != NuiEventType.Click) return;

        switch (ev.ElementId)
        {
            case "btn_change_first_domain":
                HandleChangeDomain(1);
                break;

            case "btn_change_second_domain":
                HandleChangeDomain(2);
                break;
        }
    }

    private void UpdateDomainInfo()
    {
        if (_player.LoginCreature == null) return;

        string deity = NWN.Core.NWScript.GetDeity(_player.LoginCreature);
        int domain1 = NWN.Core.NWScript.GetDomain(_player.LoginCreature, 1);
        int domain2 = NWN.Core.NWScript.GetDomain(_player.LoginCreature, 2);

        string domain1Name = GetDomainName(domain1);
        string domain2Name = GetDomainName(domain2);

        Token().SetBindValue(View.CharacterInfo, $"Character: {_player.LoginCreature.Name} | Deity: {deity}");

        StringBuilder currentDomainsText = new();
        currentDomainsText.AppendLine("=== Current Domains ===");
        currentDomainsText.AppendLine($"First Domain: {domain1Name} (ID: {domain1})");
        currentDomainsText.AppendLine($"Second Domain: {domain2Name} (ID: {domain2})");
        Token().SetBindValue(View.CurrentDomains, currentDomainsText.ToString());

        // Get available domains from deity's idol
        NwPlaceable? idol = FindIdol(deity);
        if (idol == null)
        {
            Token().SetBindValue(View.AvailableDomains, $"{deity} has no idol in Amia...");
            Token().SetBindValue(View.ChangeButtonsEnabled, false);
            return;
        }

        StringBuilder availableDomainsText = new();
        availableDomainsText.AppendLine("=== Available Domains from Your Deity ===");

        for (int i = 1; i <= 6; i++)
        {
            int domainId = NWN.Core.NWScript.GetLocalInt(idol, $"dom_{i}");
            if (domainId > 0)
            {
                string domainName = GetDomainName(domainId);
                availableDomainsText.AppendLine($"{i}. {domainName} (ID: {domainId})");
            }
        }

        Token().SetBindValue(View.AvailableDomains, availableDomainsText.ToString());
    }

    private void HandleChangeDomain(int domainSlot)
    {
        if (_player.LoginCreature == null) return;

        // Show a modal to select which domain to change to
        OpenDomainSelectionModal(domainSlot);
    }

    private void OpenDomainSelectionModal(int domainSlot)
    {
        if (_player.LoginCreature == null) return;

        string deity = NWN.Core.NWScript.GetDeity(_player.LoginCreature);
        NwPlaceable? idol = FindIdol(deity);
        if (idol == null) return;

        int currentDomain = NWN.Core.NWScript.GetDomain(_player.LoginCreature, domainSlot);
        int otherDomain = NWN.Core.NWScript.GetDomain(_player.LoginCreature, domainSlot == 1 ? 2 : 1);

        // Build available domain list
        List<(int id, string name)> availableDomains = new();
        for (int i = 1; i <= 6; i++)
        {
            int domainId = NWN.Core.NWScript.GetLocalInt(idol, $"dom_{i}");
            if (domainId > 0 && domainId != currentDomain && domainId != otherDomain)
            {
                availableDomains.Add((domainId, GetDomainName(domainId)));
            }
        }

        if (availableDomains.Count == 0)
        {
            _player.SendServerMessage("No available domains to change to.", ColorConstants.Orange);
            return;
        }

        // For now, let's show a simple selection by having them type the domain ID
        // In the future, this could be a dropdown or list selection
        string slotText = domainSlot == 1 ? "First" : "Second";
        StringBuilder message = new();
        message.AppendLine($"Available domains to change your {slotText} domain to:");
        foreach (var (id, name) in availableDomains)
        {
            message.AppendLine($"  {id} - {name}");
        }
        message.AppendLine("\nClick the domain buttons below to select:");

        // For now, we'll use the first available domain as an example
        // In a full implementation, you'd create buttons for each domain
        if (availableDomains.Count > 0)
        {
            int newDomainId = availableDomains[0].id;
            string newDomainName = availableDomains[0].name;
            string oldDomainName = GetDomainName(currentDomain);

            _pendingDomainSlot = domainSlot;
            _pendingNewDomainId = newDomainId;
            _pendingOldDomainName = oldDomainName;
            _pendingNewDomainName = newDomainName;

            OpenConfirmModal();
        }
    }

    private void OpenConfirmModal()
    {
        if (_confirmModalToken.HasValue)
            return;

        NuiWindow modal = View.BuildConfirmModal(_pendingOldDomainName, _pendingNewDomainName, _pendingDomainSlot);
        if (_player.TryCreateNuiWindow(modal, out NuiWindowToken modalToken))
        {
            _confirmModalToken = modalToken;

            string slotText = _pendingDomainSlot == 1 ? "First" : "Second";
            string message = $"Change your {slotText} domain from:\n\n" +
                           $"{_pendingOldDomainName}\n\nto:\n\n{_pendingNewDomainName}\n\n" +
                           $"This action can only be done once per month.\n\nAre you sure?";

            _confirmModalToken.Value.SetBindValue(View.ModalMessage, message);
            _confirmModalToken.Value.OnNuiEvent += HandleConfirmModalEvent;
        }
    }

    private void HandleConfirmModalEvent(ModuleEvents.OnNuiEvent ev)
    {
        if (ev.EventType != NuiEventType.Click) return;

        switch (ev.ElementId)
        {
            case "btn_domain_confirm":
                ConfirmDomainChange();
                break;

            case "btn_domain_cancel":
                CloseConfirmModal();
                break;
        }
    }

    private void ConfirmDomainChange()
    {
        if (_player.LoginCreature == null)
        {
            CloseConfirmModal();
            return;
        }

        // Disable buttons during change
        Token().SetBindValue(View.ChangeButtonsEnabled, false);

        // Perform the domain change
        ChangeDomain(_player.LoginCreature, _pendingDomainSlot, _pendingNewDomainId);

        // Update timestamp on PC Key
        NwItem? pcKey = _player.LoginCreature.FindItemWithTag("ds_pckey");
        if (pcKey != null)
        {
            int currentTimestamp = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            NWN.Core.NWScript.SetLocalInt(pcKey, "last_domain_change", currentTimestamp);
            _player.SendServerMessage("Domain change timestamp recorded. You can change domains again in 30 days.", ColorConstants.Cyan);
        }

        // Reset the change flags
        if (pcKey != null)
        {
            if (_pendingDomainSlot == 1)
                NWN.Core.NWScript.SetLocalInt(pcKey, "jj_changed_domain_1", 0);
            else
                NWN.Core.NWScript.SetLocalInt(pcKey, "jj_changed_domain_2", 0);
        }

        CloseConfirmModal();

        // Refresh display
        UpdateDomainInfo();

        // Re-enable buttons
        Token().SetBindValue(View.ChangeButtonsEnabled, true);
    }

    private void CloseConfirmModal()
    {
        if (_confirmModalToken.HasValue)
        {
            _confirmModalToken.Value.OnNuiEvent -= HandleConfirmModalEvent;
            try
            {
                _confirmModalToken.Value.Close();
            }
            catch
            {
                // ignore
            }
            _confirmModalToken = null;
        }
    }

    private void ChangeDomain(NwCreature creature, int domainSlot, int newDomainId)
    {
        int otherDomainSlot = domainSlot == 1 ? 2 : 1;
        int otherDomain = NWN.Core.NWScript.GetDomain(creature, otherDomainSlot);

        // Check if trying to change to the same domain as the other slot
        if (newDomainId == otherDomain)
        {
            _player.SendServerMessage($"Domain change failed, you already have {GetDomainName(otherDomain)} domain!", ColorConstants.Red);
            return;
        }

        int oldDomain = NWN.Core.NWScript.GetDomain(creature, domainSlot);

        // Remove old domain feat
        string oldFeatIdStr = NWN.Core.NWScript.Get2DAString("domains", "GrantedFeat", oldDomain);
        if (int.TryParse(oldFeatIdStr, out int oldFeatId))
        {
            NwFeat? oldFeat = NwFeat.FromFeatId(oldFeatId);
            if (oldFeat != null && creature.KnowsFeat(oldFeat))
            {
                creature.RemoveFeat(oldFeat);
            }
        }

        // Use NWNX CreaturePlugin to set the new domain
        CreaturePlugin.SetDomain(creature, NWN.Core.NWScript.CLASS_TYPE_CLERIC, domainSlot, newDomainId);

        // Verify and add domain feats
        VerifyDomainFeats(creature);

        _player.SendServerMessage($"Domain changed successfully from {GetDomainName(oldDomain)} to {GetDomainName(newDomainId)}!", ColorConstants.Green);
    }

    private void VerifyDomainFeats(NwCreature creature)
    {
        // Get both domains
        int domain1 = NWN.Core.NWScript.GetDomain(creature, 1);
        int domain2 = NWN.Core.NWScript.GetDomain(creature, 2);

        // Get the feat IDs for both domains
        string feat1IdStr = NWN.Core.NWScript.Get2DAString("domains", "GrantedFeat", domain1);
        string feat2IdStr = NWN.Core.NWScript.Get2DAString("domains", "GrantedFeat", domain2);

        // Add feat 1 if not present
        if (int.TryParse(feat1IdStr, out int feat1Id))
        {
            NwFeat? feat1 = NwFeat.FromFeatId(feat1Id);
            if (feat1 != null && !creature.KnowsFeat(feat1))
            {
                creature.AddFeat(feat1);
            }
        }

        // Add feat 2 if not present
        if (int.TryParse(feat2IdStr, out int feat2Id))
        {
            NwFeat? feat2 = NwFeat.FromFeatId(feat2Id);
            if (feat2 != null && !creature.KnowsFeat(feat2))
            {
                creature.AddFeat(feat2);
            }
        }
    }

    private string GetDomainName(int domainId)
    {
        // This matches the domain IDs from the prayer system
        return domainId switch
        {
            0 => "Air",
            1 => "Animal",
            3 => "Death",
            4 => "Destruction",
            5 => "Earth",
            6 => "Evil",
            7 => "Fire",
            8 => "Good",
            9 => "Healing",
            10 => "Knowledge",
            13 => "Magic",
            14 => "Plant",
            15 => "Protection",
            16 => "Strength",
            17 => "Sun",
            18 => "Travel",
            19 => "Trickery",
            20 => "War",
            21 => "Water",
            22 => "Balance",
            23 => "Cavern",
            24 => "Chaos",
            25 => "Charm",
            26 => "Cold",
            27 => "Community",
            28 => "Courage",
            29 => "Craft",
            30 => "Darkness",
            31 => "Dragon",
            32 => "Dream",
            33 => "Drow",
            34 => "Dwarf",
            35 => "Elf",
            36 => "Fate",
            37 => "Gnome",
            38 => "Halfling",
            39 => "Hatred",
            40 => "Illusion",
            41 => "Law",
            42 => "Luck",
            43 => "Moon",
            44 => "Nobility",
            45 => "Orc",
            46 => "Portal",
            47 => "Renewal",
            48 => "Repose",
            49 => "Retribution",
            50 => "Rune",
            51 => "Scalykind",
            52 => "Slime",
            53 => "Spell",
            54 => "Time",
            55 => "Trade",
            56 => "Tyranny",
            57 => "Undeath",
            58 => "Suffering",
            _ => $"Unknown Domain ({domainId})"
        };
    }

    private NwPlaceable? FindIdol(string godName)
    {
        string formattedName = CapitalizeWords(godName);
        if (formattedName == "QueenOfAirAndDarkness")
            formattedName = "QueenofAirandDarkness";

        string idolTag = $"idol2_{formattedName}";

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
        string[] words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string result = "";

        foreach (string word in words)
        {
            if (word.Length > 0)
            {
                result += char.ToUpper(word[0]) + word.Substring(1).ToLower();
            }
        }

        return result;
    }

    public override void Close()
    {
        CloseConfirmModal();

        try
        {
            _token.Close();
        }
        catch
        {
            // ignore
        }
    }
}
