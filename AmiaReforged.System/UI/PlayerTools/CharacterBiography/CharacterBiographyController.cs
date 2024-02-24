using AmiaReforged.Core.UserInterface;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.System.UI.PlayerTools.CharacterBiography;

public class CharacterBiographyController : WindowController<CharacterBiographyView>
{
    public override void Init()
    {
        string? characterBio = Token.Player.LoginCreature?.Description;
        Token.SetBindValue(View.CharacterBiography!, characterBio);
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        switch (eventData.EventType)
        {
            case NuiEventType.Click:
                HandleButtonClick(eventData);
                break;
        }
    }

    private void HandleButtonClick(ModuleEvents.OnNuiEvent eventData)
    {
        if (eventData.ElementId == View.SaveButton.Id)
        {
            SaveCharacterBiography();
        }
        else if (eventData.ElementId == View.DiscardButton.Id)
        {
            DiscardCharacterBiography();
        }
    }

    private void SaveCharacterBiography()
    {
        string? characterBio = Token.GetBindValue(View.CharacterBiography);
        
        if (Token.Player.LoginCreature != null && characterBio != null)
        {
            Token.Player.LoginCreature.Description = characterBio;
        }

        Token.Player.ExportCharacter();
        Token.Close();
    }

    private void DiscardCharacterBiography()
    {
        Token.Close();
    }

    protected override void OnClose()
    {
        // Do nothing
    }
}