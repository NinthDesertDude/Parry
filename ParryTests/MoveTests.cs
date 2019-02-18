using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Parry.Tests
{
    [TestClass]
    public class MoveTests
    {
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

        /// <summary>
        /// Ensures the cooldown works correctly.
        /// </summary>
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
            Character chr1 = new Character();
            List<Character> emptyList = new List<Character>();

            move.UsesPerTurn = 20;
            move.UsesPerTurnProgress = 20;
            move.TurnFraction = 1.1f;
            move.TurnFractionProgress = 1.1f;

            // Every 10 intervals, the 1.1 turn fraction causes a round spent charging.
            // Also ensures uses per turn doesn't decrease when a round is spent charging.
            for (int i = 0; i < 2; i++)
            {
                int usesPerTurn = move.UsesPerTurnProgress;
                Assert.IsFalse(move.Perform(chr1, emptyList, emptyList), "Expected to charge (return false).");
                Assert.AreEqual(usesPerTurn, move.UsesPerTurnProgress);

                for (int j = 0; j < 10; j++)
                {
                    Assert.IsTrue(move.Perform(chr1, emptyList, emptyList), "Expected to finish charging (return true).");
                }
            }

            //  move should always perform for turn fractions under 1 if all else is fine.
            move.TurnFraction = 0.8f;
            move.TurnFractionProgress = 0;

            float origValue = move.TurnFractionProgress;
            move.Perform(chr1, emptyList, emptyList);
            Assert.AreEqual(origValue, move.TurnFractionProgress,
                $"Got {move.TurnFractionProgress} instead of 0 for TurnFractionProgress.");
        }
    }
}