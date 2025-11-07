using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Properties.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Properties;

/// <summary>
/// Background service that periodically checks rentable properties for eviction eligibility
/// and executes evictions when the grace period has expired.
/// </summary>
[ServiceBinding(typeof(PropertyEvictionService))]
public sealed class PropertyEvictionService : IDisposable
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static readonly TimeSpan EvictionCheckInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(30); // Run 30 seconds after server starts

    private readonly IRentablePropertyRepository _repository;
    private readonly ICommandHandler<EvictPropertyCommand> _evictCommandHandler;
    private readonly PropertyRentalPolicy _policy;
    private readonly Func<DateTimeOffset> _timeProvider;
    
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _runner;

    public PropertyEvictionService(
        IRentablePropertyRepository repository,
        ICommandHandler<EvictPropertyCommand> evictCommandHandler,
        PropertyRentalPolicy policy)
        : this(repository, evictCommandHandler, policy, () => DateTimeOffset.UtcNow)
    {
    }

    // Internal constructor for testing with time injection
    internal PropertyEvictionService(
        IRentablePropertyRepository repository,
        ICommandHandler<EvictPropertyCommand> evictCommandHandler,
        PropertyRentalPolicy policy,
        Func<DateTimeOffset> timeProvider)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _evictCommandHandler = evictCommandHandler ?? throw new ArgumentNullException(nameof(evictCommandHandler));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

        _runner = Task.Run(() => RunAsync(_cts.Token));
    }

    public void Dispose()
    {
        try
        {
            _cts.Cancel();
            if (!_runner.IsCompleted)
            {
                _runner.Wait(TimeSpan.FromSeconds(5));
            }
        }
        catch (AggregateException ex)
        {
            Log.Debug(ex, "Property eviction loop stopped with aggregate exception.");
        }
        catch (ObjectDisposedException)
        {
        }
        finally
        {
            _cts.Dispose();
        }
    }

    private async Task RunAsync(CancellationToken token)
    {
        Log.Info("Property eviction service starting. Initial delay: {InitialDelay}, Check interval: {CheckInterval}",
            InitialDelay,
            EvictionCheckInterval);

        try
        {
            // Give the server time to finish bootstrapping before the first eviction check
            await Task.Delay(InitialDelay, token).ConfigureAwait(false);
        }
        catch (TaskCanceledException)
        {
            Log.Info("Property eviction service cancelled during initial delay.");
            return;
        }

        Log.Info("Property eviction service initial delay complete. Starting periodic checks.");

        PeriodicTimer timer = new(EvictionCheckInterval);

        try
        {
            do
            {
                try
                {
                    await ExecuteEvictionCycleAsync(token).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Unhandled property eviction failure.");
                }
            }
            while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false));
        }
        catch (TaskCanceledException)
        {
            // Service shutting down
        }
        finally
        {
            timer.Dispose();
        }
    }

    internal async Task ExecuteEvictionCycleAsync(CancellationToken token)
    {
        DateTimeOffset evaluationTime = _timeProvider();
        List<RentablePropertySnapshot> properties = await _repository.GetAllPropertiesAsync(token).ConfigureAwait(false);

        Log.Info("Eviction cycle starting. Evaluating {PropertyCount} properties for eviction eligibility at {EvaluationTime}.", 
            properties.Count, 
            evaluationTime);

        int evictionCount = 0;
        foreach (RentablePropertySnapshot property in properties)
        {
            token.ThrowIfCancellationRequested();

            Log.Info("Evaluating property {PropertyId} ({InternalName}): Status={Status}, HasRental={HasRental}",
                property.Definition.Id,
                property.Definition.InternalName,
                property.OccupancyStatus,
                property.ActiveRental != null);

            if (!ShouldEvaluateProperty(property))
            {
                Log.Info("Skipping property {PropertyId} - not eligible for evaluation (Status={Status}, HasRental={HasRental})",
                    property.Definition.Id,
                    property.OccupancyStatus,
                    property.ActiveRental != null);
                continue;
            }

            bool eligible = IsEligibleForEviction(property, evaluationTime);
            
            Log.Info("Property {PropertyId} ({InternalName}): Eligible={Eligible}, " +
                     "NextDue={NextDue}, LastSeen={LastSeen}, GraceDays={GraceDays}",
                property.Definition.Id,
                property.Definition.InternalName,
                eligible,
                property.ActiveRental?.NextPaymentDueDate,
                property.ActiveRental?.LastOccupantSeenUtc,
                property.Definition.EvictionGraceDays);

            if (!eligible)
            {
                continue;
            }

            await ProcessEvictionAsync(property, token).ConfigureAwait(false);
            evictionCount++;
        }

        if (evictionCount > 0)
        {
            Log.Info("Evicted {EvictionCount} properties during this cycle.", evictionCount);
        }
        else
        {
            Log.Info("No properties were evicted during this cycle.");
        }
    }

    private static bool ShouldEvaluateProperty(RentablePropertySnapshot property)
    {
        // Only evaluate rented properties - owned and vacant properties are never evicted
        if (property.OccupancyStatus != PropertyOccupancyStatus.Rented)
        {
            return false;
        }

        // Must have an active rental agreement to be evictable
        if (property.ActiveRental is null)
        {
            return false;
        }

        return true;
    }

    private bool IsEligibleForEviction(RentablePropertySnapshot property, DateTimeOffset evaluationTime)
    {
        if (property.ActiveRental is null)
        {
            return false;
        }

        return _policy.IsEvictionEligible(
            property.ActiveRental,
            property.Definition,
            evaluationTime);
    }

    private async Task ProcessEvictionAsync(RentablePropertySnapshot property, CancellationToken token)
    {
        Log.Info("Evicting property {PropertyId} ({InternalName}) due to unpaid rent.",
            property.Definition.Id,
            property.Definition.InternalName);

        CommandResult result = await _evictCommandHandler.HandleAsync(
            new EvictPropertyCommand(property), token).ConfigureAwait(false);

        if (!result.Success)
        {
            Log.Error("Failed to evict property {PropertyId} ({InternalName}): {ErrorMessage}",
                property.Definition.Id,
                property.Definition.InternalName,
                result.ErrorMessage);
        }
        else
        {
            Log.Info("Successfully evicted property {PropertyId} ({InternalName}).",
                property.Definition.Id,
                property.Definition.InternalName);
        }
    }
}
