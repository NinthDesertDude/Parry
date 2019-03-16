using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Parry.Tests
{
    [TestClass]
    public class CharacterTests
    {
        /// <summary>
        /// Character Ids increment by 1 and are unique.
        /// </summary>
        [TestMethod]
        public void CreateCharactersTest()
        {
            Character chr1 = new Character();
            Character chr2 = new Character();

            Assert.AreEqual(chr1.Id + 1, chr2.Id);
        }

        /// <summary>
        /// Character Ids of deep clones can be identical or new.
        /// </summary>
        [TestMethod]
        public void CreateClonedCharactersTest()
        {
            Character chr1 = new Character();
            Character chr2 = new Character(chr1, true);
            Assert.AreEqual(chr1.Id, chr2.Id, $"Deep copy should've had ID of {chr1.Id}, but had {chr2.Id} instead.");

            Character chr3 = new Character(chr1, true, true);
            Assert.AreNotEqual(chr1.Id, chr3.Id, $"Deep copy should've had unique ID, but had {chr1.Id} instead.");
        }

        /// <summary>
        /// Character events can be raised with associated functions.
        /// </summary>
        [TestMethod]
        public void RaiseEventTest()
        {
            Character chr1 = new Character();
            Character chr2 = new Character();
            bool eventHit = false;
            chr1.AfterMove += () => { eventHit = true; };
            chr1.RaiseAfterMove();

            Assert.IsTrue(eventHit);
        }

        /// <summary>
        /// GetTargets() works correctly.
        /// </summary>
        [TestMethod]
        public void GetTargetsTest()
        {
            // Override targets should contribute to targets when there are no moves.
            Character chr1 = new Character();
            chr1.TeamID = 1;
            chr1.DefaultTargetBehavior.OverrideTargets = new List<Character>() { new Character() };
            var targets = chr1.GetTargets()[0];
            Assert.IsTrue(targets.Count == 1, $"There were {targets.Count} targets instead of 1.");
            chr1.DefaultTargetBehavior.OverrideTargets = null;

            // Ensures there are no targets to begin with.
            chr1.MoveSelectBehavior.Moves.Add(new Move());
            chr1.MoveSelectBehavior.Perform(new List<List<Character>>());
            targets = chr1.GetTargets()[0];
            Assert.IsTrue(targets.Count == 0, $"There were {targets.Count} targets instead of 0.");

            // Reads from default target behavior -> targets.
            chr1.DefaultTargetBehavior.Perform(
                new List<List<Character>>() { new List<Character>() { new Character() } }, chr1);
            targets = chr1.GetTargets()[0];
            Assert.IsTrue(targets.Count == 1, $"There were {targets.Count} targets instead of 1.");

            // Reads from default target behavior -> override targets.
            // Takes precedence over default target behavior -> targets.
            chr1.DefaultTargetBehavior.OverrideTargets = new List<Character>() { new Character(), new Character() };
            targets = chr1.GetTargets()[0];
            Assert.IsTrue(targets.Count == 2, $"There were {targets.Count} targets instead of 2.");

            // Reads from move target behavior -> targets.
            // Takes precedence over default target behavior -> override targets.
            Move move = new Move();
            chr1.MoveSelectBehavior.Moves.Clear();
            chr1.MoveSelectBehavior.Moves.Add(move);
            chr1.MoveSelectBehavior.Perform(new List<List<Character>>());
            move.TargetBehavior = new TargetBehavior() { MaxNumberTargets = 4 };

            move.TargetBehavior.Perform(
                new List<List<Character>>() { new List<Character>() { new Character(), new Character(), new Character() } }, chr1);
            targets = chr1.GetTargets()[0];
            Assert.IsTrue(targets.Count == 3, $"There were {targets.Count} targets instead of 3.");

            // Reads from move target behavior -> override targets.
            // Takes precedence over move target behavior -> targets.
            move.TargetBehavior.OverrideTargets =
                new List<Character>() { new Character(), new Character(), new Character(), new Character() };            
            targets = chr1.GetTargets()[0];
            Assert.IsTrue(targets.Count == 4, $"There were {targets.Count} targets instead of 4.");
        }

        /// <summary>
        /// GetTargetsFlat() correctly combines targets from multiple moves.
        /// </summary>
        [TestMethod]
        public void GetTargetsFlatTest()
        {
            Character chr = new Character();
            chr.TeamID = 1;

            Move move1 = new Move() { TargetBehavior = new TargetBehavior() };
            Move move2 = new Move() { TargetBehavior = new TargetBehavior() };
            move1.TurnFraction = 0.5f;
            move2.TurnFraction = 0.5f;
            move1.TargetBehavior.OverrideTargets = new List<Character>() { new Character() };
            move2.TargetBehavior.OverrideTargets = new List<Character>() { new Character() };
            chr.MoveSelectBehavior.Moves.Add(move1);
            chr.MoveSelectBehavior.Moves.Add(move2);

            chr.MoveSelectBehavior.GetMoves = (combatHistory, motives, moves) => { return moves; };
            chr.MoveSelectBehavior.Perform(new List<List<Character>>());
            List<Character> targets = chr.GetTargetsFlat();

            Assert.IsTrue(targets.Count == 2, $"There were {targets.Count} targets instead of 2.");
        }
    }
}