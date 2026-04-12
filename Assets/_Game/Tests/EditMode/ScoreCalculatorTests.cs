using NUnit.Framework;

[TestFixture]
public class ScoreCalculatorTests
{
    // TEST 6 — ScoreManager: Final Score Calculation
    // Type: Unit (Whitebox)
    // Kill points: Grunt=100, Scout=150, Berserker=200, Mutant=175, Heavy=125, Boss=1000
    // Bonus: Acc>=70%=+500, Acc>=50%=+250, Time<5min=+200, StageComplete=+300
    // Penalty: -100 per death

    [Test]
    public void Score_TenGrunts_HighAccuracy_Fast_NoDeaths_IsGradeA()
    {
        int score = ScoreCalculator.Calculate(10 * 100, 0.80f, 180f, 0, true);
        // 1000 + 500(acc>=70%) + 200(time<300s) + 300(complete) = 2000
        Assert.AreEqual(2000, score);
        Assert.AreEqual("A", ScoreCalculator.GetGrade(score));
    }

    [Test]
    public void Score_NoKills_NoAccuracy_TenDeaths_IsGradeD()
    {
        int score = ScoreCalculator.Calculate(0, 0f, 999f, 10, false);
        // 0 - 1000(deaths) = -1000
        Assert.AreEqual(-1000, score);
        Assert.AreEqual("D", ScoreCalculator.GetGrade(score));
    }

    [Test]
    public void Score_OneBossKill_StageComplete_IsGradeB()
    {
        int score = ScoreCalculator.Calculate(1000, 0.60f, 999f, 0, true);
        // 1000 + 250(acc>=50%) + 0(time) + 300(complete) = 1550
        Assert.AreEqual(1550, score);
        Assert.AreEqual("B", ScoreCalculator.GetGrade(score));
    }

    [Test]
    public void Score_Exactly3000_IsGradeS_Boundary()
    {
        Assert.AreEqual("S", ScoreCalculator.GetGrade(3000));
    }

    [Test]
    public void Score_1999_IsGradeB_NotA()
    {
        Assert.AreEqual("B", ScoreCalculator.GetGrade(1999));
    }
}
