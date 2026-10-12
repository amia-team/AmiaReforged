using System.Security.Cryptography;
using System.Text;
using Anvil.API;
namespace AmiaReforged.Core.Services.Sailing;

public interface IMaritimeWallet
{
    string Account { get; }
    string Character { get; }
    long Gold { get; }
    bool IsValid { get; }
    void ChangeGold(int delta, string? receipt = null);
}

/// <summary>Matches chart discovery's account convention; durable records contain no raw CD key or player handle.</summary>
public static class MaritimeIdentity
{
    public static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    public static string Account(NwPlayer player) => Hash(player.CDKey);
}

public sealed class MaritimeWallet : IMaritimeWallet
{
    private readonly NwPlayer _player;
    private readonly NwCreature _creature;
    public string Account { get; }
    public string Character { get; }
    public MaritimeWallet(NwPlayer player)
    {
        _player = player;
        _creature = player.LoginCreature ?? throw new InvalidOperationException("No login character.");
        Account = MaritimeIdentity.Account(player);
        Character = MaritimeIdentity.Hash(Account + ":" + _player.BicFileName);
    }
    public bool IsValid => _player.IsValid && _creature.IsValid && _player.ControlledCreature == _creature && _player.LoginCreature == _creature;
    public long Gold => IsValid ? _creature.Gold : -1;
    public void ChangeGold(int delta, string? receipt = null)
    {
        if (!IsValid) throw new InvalidOperationException("Your login character must be controlling this transaction.");
        long before = Gold, after = before + delta;
        if (after < 0 || after > uint.MaxValue) throw new InvalidOperationException("Gold limit or insufficient gold.");
        if (delta < 0) _creature.TakeGold(checked(-delta)); else if (delta > 0) _creature.GiveGold(delta);
        if (Gold != after) throw new InvalidOperationException("Native gold operation did not apply the expected amount; administrator recovery required.");
        if (receipt != null) _creature.GetObjectVariable<LocalVariableString>("SAILING_MARITIME_RECEIPT").Value = receipt;
        _player.ExportCharacter();
    }
}
