using NUnit.Framework;

[TestFixture]
public class AmmoMathTests
{
    // TEST 3 — Inventory: Empty Magazine
    // Type: Unit (Whitebox)

    [Test]
    public void CanUseAmmo_Zero_ReturnsFalse()
    {
        Assert.IsFalse(AmmoMath.CanUseAmmo(0));
    }

    [Test]
    public void CanUseAmmo_One_ReturnsTrue()
    {
        Assert.IsTrue(AmmoMath.CanUseAmmo(1));
    }

    // TEST 4 — Inventory: Reserve Cap
    // Type: Unit (Whitebox)

    [Test]
    public void AddAmmoToReserve_ExceedsMax_ClampsToMax()
    {
        int result = AmmoMath.AddAmmoToReserve(90, 999, 100);
        Assert.AreEqual(100, result, "Reserve exceeded maxReserve cap.");
    }

    [Test]
    public void AddAmmoToReserve_NormalAmount_AddsCorrectly()
    {
        int result = AmmoMath.AddAmmoToReserve(20, 15, 100);
        Assert.AreEqual(35, result);
    }

    // TEST 5 — Inventory: Reload Math
    // Type: Unit (Whitebox)

    [Test]
    public void Reload_PartialMagazine_TransfersFromReserve()
    {
        var (mag, res) = AmmoMath.CalculateReload(5, 30, 10);
        Assert.AreEqual(15, mag, "Magazine should be 5 + 10 = 15.");
        Assert.AreEqual(0,  res, "Reserve should be depleted.");
    }

    [Test]
    public void Reload_FullMagazine_NoTransferOccurs()
    {
        var (mag, res) = AmmoMath.CalculateReload(30, 30, 50);
        Assert.AreEqual(30, mag, "Full magazine should stay at 30.");
        Assert.AreEqual(50, res, "Reserve should be unchanged.");
    }
}
