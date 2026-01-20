using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using NLog;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.CharacterArchive;

/// <summary>
/// Scry-compliant presenter for the Character Archive system.
/// Manages a single player's archive window with dynamic binding updates.
/// </summary>
public class CharacterArchiveScryPresenter : ScryPresenter<CharacterArchiveView>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly NwPlayer _player;
    private readonly CharacterArchiveService _service;
    private NuiWindowToken? _token;
    private List<CharacterFileInfo> _characters = new();
    private bool _showingVault = true;
    private int _currentPage = 0;
    private const int CharactersPerPage = 10;

    public CharacterArchiveScryPresenter(NwPlayer player, CharacterArchiveService service)
    {
        _player = player;
        _service = service;
        View = new CharacterArchiveView();
    }

    public override CharacterArchiveView View { get; }
    public override NuiWindowToken Token() => _token ?? default(NuiWindowToken);

    public override void InitBefore()
    {
        // Load vault characters initially
        _showingVault = true;
        _characters = _service.GetVaultCharacters(_player.CDKey);
    }

    public override void Create()
    {
        NuiWindow window = new NuiWindow(View.RootLayout(), View.WindowTitle)
        {
            Geometry = new NuiRect(CharacterArchiveView.WindowPosX, CharacterArchiveView.WindowPosY,
                                   CharacterArchiveView.WindowWidth, CharacterArchiveView.WindowHeight),
            Resizable = true,
            Closable = true,
            Collapsed = false
        };

        if (!_player.TryCreateNuiWindow(window, out NuiWindowToken token))
        {
            _player.SendServerMessage("Failed to open character archive window.");
            Log.Error($"Failed to create NUI window for {_player.PlayerName}");
            return;
        }

        _token = token;

        // Set initial bindings
        UpdateBindings();

        Log.Info($"Opened character archive window for {_player.PlayerName} (CDKey: {_player.CDKey})");
    }

    private void UpdateBindings()
    {
        if (!_token.HasValue) return;

        string title = _showingVault ? "Character Vault" : "Character Archive";
        int totalPages = (_characters.Count + CharactersPerPage - 1) / CharactersPerPage;
        if (totalPages == 0) totalPages = 1;

        string infoText = _showingVault
            ? $"Characters in Vault: {_characters.Count}"
            : $"Characters in Archive: {_characters.Count}";
        string buttonLabel = _showingVault ? "Archive" : "Restore";

        _token.Value.SetBindValue(View.WindowTitle, title);
        _token.Value.SetBindValue(View.InfoText, infoText);
        _token.Value.SetBindValue(View.MoveButtonLabel, buttonLabel);
        _token.Value.SetBindValue(View.PageInfo, $"Page {_currentPage + 1} / {totalPages}");
        _token.Value.SetBindValue(View.ShowPrevPage, _currentPage > 0);
        _token.Value.SetBindValue(View.ShowNextPage, (_currentPage + 1) * CharactersPerPage < _characters.Count);

        // Calculate which characters to show on this page
        int startIndex = _currentPage * CharactersPerPage;
        int endIndex = Math.Min(startIndex + CharactersPerPage, _characters.Count);

        // Update individual character row bindings (10 per page)
        for (int i = 0; i < CharactersPerPage; i++)
        {
            int characterIndex = startIndex + i;

            if (characterIndex < endIndex)
            {
                CharacterFileInfo character = _characters[characterIndex];

                // Set character name
                _token.Value.SetBindValue(View.CharacterNames[i], character.CharacterName);

                // Fix portrait resref - if it ends with underscore, append just 's', otherwise append '_s'
                string portraitResRef = character.PortraitResRef.EndsWith("_")
                    ? character.PortraitResRef + "s"
                    : character.PortraitResRef + "_s";

                _token.Value.SetBindValue(View.CharacterPortraits[i], portraitResRef);
                _token.Value.SetBindValue(View.CharacterRowVisible[i], true);

                Log.Debug($"Row {i} (Character {characterIndex}): '{character.CharacterName}' portrait: '{portraitResRef}'");
            }
            else
            {
                // Hide unused rows
                _token.Value.SetBindValue(View.CharacterRowVisible[i], false);
            }
        }

        Log.Debug($"Updated bindings: Page {_currentPage + 1}/{totalPages}, showing {endIndex - startIndex} characters");
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        if (eventData.EventType != NuiEventType.Click) return;

        Log.Debug($"NUI Event: {eventData.ElementId}");

        switch (eventData.ElementId)
        {
            case "btn_show_vault":
                ShowVault();
                break;

            case "btn_show_archive":
                ShowArchive();
                break;

            case "btn_prev_page":
                PreviousPage();
                break;

            case "btn_next_page":
                NextPage();
                break;

            case "btn_close":
                Close();
                break;

            default:
                // Check if it's a move button (format: btn_move_0, btn_move_1, etc.)
                if (eventData.ElementId.StartsWith("btn_move_"))
                {
                    string indexStr = eventData.ElementId.Substring("btn_move_".Length);
                    if (int.TryParse(indexStr, out int rowIndex))
                    {
                        HandleMoveCharacter(rowIndex);
                    }
                }
                break;
        }
    }

    private void PreviousPage()
    {
        if (_currentPage > 0)
        {
            _currentPage--;
            UpdateBindings();
            Log.Debug($"Navigated to previous page: {_currentPage + 1}");
        }
    }

    private void NextPage()
    {
        int maxPage = (_characters.Count + CharactersPerPage - 1) / CharactersPerPage - 1;
        if (_currentPage < maxPage)
        {
            _currentPage++;
            UpdateBindings();
            Log.Debug($"Navigated to next page: {_currentPage + 1}");
        }
    }

    private void ShowVault()
    {
        _showingVault = true;
        _currentPage = 0;
        _characters = _service.GetVaultCharacters(_player.CDKey);
        UpdateBindings();
        Log.Debug($"Showing vault for {_player.PlayerName}: {_characters.Count} characters");
    }

    private void ShowArchive()
    {
        _showingVault = false;
        _currentPage = 0;
        _characters = _service.GetArchiveCharacters(_player.CDKey);
        UpdateBindings();
        Log.Debug($"Showing archive for {_player.PlayerName}: {_characters.Count} characters");
    }

    private void HandleMoveCharacter(int rowIndex)
    {
        // Calculate actual character index from row index and current page
        int characterIndex = (_currentPage * CharactersPerPage) + rowIndex;

        if (characterIndex < 0 || characterIndex >= _characters.Count)
        {
            Log.Warn($"Invalid character index: {characterIndex} (row {rowIndex}, page {_currentPage})");
            return;
        }

        CharacterFileInfo character = _characters[characterIndex];

        // Check if trying to move currently playing character
        string currentCharName = _player.ControlledCreature?.Name ?? "";
        if (_showingVault && character.CharacterName.Equals(currentCharName, StringComparison.OrdinalIgnoreCase))
        {
            _player.SendServerMessage("ERROR: You may not archive the character you are currently playing.");
            Log.Info($"{_player.PlayerName} attempted to archive active character {character.CharacterName}");
            return;
        }

        MoveResult result;
        if (_showingVault)
        {
            result = _service.MoveToArchive(_player.CDKey, character.FileName);
            if (result.Success)
            {
                _player.SendServerMessage($"Moved '{character.CharacterName}' to archive.");
                ShowVault(); // Refresh vault view and reset to page 0
            }
            else
            {
                _player.SendServerMessage($"Failed to archive '{character.CharacterName}': {result.ErrorMessage}");
            }
        }
        else
        {
            result = _service.MoveToVault(_player.CDKey, character.FileName);
            if (result.Success)
            {
                _player.SendServerMessage($"Restored '{character.CharacterName}' to vault.");
                ShowArchive(); // Refresh archive view and reset to page 0
            }
            else
            {
                _player.SendServerMessage($"Failed to restore '{character.CharacterName}': {result.ErrorMessage}");
            }
        }

        Log.Info($"{_player.PlayerName} moved {character.CharacterName} " +
                 $"from {(_showingVault ? "vault to archive" : "archive to vault")}: {result.Success}");
    }

    public override void Close()
    {
        if (_token.HasValue)
        {
            _token.Value.Close();
        }
        Log.Debug($"Closed character archive window for {_player.PlayerName}");
    }
}

