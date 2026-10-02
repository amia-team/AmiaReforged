using System.Runtime.CompilerServices;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Repositories;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application;

/// <summary>
/// Guards the entire load/mutate/save sequence shared by commands and event processing.
/// Bounded stripes avoid retaining a semaphore for every character ever loaded.
/// </summary>
internal sealed class CodexMutationLock : IDisposable
{
    private static readonly ConditionalWeakTable<IPlayerCodexRepository, SemaphoreSlim[]> Gates = new();
    private SemaphoreSlim? _gate;

    private CodexMutationLock(SemaphoreSlim gate) => _gate = gate;

    public static async Task<CodexMutationLock> AcquireAsync(
        IPlayerCodexRepository repository, CharacterId characterId, CancellationToken ct)
    {
        SemaphoreSlim[] gates = Gates.GetValue(repository,
            _ => Enumerable.Range(0, 64).Select(_ => new SemaphoreSlim(1, 1)).ToArray());
        SemaphoreSlim gate = gates[(uint)characterId.GetHashCode() % (uint)gates.Length];
        await gate.WaitAsync(ct);
        return new CodexMutationLock(gate);
    }

    public void Dispose() => Interlocked.Exchange(ref _gate, null)?.Release();
}
