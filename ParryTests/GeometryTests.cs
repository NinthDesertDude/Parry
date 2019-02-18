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
    }
}