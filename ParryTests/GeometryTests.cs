using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Parry.Tests
{
    [TestClass]
    public class GeometryTests
    {
        /// <summary>
        /// Ensures that constructors set appropriate values for each shape.
        /// </summary>
        [TestMethod]
        public void ConstructionTest()
        {
            Geometry geo = new Geometry(1, 2, 3);
            Assert.IsTrue(geo.XPos == 1, "Geometry x-position didn't match expected value.");
            Assert.IsTrue(geo.YPos == 2, "Geometry y-position didn't match expected value.");
            Assert.IsTrue(geo.Radius == 3, "Geometry radius didn't match expected value.");

            Geometry geo2 = new Geometry(1, 2, 3, 4);
            Assert.IsTrue(geo2.XPos == 1, "Geometry x-position didn't match expected value.");
            Assert.IsTrue(geo2.YPos == 2, "Geometry y-position didn't match expected value.");
            Assert.IsTrue(geo2.Width == 3, "Geometry width didn't match expected value.");
            Assert.IsTrue(geo2.Height == 4, "Geometry height didn't match expected value.");

            Geometry geo3 = new Geometry(geo2);
            Assert.IsTrue(geo3.Width == 3, "Geometry didn't copy correctly for width.");
        }

        /// <summary>
        /// Ensures circles intersect points and character positions correctly.
        /// </summary>
        [TestMethod]
        public void IsIntersectingCircleTest()
        {
            Geometry geo = new Geometry(0, 0, 0);
            Assert.IsTrue(geo.IsIntersecting(0, 0),
                "Expected point 0,0 to intersect circle at pos 0,0 with radius 0.");

            var chars = new List<Character>() { new Character(), new Character() };
            chars[0].CharStats.Location.Data = new System.Tuple<float, float>(0, 0);
            chars[1].CharStats.Location.Data = new System.Tuple<float, float>(0, 1);

            List<Character> intersectingChars = geo.IsIntersecting(chars);
            Assert.IsTrue(intersectingChars.Contains(chars[0]),
                "Expected char at point 0,0 to intersect circle at pos 0,0 with radius 0.");
            Assert.IsFalse(intersectingChars.Contains(chars[1]),
                "Expected char at point 0,1 not to intersect circle at pos 0,0 with radius 0.");

            geo = new Geometry(0, 0, 1);
            intersectingChars = geo.IsIntersecting(chars);
            Assert.IsTrue(intersectingChars.Contains(chars[1]),
                "Expected char at point 0,1 to intersect circle at pos 0,0 with radius 1.");

            geo = new Geometry(0.2f, 0.3f, 0.5f);
            Assert.IsTrue(intersectingChars.Contains(chars[0]),
                "Expected char at point 0,0 to intersect circle at pos 0.2,0.3 with radius 0.5.");
        }

        /// <summary>
        /// Ensures rectangles intersect points and character positions correctly.
        /// </summary>
        [TestMethod]
        public void IsIntersectingRectangleTest()
        {
            Geometry geo = new Geometry(0, 0, 0, 0);
            Assert.IsTrue(geo.IsIntersecting(0, 0),
                "Expected point 0,0 to intersect rectangle at pos 0,0 with no width or height.");

            var chars = new List<Character>() { new Character(), new Character() };
            chars[0].CharStats.Location.Data = new System.Tuple<float, float>(0, 0);
            chars[1].CharStats.Location.Data = new System.Tuple<float, float>(1, 1);

            List<Character> intersectingChars = geo.IsIntersecting(chars);
            Assert.IsTrue(intersectingChars.Contains(chars[0]),
                "Expected char at point 0,0 to intersect rectangle at pos 0,0 with no width or height.");
            Assert.IsFalse(intersectingChars.Contains(chars[1]),
                "Expected char at point 1,1 not to intersect rectangle at pos 0,0 with no width or height.");

            geo = new Geometry(0, 0, 1, 1);
            intersectingChars = geo.IsIntersecting(chars);
            Assert.IsTrue(intersectingChars.Contains(chars[1]),
                "Expected char at point 1,1 to intersect rectangle at pos 0,0 with width/height of 1.");

            geo = new Geometry(0.2f, 0.3f, 0.5f, 0.6f);
            chars[1].CharStats.Location.Data = new System.Tuple<float, float>(1, 1);
            Assert.IsTrue(intersectingChars.Contains(chars[0]),
                "Expected char at point 0,0 to intersect circle at pos 0.2,0.3 with radius 0.5.");
        }

        /// <summary>
        /// Ensures zone enter and zone exit events are raised when characters
        /// are found to have changed status with relation to being in or
        /// outside a zone.
        /// </summary>
        [TestMethod]
        public void ZoneEventsAndGetCharactersInZoneTest()
        {
            Geometry geo = new Geometry(0, 0, 10, 10);

            Character chr1 = new Character();
            chr1.CharStats.Location.RawData = new System.Tuple<float, float>(1, 1);
            Character chr2 = new Character();
            chr2.CharStats.Location.RawData = new System.Tuple<float, float>(2, 2);
            Character chr3 = new Character();
            chr3.CharStats.Location.RawData = new System.Tuple<float, float>(11, 11);
            Character chr4 = new Character();
            chr4.CharStats.Location.RawData = new System.Tuple<float, float>(12, 12);
            List<Character> allChars = new List<Character>() { chr1, chr2, chr3, chr4 };
            
            // Ensure no characters in zone by default.
            int numCharsInZone = geo.GetCharactersInZone().Count;
            Assert.IsTrue(numCharsInZone == 0,
                $"Expected 0 characters in zone, got {numCharsInZone} instead.");

            bool didEnter1 = false;
            bool didEnter2 = false;
            bool didEnter3 = false;
            bool didEnter1_Geo = false;
            bool didEnter2_Geo = false;
            bool didEnter3_Geo = false;
            chr1.EnterZone += geometry => { didEnter1 = true;  };
            chr2.EnterZone += geometry => { didEnter2 = true; };
            chr3.EnterZone += geometry => { didEnter3 = true; };
            geo.ZoneEntered += chr => {
                if (chr == chr1) { didEnter1_Geo = true; }
                else if (chr == chr2) { didEnter2_Geo = true; }
                else if (chr == chr3) { didEnter3_Geo = true; }
            };

            // Should trigger EnterZone for chr1 and chr2.
            List<Character> charsReturned = geo.IsIntersecting(allChars);

            // Ensure EnterZone is triggered for chr1 and chr2, but not chr3.
            Assert.IsTrue(didEnter1, $"Expected didEnter1 to be true, got {didEnter1} instead.");
            Assert.IsTrue(didEnter2, $"Expected didEnter2 to be true, got {didEnter2} instead.");
            Assert.IsFalse(didEnter3, $"Expected didEnter3 to be false, got {didEnter3} instead.");
            Assert.IsTrue(didEnter1_Geo, $"Expected didEnter1_Geo to be true, got {didEnter1_Geo} instead.");
            Assert.IsTrue(didEnter2_Geo, $"Expected didEnter2_Geo to be true, got {didEnter2_Geo} instead.");
            Assert.IsFalse(didEnter3_Geo, $"Expected didEnter3_Geo to be false, got {didEnter3_Geo} instead.");

            numCharsInZone = geo.GetCharactersInZone().Count;
            Assert.IsTrue(numCharsInZone == 2,
                $"Expected 2 characters in zone (part 1), got {numCharsInZone} instead.");

            chr2.CharStats.Location.RawData = new System.Tuple<float, float>(13, 13);
            chr4.CharStats.Location.RawData = new System.Tuple<float, float>(2, 2);

            bool didExit1 = false;
            bool didExit2 = false;
            bool didEnter4 = false;
            bool didExit1_Geo = false;
            bool didExit2_Geo = false;
            bool didEnter4_Geo = false;
            chr1.ExitZone += geometry => { didExit1 = true; };
            chr2.ExitZone += geometry => { didExit2 = true; };
            chr4.EnterZone += geometry => { didEnter4 = true; };
            geo.ZoneExited += chr => {
                if (chr == chr1) { didExit1_Geo = true; }
                else if (chr == chr2) { didExit2_Geo = true; }
            };
            geo.ZoneEntered += chr => {
                if (chr == chr4) { didEnter4_Geo = true; }
            };

            // Should trigger EnterZone for chr4 and ExitZone for chr2, but not chr1.
            charsReturned = geo.IsIntersecting(allChars);

            Assert.IsTrue(didEnter4, $"Expected didEnter4 to be true, got {didEnter4} instead.");
            Assert.IsTrue(didExit2, $"Expected didExit2 to be true, got {didExit2} instead.");
            Assert.IsFalse(didExit1, $"Expected didExit1 to be false, got {didExit1} instead.");
            Assert.IsTrue(didEnter4_Geo, $"Expected didEnter4_Geo to be true, got {didEnter4_Geo} instead.");
            Assert.IsTrue(didExit2_Geo, $"Expected didExit2_Geo to be true, got {didExit2_Geo} instead.");
            Assert.IsFalse(didExit1_Geo, $"Expected didExit1_Geo to be false, got {didExit1_Geo} instead.");

            numCharsInZone = geo.GetCharactersInZone().Count;
            Assert.IsTrue(numCharsInZone == 2,
                $"Expected 2 characters in zone (part 2), got {numCharsInZone} instead.");
        }
    }
}