﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Parry
{
    /// <summary>
    /// Describes the AI targeting behaviors with tendencies and
    /// a weighting system to determine the best target.
    /// </summary>
    public class MovementBehavior
    {
        #region Variables
        /// <summary>
        /// The first function that returns true given the characters will
        /// apply the associated motion origin and motion when performing
        /// movement behavior.
        /// </summary>
        public List<Movement> Movements;

        /// <summary>
        /// When targets are added, this list is used instead of defaulting to
        /// non-allied characters. This is independent of targeting behavior,
        /// so it doesn't consider max number of targets.
        /// Default empty list.
        /// </summary>
        public List<Character> Targets;

        /// <summary>
        /// When target locations are added, this list will be used in addition
        /// to normal targets.
        /// Default empty list.
        /// </summary>
        public List<Tuple<float, float>> TargetLocations;

        /// <summary>
        /// Motions that use distance will use the first value or both if a
        /// range is required.
        /// Default value is 0, 0.
        /// </summary>
        public Tuple<int, int> DistanceRange;

        /// <summary>
        /// If true, targets will be derived from the associated targeting
        /// behavior of the owning character. Looks first at the chosen move's
        /// targeting behavior, then character's default targeting behavior,
        /// and in each case considers OverrideTargets before Targets.
        /// False by default.
        /// </summary>
        public bool UseTargetingTargets;

        /// <summary>
        /// Leave as null to let characters move in straight lines towards their destination, considering no obstacles.
        /// Override to change this behavior, e.g. to adjust for pathfinding and obstacles.
        /// 
        /// First argument is character destination, already accounting movement speed.
        /// Returns the destination the character ends up at.
        /// </summary>
        public Func<Tuple<float, float>, Tuple<float, float>> OverrideMovementHandling;
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
            Targets = new List<Character>();
            TargetLocations = new List<Tuple<float, float>>();
            DistanceRange = new Tuple<int, int>(0, 0);
            UseTargetingTargets = false;
            OverrideMovementHandling = null;
        }

        /// <summary>
        /// Creates a new movement behavior without targets.
        /// </summary>
        public MovementBehavior(List<Movement> movements)
        {
            Movements = new List<Movement>(movements);
            Targets = new List<Character>();
            TargetLocations = new List<Tuple<float, float>>();
            DistanceRange = new Tuple<int, int>(0, 0);
            UseTargetingTargets = false;
            OverrideMovementHandling = null;
        }

        /// <summary>
        /// Creates a new movement behavior using targets.
        /// </summary>
        public MovementBehavior(List<Movement> movements, List<Character> targets)
        {
            Movements = movements;
            Targets = targets;
            TargetLocations = new List<Tuple<float, float>>();
            DistanceRange = new Tuple<int, int>(0, 0);
            UseTargetingTargets = false;
            OverrideMovementHandling = null;
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
            TargetLocations = other.TargetLocations;
            DistanceRange = other.DistanceRange;
            UseTargetingTargets = other.UseTargetingTargets;
            OverrideMovementHandling = other.OverrideMovementHandling;
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
            /// The averaged position of all non-allied characters, or of
            /// targets if set.
            /// </summary>
            Average,

            /// <summary>
            /// The first non-allied character, or the first target if set.
            /// </summary>
            First,

            /// <summary>
            /// The furthest non-allied character, or the furthest target if
            /// set. Useful against enemies that keep a distance.
            /// </summary>
            Furthest,

            /// <summary>
            /// The nearest non-allied character, or the nearest target if set.
            /// Useful to against enemies closing in.
            /// </summary>
            Nearest,

            /// <summary>
            /// The non-allied character closest to the center of attack range,
            /// or the target if set. Useful to maximize number of enemies in
            /// range.
            /// </summary>
            NearestToCenter
        }

        /// <summary>
        /// Different movements in relation to a point.
        /// </summary>
        public enum Motion
        {
            /// <summary>
            /// Move away from the point indefinitely.
            /// </summary>
            Away,

            /// <summary>
            /// Move away from the point up to a certain distance,
            /// and don't move towards if further than this distance.
            /// </summary>
            AwayUpToDistance,

            /// <summary>
            /// Move towards or away from the target as needed until at a
            /// specified distance.
            /// </summary>
            ToDistance,

            /// <summary>
            /// Move towards the point until exactly at the same position.
            /// </summary>
            Towards,

            /// <summary>
            /// Move towards the point up to a certain distance, and
            /// don't move away if closer than this distance.
            /// </summary>
            TowardsUpToDistance,

            /// <summary>
            /// Move until between a minimum and maximum distance of the point.
            /// </summary>
            WithinDistanceRange
        }
        #endregion

        #region Methods
        /// <summary>
        /// Performs the movement.
        /// </summary>
        public Tuple<float, float> Perform(List<Character> chars, Character self)
        {
            List<Tuple<float, float>> targets = (UseTargetingTargets)
                ? self.GetTargetsFlat().Select(o => o.CharStats.Location.Data).ToList()
                : (Targets.Count > 0)
                    ? Targets.Select(o => o.CharStats.Location.Data).ToList()
                    : chars.Where(o => o.TeamID != self.TeamID)
                        .Select(o => o.CharStats.Location.Data)
                        .ToList();

            targets.AddRange(TargetLocations);

            if (targets.Count == 0)
            {
                return self.CharStats.Location.Data;
            }

            MotionOrigin appliedMotionOrigin = MotionOrigin.First;
            Motion appliedMotion = Motion.Towards;

            // Gets the desired motion and motion origin.
            for (int i = 0; i < Movements.Count; i++)
            {
                var movement = Movements[i];
                if (movement.ShouldApply(chars))
                {
                    appliedMotionOrigin = movement.Origin;
                    appliedMotion = movement.Motion;
                    break;
                }
            }

            // Gets the desired location to use as an origin.
            Tuple<float, float> origin = null;
            switch (appliedMotionOrigin)
            {
                case MotionOrigin.Average:
                    float x = 0;
                    float y = 0;
                    for (int j = 0; j < targets.Count; j++)
                    {
                        x += targets[j].Item1;
                        y += targets[j].Item2;
                    }
                    origin = new Tuple<float, float>(x / targets.Count, y / targets.Count);
                    break;
                case MotionOrigin.First:
                    origin = targets.Count > 0
                        ? targets[0]
                        : new Tuple<float, float>(0, 0);
                    break;
                case MotionOrigin.Furthest:
                    float dist = float.MinValue;
                    float x1 = self.CharStats.Location.Data.Item1;
                    float y1 = self.CharStats.Location.Data.Item2;
                    float x2, y2;
                    for (int j = 0; j < targets.Count; j++)
                    {
                        x2 = targets[j].Item1;
                        y2 = targets[j].Item2;
                        float newDist = (float)Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
                        if (newDist > dist)
                        {
                            dist = newDist;
                            origin = targets[j];
                        }
                    }
                    break;
                case MotionOrigin.Nearest:
                    dist = float.MaxValue;
                    x1 = self.CharStats.Location.Data.Item1;
                    y1 = self.CharStats.Location.Data.Item2;
                    for (int j = 0; j < targets.Count; j++)
                    {
                        x2 = targets[j].Item1;
                        y2 = targets[j].Item2;
                        float newDist = (float)Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
                        if (newDist < dist)
                        {
                            dist = newDist;
                            origin = targets[j];
                        }
                    }
                    break;
                case MotionOrigin.NearestToCenter:
                    float centerDist = self.CombatStats.MinRangeRequired.Data +
                        (self.CombatStats.MaxRangeAllowed.Data - self.CombatStats.MinRangeRequired.Data) / 2;
                    dist = float.MaxValue;
                    x1 = self.CharStats.Location.Data.Item1;
                    y1 = self.CharStats.Location.Data.Item2;

                    for (int j = 0; j < targets.Count; j++)
                    {
                        x2 = targets[j].Item1;
                        y2 = targets[j].Item2;
                        float newDist = (float)Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
                        float adjustedDist = Math.Abs(newDist - centerDist);
                        if (adjustedDist < dist)
                        {
                            dist = adjustedDist;
                            origin = targets[j];
                        }
                    }
                    break;
            }

            // Moves with the desired motion.
            double distance = Math.Sqrt(
                Math.Pow(self.CharStats.Location.Data.Item1 - origin.Item1, 2) +
                Math.Pow(self.CharStats.Location.Data.Item2 - origin.Item2, 2));

            double dirToOrigin = Math.Atan2(
                origin.Item2 - self.CharStats.Location.Data.Item2,
                origin.Item1 - self.CharStats.Location.Data.Item1);

            double dirToSelf = dirToOrigin + Math.PI;
            double intendedDir = dirToOrigin;
            double realDist = 0;

            switch (appliedMotion)
            {
                case Motion.Away:
                    intendedDir = dirToSelf;
                    realDist = self.CombatStats.MovementRate.Data;
                    break;
                case Motion.AwayUpToDistance:
                    intendedDir = dirToSelf;
                    realDist = Math.Min(self.CombatStats.MovementRate.Data, DistanceRange.Item1);
                    break;
                case Motion.Towards:
                    realDist = Math.Min(self.CombatStats.MovementRate.Data, distance);
                    break;
                case Motion.TowardsUpToDistance:
                    realDist = Math.Min(Math.Min(self.CombatStats.MovementRate.Data, distance), DistanceRange.Item1);
                    break;
                case Motion.WithinDistanceRange:
                    if (distance < DistanceRange.Item1)
                    {
                        intendedDir = dirToSelf;
                        realDist = Math.Min(self.CombatStats.MovementRate.Data, DistanceRange.Item1 - distance);
                    }
                    else if (distance > DistanceRange.Item2)
                    {
                        realDist = Math.Min(self.CombatStats.MovementRate.Data, DistanceRange.Item2);
                    }
                    else
                    {
                        realDist = distance;
                    }
                    break;
                case Motion.ToDistance:
                    double midpoint = DistanceRange.Item1 + (DistanceRange.Item2 - DistanceRange.Item1) / 2.0;
                    if (distance > DistanceRange.Item2)
                    {
                        realDist = Math.Min(self.CombatStats.MovementRate.Data, distance - midpoint);
                    }
                    else if (distance > midpoint)
                    {
                        realDist = Math.Min(self.CombatStats.MovementRate.Data, distance - midpoint);
                    }
                    else if (distance < midpoint)
                    {
                        intendedDir = dirToSelf;
                        realDist = Math.Min(self.CombatStats.MovementRate.Data, midpoint - distance);
                    }
                    else
                    {
                        realDist = 0;
                    }
                    break;
            }

            var finalDest = new Tuple<float, float>(
                (float)Math.Round(self.CharStats.Location.Data.Item1 + realDist * Math.Cos(intendedDir), 10),
                (float)Math.Round(self.CharStats.Location.Data.Item2 + realDist * Math.Sin(intendedDir), 10));

            return (OverrideMovementHandling == null)
                ? finalDest
                : OverrideMovementHandling.Invoke(finalDest);
        }
        #endregion
    }
}
