using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard.Utilities.GameSettings;

public sealed class PartyAdvertiserPresenter : ScryPresenter<PartyAdvertiserView>
{
    private readonly NwPlayer _player;
    private NuiWindowToken _token;
    private NuiWindow? _window;
    private const string StorageTag = "ds_party_pole";
    private const int MaxSlots = 10;

    // Filter state: 0 = Show All, 1 = RP, 2 = Hunt
    private int _currentFilter;

    // Toggle button state tracking
    private bool _showName = true;
    private bool _showLevel = true;
    private bool _showBuild;
    private bool _showArea;
    private bool _lookingForRp;
    private bool _lookingForHunt;

    // Geometry bind to force window position
    private readonly NuiBind<NuiRect> _geometryBind = new("window_geometry");
    private static readonly NuiRect WindowPosition = new(360f, 100f, 520f, 670f);

    public override PartyAdvertiserView View { get; }
    public override NuiWindowToken Token() => _token;

    public PartyAdvertiserPresenter(PartyAdvertiserView view, NwPlayer player)
    {
        View = view;
        _player = player;
    }

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), null!)
        {
            Geometry = _geometryBind,
            Border = false,
            Transparent = true,
            Resizable = false,
            Closable = false,
            Collapsed = false
        };
    }

    public override void Create()
    {
        if (_window is null)
        {
            _player.SendServerMessage("The party advertiser window could not be created.", ColorConstants.Orange);
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);

        // Force the window position using the bind
        Token().SetBindValue(_geometryBind, WindowPosition);

        // Initialize toggle button labels
        UpdateToggleButtonLabels();
        Token().SetBindValue(View.CustomMessage, "");

        UpdateAdvertisingStatus();
        RefreshPartyList();
    }

    private void UpdateToggleButtonLabels()
    {
        Token().SetBindValue(View.ShowNameLabel, _showName ? "[X] Name" : "[ ] Name");
        Token().SetBindValue(View.ShowLevelLabel, _showLevel ? "[X] Level" : "[ ] Level");
        Token().SetBindValue(View.ShowBuildLabel, _showBuild ? "[X] Class" : "[ ] Class");
        Token().SetBindValue(View.ShowAreaLabel, _showArea ? "[X] Area" : "[ ] Area");
        Token().SetBindValue(View.LookingForRpLabel, _lookingForRp ? "[X] RP" : "[ ] RP");
        Token().SetBindValue(View.LookingForHuntLabel, _lookingForHunt ? "[X] Hunt" : "[ ] Hunt");
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent ev)
    {
        if (ev.EventType == NuiEventType.Click)
        {
            switch (ev.ElementId)
            {
                case "btn_toggle_advertise":
                    HandleToggleAdvertise();
                    break;
                case "btn_refresh":
                    RefreshPartyList();
                    break;
                case "btn_close":
                    Close();
                    break;
                case "btn_filter_all":
                    SetFilter(0);
                    break;
                case "btn_filter_rp":
                    SetFilter(1);
                    break;
                case "btn_filter_hunt":
                    SetFilter(2);
                    break;
                case "btn_toggle_name":
                    _showName = !_showName;
                    UpdateToggleButtonLabels();
                    break;
                case "btn_toggle_level":
                    _showLevel = !_showLevel;
                    UpdateToggleButtonLabels();
                    break;
                case "btn_toggle_build":
                    _showBuild = !_showBuild;
                    UpdateToggleButtonLabels();
                    break;
                case "btn_toggle_area":
                    _showArea = !_showArea;
                    UpdateToggleButtonLabels();
                    break;
                case "btn_toggle_rp":
                    _lookingForRp = !_lookingForRp;
                    UpdateToggleButtonLabels();
                    break;
                case "btn_toggle_hunt":
                    _lookingForHunt = !_lookingForHunt;
                    UpdateToggleButtonLabels();
                    break;
            }
        }
    }

    private void SetFilter(int filter)
    {
        _currentFilter = filter;

        RefreshPartyList();
    }

    private void HandleToggleAdvertise()
    {
        NwCreature? creature = _player.LoginCreature;
        if (creature == null)
        {
            _player.SendServerMessage("Error: Could not find your character.", ColorConstants.Red);
            return;
        }

        // Get or create storage object
        NwWaypoint? storage = NwObject.FindObjectsWithTag<NwWaypoint>(StorageTag).FirstOrDefault();
        if (storage == null)
        {
            _player.SendServerMessage("Party advertiser storage not found. Contact a DM.", ColorConstants.Red);
            return;
        }

        // Check if already in list
        for (int i = 1; i <= MaxSlots; i++)
        {
            NwCreature? listedCreature = storage.GetObjectVariable<LocalVariableObject<NwCreature>>($"ds_lonely_pc_{i}").Value;

            if (listedCreature == creature)
            {
                // Remove from list
                storage.GetObjectVariable<LocalVariableObject<NwCreature>>($"ds_lonely_pc_{i}").Delete();
                storage.GetObjectVariable<LocalVariableString>($"ds_lonely_time_{i}").Delete();
                _player.SendServerMessage("You have been removed from the party people list!", ColorConstants.Cyan);
                UpdateAdvertisingStatus();
                RefreshPartyList();
                return;
            }
        }

        // Not in list - add them
        AddToList(creature, storage);
        UpdateAdvertisingStatus();
        RefreshPartyList();
    }

    private void AddToList(NwCreature creature, NwWaypoint storage)
    {
        int selectedSlot = -1;
        int stalestSlot = -1;
        long stalestTime = long.MaxValue;
        long currentTime = DateTime.UtcNow.Ticks;

        // Find an empty slot or the stalest entry
        for (int i = 1; i <= MaxSlots; i++)
        {
            NwCreature? listedCreature = storage.GetObjectVariable<LocalVariableObject<NwCreature>>($"ds_lonely_pc_{i}").Value;

            // Check if slot is empty or creature no longer valid
            if (listedCreature == null || !listedCreature.IsValid || !listedCreature.IsPlayerControlled)
            {
                storage.GetObjectVariable<LocalVariableObject<NwCreature>>($"ds_lonely_pc_{i}").Delete();
                storage.GetObjectVariable<LocalVariableString>($"ds_lonely_time_{i}").Delete();
                selectedSlot = i;
                break;
            }

            // Track stalest entry
            string addTimeStr = storage.GetObjectVariable<LocalVariableString>($"ds_lonely_time_{i}").Value ?? "0";
            long addTime = long.TryParse(addTimeStr, out long time) ? time : 0;
            if (addTime < stalestTime)
            {
                stalestTime = addTime;
                stalestSlot = i;
            }
        }

        // If no empty slot found, use the stalest one
        if (selectedSlot == -1)
        {
            selectedSlot = stalestSlot;
        }

        // Get custom message and use internal state for display options
        string customMessage = Token().GetBindValue(View.CustomMessage) ?? "";

        // Build the display info string based on selected options (using internal state)
        string displayInfo = BuildDisplayInfo(creature, _showName, _showLevel, _showBuild, _showArea, _lookingForRp, _lookingForHunt, customMessage);

        // Add player to list
        storage.GetObjectVariable<LocalVariableObject<NwCreature>>($"ds_lonely_pc_{selectedSlot}").Value = creature;
        storage.GetObjectVariable<LocalVariableString>($"ds_lonely_time_{selectedSlot}").Value = currentTime.ToString();
        storage.GetObjectVariable<LocalVariableString>($"ds_lonely_info_{selectedSlot}").Value = displayInfo;

        _player.SendServerMessage("You have been added to the party people list!", ColorConstants.Cyan);
    }

    private string BuildDisplayInfo(NwCreature creature, bool showName, bool showLevel, bool showBuild, bool showArea, bool lookingForRp, bool lookingForHunt, string customMessage)
    {
        List<string> parts = new();

        if (showName)
        {
            parts.Add(creature.Name);
        }

        if (showLevel)
        {
            parts.Add($"Lvl {creature.Level}");
        }

        if (showBuild)
        {
            // Get the full class build using short names
            string classBuild = GetClassBuild(creature);
            if (!string.IsNullOrEmpty(classBuild))
            {
                parts.Add($"({classBuild})");
            }
        }

        if (showArea && creature.Area != null)
        {
            parts.Add($"Area: {creature.Area.Name}");
        }

        List<string> looking = new();
        if (lookingForRp) looking.Add("RP");
        if (lookingForHunt) looking.Add("Hunt");
        if (looking.Count > 0)
        {
            parts.Add($"Wants: {string.Join("/", looking)}");
        }

        string result = string.Join(" | ", parts);

        // Add custom message if present
        if (!string.IsNullOrWhiteSpace(customMessage))
        {
            result = string.IsNullOrEmpty(result) ? customMessage : $"{result}\n  \"{customMessage}\"";
        }

        return result;
    }

    private string GetClassBuild(NwCreature creature)
    {
        List<string> classShorts = new();

        // Get the classes 2da table
        var classesTable = NwGameTables.GetTable("classes");

        foreach (var classInfo in creature.Classes)
        {
            if (classInfo.Level > 0)
            {
                string shortName = "";

                // Try to get short name from 2da - the Short column contains TLK StrRef numbers
                if (classesTable != null)
                {
                    int classId = (int)classInfo.Class.ClassType;
                    string? strRefStr = classesTable.GetString(classId, "Short");
                    if (!string.IsNullOrEmpty(strRefStr) && int.TryParse(strRefStr, out int strRefInt))
                    {
                        // Use NWScript to resolve the TLK string reference
                        shortName = NWScript.GetStringByStrRef(strRefInt);
                    }
                }

                // Fallback to first 3 characters of class name if no short name found
                if (string.IsNullOrEmpty(shortName))
                {
                    string className = classInfo.Class.Name.ToString();
                    shortName = className.Substring(0, Math.Min(3, className.Length));
                }

                classShorts.Add(shortName);
            }
        }

        return string.Join("/", classShorts);
    }


    private void UpdateAdvertisingStatus()
    {
        NwCreature? creature = _player.LoginCreature;
        if (creature == null) return;

        NwWaypoint? storage = NwObject.FindObjectsWithTag<NwWaypoint>(StorageTag).FirstOrDefault();
        if (storage == null) return;

        // Check if player is in the list
        bool isAdvertising = false;
        for (int i = 1; i <= MaxSlots; i++)
        {
            NwCreature? listedCreature = storage.GetObjectVariable<LocalVariableObject<NwCreature>>($"ds_lonely_pc_{i}").Value;
            if (listedCreature == creature)
            {
                isAdvertising = true;
                break;
            }
        }

        Token().SetBindValue(View.IsAdvertising, isAdvertising);
        Token().SetBindValue(View.AdvertiseButtonLabel, isAdvertising ? "Remove from List" : "Add to List");
    }

    private void RefreshPartyList()
    {
        NwWaypoint? storage = NwObject.FindObjectsWithTag<NwWaypoint>(StorageTag).FirstOrDefault();
        if (storage == null)
        {
            Token().SetBindValue(View.PartyListText, "Party advertiser storage not found.");
            return;
        }

        long currentTime = DateTime.UtcNow.Ticks;
        List<string> partyList = new();

        for (int i = 1; i <= MaxSlots; i++)
        {
            NwCreature? listedCreature = storage.GetObjectVariable<LocalVariableObject<NwCreature>>($"ds_lonely_pc_{i}").Value;

            if (listedCreature != null && listedCreature.IsValid && listedCreature.IsPlayerControlled)
            {
                string addTimeStr = storage.GetObjectVariable<LocalVariableString>($"ds_lonely_time_{i}").Value ?? "0";
                long addTime = long.TryParse(addTimeStr, out long time) ? time : 0;
                long ticksDiff = currentTime - addTime;
                int minutesAgo = (int)(ticksDiff / TimeSpan.TicksPerMinute);

                // Get saved display info, or fall back to basic info
                string displayInfo = storage.GetObjectVariable<LocalVariableString>($"ds_lonely_info_{i}").Value ?? "";
                if (string.IsNullOrEmpty(displayInfo))
                {
                    // Fallback for entries without saved info
                    displayInfo = $"{listedCreature.Name} (Lvl {listedCreature.Level})";
                }

                // Apply filter
                bool includeEntry = _currentFilter switch
                {
                    1 => displayInfo.Contains("Wants:") && displayInfo.Contains("RP"), // RP filter
                    2 => displayInfo.Contains("Wants:") && displayInfo.Contains("Hunt"), // Hunt filter
                    _ => true // Show All
                };

                if (includeEntry)
                {
                    partyList.Add($"{displayInfo} ({minutesAgo} minutes ago)");
                }
            }
            else if (listedCreature != null)
            {
                // Clean up invalid entries
                storage.GetObjectVariable<LocalVariableObject<NwCreature>>($"ds_lonely_pc_{i}").Delete();
                storage.GetObjectVariable<LocalVariableString>($"ds_lonely_time_{i}").Delete();
                storage.GetObjectVariable<LocalVariableString>($"ds_lonely_info_{i}").Delete();
            }
        }

        if (partyList.Count == 0)
        {
            string filterText = _currentFilter switch
            {
                1 => "No one is currently looking for RP.",
                2 => "No one is currently looking to Hunt.",
                _ => "No one is currently looking for a party."
            };
            Token().SetBindValue(View.PartyListText, filterText);
        }
        else
        {
            Token().SetBindValue(View.PartyListText, string.Join("\n\n", partyList));
        }
    }

    public override void UpdateView()
    {
        // No dynamic updates needed
    }

    public override void Close()
    {
        // Don't call RaiseCloseEvent() here - it causes infinite recursion
        // The WindowDirector handles cleanup when CloseWindow() is called
        _token.Close();
    }
}
