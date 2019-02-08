using System;
using System.Collections.Generic;

namespace Parry
{
    /// <summary>
    /// Describes a shape at specific coordinates in combat. It can be used to
    /// act as a solid obstacle, select targets that fall within, or affect
    /// the stats of characters in the zone.
    /// </summary>
    public class Geometry
    {
        #region Variables
        /// <summary>
        /// Keeps track of all characters in the geometry.
        /// </summary>
        private List<Character> CharactersInZone;

        /// <summary>
        /// For rectangles, the height of the rectangle. Readonly.
        /// </summary>
        public float Height
        {
            get;
            private set;
        }

        /// <summary>
        /// For circles, the radius of the circle. Readonly.
        /// </summary>
        public float Radius
        {
            get;
            private set;
        }

        /// <summary>
        /// The shape of the geometry. Readonly.
        /// </summary>
        public Constants.GeometryShapes Shape
        {
            get;
            private set;
        }

        /// <summary>
        /// For rectangles, the width of the rectangle. Readonly.
        /// </summary>
        public float Width
        {
            get;
            private set;
        }

        /// <summary>
        /// The horizontal position on the battlefield.
        /// </summary>
        public float XPos
        {
            get;
            set;
        }

        /// <summary>
        /// The vertical position on the battlefield.
        /// </summary>
        public float YPos
        {
            get;
            set;
        }
        #endregion

        #region Events
        /// <summary>
        /// The event raised when a character enters a zone.
        /// First argument is the zone which was entered.
        /// Second argument is the character that entered the zone.
        /// </summary>
        public event Action<Geometry, Character> ZoneEntered;

        /// <summary>
        /// The event raised when a character exits a zone.
        /// First argument is the zone which was exited.
        /// Second argument is the character that exited the zone.
        /// </summary>
        public event Action<Geometry, Character> ZoneExited;
        #endregion

        #region Constructors
        /// <summary>
        /// Defines a rectangle at the given coordinates.
        /// </summary>
        /// <param name="x">Horizontal position on the battlefield.</param>
        /// <param name="y">Vertical position on the battlefield.</param>
        /// <param name="width">Width of the rectangle.</param>
        /// <param name="height">Height of the rectangle.</param>
        public Geometry(float x, float y, float width, float height)
        {
            XPos = x;
            YPos = y;
            Width = width;
            Height = height;
            Radius = 0;
            Shape = Constants.GeometryShapes.Rectangle;
            CharactersInZone = new List<Character>();
    }

        /// <summary>
        /// Defines a circle at the given coordinates.
        /// </summary>
        /// <param name="x">Horizontal position on the battlefield.</param>
        /// <param name="y">Vertical position on the battlefield.</param>
        /// <param name="radius">Size of the circle.</param>
        public Geometry(float x, float y, float radius)
        {
            XPos = x;
            YPos = y;
            Width = 0;
            Height = 0;
            Radius = radius;
            Shape = Constants.GeometryShapes.Circle;
            CharactersInZone = new List<Character>();
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        public Geometry(Geometry other)
        {
            XPos = other.XPos;
            YPos = other.YPos;
            Width = other.Width;
            Height = other.Height;
            Radius = other.Radius;
            Shape = other.Shape;
            CharactersInZone = new List<Character>(other.CharactersInZone);
        }
        #endregion

        #region Methods
        /// <summary>
        /// Returns the cached list of characters in this zone. Use
        /// IsIntersecting to refresh the cache.
        /// </summary>
        public List<Character> GetCharactersInZone()
        {
            return new List<Character>(CharactersInZone);
        }

        /// <summary>
        /// Returns true if the given point is inside or on the perimeter
        /// of the geometry. Does not update characters in the boundary,
        /// and does not trigger zone events.
        /// </summary>
        /// <param name="x">
        /// The x-component of the position to test.
        /// </param>
        /// <param name="y">
        /// The y-component of the position to test.
        /// </param>
        public bool IsIntersecting(float x, float y)
        {
            switch (Shape)
            {
                case Constants.GeometryShapes.Circle:
                    return Math.Sqrt(x * x - XPos * XPos
                        + (y * y - YPos * YPos)) <= Radius;
                case Constants.GeometryShapes.Rectangle:
                    return x >= XPos && x <= XPos + Width &&
                        y >= YPos && y <= YPos + Height;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Returns a list of characters that are inside or on the perimeter
        /// of the geometry. If a character begins or stops intersecting the
        /// geometry, triggers zone events.
        /// </summary>
        /// <param name="chars">
        /// A list of all characters.
        /// </param>
        public List<Character> IsIntersecting(List<Character> chars)
        {
            for (int i = 0; i < chars.Count; i++)
            {
                bool doesIntersect = IsIntersecting(
                    chars[i].CharStats.Location.Data.Item1,
                    chars[i].CharStats.Location.Data.Item2);

                if (doesIntersect && !CharactersInZone.Contains(chars[i]))
                {
                    CharactersInZone.Add(chars[i]);
                    ZoneEntered?.Invoke(this, chars[i]);
                }
                else if (!doesIntersect && CharactersInZone.Contains(chars[i]))
                {
                    CharactersInZone.Remove(chars[i]);
                    ZoneExited?.Invoke(this, chars[i]);
                }
            }

            return CharactersInZone;
        }
        #endregion
    }
}
