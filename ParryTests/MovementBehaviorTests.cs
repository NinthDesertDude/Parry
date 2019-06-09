using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using static Parry.MovementBehavior;

namespace Parry.Tests
{
    [TestClass]
    public class MovementBehaviorTests
    {
        private Character ally1 = new Character()
        {
            CombatStats = new CombatStats() { MovementRate = new Stat<float>(4) },
            CharStats = new CharacterStats() { Location = new Stat<Tuple<float, float>>(new Tuple<float, float>(10, 10)) }
        };

        private Character ally2 = new Character()
        {
            CombatStats = new CombatStats() { MovementRate = new Stat<float>(10) },
            CharStats = new CharacterStats() { Location = new Stat<Tuple<float, float>>(new Tuple<float, float>(15, 15)) }
        };

        private Character enemy1 = new Character()
        {
            TeamID = 1,
            CharStats = new CharacterStats() { Location = new Stat<Tuple<float, float>>(new Tuple<float, float>(5, 5)) }
        };

        private Character enemy2 = new Character()
        {
            TeamID = 1,
            CharStats = new CharacterStats() { Location = new Stat<Tuple<float, float>>(new Tuple<float, float>(50, 50)) }
        };

        private Character enemy3 = new Character()
        {
            TeamID = 1,
            CharStats = new CharacterStats() { Location = new Stat<Tuple<float, float>>(new Tuple<float, float>(8, 8)) }
        };

        /// <summary>
        /// Tests the various constructor methods.
        /// </summary>
        [TestMethod]
        public void ConstructorTest()
        {
            MovementBehavior behavior = new MovementBehavior(MotionOrigin.Furthest, Motion.Away);
            MovementBehavior behavior2 = new MovementBehavior(behavior);
            Assert.IsTrue(behavior2.Movements[0].Origin == MotionOrigin.Furthest,
                $"Expected {MotionOrigin.Furthest}, but got {behavior2.Movements[0].Origin} instead.");

            MovementBehavior behavior3 = new MovementBehavior(
                new List<Movement>() { new Movement(MotionOrigin.Nearest, Motion.Away) });
            Assert.IsTrue(behavior3.Movements[0].Origin == MotionOrigin.Nearest,
                $"Expected {MotionOrigin.Nearest}, but got {behavior3.Movements[0].Origin} instead.");

            MovementBehavior behavior4 = new MovementBehavior(
                new List<Movement>(), new List<Character>() { new Character() });
            Assert.IsTrue(behavior4.Targets.Count == 1,
                $"Expected one target, got {behavior4.Targets.Count} instead");
        }

        /// <summary>
        /// Ensures movements can be defined and passed in, and the last to
        /// run successfully will be the movement used.
        /// </summary>
        [TestMethod]
        public void MovementsTest()
        {
            Character chr = new Character()
            {
                CombatStats = new CombatStats() { MovementRate = new Stat<float>(100) },
                CharStats = new CharacterStats() { Location = new Stat<Tuple<float, float>>(new Tuple<float, float>(10, 10)) }
            };

            // First movement shouldn't be chosen because it returns false.
            List<Movement> movements = new List<Movement>();
            movements.Add(new Movement((_) => { return false; }, MotionOrigin.First, Motion.Towards));
            movements.Add(new Movement((_) => { return true; }, MotionOrigin.Average, Motion.Towards));

            MovementBehavior behavior = new MovementBehavior(movements);
            List<Character> chars = new List<Character>() { chr, enemy1, enemy2 };

            Tuple<float, float> result = behavior.Perform(chars, chr);
            var newLoc = new Tuple<double, double>(result.Item1, result.Item2);
            var expectedLoc = new Tuple<float, float>(
                (enemy1.CharStats.Location.RawData.Item1 + enemy2.CharStats.Location.RawData.Item1) / 2,
                (enemy1.CharStats.Location.RawData.Item2 + enemy2.CharStats.Location.RawData.Item2) / 2);

            Assert.IsTrue(expectedLoc.Item1 == newLoc.Item1,
                $"Expected {expectedLoc.Item1}, but got {newLoc.Item1} instead.");
            Assert.IsTrue(expectedLoc.Item2 == newLoc.Item2,
                $"Expected {expectedLoc.Item2}, but got {newLoc.Item2} instead.");
        }

        /// <summary>
        /// Guarantees Perform works with MotionOrigin.Average.
        /// </summary>
        [TestMethod]
        public void PerformAverageTest()
        {
            MovementBehavior movement = new MovementBehavior(MotionOrigin.Average, Motion.Towards);
            Character chr = new Character();
            chr.CombatStats.MovementRate.RawData = 1000;

            List<Character> chars = new List<Character>() { chr, enemy2, enemy3 };
            double avgX = (enemy2.CharStats.Location.RawData.Item1 + enemy3.CharStats.Location.RawData.Item1) / 2;
            double avgY = (enemy2.CharStats.Location.RawData.Item2 + enemy3.CharStats.Location.RawData.Item2) / 2;

            Tuple<float, float> result = movement.Perform(chars, chr);
            Assert.IsTrue(result.Item1 == avgX, $"Expected X position {result.Item1} to be {avgX}.");
            Assert.IsTrue(result.Item2 == avgY, $"Expected Y position {result.Item2} to be {avgY}.");
        }

        /// <summary>
        /// Guarantees Perform works with Motion.Away.
        /// </summary>
        [TestMethod]
        public void PerformAwayTest()
        {
            float magnitude = ally1.CombatStats.MovementRate.RawData;

            MovementBehavior movement = new MovementBehavior(MotionOrigin.First, Motion.Away);
            List<Character> chars = new List<Character>() { ally1, enemy1, enemy2 };

            Tuple<float, float> result = movement.Perform(chars, ally1);
            var newLoc = new Tuple<double, double>(Math.Round(result.Item1, 6), Math.Round(result.Item2, 6));
            var expectedLoc = new Tuple<double, double>(
                Math.Round(ally1.CharStats.Location.RawData.Item1 + magnitude * Math.Cos(Math.PI / 4), 6),
                Math.Round(ally1.CharStats.Location.RawData.Item2 + magnitude * Math.Sin(Math.PI / 4), 6));

            Assert.IsTrue(expectedLoc.Item1 == newLoc.Item1,
                $"Expected {expectedLoc.Item1}, but got {newLoc.Item1} instead.");
            Assert.IsTrue(expectedLoc.Item2 == newLoc.Item2,
                $"Expected {expectedLoc.Item2}, but got {newLoc.Item2} instead.");
        }

        /// <summary>
        /// Guarantees Perform works with Motion.AwayUpToDistance.
        /// </summary>
        [TestMethod]
        public void PerformAwayUpToDistanceTest()
        {
            const int allowedDistGreaterThanMovement = 12;
            const int allowedDistLessThanMovement = 3;

            // Tests movement away, up to a distance given a distance greater than movement rate.
            float magnitudeLesser = Math.Min(ally1.CombatStats.MovementRate.RawData, allowedDistGreaterThanMovement);
            MovementBehavior movement = new MovementBehavior(MotionOrigin.First, Motion.AwayUpToDistance);
            movement.DistanceRange = new Tuple<int, int>(allowedDistGreaterThanMovement, 0);
            List<Character> chars = new List<Character>() { ally1, enemy2, enemy1 };

            Tuple<float, float> result = movement.Perform(chars, ally1);
            var newLoc = new Tuple<double, double>(Math.Round(result.Item1, 6), Math.Round(result.Item2, 6));
            var expectedLoc = new Tuple<double, double>(
                Math.Round(ally1.CharStats.Location.RawData.Item1 - magnitudeLesser * Math.Cos(Math.PI / 4), 6),
                Math.Round(ally1.CharStats.Location.RawData.Item2 - magnitudeLesser * Math.Sin(Math.PI / 4), 6));

            Assert.IsTrue(expectedLoc.Item1 == newLoc.Item1,
                $"Expected {expectedLoc.Item1}, but got {newLoc.Item1} instead.");
            Assert.IsTrue(expectedLoc.Item2 == newLoc.Item2,
                $"Expected {expectedLoc.Item2}, but got {newLoc.Item2} instead.");

            // Tests movement away, up to a distance given a distance less than movement rate.
            float magnitudeGreater = Math.Min(ally1.CombatStats.MovementRate.RawData, allowedDistLessThanMovement);
            movement.DistanceRange = new Tuple<int, int>(allowedDistLessThanMovement, 0);

            result = movement.Perform(chars, ally1);
            newLoc = new Tuple<double, double>(Math.Round(result.Item1, 6), Math.Round(result.Item2, 6));
            expectedLoc = new Tuple<double, double>(
                Math.Round(ally1.CharStats.Location.RawData.Item1 - magnitudeGreater * Math.Cos(Math.PI / 4), 6),
                Math.Round(ally1.CharStats.Location.RawData.Item2 - magnitudeGreater * Math.Sin(Math.PI / 4), 6));

            Assert.IsTrue(expectedLoc.Item1 == newLoc.Item1,
                $"Expected {expectedLoc.Item1}, but got {newLoc.Item1} instead.");
            Assert.IsTrue(expectedLoc.Item2 == newLoc.Item2,
                $"Expected {expectedLoc.Item2}, but got {newLoc.Item2} instead.");
        }

        /// <summary>
        /// Guarantees MotionOrigin.First works with multiple targets as expected.
        /// </summary>
        [TestMethod]
        public void PerformFirstWithMultipleTargetsTest()
        {
            float magnitude = ally1.CombatStats.MovementRate.RawData;

            MovementBehavior movement = new MovementBehavior(MotionOrigin.First, Motion.Towards);
            List<Character> chars = new List<Character>() { ally1, enemy1, enemy2 };

            Tuple<float, float> result = movement.Perform(chars, ally1);
            var newLoc = new Tuple<double, double>(Math.Round(result.Item1, 6), Math.Round(result.Item2, 6));
            var expectedLoc = new Tuple<double, double>(
                Math.Round(ally1.CharStats.Location.RawData.Item1 - magnitude * Math.Cos(Math.PI / 4), 6),
                Math.Round(ally1.CharStats.Location.RawData.Item2 - magnitude * Math.Sin(Math.PI / 4), 6));

            Assert.IsTrue(expectedLoc.Item1 == newLoc.Item1,
                $"Expected {expectedLoc.Item1}, but got {newLoc.Item1} instead.");
            Assert.IsTrue(expectedLoc.Item2 == newLoc.Item2,
                $"Expected {expectedLoc.Item2}, but got {newLoc.Item2} instead.");
        }

        /// <summary>
        /// Guarantees Perform works with MotionOrigin.Furthest.
        /// </summary>
        [TestMethod]
        public void PerformFurthestTest()
        {
            MovementBehavior movement = new MovementBehavior(MotionOrigin.Furthest, Motion.Towards);
            Character chr = new Character();
            chr.CombatStats.MovementRate.RawData = 1000;

            List<Character> chars = new List<Character>() { chr, enemy3, enemy2 };
            var expectedLoc = new Tuple<float, float>(
                enemy2.CharStats.Location.RawData.Item1,
                enemy2.CharStats.Location.RawData.Item2
            );

            Tuple<float, float> result = movement.Perform(chars, chr);
            Assert.IsTrue(result.Item1 == expectedLoc.Item1, $"Expected X position {result.Item1} to be {expectedLoc.Item1}.");
            Assert.IsTrue(result.Item2 == expectedLoc.Item2, $"Expected Y position {result.Item2} to be {expectedLoc.Item2}.");
        }

        /// <summary>
        /// Guarantees Perform works with MotionOrigin.Nearest.
        /// </summary>
        [TestMethod]
        public void PerformNearestTest()
        {
            MovementBehavior movement = new MovementBehavior(MotionOrigin.Nearest, Motion.Towards);
            List<Character> chars = new List<Character>() { ally1, enemy2, enemy3 };
            var expectedLoc = new Tuple<float, float>(
                enemy3.CharStats.Location.RawData.Item1,
                enemy3.CharStats.Location.RawData.Item2
            );

            Tuple<float, float> result = movement.Perform(chars, ally1);
            Assert.IsTrue(result.Item1 == expectedLoc.Item1, $"Expected X position {result.Item1} to be {expectedLoc.Item1}.");
            Assert.IsTrue(result.Item2 == expectedLoc.Item2, $"Expected Y position {result.Item2} to be {expectedLoc.Item2}.");
        }

        /// <summary>
        /// Guarantees Perform works with MotionOrigin.Nearest.
        /// </summary>
        [TestMethod]
        public void PerformNearestToCenterTest()
        {
            MovementBehavior movement = new MovementBehavior(MotionOrigin.NearestToCenter, Motion.Towards);
            Character chr = new Character()
            {
                CombatStats = new CombatStats()
                {
                    MaxRangeAllowed = new Stat<int>(130),
                    MinRangeRequired = new Stat<int>(10),
                    MovementRate = new Stat<float>(1000)
                }
            };

            List<Character> chars = new List<Character>() { chr, enemy1, enemy2, enemy3 };
            var expectedLoc = new Tuple<float, float>(
                enemy2.CharStats.Location.RawData.Item1,
                enemy2.CharStats.Location.RawData.Item2
            );

            Tuple<float, float> result = movement.Perform(chars, chr);
            Assert.IsTrue(result.Item1 == expectedLoc.Item1, $"Expected X position {result.Item1} to be {expectedLoc.Item1}.");
            Assert.IsTrue(result.Item2 == expectedLoc.Item2, $"Expected Y position {result.Item2} to be {expectedLoc.Item2}.");
        }

        /// <summary>
        /// Guarantees Perform works as expected when no enemies are provided.
        /// </summary>
        [TestMethod]
        public void PerformNoEnemiesTest()
        {
            MovementBehavior movement = new MovementBehavior(MotionOrigin.First, Motion.Towards);
            List<Character> chars = new List<Character>() { ally1, ally2 };

            Tuple<float, float> result = movement.Perform(chars, ally1);
            Assert.IsTrue(result.Item1 == ally1.CharStats.Location.RawData.Item1, "Expected no change in x.");
            Assert.IsTrue(result.Item2 == ally1.CharStats.Location.RawData.Item2, "Expected no change in y.");
        }

        /// <summary>
        /// Guarantees Perform works with Motion.ToDistance.
        /// </summary>
        [TestMethod]
        public void PerformToDistanceTest()
        {
            // Tests with distance > midpoint > distance range.
            double magnitude = 0.5; // Midpoint of 0 and 1.

            MovementBehavior movement = new MovementBehavior(MotionOrigin.First, Motion.ToDistance);
            movement.DistanceRange = new Tuple<int, int>(0, 1);
            List<Character> chars = new List<Character>() { ally1, enemy3 };

            Tuple<float, float> result = movement.Perform(chars, ally1);
            var newLoc = new Tuple<double, double>(Math.Round(result.Item1, 5), Math.Round(result.Item2, 5));
            var expectedLoc = new Tuple<double, double>(
                Math.Round(enemy3.CharStats.Location.RawData.Item1 + magnitude * Math.Cos(Math.PI / 4), 5),
                Math.Round(enemy3.CharStats.Location.RawData.Item2 + magnitude * Math.Sin(Math.PI / 4), 5));

            Assert.IsTrue(expectedLoc.Item1 == newLoc.Item1,
                $"Expected {expectedLoc.Item1}, but got {newLoc.Item1} instead.");
            Assert.IsTrue(expectedLoc.Item2 == newLoc.Item2,
                $"Expected {expectedLoc.Item2}, but got {newLoc.Item2} instead.");

            // Tests as above, with minimum distance. Final loc should be 1.5 from target.
            magnitude = 1.5; // Midpoint of 1 and 2.

            movement.DistanceRange = new Tuple<int, int>(1, 2);
            chars = new List<Character>() { ally2, enemy3 };

            result = movement.Perform(chars, ally2);
            newLoc = new Tuple<double, double>(Math.Round(result.Item1, 6), Math.Round(result.Item2, 6));
            expectedLoc = new Tuple<double, double>(
                Math.Round(enemy3.CharStats.Location.RawData.Item1 + magnitude * Math.Cos(Math.PI / 4), 6),
                Math.Round(enemy3.CharStats.Location.RawData.Item2 + magnitude * Math.Sin(Math.PI / 4), 6));

            Assert.IsTrue(expectedLoc.Item1 == newLoc.Item1,
                $"Expected {expectedLoc.Item1}, but got {newLoc.Item1} instead.");
            Assert.IsTrue(expectedLoc.Item2 == newLoc.Item2,
                $"Expected {expectedLoc.Item2}, but got {newLoc.Item2} instead.");

            // Tests with distance range > distance > midpoint.
            magnitude = 6.1005050633883346; // Midpoint of 16 - distance.

            movement.DistanceRange = new Tuple<int, int>(11, 21);
            chars = new List<Character>() { ally2, enemy3 };

            result = movement.Perform(chars, ally2);
            newLoc = new Tuple<double, double>(Math.Round(result.Item1, 5), Math.Round(result.Item2, 5));
            expectedLoc = new Tuple<double, double>(
                Math.Round(ally2.CharStats.Location.RawData.Item1 + magnitude * Math.Cos(Math.PI / 4), 5),
                Math.Round(ally2.CharStats.Location.RawData.Item2 + magnitude * Math.Sin(Math.PI / 4), 5));

            Assert.IsTrue(expectedLoc.Item1 == newLoc.Item1,
                $"Expected {expectedLoc.Item1}, but got {newLoc.Item1} instead.");
            Assert.IsTrue(expectedLoc.Item2 == newLoc.Item2,
                $"Expected {expectedLoc.Item2}, but got {newLoc.Item2} instead.");

            // Tests with distance range > distance > midpoint.
            magnitude = 1.3284271247461903; // Midpoint of distance - 1.5

            movement.DistanceRange = new Tuple<int, int>(0, 3);
            chars = new List<Character>() { ally1, enemy3 };

            result = movement.Perform(chars, ally1);
            newLoc = new Tuple<double, double>(Math.Round(result.Item1, 5), Math.Round(result.Item2, 5));
            expectedLoc = new Tuple<double, double>(
                Math.Round(ally1.CharStats.Location.RawData.Item1 - magnitude * Math.Cos(Math.PI / 4), 5),
                Math.Round(ally1.CharStats.Location.RawData.Item2 - magnitude * Math.Sin(Math.PI / 4), 5));

            Assert.IsTrue(expectedLoc.Item1 == newLoc.Item1,
                $"Expected {expectedLoc.Item1}, but got {newLoc.Item1} instead.");
            Assert.IsTrue(expectedLoc.Item2 == newLoc.Item2,
                $"Expected {expectedLoc.Item2}, but got {newLoc.Item2} instead.");
        }

        /// <summary>
        /// Guarantees Perform works with Motion.Towards.
        /// </summary>
        [TestMethod]
        public void PerformTowardsTest()
        {
            float magnitude = ally1.CombatStats.MovementRate.RawData;

            // Moves towards the target, but not enough to reach them.
            MovementBehavior movement = new MovementBehavior(MotionOrigin.First, Motion.Towards);
            List<Character> chars = new List<Character>() { ally1, enemy1 };

            Tuple<float, float> result = movement.Perform(chars, ally1);
            var newLoc = new Tuple<double, double>(Math.Round(result.Item1, 6), Math.Round(result.Item2, 6));
            var expectedLoc = new Tuple<double, double>(
                Math.Round(ally1.CharStats.Location.RawData.Item1 - magnitude * Math.Cos(Math.PI / 4), 6),
                Math.Round(ally1.CharStats.Location.RawData.Item2 - magnitude * Math.Sin(Math.PI / 4), 6));

            Assert.IsTrue(expectedLoc.Item1 == newLoc.Item1,
                $"Expected {expectedLoc.Item1}, but got {newLoc.Item1} instead.");
            Assert.IsTrue(expectedLoc.Item2 == newLoc.Item2,
                $"Expected {expectedLoc.Item2}, but got {newLoc.Item2} instead.");

            // Moves towards the target, enough to pass them by; should reach them instead.
            Character chr2 = new Character() { CombatStats = new CombatStats() { MovementRate = new Stat<float>(20) } };
            chars = new List<Character>() { chr2, enemy1 };

            result = movement.Perform(chars, chr2);
            newLoc = new Tuple<double, double>(Math.Round(result.Item1, 6), Math.Round(result.Item2, 6));
            expectedLoc = new Tuple<double, double>(
                enemy1.CharStats.Location.RawData.Item1,
                enemy1.CharStats.Location.RawData.Item2);

            Assert.IsTrue(expectedLoc.Item1 == newLoc.Item1,
                $"Expected {expectedLoc.Item1}, but got {newLoc.Item1} instead.");
            Assert.IsTrue(expectedLoc.Item2 == newLoc.Item2,
                $"Expected {expectedLoc.Item2}, but got {newLoc.Item2} instead.");
        }

        /// <summary>
        /// Guarantees Perform works with Motion.TowardsUpToDistance.
        /// </summary>
        [TestMethod]
        public void PerformTowardsUpToDistanceTest()
        {
            float magnitude = ally1.CombatStats.MovementRate.RawData;

            // Tests movement towards with a distance > movement rate.
            MovementBehavior movement = new MovementBehavior(MotionOrigin.First, Motion.TowardsUpToDistance);
            movement.DistanceRange = new Tuple<int, int>(100, 0);
            List<Character> chars = new List<Character>() { ally1, enemy2, enemy1 };

            Tuple<float, float> result = movement.Perform(chars, ally1);
            var newLoc = new Tuple<double, double>(Math.Round(result.Item1, 6), Math.Round(result.Item2, 6));
            var expectedLoc = new Tuple<double, double>(
                Math.Round(ally1.CharStats.Location.RawData.Item1 + magnitude * Math.Cos(Math.PI / 4), 6),
                Math.Round(ally1.CharStats.Location.RawData.Item2 + magnitude * Math.Sin(Math.PI / 4), 6));

            Assert.IsTrue(expectedLoc.Item1 == newLoc.Item1,
                $"Expected {Math.Round(expectedLoc.Item1, 6)}, but got {newLoc.Item1} instead.");
            Assert.IsTrue(expectedLoc.Item2 == newLoc.Item2,
                $"Expected {expectedLoc.Item2}, but got {newLoc.Item2} instead.");

            // Tests movement towards with a distance < movement rate. Should be at target.
            chars = new List<Character>() { ally1, enemy3 };

            result = movement.Perform(chars, ally1);
            newLoc = new Tuple<double, double>(result.Item1, result.Item2);
            expectedLoc = new Tuple<double, double>(
                enemy3.CharStats.Location.RawData.Item1,
                enemy3.CharStats.Location.RawData.Item2);

            Assert.IsTrue(expectedLoc.Item1 == newLoc.Item1,
                $"Expected {expectedLoc.Item1}, but got {newLoc.Item1} instead.");
            Assert.IsTrue(expectedLoc.Item2 == newLoc.Item2,
                $"Expected {expectedLoc.Item2}, but got {newLoc.Item2} instead.");
        }

        /// <summary>
        /// Guarantees Perform works with Motion.WithinDistanceRange.
        /// </summary>
        [TestMethod]
        public void PerformWithinDistanceRangeTest()
        {
            // Tests moving within an exact distance range.
            MovementBehavior movement = new MovementBehavior(MotionOrigin.First, Motion.WithinDistanceRange);
            movement.DistanceRange = new Tuple<int, int>(0, 100);
            List<Character> chars = new List<Character>() { ally1, enemy3 };

            Tuple<float, float> result = movement.Perform(chars, ally1);
            var expectedLoc = new Tuple<double, double>(
                enemy3.CharStats.Location.RawData.Item1,
                enemy3.CharStats.Location.RawData.Item2);

            Assert.IsTrue(expectedLoc.Item1 == result.Item1,
                $"Expected {Math.Round(expectedLoc.Item1, 6)}, but got {result.Item1} instead.");
            Assert.IsTrue(expectedLoc.Item2 == result.Item2,
                $"Expected {expectedLoc.Item2}, but got {result.Item2} instead.");

            // Tests moving with a maximum distance. Should move past target, but cap at distance of 1.
            movement.DistanceRange = new Tuple<int, int>(0, 1);
            result = movement.Perform(chars, ally1);
            var newLoc = new Tuple<double, double>(Math.Round(result.Item1, 6), Math.Round(result.Item2, 6));
            expectedLoc = new Tuple<double, double>(
                Math.Round(ally1.CharStats.Location.RawData.Item1 - Math.Cos(Math.PI / 4), 6),
                Math.Round(ally1.CharStats.Location.RawData.Item2 - Math.Sin(Math.PI / 4), 6));

            Assert.IsTrue(expectedLoc.Item1 == newLoc.Item1,
                $"Expected {Math.Round(expectedLoc.Item1, 6)}, but got {newLoc.Item1} instead.");
            Assert.IsTrue(expectedLoc.Item2 == newLoc.Item2,
                $"Expected {expectedLoc.Item2}, but got {newLoc.Item2} instead.");

            // Tests moving with a minimum distance. Should move backwards to distance of 5.
            const int minRange = 5;
            double magnitude =  minRange - 2 * Math.Sqrt(2); // distance between (10, 10) and (8, 8)

            movement.DistanceRange = new Tuple<int, int>(minRange, 0);
            result = movement.Perform(chars, ally1);
            newLoc = new Tuple<double, double>(Math.Round(result.Item1, 6), Math.Round(result.Item2, 6));
            expectedLoc = new Tuple<double, double>(
                Math.Round(ally1.CharStats.Location.RawData.Item1 + magnitude * Math.Cos(Math.PI / 4), 6),
                Math.Round(ally1.CharStats.Location.RawData.Item2 + magnitude * Math.Sin(Math.PI / 4), 6));

            Assert.IsTrue(expectedLoc.Item1 == newLoc.Item1,
                $"Expected {Math.Round(expectedLoc.Item1, 6)}, but got {newLoc.Item1} instead.");
            Assert.IsTrue(expectedLoc.Item2 == newLoc.Item2,
                $"Expected {expectedLoc.Item2}, but got {newLoc.Item2} instead.");
        }

        /// <summary>
        /// Uses target locations when set.
        /// </summary>
        [TestMethod]
        public void TargetLocationsTest()
        {
            MovementBehavior movement = new MovementBehavior(MotionOrigin.First, Motion.Towards);
            var location = new Tuple<float, float>(10, 14);
            movement.TargetLocations.Add(location);

            Tuple<float, float> result = movement.Perform(new List<Character>(), ally1);

            Assert.IsTrue(location.Item1 == result.Item1,
                $"Expected {location.Item1}, but got {result.Item1} instead.");
            Assert.IsTrue(location.Item2 == result.Item2,
                $"Expected {location.Item2}, but got {result.Item2} instead.");
        }

        /// <summary>
        /// Ensures targets are used when defined.
        /// </summary>
        [TestMethod]
        public void TargetsTest()
        {
            MovementBehavior movement = new MovementBehavior(MotionOrigin.First, Motion.Towards);
            movement.Targets = new List<Character>() { enemy3 };

            Tuple<float, float> result = movement.Perform(new List<Character>(), ally1);

            Assert.IsTrue(enemy3.CharStats.Location.RawData.Item1 == result.Item1,
                $"Expected {enemy3.CharStats.Location.RawData.Item1}, but got {result.Item1} instead.");
            Assert.IsTrue(enemy3.CharStats.Location.RawData.Item2 == result.Item2,
                $"Expected {enemy3.CharStats.Location.RawData.Item2}, but got {result.Item2} instead.");
        }

        /// <summary>
        /// Ensures that when targeting targets is on, the character's target list is used.
        /// </summary>
        [TestMethod]
        public void UseTargetingTargetsTest()
        {
            MovementBehavior movement = new MovementBehavior(MotionOrigin.First, Motion.Towards);
            movement.UseTargetingTargets = true;
            Character chr = new Character();
            chr.CharStats.Location.RawData = new Tuple<float, float>(8, 7);
            chr.CombatStats.MovementRate.RawData = 10;
            chr.DefaultTargetBehavior.OverrideTargets = new List<Character>() { ally1 };

            Tuple<float, float> result = movement.Perform(new List<Character>(), chr);

            Assert.IsTrue(ally1.CharStats.Location.RawData.Item1 == result.Item1,
                $"Expected {ally1.CharStats.Location.RawData.Item1}, but got {result.Item1} instead.");
            Assert.IsTrue(ally1.CharStats.Location.RawData.Item2 == result.Item2,
                $"Expected {ally1.CharStats.Location.RawData.Item2}, but got {result.Item2} instead.");
        }
    }
}