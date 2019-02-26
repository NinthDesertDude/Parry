using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Parry.Tests
{
    [TestClass]
    public class MoveTests
    {
        [TestMethod]
        public void CanPerformTest()
        {
            Character chr = new Character();

            // The move shouldn't be performed if users per turn is 0.
            Move move = new Move();
            Assert.IsFalse(move.CanPerform(chr), $"Move started with {move.UsesPerTurnProgress} for uses per turn progress, which should be 0.");

            move.UsesPerTurnProgress = 1;
            Assert.IsTrue(move.CanPerform(chr));

            // The move shouldn't be performed if IsMoveEnabled is false.
            move.IsMoveEnabled = false;
            Assert.IsFalse(move.CanPerform(chr),
                "When IsMoveEnabled was false, the move was still available.");

            // The move shouldn't be performed if cooldown left is nonzero.
            move.IsMoveEnabled = true;
            move.CooldownProgress = 1;
            Assert.IsFalse(move.CanPerform(chr),
                "When CooldownProgress wasn't 0, the move was still available.");

            // The move shouldn't be used if there is no turn fraction left.
            move.CooldownProgress = 0;
            chr.MoveSelectBehavior.TurnFractionLeft = 1.1f;
            Assert.IsFalse(move.CanPerform(chr),
                "When TurnFractionProgress wasn't 0, the move was still available.");
        }

        [TestMethod]
        public void NextTurnTest()
        {
            Character chr = new Character();

            Move move = new Move();
            move.UsesPerTurnProgress = 2;
            move.CooldownProgress = 2;
            chr.MoveSelectBehavior.TurnFractionLeft = 1;
            move.NextTurn();
            Assert.AreEqual(move.UsesPerTurnProgress, 0, "Uses per turn should be 0.");
            Assert.AreEqual(move.CooldownProgress, 1, "Cooldown progress should be 1.");
            Assert.AreEqual(chr.MoveSelectBehavior.TurnFractionLeft, 1, "Turn fraction progress should be unchanged at value 1.");

            move.NextTurn();
        }

        /// <summary>
        /// Ensures uses per turn in the Perform() function is working as
        /// expected.
        /// </summary>
        [TestMethod]
        public void PerformUsesPerTurnTest()
        {
            Move move = new Move();
            Character chr1 = new Character();
            List<Character> emptyList = new List<Character>();

            Assert.IsTrue(move.UsesPerTurnProgress == 0,
                "Expected uses per turn to be zero.");
            Assert.IsFalse(move.Perform(chr1, emptyList, emptyList),
                "Expected Perform() to return false when uses per turn is 0.");
        }

        [TestMethod]
        public void PerformCooldownTest()
        {
            Move move = new Move();
            Character chr1 = new Character();
            List<Character> emptyList = new List<Character>();
        }

        /// <summary>
        /// Ensures Perform() returns false for a null action.
        /// </summary>
        [TestMethod]
        public void PerformNullActionTest()
        {
            Move move = new Move();
            Character chr1 = new Character();
            List<Character> emptyList = new List<Character>();

            move.PerformAction = null;
            Assert.IsFalse(move.Perform(chr1, emptyList, emptyList),
                "Expected Perform() to return false for a null action.");
        }

        /// <summary>
        /// Ensures the turn fraction behavior in the Perform() function is
        /// working as expected.
        /// </summary>
        [TestMethod]
        public void PerformTurnFractionTest()
        {
            Move move = new Move();
            Character chr = new Character();
            List<Character> emptyList = new List<Character>();

            move.UsesPerTurn = 20;
            move.UsesPerTurnProgress = 20;
            move.TurnFraction = 1.1f;
            chr.MoveSelectBehavior.TurnFractionLeft = 1.1f;

            // Every 10 intervals, the 1.1 turn fraction causes a round spent charging.
            // Also ensures uses per turn doesn't decrease when a round is spent charging.
            for (int i = 0; i < 2; i++)
            {
                int usesPerTurn = move.UsesPerTurnProgress;
                Assert.IsFalse(move.Perform(chr, emptyList, emptyList), "Expected to charge (return false).");
                Assert.AreEqual(usesPerTurn, move.UsesPerTurnProgress);

                for (int j = 0; j < 10; j++)
                {
                    Assert.IsTrue(move.Perform(chr, emptyList, emptyList), "Expected to finish charging (return true).");
                }
            }

            //  move should always perform for turn fractions under 1 if all else is fine.
            move.TurnFraction = 0.8f;
            chr.MoveSelectBehavior.TurnFractionLeft = 0;

            float origValue = chr.MoveSelectBehavior.TurnFractionLeft;
            move.Perform(chr, emptyList, emptyList);
            Assert.AreEqual(origValue, chr.MoveSelectBehavior.TurnFractionLeft,
                $"Got {chr.MoveSelectBehavior.TurnFractionLeft} instead of 0 for TurnFractionProgress.");
        }
    }
}