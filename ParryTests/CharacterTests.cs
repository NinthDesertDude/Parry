﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            // Ensures there are no targets to begin with.
            Character chr1 = new Character();
            chr1.TeamID = 1;
            List<Character> targets = chr1.GetTargets();
            Assert.IsTrue(targets.Count == 0, $"There were {targets.Count} targets instead of 0.");

            // Reads from default target behavior -> targets.
            chr1.DefaultTargetBehavior.Perform(
                new List<List<Character>>() { new List<Character>() { new Character() } }, chr1);
            targets = chr1.GetTargets();
            Assert.IsTrue(targets.Count == 1, $"There were {targets.Count} targets instead of 1.");

            // Reads from default target behavior -> override targets.
            // Takes precedence over default target behavior -> targets.
            chr1.DefaultTargetBehavior.OverrideTargets = new List<Character>() { new Character(), new Character() };
            targets = chr1.GetTargets();
            Assert.IsTrue(targets.Count == 2, $"There were {targets.Count} targets instead of 2.");

            // Reads from move target behavior -> targets.
            // Takes precedence over default target behavior -> override targets.
            Move move = new Move();
            chr1.MoveSelectBehavior.Moves.Add(move);
            chr1.MoveSelectBehavior.Perform(new List<List<Character>>());
            move.TargetBehavior = new TargetBehavior() { MaxNumberTargets = 4 };

            move.TargetBehavior.Perform(
                new List<List<Character>>() { new List<Character>() { new Character(), new Character(), new Character() } }, chr1);
            targets = chr1.GetTargets();
            Assert.IsTrue(targets.Count == 3, $"There were {targets.Count} targets instead of 3.");

            // Reads from move target behavior -> override targets.
            // Takes precedence over move target behavior -> targets.
            move.TargetBehavior.OverrideTargets =
                new List<Character>() { new Character(), new Character(), new Character(), new Character() };            
            targets = chr1.GetTargets();
            Assert.IsTrue(targets.Count == 4, $"There were {targets.Count} targets instead of 4.");
        }
    }
}