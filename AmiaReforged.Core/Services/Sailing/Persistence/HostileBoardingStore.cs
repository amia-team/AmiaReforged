using System.Text.Json;
using AmiaReforged.Core.Models.Sailing;
using Anvil.Services;

namespace AmiaReforged.Core.Services.Sailing;

public interface IHostileBoardingStore
{
    Task<HostileBoardingVisit?> Load(string actor);
    Task Save(HostileBoardingVisit visit);
    Task Remove(string actor);
}

/// <summary>Independent recovery journal; never changes shared database/persistence schemas.</summary>
[ServiceBinding(typeof(HostileBoardingStore))]
public sealed class HostileBoardingStore(SailingFileStore files) : IHostileBoardingStore
{
    private string VisitPath(string actor) => Path.Combine(files.RootDirectory, "hostile-boarding", MaritimeIdentity.Hash(actor) + ".json");
    public async Task<HostileBoardingVisit?> Load(string actor)
    {
        try
        {
            await using FileStream stream = new(VisitPath(actor), FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete, 4096, true);
            var visit = await JsonSerializer.DeserializeAsync<HostileBoardingVisit>(stream).ConfigureAwait(false);
            if (visit == null || visit.Actor != actor || !Valid(visit)) throw new InvalidDataException("Invalid hostile boarding recovery journal.");
            return visit;
        }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
    }
    private static bool Valid(HostileBoardingVisit v) => !string.IsNullOrWhiteSpace(v.Actor) &&
        !string.IsNullOrWhiteSpace(v.OriginId) && !string.IsNullOrWhiteSpace(v.TargetId) && v.OriginId != v.TargetId &&
        v.Origin is { IsValid: true } && v.Target is { IsValid: true } && v.Recovery is { IsValid: true } &&
        !string.Equals(v.Recovery.Area, v.Target.Area, StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(v.Recovery.Area, v.Origin.Area, StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(v.Recovery.Area, v.TargetCabinArea, StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(v.Recovery.Area, v.OriginCabinArea, StringComparison.OrdinalIgnoreCase);

    public Task Save(HostileBoardingVisit visit)
    {
        if (!Valid(visit)) throw new InvalidDataException("Incomplete hostile boarding recovery plan.");
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(visit);
        string path = VisitPath(visit.Actor);
        return Task.Run(() =>
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (FileStream stream = new(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { stream.Write(bytes); stream.Flush(true); }
                File.Move(temporary, path, true);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        });
    }
    public Task Remove(string actor) => Task.Run(() => File.Delete(VisitPath(actor)));
}
