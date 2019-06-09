using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Parry.Tests
{
    [TestClass]
    public class MoveTests
    {
        [TestMethod]
        public void CanPerformTest()
        {
            Character chr = new Character();

            // The move should be performable if uses per turn > uses per turn progress.
            Move move = new Move();
            Assert.IsTrue(move.CanPerform(),
                $"Move started with {move.UsesPerTurnProgress} which should be less than {move.UsesPerTurn}.");

            // The move should not be performed if uses per turn <= uses per turn progress.
            move.UsesPerTurnProgress = move.UsesPerTurn;
            Assert.IsFalse(move.CanPerform(),
                $"Move shouldn't be performed if uses per turn progress matches uses per turn.");

            // The move shouldn't be performed if IsMoveEnabled is false.
            move.IsMoveEnabled = false;
            Assert.IsFalse(move.CanPerform(),
                "When IsMoveEnabled was false, the move was still available.");

            // The move shouldn't be performed if cooldown left is nonzero.
            move.IsMoveEnabled = true;
            move.CooldownProgress = 1;
            Assert.IsFalse(move.CanPerform(),
                "When CooldownProgress wasn't 0, the move was still available.");
        }

        [TestMethod]
        public void RefreshForNextTurnTest()
        {
            Move move = new Move();
            move.UsesPerTurn = 3;
            move.UsesPerTurnProgress = 1;
            move.CooldownProgress = 2;

            move.RefreshForNextTurn();

            Assert.AreEqual(move.UsesPerTurnProgress, 0, "Uses per turn should be 0.");
            Assert.AreEqual(move.CooldownProgress, 1, "Cooldown progress should be 1.");
        }

        [TestMethod]
        public void PerformTest()
        {
            Move move = new Move();
            move.Cooldown = 1;
            move.PerformAction = null;

            Assert.IsTrue(move.UsesPerTurnProgress == 0, "Expected uses per turn progress to be 0 by default.");
            Assert.IsTrue(move.CooldownProgress == 0, "Expected cooldown progress to be 0 by default.");
            move.Perform(null, null, null);

            Assert.IsTrue(move.UsesPerTurnProgress == move.UsesPerTurn && move.UsesPerTurn == 1,
                $"Uses per turn progress: {move.UsesPerTurnProgress}, uses per turn: {move.UsesPerTurn}. Both should be 1.");

            Assert.IsTrue(move.CooldownProgress == move.Cooldown && move.Cooldown == 1,
                $"Cooldown progress: {move.CooldownProgress}, cooldown: {move.Cooldown}. Both should be 1.");
        }
    }
}