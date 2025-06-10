using System.Text;
using Anvil.API;
using Microsoft.IdentityModel.Tokens;
using NLog;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.BuffRemover;

public class BuffRemoverModel
{
    // This message has been approved by Zoltan.
    private const string ZoltanIsStupidMessage =
        "Well, this is embarrassing. Screenshot this and tell Zoltan he sucks.";

    private readonly Dictionary<string, Effect> _labelDict = new();

    private readonly NwPlayer _player;

    public BuffRemoverModel(NwPlayer player)
    {
        _player = player;
    }

    public List<string> Labels { get; set; } = new();
    public List<Effect> RemovableEffects { get; private set; } = new();

    public List<string> GetEffectLabels()
    {
        List<string> effectLabels = new();
        foreach (Effect effect in RemovableEffects)
        {
            string effectString = EffectString(effect);
            LogManager.GetCurrentClassLogger().Info($"{effectString}");

            if (effectLabels.Any(e => e.StartsWith(effect.Spell?.Name.ToString() ?? string.Empty))) continue;

            effectLabels.Add(effectString);

            _labelDict.TryAdd(effectString, effect);
        }

        return effectLabels.Distinct().ToList();
    }

    private string EffectString(Effect effect)
    {
        StringBuilder labelBuilder = new();

        string spellName = effect.Spell?.Name.ToString() ?? string.Empty;

        if (!spellName.IsNullOrEmpty())
        {
            labelBuilder.Append(spellName);

            return labelBuilder.ToString();
        }

        string effectLabel = effect.EffectType.ToString();
        labelBuilder.Append(effectLabel + ":");

        // foreach (string? param in effect.StringParams)
        // {
        //     if (param.IsNullOrEmpty()) continue;
        //     labelBuilder.Append(param + " ");
        // }

        labelBuilder.Append(effect.IntParams[0]);

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
            return new();
        }

        // First, we want all linked effects...Limit only one linked effect, because we will
        // only need to remove one to remove all linked effects.
        List<Effect> linkedEffects = new();

        IEnumerable<Effect> linkedMagicalEffects = character.ActiveEffects
            .Where(e => e.SubType == EffectSubType.Magical && !e.LinkId.IsNullOrEmpty()).ToList();
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

        Effect effect = _labelDict[Labels[clickArrayIndex]];

        List<Effect> spellEffectList = character.ActiveEffects
            .Where(e => e.Spell.Name.ToString() == effect.Spell.Name.ToString() &&
                        !e.Spell.ToString().IsNullOrEmpty() && e != effect).ToList();

        spellEffectList.ForEach(e => character.RemoveEffect(e));

        character.RemoveEffect(effect);
    }
}