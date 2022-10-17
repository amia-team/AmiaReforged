using AmiaReforged.Core.Types;

namespace AmiaReforged.Core.Models;

public class AmiaPlayer
{
    private readonly ICharacterAccessor _characterService;
    public string PublicCdKey { get; }
    public IReadOnlyList<AmiaCharacter> Characters => GetCharacters();

    public AmiaPlayer(string publicCdKey, ICharacterAccessor characterService)
    {
        PublicCdKey = publicCdKey;
        _characterService = characterService;
    }

    private IReadOnlyList<AmiaCharacter> GetCharacters()
    {
        return _characterService.GetCharacters(PublicCdKey);
    }

    public void AddCharacter(AmiaCharacter character)
    {
        _characterService.AddCharacter(PublicCdKey, character);
    }
}