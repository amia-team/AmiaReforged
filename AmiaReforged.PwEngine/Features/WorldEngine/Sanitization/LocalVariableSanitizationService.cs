using System;
using System.Collections.Generic;
using System.Linq;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Sanitization;

[ServiceBinding(typeof(ILocalVariableSanitizationService))]
public sealed class LocalVariableSanitizationService : ILocalVariableSanitizationService, IDisposable
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly List<ILocalVariableSanitizer> _sanitizers = new();
    private readonly object _syncRoot = new();

    public LocalVariableSanitizationService()
    {
        NwModule.Instance.OnClientEnter += HandleClientEnter;
    }

    public void RegisterSanitizer(ILocalVariableSanitizer sanitizer)
    {
        if (sanitizer is null)
        {
            throw new ArgumentNullException(nameof(sanitizer));
        }

        lock (_syncRoot)
        {
            if (_sanitizers.Contains(sanitizer))
            {
                return;
            }

            _sanitizers.Add(sanitizer);
        }
    }

    public void Sanitize(NwPlayer player)
    {
        if (player is not { IsValid: true } || player.IsDM)
        {
            return;
        }

        foreach (NwCreature creature in EnumerateCreatures(player))
        {
            Sanitize(creature);
        }
    }

    public void Sanitize(NwCreature creature)
    {
        if (creature is not { IsValid: true })
        {
            return;
        }

        ILocalVariableSanitizer[] snapshot;
        lock (_syncRoot)
        {
            snapshot = _sanitizers.ToArray();
        }

        foreach (ILocalVariableSanitizer sanitizer in snapshot)
        {
            try
            {
                sanitizer.Sanitize(creature);
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "Local variable sanitizer {SanitizerName} failed while processing creature {CreatureName} ({ObjectId}).",
                    sanitizer.Name,
                    creature.Name,
                    creature.ObjectId);
            }
        }
    }

    public void Dispose()
    {
        NwModule.Instance.OnClientEnter -= HandleClientEnter;
    }

    private void HandleClientEnter(ModuleEvents.OnClientEnter eventData)
    {
        Sanitize(eventData.Player);
    }

    private static IEnumerable<NwCreature> EnumerateCreatures(NwPlayer player)
    {
        if (player.LoginCreature is { IsValid: true } login)
        {
            yield return login;
        }

        if (player.ControlledCreature is { IsValid: true } controlled && controlled != player.LoginCreature)
        {
            yield return controlled;
        }
    }
}
