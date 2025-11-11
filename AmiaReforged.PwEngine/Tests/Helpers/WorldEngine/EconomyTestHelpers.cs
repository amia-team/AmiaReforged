using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.ValueObjects;

namespace AmiaReforged.PwEngine.Tests.Helpers.WorldEngine;

/// <summary>
/// Test helpers for creating Economy-related test objects.
/// </summary>
public static class EconomyTestHelpers
{
    /// <summary>
    /// Creates a test GoldAmount with a default value.
    /// </summary>
    public static GoldAmount CreateGoldAmount(int amount = 1000) =>
        GoldAmount.Parse(amount);

    /// <summary>
    /// Creates a test TransactionReason with a default value.
    /// </summary>
    public static TransactionReason CreateReason(string? reason = null) =>
        TransactionReason.Parse(reason ?? "Test transaction");

    /// <summary>
    /// Creates a test coinhouse tag.
    /// </summary>
    public static CoinhouseTag CreateCoinhouseTag(string? name = null) =>
        CoinhouseTag.Parse(name ?? "test_bank");

    /// <summary>
    /// Creates a test settlement ID.
    /// </summary>
    public static SettlementId CreateSettlementId(int id = 1) =>
        SettlementId.Parse(id);

    /// <summary>
    /// Creates a test transaction ID.
    /// </summary>
    public static TransactionId CreateTransactionId() =>
        TransactionId.NewId();
}

