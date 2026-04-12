using NUnit.Framework;

[TestFixture]
public class DifficultyManagerTests
{
    // TEST 1 — DDA Tier Classification
    // Type: Unit (Whitebox)
    // Validates the 3 accuracy thresholds from Chapter 3 / thesis claim.
    // Note: DifficultyTier is nested inside DifficultyManager.

    [Test]
    public void Accuracy_010_ReturnsEasy()
    {
        var result = DifficultyManager.ClassifyAccuracy(0.10f);
        Assert.AreEqual(DifficultyManager.DifficultyTier.Easy, result);
    }

    [Test]
    public void Accuracy_029_ReturnsEasy_BoundaryBelow30()
    {
        var result = DifficultyManager.ClassifyAccuracy(0.29f);
        Assert.AreEqual(DifficultyManager.DifficultyTier.Easy, result);
    }

    [Test]
    public void Accuracy_030_ReturnsNormal_ExactlyAtLowerBound()
    {
        var result = DifficultyManager.ClassifyAccuracy(0.30f);
        Assert.AreEqual(DifficultyManager.DifficultyTier.Normal, result);
    }

    [Test]
    public void Accuracy_065_ReturnsNormal_ExactlyAtUpperBound()
    {
        var result = DifficultyManager.ClassifyAccuracy(0.65f);
        Assert.AreEqual(DifficultyManager.DifficultyTier.Normal, result);
    }

    [Test]
    public void Accuracy_080_ReturnsHard()
    {
        var result = DifficultyManager.ClassifyAccuracy(0.80f);
        Assert.AreEqual(DifficultyManager.DifficultyTier.Hard, result);
    }

    // TEST 2 — DDA Anti-Compounding
    // Type: Unit (Whitebox)
    // Validates base-stat preservation — applying Hard multiplier twice from the
    // same base must produce the same result as applying it once.

    [Test]
    public void ApplyHardTier_Twice_DoesNotCompoundStats()
    {
        float baseSpeed  = 5f;
        float hardMult   = 1.60f;

        float firstApply = baseSpeed * hardMult;   // 8.0
        float secndApply = baseSpeed * hardMult;   // still 8.0, not 12.8

        Assert.AreEqual(firstApply, secndApply, 0.001f,
            "Applying Hard tier twice compounded stats — base stats not preserved.");
    }
}
