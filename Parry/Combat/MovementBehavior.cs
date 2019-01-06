using System;
using System.Collections.Generic;
using System.Linq;

namespace Parry.Combat
{
    /// <summary>
    /// Describes the AI targeting behaviors with tendencies and
    /// a weighting system to determine the best target.
    /// </summary>
    public class MovementBehavior
    {
        #region Variables
        /// <summary>
        /// The first function that returns true given the combatants will
        /// apply the associated motion origin and motion when performing
        /// movement behavior.
        /// </summary>
        public List<Movement> Movements;

        /// <summary>
        /// When targets are set, that list is used instead of defaulting to
        /// non-allied combatants.
        /// </summary>
        public List<Combatant> Targets;

        /// <summary>
        /// Motions that use distance will use the first value or both if a
        /// range is required.
        /// Default value is 0, 0.
        /// </summary>
        public Tuple<int, int> DistanceRange;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a movement behavior with a default movement that uses the
        /// provided motion and motion origin. When this behavior is performed,
        /// this origin and motion will always be used.
        /// </summary>
        public MovementBehavior(MotionOrigin origin, Motion motion)
        {
            Movements = new List<Movement>() { new Movement(origin, motion) };
            Targets = new List<Combatant>();
            DistanceRange = new Tuple<int, int>(0, 0);
        }

        /// <summary>
        /// Creates a new movement behavior without targets.
        /// </summary>
        public MovementBehavior(List<Movement> movements)
        {
            Movements = new List<Movement>(movements);
            Targets = new List<Combatant>();
            DistanceRange = new Tuple<int, int>(0, 0);
        }

        /// <summary>
        /// Creates a new movement behavior using targets.
        /// </summary>
        public MovementBehavior(List<Movement> movements, List<Combatant> targets)
        {
            Movements = movements;
            Targets = targets;
            DistanceRange = new Tuple<int, int>(0, 0);
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="other">
        /// The instance to copy all values from.
        /// </param>
        public MovementBehavior(MovementBehavior other)
        {
            Movements = other.Movements;
            Targets = other.Targets;
            DistanceRange = other.DistanceRange;
        }
        #endregion

        #region Enums
        /// <summary>
        ///  Movements work by moving in relation to a point. This is the point
        /// that movement centers around when there are targets in range of
        /// attack.
        /// </summary>
        public enum MotionOrigin
        {
            /// <summary>
            /// The first non-allied combatant, or the first target if set.
            /// </summary>
            First,

            /// <summary>
            /// The nearest non-allied combatant, or the nearest target if set.
            /// Useful to against enemies closing in.
            /// </summary>
            Nearest,

            /// <summary>
            /// The furthest non-allied combatant, or the furthest target if
            /// set. Useful against enemies that keep a distance.
            /// </summary>
            Furthest,

            /// <summary>
            /// The non-allied combatant closest to the center of attack range,
            /// or the target if set. Useful to maximize number of enemies in
            /// range.
            /// </summary>
            NearestToCenter,

            /// <summary>
            /// The averaged position of all non-allied combatants, or of
            /// targets if set.
            /// </summary>
            Average
        }

        /// <summary>
        /// Different movements in relation to a point.
        /// </summary>
        public enum Motion
        {
            /// <summary>
            /// Move towards the point up to a certain distance, and
            /// don't move away if closer than this distance.
            /// </summary>
            TowardsUpToDistance,

            /// <summary>
            /// Move away from the point up to a certain distance,
            /// and don't move towards if further than this distance.
            /// </summary>
            AwayUpToDistance,

            /// <summary>
            /// Move until between a minimum and maximum distance of the point.
            /// </summary>
            WithinDistanceRange,

            /// <summary>
            /// Move until between a minimum and maximum distance of the point,
            /// closest to the point if possible.
            /// </summary>
            WithinDistanceRangeNear,

            /// <summary>
            /// Move until between a minimum and maximum distance of the point,
            /// furthest to the point if possible.
            /// </summary>
            WithinDistanceRangeFar,

            /// <summary>
            /// Move until between a minimum and maximum distance of the point,
            /// preferring the center of that range if possible.
            /// </summary>
            WithinDistanceRangeCenter,

            /// <summary>
            /// Move towards the point until exactly at the same position.
            /// </summary>
            Towards,

            /// <summary>
            /// Move away from the point indefinitely.
            /// </summary>
            Away
        }
        #endregion

        #region Methods
        /// <summary>
        /// Performs the movement.
        /// </summary>
        public Tuple<float, float> Perform(List<Combatant> combatants, Combatant self)
        {
            MotionOrigin appliedMotionOrigin = MotionOrigin.First;
            Motion appliedMotion = Motion.Towards;

            // Gets the desired motion and motion origin.
            for (int i = 0; i < Movements.Count; i++)
            {
                var movement = Movements[i];
                if (movement.Function(combatants))
                {
                    appliedMotionOrigin = movement.Origin;
                    appliedMotion = movement.Motion;
                    break;
                }
            }

            // Filters to targets only or non-allied combatants if not set.
            List<Combatant> realTargets = (Targets.Count > 0)
                ? Targets
                : combatants.Where(o => o.WrappedChar.TeamID != self.WrappedChar.TeamID).ToList();
            {
                realTargets = Targets;
            }

            // Gets the desired location to use as an origin.
            Tuple<float, float> origin = null;
            switch (appliedMotionOrigin)
            {
                case MotionOrigin.Average:
                    float x = 0;
                    float y = 0;
                    for (int j = 0; j < realTargets.Count; j++)
                    {
                        x += realTargets[j].WrappedChar.Location.Data.Item1;
                        y += realTargets[j].WrappedChar.Location.Data.Item2;
                    }
                    origin = new Tuple<float, float>(x / realTargets.Count, y / realTargets.Count);
                    break;
                case MotionOrigin.First:
                    origin = realTargets.Count > 0
                        ? realTargets[0].WrappedChar.Location.Data
                        : new Tuple<float, float>(0, 0);
                    break;
                case MotionOrigin.Furthest:
                    float dist = float.MinValue;
                    float x1 = self.WrappedChar.Location.Data.Item1;
                    float y1 = self.WrappedChar.Location.Data.Item2;
                    float x2, y2;
                    for (int j = 0; j < realTargets.Count; j++)
                    {
                        x2 = realTargets[j].WrappedChar.Location.Data.Item1;
                        y2 = realTargets[j].WrappedChar.Location.Data.Item2;
                        float newDist = (float)Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
                        if (newDist > dist)
                        {
                            dist = newDist;
                            origin = realTargets[j].WrappedChar.Location.Data;
                        }
                    }
                    break;
                case MotionOrigin.Nearest:
                    dist = float.MaxValue;
                    x1 = self.WrappedChar.Location.Data.Item1;
                    y1 = self.WrappedChar.Location.Data.Item2;
                    for (int j = 0; j < realTargets.Count; j++)
                    {
                        x2 = realTargets[j].WrappedChar.Location.Data.Item1;
                        y2 = realTargets[j].WrappedChar.Location.Data.Item2;
                        float newDist = (float)Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
                        if (newDist < dist)
                        {
                            dist = newDist;
                            origin = realTargets[j].WrappedChar.Location.Data;
                        }
                    }
                    break;
                case MotionOrigin.NearestToCenter:
                    float center = self.WrappedChar.Stats.MinRangeRequired.Data +
                        (self.WrappedChar.Stats.MaxRangeAllowed.Data - self.WrappedChar.Stats.MinRangeRequired.Data) / 2;
                    dist = float.MaxValue;
                    x1 = self.WrappedChar.Location.Data.Item1;
                    y1 = self.WrappedChar.Location.Data.Item2;
                    for (int j = 0; j < realTargets.Count; j++)
                    {
                        x2 = realTargets[j].WrappedChar.Location.Data.Item1;
                        y2 = realTargets[j].WrappedChar.Location.Data.Item2;
                        float newDist = (float)Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
                        if (Math.Abs(center - newDist) < dist)
                        {
                            dist = newDist;
                            origin = realTargets[j].WrappedChar.Location.Data;
                        }
                    }
                    break;
            }

            // Moves with the desired motion.
            double distance = Math.Sqrt(
                Math.Pow(self.WrappedChar.Location.Data.Item1 - origin.Item1, 2) +
                Math.Pow(self.WrappedChar.Location.Data.Item2 - origin.Item2, 2));

            double dirToOrigin = Math.Atan2(
                origin.Item2 - self.WrappedChar.Location.Data.Item2,
                origin.Item1 - self.WrappedChar.Location.Data.Item1);

            double dirToSelf = dirToOrigin + Math.PI;
            double intendedDir = dirToOrigin;
            double realDist = 0;

            switch (appliedMotion)
            {
                case Motion.Away:
                    intendedDir = dirToSelf;
                    realDist = self.WrappedChar.Stats.MovementRate.Data;
                    break;
                case Motion.AwayUpToDistance:
                    intendedDir = dirToSelf;
                    realDist = Math.Min(self.WrappedChar.Stats.MovementRate.Data, DistanceRange.Item1);
                    break;
                case Motion.Towards:
                    intendedDir = dirToOrigin;
                    realDist = Math.Min(self.WrappedChar.Stats.MovementRate.Data, distance);
                    break;
                case Motion.TowardsUpToDistance:
                    intendedDir = dirToOrigin;
                    realDist = Math.Min(Math.Min(self.WrappedChar.Stats.MovementRate.Data, distance), DistanceRange.Item1);
                    break;
                case Motion.WithinDistanceRange:
                    if (distance < DistanceRange.Item1)
                    {
                        intendedDir = dirToSelf;
                        realDist = Math.Min(self.WrappedChar.Stats.MovementRate.Data, DistanceRange.Item1);
                    }
                    else if (distance > DistanceRange.Item2)
                    {
                        intendedDir = dirToOrigin;
                        realDist = Math.Min(self.WrappedChar.Stats.MovementRate.Data, DistanceRange.Item1);
                    }
                    else
                    {
                        return self.WrappedChar.Location.Data;
                    }
                    break;
                case Motion.WithinDistanceRangeCenter:
                    int midpoint = DistanceRange.Item1 + (DistanceRange.Item2 - DistanceRange.Item1) / 2;

                    if (distance < DistanceRange.Item1)
                    {
                        intendedDir = dirToSelf;
                        realDist = Math.Min(self.WrappedChar.Stats.MovementRate.Data, midpoint);
                    }
                    else if (distance > DistanceRange.Item2)
                    {
                        intendedDir = dirToOrigin;
                        realDist = Math.Min(self.WrappedChar.Stats.MovementRate.Data, midpoint);
                    }
                    else
                    {
                        return self.WrappedChar.Location.Data;
                    }
                    break;
                case Motion.WithinDistanceRangeFar:
                    if (distance < DistanceRange.Item1)
                    {
                        intendedDir = dirToSelf;
                        realDist = Math.Min(self.WrappedChar.Stats.MovementRate.Data, DistanceRange.Item2);
                    }
                    else if (distance > DistanceRange.Item2)
                    {
                        intendedDir = dirToOrigin;
                        realDist = Math.Min(self.WrappedChar.Stats.MovementRate.Data, DistanceRange.Item2);
                    }
                    else
                    {
                        return self.WrappedChar.Location.Data;
                    }
                    break;
                case Motion.WithinDistanceRangeNear:
                    double distanceToMinRange = Math.Sqrt(
                        Math.Pow(self.WrappedChar.Location.Data.Item1 - DistanceRange.Item1, 2) +
                        Math.Pow(self.WrappedChar.Location.Data.Item2 - DistanceRange.Item2, 2));

                    if (distance < DistanceRange.Item1)
                    {
                        intendedDir = dirToSelf;
                        realDist = Math.Min(self.WrappedChar.Stats.MovementRate.Data, distanceToMinRange);
                    }
                    else if (distance > DistanceRange.Item2)
                    {
                        intendedDir = dirToOrigin;
                        realDist = Math.Min(self.WrappedChar.Stats.MovementRate.Data, distanceToMinRange);
                    }
                    else
                    {
                        return self.WrappedChar.Location.Data;
                    }
                    break;
            }

            return new Tuple<float, float>(
                (float)(realDist * Math.Cos(intendedDir)),
                (float)(realDist * Math.Sin(intendedDir)));
        }
        #endregion
    }
}
