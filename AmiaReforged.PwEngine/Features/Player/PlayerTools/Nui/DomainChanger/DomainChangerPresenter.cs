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
    private List<(int id, string name)> _availableDomains = new();

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
            Geometry = new NuiRect(50f, 100f, 540f, 300f),
            Resizable = true
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
        Token().SetBindValue(View.ShowError, false);
        Token().SetBindValue(View.ErrorMessage, "");

        if (_player.LoginCreature == null)
        {
            Token().SetBindValue(View.CharacterName, "N/A");
            Token().SetBindValue(View.DeityName, "N/A");
            Token().SetBindValue(View.Domain1Name, "N/A");
            Token().SetBindValue(View.Domain2Name, "N/A");
            Token().SetBindValue(View.DomainOptions, new List<NuiComboEntry> { new NuiComboEntry("No character available", 0) });
            Token().SetBindValue(View.SelectedDomainIndex, 0);
            Token().SetBindValue(View.ChangeButtonsEnabled, false);
            Token().SetBindValue(View.ShowError, true);
            Token().SetBindValue(View.ErrorMessage, "You must be logged in to change domains.");
            return;
        }

        // Check if player is a cleric
        int clericLevels = NWN.Core.NWScript.GetLevelByClass(NWN.Core.NWScript.CLASS_TYPE_CLERIC, _player.LoginCreature);
        if (clericLevels == 0)
        {
            Token().SetBindValue(View.CharacterName, _player.LoginCreature.Name);
            Token().SetBindValue(View.DeityName, "N/A");
            Token().SetBindValue(View.Domain1Name, "N/A");
            Token().SetBindValue(View.Domain2Name, "N/A");
            Token().SetBindValue(View.DomainOptions, new List<NuiComboEntry> { new NuiComboEntry("Not available", 0) });
            Token().SetBindValue(View.SelectedDomainIndex, 0);
            Token().SetBindValue(View.ChangeButtonsEnabled, false);
            Token().SetBindValue(View.ShowError, true);
            Token().SetBindValue(View.ErrorMessage, "You must be a Cleric to change domains.");
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

                Token().SetBindValue(View.CharacterName, _player.LoginCreature.Name);
                string deity = NWN.Core.NWScript.GetDeity(_player.LoginCreature);
                Token().SetBindValue(View.DeityName, deity);

                int domain1 = NWN.Core.NWScript.GetDomain(_player.LoginCreature, 1);
                int domain2 = NWN.Core.NWScript.GetDomain(_player.LoginCreature, 2);
                Token().SetBindValue(View.Domain1Name, GetDomainName(domain1));
                Token().SetBindValue(View.Domain2Name, GetDomainName(domain2));

                Token().SetBindValue(View.DomainOptions, new List<NuiComboEntry> { new NuiComboEntry("Nope", 0) });
                Token().SetBindValue(View.SelectedDomainIndex, 0);
                Token().SetBindValue(View.ChangeButtonsEnabled, false);
                Token().SetBindValue(View.ShowError, true);
                Token().SetBindValue(View.ErrorMessage, $"{remainingDays} days until you can change domains again.");
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
            case "btn_change_domain_1":
                HandleChangeDomain(1);
                break;

            case "btn_change_domain_2":
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

        // Set character and deity info
        Token().SetBindValue(View.CharacterName, _player.LoginCreature.Name);
        Token().SetBindValue(View.DeityName, deity);
        Token().SetBindValue(View.Domain1Name, domain1Name);
        Token().SetBindValue(View.Domain2Name, domain2Name);

        // Get available domains from deity's idol
        NwPlaceable? idol = FindIdol(deity);
        if (idol == null)
        {
            Token().SetBindValue(View.DomainOptions, new List<NuiComboEntry> { new NuiComboEntry($"{deity} has no idol in Amia...", 0) });
            Token().SetBindValue(View.SelectedDomainIndex, 0);
            Token().SetBindValue(View.ChangeButtonsEnabled, false);
            return;
        }

        // Build available domains list
        _availableDomains.Clear();
        for (int i = 1; i <= 6; i++)
        {
            int domainId = NWN.Core.NWScript.GetLocalInt(idol, $"dom_{i}");
            if (domainId > 0)
            {
                _availableDomains.Add((domainId, GetDomainName(domainId)));
            }
        }

        if (_availableDomains.Count == 0)
        {
            Token().SetBindValue(View.DomainOptions, new List<NuiComboEntry> { new NuiComboEntry("No domains available", 0) });
            Token().SetBindValue(View.SelectedDomainIndex, 0);
            Token().SetBindValue(View.ChangeButtonsEnabled, false);
            return;
        }

        // Populate dropdown with domain entries
        List<NuiComboEntry> domainEntries = _availableDomains.Select((d, index) => new NuiComboEntry(d.name, index)).ToList();
        Token().SetBindValue(View.DomainOptions, domainEntries);
        Token().SetBindValue(View.SelectedDomainIndex, 0);
    }

    private void HandleChangeDomain(int domainSlot)
    {
        if (_player.LoginCreature == null) return;

        // Get the selected index from the dropdown
        int selectedIndex = Token().GetBindValue(View.SelectedDomainIndex);

        if (selectedIndex < 0 || selectedIndex >= _availableDomains.Count)
        {
            _player.SendServerMessage("Invalid domain selection.", ColorConstants.Orange);
            return;
        }

        int newDomainId = _availableDomains[selectedIndex].id;
        int currentDomain = NWN.Core.NWScript.GetDomain(_player.LoginCreature, domainSlot);
        int otherDomainSlot = domainSlot == 1 ? 2 : 1;
        int otherDomain = NWN.Core.NWScript.GetDomain(_player.LoginCreature, otherDomainSlot);

        // Check if trying to change to the same domain
        if (newDomainId == currentDomain)
        {
            _player.SendServerMessage($"You already have {GetDomainName(newDomainId)} in that slot!", ColorConstants.Orange);
            return;
        }

        // Check if trying to change to the same domain as the other slot
        if (newDomainId == otherDomain)
        {
            _player.SendServerMessage($"You already have {GetDomainName(otherDomain)} in your other domain slot!", ColorConstants.Red);
            return;
        }

        // Disable buttons during change
        Token().SetBindValue(View.ChangeButtonsEnabled, false);

        // Perform the domain change
        ChangeDomain(_player.LoginCreature, domainSlot, newDomainId);

        // Update timestamp on PC Key
        NwItem? pcKey = _player.LoginCreature.FindItemWithTag("ds_pckey");
        if (pcKey != null)
        {
            int currentTimestamp = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            NWN.Core.NWScript.SetLocalInt(pcKey, "last_domain_change", currentTimestamp);
            _player.SendServerMessage("Domain change timestamp recorded. You can change domains again in 30 days.", ColorConstants.Cyan);
        }

        // Refresh display
        UpdateDomainInfo();

        // Re-enable buttons
        Token().SetBindValue(View.ChangeButtonsEnabled, true);
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
