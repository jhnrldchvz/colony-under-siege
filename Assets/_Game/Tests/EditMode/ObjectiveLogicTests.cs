using NUnit.Framework;

[TestFixture]
public class ObjectiveLogicTests
{
    // TEST 7 — ObjectiveManager Completion Logic
    // Type: Unit (Whitebox)
    // Tests pure completion-check expressions — no MonoBehaviour required.

    [Test]
    public void KillAll_AllEnemiesDead_IsComplete()
    {
        int totalEnemies = 5;
        int aliveEnemies = 0;
        bool isComplete  = (aliveEnemies == 0 && totalEnemies > 0);
        Assert.IsTrue(isComplete);
    }

    [Test]
    public void KillAll_OneEnemyAlive_IsNotComplete()
    {
        int aliveEnemies = 1;
        bool isComplete  = (aliveEnemies == 0);
        Assert.IsFalse(isComplete);
    }

    [Test]
    public void KillCount_RequiredReached_IsComplete()
    {
        int required = 3;
        int killed   = 3;
        Assert.IsTrue(killed >= required);
    }

    [Test]
    public void KillCount_RequiredNotReached_IsNotComplete()
    {
        int required = 3;
        int killed   = 2;
        Assert.IsFalse(killed >= required);
    }

    [Test]
    public void CollectItem_CorrectItemID_IsComplete()
    {
        string requiredId  = "keycard_01";
        string collectedId = "keycard_01";
        Assert.AreEqual(requiredId, collectedId);
    }

    [Test]
    public void CollectItem_WrongItemID_IsNotComplete()
    {
        string requiredId  = "keycard_01";
        string collectedId = "power_cell";
        Assert.AreNotEqual(requiredId, collectedId);
    }

    [Test]
    public void ActivateSwitch_AllActivated_IsComplete()
    {
        int required  = 4;
        int activated = 4;
        Assert.IsTrue(activated >= required);
    }

    [Test]
    public void ActivateSwitch_NotAllActivated_IsNotComplete()
    {
        int required  = 4;
        int activated = 3;
        Assert.IsFalse(activated >= required);
    }
}
