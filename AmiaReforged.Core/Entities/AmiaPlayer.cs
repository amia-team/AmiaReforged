namespace AmiaReforged.Core.Entities;

public class AmiaPlayer
{
    private readonly ICharacterRepository _characterRepository;
    public string PublicCdKey { get; }
    public IEnumerable<AmiaCharacter> Characters => GetCharacters();

    public AmiaPlayer(string publicCdKey, ICharacterRepository characterRepository)
    {
        PublicCdKey = publicCdKey;
        _characterRepository = characterRepository;
    }

    private IEnumerable<AmiaCharacter> GetCharacters()
    {
        return _characterRepository.GetCharacters(PublicCdKey);
    }

    public void AddCharacter(AmiaCharacter character)
    {
        Characters.ToList().Add(character);
    }
}

public interface ICharacterRepository
{
    IEnumerable<AmiaCharacter> GetCharacters(string publicCdKey);
}