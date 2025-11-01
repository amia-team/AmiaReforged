using System.Collections.Generic;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;

/// <summary>
/// Supplies enriched persona projections for feature workflows.
/// </summary>
public interface IPersonaDescriptorService
{
    /// <summary>
    /// Resolves a single persona descriptor or throws when the persona does not exist.
    /// </summary>
    PersonaDescriptor Describe(PersonaId personaId);

    /// <summary>
    /// Attempts to resolve a persona descriptor.
    /// </summary>
    bool TryDescribe(PersonaId personaId, out PersonaDescriptor? descriptor);

    /// <summary>
    /// Resolves descriptors for multiple personas, skipping those that cannot be found.
    /// </summary>
    IReadOnlyList<PersonaDescriptor> DescribeMany(IEnumerable<PersonaId> personaIds);
}
