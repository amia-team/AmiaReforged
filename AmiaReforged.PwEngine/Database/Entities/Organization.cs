using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;

namespace AmiaReforged.PwEngine.Database.Entities;

public class Organization
{
    [Key] public Guid Id { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    /// <summary>
    /// PersonaId for cross-subsystem references (transactions, reputation, ownership).
    /// Format: "Organization:{Id}"
    /// </summary>
    public string? PersonaIdString { get; set; }

    // Strong-typed properties for domain logic
    [NotMapped]
    public OrganizationId OrganizationId => OrganizationId.From(Id);

    [NotMapped]
    public PersonaId PersonaId =>
        PersonaIdString != null
            ? PersonaId.Parse(PersonaIdString)
            : OrganizationId.ToPersonaId();
}
