using Anvil.API;
using AmiaReforged.PwEngine.Features.AI.Core.Models;

namespace AmiaReforged.PwEngine.Features.AI.Core.Interfaces;

/// <summary>
/// Interface for resolving creature type/classification.
/// Ported from GetCreatureType() in inc_ds_ondeath.nss.
/// </summary>
public interface ICreatureClassifier
{
    /// <summary>
    /// Classifies a game object as a specific creature type.
    /// </summary>
    /// <param name="gameObject">The object to classify.</param>
    /// <returns>The creature classification.</returns>
    CreatureClassification Classify(NwGameObject gameObject);
}
