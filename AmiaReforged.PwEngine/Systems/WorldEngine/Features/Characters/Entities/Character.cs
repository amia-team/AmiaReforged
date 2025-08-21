namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.Characters.Entities;

public enum CharacterStatus
{
    Active = 1,
    Retired = 2
}

public sealed class Character : Entity
{
    public Guid PersonaId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string NameNormalized { get; private set; } = string.Empty;

    public CharacterStatus Status { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime? RetiredUtc { get; private set; }

    private Character(Guid personaId, string name)
    {
        Id = Guid.NewGuid();
        PersonaId = personaId;
        SetName(name);
        Status = CharacterStatus.Active;
        CreatedUtc = DateTime.UtcNow;
        LastUpdated = CreatedUtc;
    }

    public static Character Create(Guid personaId, string name)
    {
        if (personaId == Guid.Empty)
            throw new ArgumentException("PersonaId must not be empty.", nameof(personaId));

        return new Character(personaId, name);
    }

    public void Rename(string newName)
    {
        EnsureActive();
        SetName(newName);
        Touch();
    }

    public void Retire()
    {
        if (Status == CharacterStatus.Retired) return;
        Status = CharacterStatus.Retired;
        RetiredUtc = DateTime.UtcNow;
        Touch();
    }

    public void Reinstate()
    {
        if (Status != CharacterStatus.Retired) return;
        Status = CharacterStatus.Active;
        RetiredUtc = null;
        Touch();
    }

    // Optional: support ownership transfer if your policies allow it
    public void TransferToPersona(Guid newPersonaId)
    {
        if (newPersonaId == Guid.Empty)
            throw new ArgumentException("New PersonaId must not be empty.", nameof(newPersonaId));

        if (newPersonaId == PersonaId) return;

        PersonaId = newPersonaId;
        Touch();
    }

    private void SetName(string name)
    {
        string trimmed = (name ?? string.Empty).Trim();
        if (trimmed.Length == 0)
            throw new ArgumentException("Name cannot be empty.", nameof(name));

        Name = trimmed;
        NameNormalized = NormalizeName(trimmed);
    }

    public static string NormalizeName(string name) => name.Trim().ToUpperInvariant();


    private void EnsureActive()
    {
        if (Status != CharacterStatus.Active)
            throw new InvalidOperationException("Character is not active.");
    }

    private void Touch() => LastUpdated = DateTime.UtcNow;
}
