using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.CharacterTools.ThousandFaces;

[ServiceBinding(typeof(ThousandFacesListener))]
public class ThousandFacesListener
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    // Feat ID for 1000 Faces (from feat.2da)
    private const int ThousandFacesFeatId = 1369;

    public ThousandFacesListener()
    {
        Log.Info(message: "ThousandFacesListener initialized.");
    }

    /// <summary>
    /// Handles item activation that grants the 1000 Faces feat to the character and deletes the item.
    /// </summary>
    [ScriptHandler("i_100faces_init")]
    public void OnItemActivated(CallInfo callInfo)
    {
        Log.Info("One Thousand Faces grant item script handler triggered");

        uint itemUsed = NWScript.GetItemActivated();
        NwItem? item = itemUsed.ToNwObject<NwItem>();

        uint pcObject = NWScript.GetItemActivator();
        NwCreature? creature = pcObject.ToNwObject<NwCreature>();

        if (creature == null || !creature.IsValid)
        {
            Log.Warn("Creature is null or invalid");
            return;
        }

        NwPlayer? player = creature.ControllingPlayer;
        if (player == null)
        {
            Log.Warn("Player is null");
            return;
        }

        // Check if the creature has at least 13 levels of Druid
        int druidLevel = creature.GetClassInfo(ClassType.Druid)?.Level ?? 0;
        if (druidLevel < 13)
        {
            player.SendServerMessage("You must have at least 13 levels of Druid to use this item!", ColorConstants.Orange);
            return;
        }

        // Check if the creature already has the feat
        if (creature.KnowsFeat((Feat)ThousandFacesFeatId))
        {
            player.SendServerMessage("You already have the One Thousand Faces ability!", ColorConstants.Orange);
            return;
        }

        // Find what character level they gained Druid 13 at
        int levelForFeat = GetLevelWhenDruid13Gained(creature);

        // Grant the feat to the creature at the appropriate level
        NwFeat? feat = NwFeat.FromFeatId(ThousandFacesFeatId);
        if (feat == null)
        {
            Log.Error("Could not find 1000 Faces feat definition");
            player.SendServerMessage("Error: Could not find feat definition.", ColorConstants.Red);
            return;
        }

        creature.AddFeat(feat, levelForFeat);

        Log.Info($"Granted One Thousand Faces feat to player: {player.PlayerName} at level {levelForFeat}");
        player.SendServerMessage($"You have gained the One Thousand Faces ability at level {levelForFeat}! Use it from your radial menu or feat list.", ColorConstants.Lime);

        // Delete the item from inventory
        if (item != null && item.IsValid)
        {
            item.Destroy();
            Log.Info("One Thousand Faces grant item destroyed");
        }
    }

    /// <summary>
    /// Determines what character level the creature gained their 13th Druid level.
    /// </summary>
    private static int GetLevelWhenDruid13Gained(NwCreature creature)
    {
        int druidLevelCount = 0;

        for (int level = 1; level <= creature.Level; level++)
        {
            CreatureLevelInfo levelInfo = creature.GetLevelStats(level);
            if (levelInfo.ClassInfo.Class.ClassType == ClassType.Druid)
            {
                druidLevelCount++;
                if (druidLevelCount == 13)
                {
                    return level;
                }
            }
        }

        // Fallback to current level if we can't determine
        return creature.Level;
    }
}

