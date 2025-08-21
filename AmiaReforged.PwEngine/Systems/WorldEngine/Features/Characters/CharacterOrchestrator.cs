using AmiaReforged.PwEngine.Systems.WorldEngine.Features.Characters.Entities;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.Characters;

[ServiceBinding(typeof(CharacterOrchestrator))]
public class CharacterOrchestrator(ICharacterRepository characters)
{
      public async Task<Character> CreateAsync(Guid personaId, string name, CancellationToken ct = default)
    {
        Character character = Character.Create(personaId, name);
        await characters.AddAsync(character, ct);
        return character;
    }

    public async Task RenameAsync(Character character, string newName, CancellationToken ct = default)
    {
        string normalized = Character.NormalizeName(newName);

        character.Rename(newName);

        await characters.UpdateAsync(character, ct);
    }

    public async Task RetireAsync(Character character, CancellationToken ct = default)
    {
        character.Retire();
        await characters.UpdateAsync(character, ct);
    }

    public async Task ReinstateAsync(Character character, CancellationToken ct = default)
    {
        character.Reinstate();
        await characters.UpdateAsync(character, ct);
    }

    public async Task TransferToPersonaAsync(Character character, Guid newPersonaId, CancellationToken ct = default)
    {
        character.TransferToPersona(newPersonaId);
        await characters.UpdateAsync(character, ct);
    }

}
