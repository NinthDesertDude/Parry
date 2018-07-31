using System;
using System.Collections.Generic;

namespace Parry.Combat
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
        public List<Character> CharactersInZone
        {
            get
            {
                return new List<Character>(CharactersInZone);
            }
            private set
            {
                CharactersInZone = value;
            }
        }

        /// <summary>
        /// For rectangles, the height of the rectangle. Readonly.
        /// </summary>
        public int Height
        {
            get;
            private set;
        }

        /// <summary>
        /// For circles, the radius of the circle. Readonly.
        /// </summary>
        public int Radius
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
        public int Width
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
        public event Action<Geometry, Combatant> ZoneEntered;

        /// <summary>
        /// The event raised when a character exits a zone.
        /// First argument is the zone which was exited.
        /// Second argument is the character that exited the zone.
        /// </summary>
        public event Action<Geometry, Combatant> ZoneExited;
        #endregion

        #region Constructors
        /// <summary>
        /// Defines a rectangle at the given coordinates.
        /// </summary>
        /// <param name="x">Horizontal position on the battlefield.</param>
        /// <param name="y">Vertical position on the battlefield.</param>
        /// <param name="width">Width of the rectangle.</param>
        /// <param name="height">Height of the rectangle.</param>
        public Geometry(int x, int y, int width, int height)
        {
            XPos = x;
            YPos = y;
            Width = width;
            Height = height;
            Radius = 0;
            Shape = Constants.GeometryShapes.Rectangle;
    }

        /// <summary>
        /// Defines a circle at the given coordinates.
        /// </summary>
        /// <param name="x">Horizontal position on the battlefield.</param>
        /// <param name="y">Vertical position on the battlefield.</param>
        /// <param name="radius">Size of the circle.</param>
        public Geometry(int x, int y, int radius)
        {
            XPos = x;
            YPos = y;
            Width = 0;
            Height = 0;
            Radius = radius;
            Shape = Constants.GeometryShapes.Circle;
        }
        #endregion

        #region Methods
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
                    return (Math.Sqrt((x * x - XPos * XPos)
                        + (y * y - YPos * YPos)) <= Radius);
                case Constants.GeometryShapes.Rectangle:
                    return (x >= XPos && x <= XPos + Width &&
                        y >= YPos && y <= YPos + Height);
                default:
                    return false;
            }
        }

        /// <summary>
        /// Returns a list of characters that are inside or on the perimeter
        /// of the geometry. If a character begins or stops intersecting the
        /// geometry, triggers zone events.
        /// </summary>
        /// <param name="characters">
        /// A list of all characters.
        /// </param>
        public List<Character> IsIntersecting(List<Character> characters)
        {
            for (int i = 0; i < characters.Count; i++)
            {
                bool doesIntersect = IsIntersecting(
                    characters[i].Location.Data.Item1,
                    characters[i].Location.Data.Item2);

                if (doesIntersect && !CharactersInZone.Contains(characters[i]))
                {
                    CharactersInZone.Add(characters[i]);
                    ZoneEntered?.Invoke(this, characters[i]);
                }
                else if (!doesIntersect && CharactersInZone.Contains(characters[i]))
                {
                    CharactersInZone.Remove(characters[i]);
                    ZoneExited?.Invoke(this, characters[i]);
                }
            }

            return CharactersInZone;
        }
        #endregion
    }
}
