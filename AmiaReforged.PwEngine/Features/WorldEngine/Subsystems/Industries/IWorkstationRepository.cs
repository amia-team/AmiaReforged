using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;

/// <summary>
/// Repository for managing workstation definitions.
/// Workstations are global — not scoped to a single industry.
/// </summary>
public interface IWorkstationRepository
{
    bool WorkstationExists(string tag);
    Workstation? GetByTag(WorkstationTag tag);
    List<Workstation> All();
    void Add(Workstation workstation);
    void Update(Workstation workstation);
    bool Delete(string tag);
    List<Workstation> Search(string? searchTerm, int page, int pageSize, out int totalCount);
}
