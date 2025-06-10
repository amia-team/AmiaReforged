namespace AmiaReforged.PwEngine.Systems.Module;

using System;
using Anvil.API;
using Anvil.Services;
using NLog;

internal sealed class Palette
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private const string StandardPaletteSuffix = "std";
    private const string CustomPaletteSuffix = "cus";

    private readonly BlueprintObjectType _paletteType;
    private readonly string _standardPaletteResRef;
    private readonly string _customPaletteResRef;

    [Inject] public ResourceManager ResourceManager { private get; init; } = null!;

    private readonly List<PaletteBlueprint> _blueprints = new();

    public Palette(string palettePrefix, BlueprintObjectType paletteType)
    {
        _paletteType = paletteType;
        _standardPaletteResRef = palettePrefix + StandardPaletteSuffix;
        _customPaletteResRef = palettePrefix + CustomPaletteSuffix;
    }

    public List<PaletteBlueprint> GetBlueprints()
    {
        _blueprints.Clear();

        TryLoadPalette(_standardPaletteResRef, "Standard");
        TryLoadPalette(_customPaletteResRef, "Custom");

        return _blueprints;
    }

    private void TryLoadPalette(string resRef, string rootPath)
    {
        using GffResource palette = ResourceManager.GetGenericFile(resRef, ResRefType.ITP);
        if (palette == null)
        {
            Log.Error("Failed to load palette {Palette}", resRef);
            return;
        }

        try
        {
            ProcessList(palette["MAIN"], rootPath);
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to parse palette file {Palette}", resRef);
        }
    }

    private void ProcessList(GffResourceField field, string path)
    {
        foreach (GffResourceField child in field.Values)
        {
            ProcessStruct(child, path);
        }
    }

    private void ProcessStruct(GffResourceField field, string path)
    {
        if (field.TryGetValue("RESREF", out GffResourceField resRefField))
        {
            string resRef = resRefField.Value<string>();
            string name = "Unknown";
            float? cr = null;
            string faction = null;

            if (field.TryGetValue("NAME", out GffResourceField creatureNameField))
            {
                name = creatureNameField.Value<string>();
            }
            else if (field.TryGetValue("STRREF", out GffResourceField creatureNameStrRefField))
            {
                name = new StrRef(creatureNameStrRefField.Value<uint>()).ToString();
            }

            if (field.TryGetValue("CR", out GffResourceField creatureChallengeRatingField))
            {
                cr = creatureChallengeRatingField.Value<float>();
            }

            if (field.TryGetValue("FACTION", out GffResourceField creatureFactionField))
            {
                faction = creatureFactionField.Value<string>();
            }

            _blueprints.Add(new()
            {
                ResRef = resRef,
                Name = name,
                Category = path,
                ChallengeRating = cr,
                Faction = faction,
                FullName = path + "/" + name,
                ObjectType = _paletteType,
            });
        }
        else
        {
            if (field.TryGetValue("STRREF", out GffResourceField groupStrRef))
            {
                path = Path.Combine(path, new StrRef(groupStrRef.Value<uint>()).ToString());
            }

            if (field.TryGetValue("LIST", out GffResourceField list))
            {
                ProcessList(list, path);
            }
        }
    }
}