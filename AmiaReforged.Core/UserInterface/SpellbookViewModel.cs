using System.Text.Json;
using AmiaReforged.Core.Models;
using NLog.Fluent;

namespace AmiaReforged.Core.UserInterface;

public class SpellbookViewModel
{
   
    public string Name { get; init; }
    public long Id { get; set; }
    public string Class { get; init; }
    public Dictionary<byte, List<PreparedSpellModel>?>? SpellBook { get; set; } = new();

    public static SpellbookViewModel FromDatabaseModel(SavedSpellbook savedBook)
    {
        SpellbookViewModel? spellbookViewModel = new()
        {
            Name = savedBook.SpellbookName,
            Class = savedBook.ClassId.ToString(),
            Id = savedBook.BookId
        };

        try
        {
            spellbookViewModel.SpellBook =
                JsonSerializer.Deserialize<Dictionary<byte, List<PreparedSpellModel>>>(savedBook.SpellbookJson);
        }
        catch (Exception)
        {
            // Nothing to do here
        }

        return spellbookViewModel;
    }
    
    public override string ToString()
    {
        string spellbookDictString = string.Empty;
        
        if (SpellBook != null)
        {
            foreach (KeyValuePair<byte, List<PreparedSpellModel>?> keyValuePair in SpellBook)
            {
                spellbookDictString += $"Level {keyValuePair.Key}:\n";
                spellbookDictString = keyValuePair.Value.Aggregate(spellbookDictString, (current, preparedSpellModel) => current + $"{preparedSpellModel}\n");
            }
        }
        
        
        return $"Name: {Name}, Id: {Id}, Class: {Class} Spellbook: {spellbookDictString}";
    }
}