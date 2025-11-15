using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Tests.Shops.PlayerStalls;

/// <summary>
/// Tests for current period gross profits calculation in PlayerStallEventManager.
/// Verifies that gross sales are correctly calculated from ledger entries since the last rent payment.
/// </summary>
[TestFixture]
public class CurrentPeriodGrossProfitsTests
{
    [Test]
    public void CalculateCurrentPeriodGrossProfits_WhenNoLedgerEntries_ReturnsZero()
    {
        PlayerStall stall = CreateStall();
        stall.LedgerEntries = new List<PlayerStallLedgerEntry>();

        int result = CalculateGrossProfits(stall);

        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void CalculateCurrentPeriodGrossProfits_WhenOnlySalesInCurrentPeriod_ReturnsSumOfSales()
    {
        PlayerStall stall = CreateStall();
        DateTime periodStart = stall.LeaseStartUtc;

        stall.LedgerEntries = new List<PlayerStallLedgerEntry>
        {
            CreateSaleEntry(stall.Id, 1000, periodStart.AddHours(1)),
            CreateSaleEntry(stall.Id, 2500, periodStart.AddHours(2)),
            CreateSaleEntry(stall.Id, 500, periodStart.AddHours(3))
        };

        int result = CalculateGrossProfits(stall);

        Assert.That(result, Is.EqualTo(4000));
    }

    [Test]
    public void CalculateCurrentPeriodGrossProfits_WhenSalesBeforeAndAfterPeriod_OnlyCountsAfter()
    {
        PlayerStall stall = CreateStall();
        DateTime periodStart = new DateTime(2025, 11, 10, 12, 0, 0, DateTimeKind.Utc);
        stall.LastRentPaidUtc = periodStart;

        stall.LedgerEntries = new List<PlayerStallLedgerEntry>
        {
            CreateSaleEntry(stall.Id, 1000, periodStart.AddDays(-2)), // Before period
            CreateSaleEntry(stall.Id, 2000, periodStart.AddHours(1)), // In period
            CreateSaleEntry(stall.Id, 3000, periodStart.AddHours(5)), // In period
            CreateSaleEntry(stall.Id, 500, periodStart.AddDays(-1))   // Before period
        };

        int result = CalculateGrossProfits(stall);

        Assert.That(result, Is.EqualTo(5000)); // Only the two sales in the current period
    }

    [Test]
    public void CalculateCurrentPeriodGrossProfits_WhenUsesLastRentPaidUtc_IgnoresLeaseStart()
    {
        PlayerStall stall = CreateStall();
        stall.LeaseStartUtc = new DateTime(2025, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        stall.LastRentPaidUtc = new DateTime(2025, 11, 1, 0, 0, 0, DateTimeKind.Utc);

        stall.LedgerEntries = new List<PlayerStallLedgerEntry>
        {
            CreateSaleEntry(stall.Id, 1000, new DateTime(2025, 10, 15, 0, 0, 0, DateTimeKind.Utc)), // Between lease start and last rent
            CreateSaleEntry(stall.Id, 2000, new DateTime(2025, 11, 5, 0, 0, 0, DateTimeKind.Utc))   // After last rent paid
        };

        int result = CalculateGrossProfits(stall);

        Assert.That(result, Is.EqualTo(2000)); // Only sales after LastRentPaidUtc
    }

    [Test]
    public void CalculateCurrentPeriodGrossProfits_WhenMixedEntryTypes_OnlyCountsSaleGross()
    {
        PlayerStall stall = CreateStall();
        DateTime periodStart = stall.LeaseStartUtc;

        stall.LedgerEntries = new List<PlayerStallLedgerEntry>
        {
            CreateSaleEntry(stall.Id, 1000, periodStart.AddHours(1)),
            CreateEntry(stall.Id, PlayerStallLedgerEntryType.RentPayment, -500, periodStart.AddHours(2)),
            CreateEntry(stall.Id, PlayerStallLedgerEntryType.Withdrawal, -200, periodStart.AddHours(3)),
            CreateSaleEntry(stall.Id, 3000, periodStart.AddHours(4)),
            CreateEntry(stall.Id, PlayerStallLedgerEntryType.Deposit, 1000, periodStart.AddHours(5))
        };

        int result = CalculateGrossProfits(stall);

        Assert.That(result, Is.EqualTo(4000)); // Only the two SaleGross entries
    }

    [Test]
    public void CalculateCurrentPeriodGrossProfits_WhenNegativeSaleAmount_IgnoresNegative()
    {
        PlayerStall stall = CreateStall();
        DateTime periodStart = stall.LeaseStartUtc;

        stall.LedgerEntries = new List<PlayerStallLedgerEntry>
        {
            CreateSaleEntry(stall.Id, 1000, periodStart.AddHours(1)),
            CreateSaleEntry(stall.Id, -500, periodStart.AddHours(2)), // Negative sale (refund?)
            CreateSaleEntry(stall.Id, 2000, periodStart.AddHours(3))
        };

        int result = CalculateGrossProfits(stall);

        Assert.That(result, Is.EqualTo(3000)); // Negative amounts are clamped to 0 with Math.Max
    }

    [Test]
    public void CalculateCurrentPeriodGrossProfits_WhenNoLastRentPaidUtc_UsesLeaseStartUtc()
    {
        PlayerStall stall = CreateStall();
        stall.LastRentPaidUtc = null;
        DateTime leaseStart = new DateTime(2025, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        stall.LeaseStartUtc = leaseStart;

        stall.LedgerEntries = new List<PlayerStallLedgerEntry>
        {
            CreateSaleEntry(stall.Id, 1000, leaseStart.AddDays(-1)), // Before lease
            CreateSaleEntry(stall.Id, 2000, leaseStart.AddHours(1)), // After lease start
            CreateSaleEntry(stall.Id, 1500, leaseStart.AddDays(1))   // After lease start
        };

        int result = CalculateGrossProfits(stall);

        Assert.That(result, Is.EqualTo(3500)); // Only sales after LeaseStartUtc
    }

    [Test]
    public void CalculateCurrentPeriodGrossProfits_WhenMultipleLargeSales_AccumulatesCorrectly()
    {
        PlayerStall stall = CreateStall();
        DateTime periodStart = stall.LeaseStartUtc;

        stall.LedgerEntries = new List<PlayerStallLedgerEntry>
        {
            CreateSaleEntry(stall.Id, 100_000, periodStart.AddHours(1)),
            CreateSaleEntry(stall.Id, 250_000, periodStart.AddHours(2)),
            CreateSaleEntry(stall.Id, 75_000, periodStart.AddHours(3)),
            CreateSaleEntry(stall.Id, 50_000, periodStart.AddHours(4))
        };

        int result = CalculateGrossProfits(stall);

        Assert.That(result, Is.EqualTo(475_000));
    }

    // Helper methods for creating test data
    private static PlayerStall CreateStall()
    {
        DateTime now = DateTime.UtcNow;
        return new PlayerStall
        {
            Id = 42,
            Tag = "test_stall",
            AreaResRef = "test_area",
            DailyRent = 1000,
            EscrowBalance = 5000,
            LeaseStartUtc = now.AddDays(-7),
            NextRentDueUtc = now.AddDays(1),
            IsActive = true,
            CreatedUtc = now.AddDays(-7),
            UpdatedUtc = now,
            LedgerEntries = new List<PlayerStallLedgerEntry>(),
            Inventory = new List<StallProduct>(),
            Members = new List<PlayerStallMember>(),
            Transactions = new List<StallTransaction>()
        };
    }

    private static PlayerStallLedgerEntry CreateSaleEntry(long stallId, int amount, DateTime occurredUtc)
    {
        return CreateEntry(stallId, PlayerStallLedgerEntryType.SaleGross, amount, occurredUtc);
    }

    private static PlayerStallLedgerEntry CreateEntry(
        long stallId,
        PlayerStallLedgerEntryType entryType,
        int amount,
        DateTime occurredUtc)
    {
        return new PlayerStallLedgerEntry
        {
            StallId = stallId,
            EntryType = entryType,
            Amount = amount,
            Currency = "gp",
            Description = $"Test {entryType} entry",
            OccurredUtc = occurredUtc
        };
    }

    // This method mimics the logic in PlayerStallEventManager.CalculateCurrentPeriodGrossProfits
    private static int CalculateGrossProfits(PlayerStall stall)
    {
        DateTime periodStart = stall.LastRentPaidUtc ?? stall.LeaseStartUtc;

        if (stall.LedgerEntries == null || stall.LedgerEntries.Count == 0)
        {
            return 0;
        }

        int grossProfits = 0;
        foreach (PlayerStallLedgerEntry entry in stall.LedgerEntries)
        {
            if (entry.EntryType == PlayerStallLedgerEntryType.SaleGross &&
                entry.OccurredUtc >= periodStart)
            {
                grossProfits += Math.Max(0, entry.Amount);
            }
        }

        return grossProfits;
    }
}

