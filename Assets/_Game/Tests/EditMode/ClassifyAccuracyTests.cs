using NUnit.Framework;

/// <summary>
/// ClassifyAccuracyTests — EditMode unit tests for DifficultyManager.ClassifyAccuracy().
///
/// Covers:
///   A3 — Tier classification (whitebox): boundary values for Easy (<0.30),
///        Normal (0.30–0.65), and Hard (>0.65) thresholds.
///
/// ClassifyAccuracy is a pure static method — no MonoBehaviour, no scene needed.
/// </summary>
[TestFixture]
public class ClassifyAccuracyTests
{
    // =========================================================================
    // A3 — ClassifyAccuracy Tier Boundaries (Whitebox)
    // =========================================================================
    // Thresholds from the implementation: <0.30 → Easy, >0.65 → Hard, else Normal.
    // Whitebox: these tests break if the threshold values are changed.

    [Test]
    [Category("Whitebox")]
    public void ClassifyAccuracy_At0f00_ReturnsEasy()
    {
        Assert.AreEqual(DifficultyManager.DifficultyTier.Easy,
            DifficultyManager.ClassifyAccuracy(0f),
            "0% accuracy must classify as Easy.");
    }

    [Test]
    [Category("Whitebox")]
    public void ClassifyAccuracy_At0f29_JustBelowEasyBoundary_ReturnsEasy()
    {
        // 0.29 < 0.30 → Easy
        Assert.AreEqual(DifficultyManager.DifficultyTier.Easy,
            DifficultyManager.ClassifyAccuracy(0.29f),
            "29% accuracy (just below 0.30) must classify as Easy.");
    }

    [Test]
    [Category("Whitebox")]
    public void ClassifyAccuracy_At0f30_EasyNormalBoundary_ReturnsNormal()
    {
        // 0.30 is NOT < 0.30 and NOT > 0.65 → Normal
        Assert.AreEqual(DifficultyManager.DifficultyTier.Normal,
            DifficultyManager.ClassifyAccuracy(0.30f),
            "30% accuracy (at the boundary) must classify as Normal, not Easy.");
    }

    [Test]
    [Category("Whitebox")]
    public void ClassifyAccuracy_At0f50_MidRange_ReturnsNormal()
    {
        Assert.AreEqual(DifficultyManager.DifficultyTier.Normal,
            DifficultyManager.ClassifyAccuracy(0.50f),
            "50% accuracy must classify as Normal.");
    }

    [Test]
    [Category("Whitebox")]
    public void ClassifyAccuracy_At0f65_NormalHardBoundary_ReturnsNormal()
    {
        // 0.65 is NOT > 0.65 → Normal
        Assert.AreEqual(DifficultyManager.DifficultyTier.Normal,
            DifficultyManager.ClassifyAccuracy(0.65f),
            "65% accuracy (at the boundary) must classify as Normal, not Hard.");
    }

    [Test]
    [Category("Whitebox")]
    public void ClassifyAccuracy_At0f66_JustAboveHardBoundary_ReturnsHard()
    {
        // 0.66 > 0.65 → Hard
        Assert.AreEqual(DifficultyManager.DifficultyTier.Hard,
            DifficultyManager.ClassifyAccuracy(0.66f),
            "66% accuracy (just above 0.65) must classify as Hard.");
    }

    [Test]
    [Category("Whitebox")]
    public void ClassifyAccuracy_At1f00_ReturnsHard()
    {
        Assert.AreEqual(DifficultyManager.DifficultyTier.Hard,
            DifficultyManager.ClassifyAccuracy(1f),
            "100% accuracy must classify as Hard.");
    }

    // =========================================================================
    // A3 — ClassifyAccuracy (Blackbox)
    // =========================================================================
    // Observable contract: clearly low accuracy → Easy, clearly high → Hard.
    // No knowledge of exact threshold values required.

    [Test]
    [Category("Blackbox")]
    public void ClassifyAccuracy_ClearlyLowAccuracy_ReturnsEasy()
    {
        Assert.AreEqual(DifficultyManager.DifficultyTier.Easy,
            DifficultyManager.ClassifyAccuracy(0.05f),
            "Very low accuracy (5%) must classify as Easy.");
    }

    [Test]
    [Category("Blackbox")]
    public void ClassifyAccuracy_ClearlyHighAccuracy_ReturnsHard()
    {
        Assert.AreEqual(DifficultyManager.DifficultyTier.Hard,
            DifficultyManager.ClassifyAccuracy(0.95f),
            "Very high accuracy (95%) must classify as Hard.");
    }

    [Test]
    [Category("Blackbox")]
    public void ClassifyAccuracy_TiersAreOrdered_EasyLessThanNormalLessThanHard()
    {
        // Tiers must follow Easy < Normal < Hard in enum ordering.
        Assert.Less(
            (int)DifficultyManager.ClassifyAccuracy(0.05f),
            (int)DifficultyManager.ClassifyAccuracy(0.50f),
            "Easy tier must have a lower enum value than Normal.");
        Assert.Less(
            (int)DifficultyManager.ClassifyAccuracy(0.50f),
            (int)DifficultyManager.ClassifyAccuracy(0.95f),
            "Normal tier must have a lower enum value than Hard.");
    }
}
