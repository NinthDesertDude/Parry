using System;
using System.Collections.Generic;
using static Parry.Combat.MovementBehavior;

namespace Parry.Combat
{
    /// <summary>
    /// Couples movement behavior (motion and motion origin) with a condition
    /// function such that the behavior is only chosen when the function
    /// evaluates to true.
    /// </summary>
    public class Movement
    {
        /// <summary>
        /// The function that defines whether this movement is applied or not.
        /// </summary>
        public readonly Func<List<Character>, bool> ShouldApply;

        /// <summary>
        /// The point whose location is used to determine motions.
        /// </summary>
        public readonly MotionOrigin Origin;

        /// <summary>
        /// The action to take, such as moving away or towards.
        /// </summary>
        public readonly Motion Motion;

        /// <summary>
        /// Creates a movement with the given origin and motion. It has no
        /// conditions to occur, so it will be executed.
        /// </summary>
        public Movement(MotionOrigin origin, Motion motion)
        {
            ShouldApply = new Func<List<Character>, bool>((o) => true);
            Origin = origin;
            Motion = motion;
        }

        /// <summary>
        /// Creates a movement with a condition, origin and motion. When used,
        /// if the provided function returns true, the associated origin and
        /// motion are used.
        /// </summary>
        public Movement(Func<List<Character>, bool> function, MotionOrigin origin, Motion motion)
        {
            ShouldApply = function;
            Origin = origin;
            Motion = motion;
        }
    }
}
