using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Storage;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Tests.Storage;

[TestFixture]
public class BankStorageItemBlacklistTests
{
    private BankStorageItemBlacklist _blacklist = null!;

    [SetUp]
    public void SetUp()
    {
        _blacklist = new BankStorageItemBlacklist();
    }

    [Test]
    [TestCase("ds_pckey")]
    [TestCase("DS_PCKEY")]
    [TestCase("amia_premium_token")]
    public void IsBlacklisted_ReturnsTrue_ForConfiguredResrefs(string resref)
    {
        Assert.That(_blacklist.IsBlacklisted(resref), Is.True);
    }

    [Test]
    [TestCase(null)]
    [TestCase("")]
    [TestCase("    ")]
    [TestCase("non_blocked_resref")]
    public void IsBlacklisted_ReturnsFalse_ForNonBlockedResrefs(string? resref)
    {
        Assert.That(_blacklist.IsBlacklisted(resref), Is.False);
    }

    [Test]
    public void BlacklistedResrefs_ExposesConfiguredValues()
    {
        CollectionAssert.Contains(_blacklist.BlacklistedResrefs, "ds_pckey");
        CollectionAssert.Contains(_blacklist.BlacklistedResrefs, "amia_premium_token");
    }
}
