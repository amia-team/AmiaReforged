using System.Text;
using Anvil.API;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using NLog;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.BuffRemover;

public class BuffRemoverModel
{
    // This message has been approved by Zoltan.
    private const string ZoltanIsStupidMessage =
        "Well, this is embarrassing. Screenshot this and tell Zoltan he sucks.";

    public List<string> Labels { get; set; } = new();
    public List<Effect> RemovableEffects { get; private set; } = new();

    private readonly NwPlayer _player;

    public BuffRemoverModel(NwPlayer player)
    {
        _player = player;
    }

    public List<string> GetEffectLabels()
    {
        List<string> labels = new();
        StringBuilder label = new();

        foreach (Effect effect in RemovableEffects)
        {
            label.Append("fx: " + EffectString(effect));
            labels.Add(label.ToString());
            label.Clear();
        }

        return labels;
    }

    private string EffectString(Effect effect)
    {
        StringBuilder labelBuilder = new();

        labelBuilder.Append(effect.EffectType + ":");

        foreach (string? param in effect.StringParams)
        {
            if (param.IsNullOrEmpty()) continue;
            labelBuilder.Append(param + " ");
        }

        foreach (int effectParam in effect.IntParams)
        {
            labelBuilder.Append(effectParam + " ");
        }

        foreach (float effectParam in effect.FloatParams)
        {
            labelBuilder.Append(effectParam + " ");
        }


        return labelBuilder.ToString();
    }

    public void UpdateEffectList()
    {
        Labels.Clear();
        RemovableEffects.Clear();

        RemovableEffects = GetRemovableEffects();

        Labels = GetEffectLabels();
    }

    private List<Effect> GetRemovableEffects()
    {
        NwCreature? character = _player.LoginCreature;
        if (character == null)
        {
            _player.SendServerMessage(ZoltanIsStupidMessage,
                ColorConstants.Red);
            return new List<Effect>();
        }

        // First, we want all linked effects...Limit only one linked effect, because we will
        // only need to remove one to remove all linked effects.
        List<Effect> linkedEffects = new();

        IEnumerable<Effect> linkedMagicalEffects = character.ActiveEffects.Where(e => e.SubType == EffectSubType.Magical && !e.LinkId.IsNullOrEmpty()).ToList();
        LogManager.GetCurrentClassLogger().Info($"Found {linkedMagicalEffects.Count()} linked effects...");
        foreach (Effect active in linkedMagicalEffects)
        {
            LogManager.GetCurrentClassLogger().Info("Link Id:" + active.LinkId);

            if (active.LinkId.IsNullOrEmpty() && linkedEffects.All(e => e.LinkId != active.LinkId))
            {
                LogManager.GetCurrentClassLogger().Info($"Adding a linked effect ({active.EffectType})...");
                linkedEffects.Add(active);
            }
        }

        return character.ActiveEffects.Where(e =>
            e.SubType == EffectSubType.Magical && EffectWhitelist.Whitelist.Contains(e.EffectType) &&
            !e.LinkId.IsNullOrEmpty()).Concat(linkedEffects).ToList();
    }


    public void RemoveAllEffects()
    {
        NwCreature? character = _player.LoginCreature;
        if (character == null)
        {
            _player.SendServerMessage(ZoltanIsStupidMessage,
                ColorConstants.Red);
            return;
        }

        foreach (Effect effect in RemovableEffects)
        {
            character.RemoveEffect(effect);
        }
    }

    public void RemoveEffectAt(int clickArrayIndex)
    {
        NwCreature? character = _player.LoginCreature;
        if (character == null)
        {
            _player.SendServerMessage(ZoltanIsStupidMessage,
                ColorConstants.Red);
            return;
        }

        character.RemoveEffect(RemovableEffects[clickArrayIndex]);
    }
}