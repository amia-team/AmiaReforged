using AmiaReforged.Classes.Warlock.Constants;
using AmiaReforged.Classes.Warlock.Types;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Warlock.Nui.WarlockPact;

public sealed class WarlockPactPresenter(WarlockPactView pactView, NwPlayer player) : ScryPresenter<WarlockPactView>
{
    public override WarlockPactView View { get; } = pactView;

    public override NuiWindowToken Token() => _token;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    private const string WindowTitle = "Choose Your Pact";

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        if (eventData.EventType == NuiEventType.Click)
            HandleButtonClick(eventData);
    }

    private void HandleButtonClick(ModuleEvents.OnNuiEvent eventData)
    {
        if (eventData.ElementId.StartsWith("pact_"))
        {
            string pactString = eventData.ElementId[5..];
            if (Enum.TryParse(pactString, out PactType pact))
            {
                NwFeat? pactFeat = WarlockPactModel.GetPactFeat(pact);
                if (pactFeat == null)
                {
                    player.SendServerMessage("Could not find pact feat!");
                    return;
                }
                Token().SetBindValue(View.PactIcon!, pactFeat.IconResRef);
                Token().SetBindValue(View.PactLabel, pactFeat.Name.ToString());
                Token().SetBindValue(View.PactText, pactFeat.Description.ToString());
                Token().SetBindValue(View.PactBind, pact);

                Token().SetBindValue(View.IsConfirmViewOpen, true);
                Token().SetBindValue(View.CanConfirm, true);
                return;
            }
        }

        if (eventData.ElementId.StartsWith("spell_"))
        {
            string spellString = eventData.ElementId[6..];
            if (int.TryParse(spellString, out int featId))
            {
                NwFeat? clickedFeat = NwFeat.FromFeatId(featId);
                if (clickedFeat == null) return;

                if (IsPactSpell(clickedFeat.FeatType))
                {
                    Token().SetBindValue(View.PactIcon!, clickedFeat.IconResRef);
                    Token().SetBindValue(View.PactLabel, clickedFeat.Name.ToString());
                    Token().SetBindValue(View.PactText, clickedFeat.Description.ToString());

                    Token().SetBindValue(View.IsConfirmViewOpen, true);
                    Token().SetBindValue(View.CanConfirm, false);
                    return;
                }

                Token().SetBindValue(View.IsConfirmViewOpen, false);
                Token().SetBindValue(View.CanConfirm, false);
                return;
            }
        }

        if (eventData.ElementId == View.ConfirmPactButton.Id)
        {
            ChoosePact();
        }
        else if (eventData.ElementId == View.BackButton.Id)
        {
            Token().SetBindValue(View.IsConfirmViewOpen, false);
        }
    }

    private static bool IsPactSpell(Feat feat)
    {
        foreach (PactType pact in Enum.GetValues<PactType>())
        {
            if (PactFeatMap.GetFeats(pact).Contains(feat))
                return true;
        }

        return false;
    }

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), WindowTitle)
        {
            Geometry = new NuiRect(400f, 400f, 600, 640f)
        };
    }

    public override void Create()
    {
        InitBefore();

        if (_window == null) return;

        player.TryCreateNuiWindow(_window, out _token);

        Token().SetBindValue(View.IsConfirmViewOpen, false);
        Token().SetBindValue(View.CanConfirm, false);
    }

    public override void Close()
    {
        _token.Close();
    }

    private void ChoosePact()
    {
        PactType? selectedPact = Token().GetBindValue(View.PactBind);

        if (selectedPact == null)
        {
            player.SendServerMessage("Could not find pact!");
            return;
        }

        WarlockPactModel.FeatData? pactData = WarlockPactModel.CreatePactData(selectedPact.Value);
        if (pactData == null)
        {
            player.SendServerMessage("Could not find pact data!");
            return;
        }

        NwCreature? warlock = player.ControlledCreature;
        if (warlock == null) return;

        NwFeat? pactBaseFeat = warlock.Feats.FirstOrDefault(f => f.FeatType == WarlockFeat.PactBase);

        if (pactBaseFeat == null)
        {
            player.SendServerMessage("Could not find the feat required to open the selection window!");
            return;
        }

        NwFeat? newPactFeat = WarlockPactModel.GetPactFeat(selectedPact.Value);
        if (newPactFeat == null)
        {
            player.SendServerMessage($"Could not find the pact feat {pactData.Name}!");
            return;
        }

        NwFeat? preKnownPactFeat = null;
        foreach (PactType pact in Enum.GetValues<PactType>())
        {
            if (warlock.KnowsFeat(NwFeat.FromFeatType((Feat)pact)!))
                preKnownPactFeat = NwFeat.FromFeatType((Feat)pact);
        }

        string environment = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE");
        if (preKnownPactFeat != null && environment == "live" && preKnownPactFeat != newPactFeat)
        {
            player.SendServerMessage("You have already selected a pact! You must choose to same one to gain the " +
                                     "updated pact spells for that pact. Changing your pact is only allowed by request."
                                         .AddWarlockColor());
            return;
        }

        foreach (PactType pact in Enum.GetValues<PactType>())
        {
            Feat[] pactFeats = PactFeatMap.GetFeats(pact);
            if (pactFeats.Length == 0) continue;

            NwFeat? currentPactFeat = WarlockPactModel.GetPactFeat(pact);
            if (currentPactFeat != null && warlock.KnowsFeat(currentPactFeat))
            {
                player.SendServerMessage($"{currentPactFeat.Name} removed".AddWarlockColor());
                warlock.RemoveFeat(currentPactFeat);
            }

            foreach (Feat feat in pactFeats)
            {
                NwFeat? existingFeat = warlock.Feats.FirstOrDefault(f => f.FeatType == feat);
                if (existingFeat == null) continue;

                player.SendServerMessage($"{existingFeat.Name} removed".AddWarlockColor());
                warlock.RemoveFeat(existingFeat);
            }
        }


        warlock.AddFeat(newPactFeat, warlock.Level);
        player.SendServerMessage($"{newPactFeat.Name} added".AddWarlockColor());

        Feat[] newPactFeats = PactFeatMap.GetFeats(selectedPact.Value);
        foreach (Feat feat in newPactFeats)
        {
            NwFeat? featToAdd = NwFeat.FromFeatType(feat);
            if (featToAdd == null)
            {
                player.SendServerMessage($"Could not find the pact feat {feat}!");
                continue;
            }
            warlock.AddFeat(featToAdd, warlock.Level);
            player.SendServerMessage($"Pact spell {featToAdd.Name} added".AddWarlockColor());
        }

        RaiseCloseEvent();

        // Allow people to switch the pact feat on test
        if (environment != "live") return;

        warlock.RemoveFeat(pactBaseFeat);
    }
}
