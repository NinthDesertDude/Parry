﻿namespace Parry
{
    /// <summary>
    /// Contains a strongly-bound list of constants and enums.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Statuses governing how a character takes critical hit damage.
        /// </summary>
        public enum CriticalStatuses
        {
            /// <summary>
            /// Characters with this status can take critical hit damage as
            /// usual.
            /// </summary>
            Normal,

            /// <summary>
            /// Characters with this status take no additional damage from a
            /// critical hit multiplier, though they still take regular
            /// damage.
            /// </summary>
            ImmuneToCriticalHits
        }

        /// <summary>
        /// Statuses governing how a character handles having no health.
        /// </summary>
        public enum HealthStatuses
        {
            /// <summary>
            /// Character is automatically removed from combat when health
            /// drops to zero or less.
            /// </summary>
            RemoveAtZero,

            /// <summary>
            /// Character is not removed from combat when health drops to zero
            /// or less.
            /// </summary>
            NoRemoval
        }

        /// <summary>
        /// Statuses governing how a character hits targets.
        /// </summary>
        public enum HitStatuses
        {
            /// <summary>
            /// Chance to hit and chance to dodge are computed.
            /// </summary>
            Normal,

            /// <summary>
            /// Skips both chance to hit and target's chance to dodge.
            /// </summary>
            AlwaysHit
        }

        /// <summary>
        /// Only moves matching the AI's motive will be chosen.
        /// Values up to 20 are reserved for internal use. Extend this enum by
        /// creating entries with values of 21 and higher and casting to int
        /// when using it.
        /// </summary>
        public enum Motives
        {
            /// <summary>
            /// The move damages enemy targets. Includes basic attacks, but
            /// also splash damage and complicated attacks.
            /// </summary>
            DamageHealth,

            /// <summary>
            /// The move lessens enemy target's stats or disadvantages them.
            /// Includes effects like decreased chance to hit or freezing
            /// enemies so they can't move.
            /// </summary>
            Weaken,

            /// <summary>
            /// The move improves self or allied target's stats or provides
            /// them an advantage. Includes effects like a bonus to critical
            /// hit chance or teleportation.
            /// </summary>
            Strengthen,

            /// <summary>
            /// The move mitigates damage from enemy targets. Includes moves
            /// like blocking.
            /// </summary>
            ResistDamage,

            /// <summary>
            /// The move restores health for allied targets. Includes effects
            /// like healing.
            /// </summary>
            RestoreHealth
        }

        /// <summary>
        /// Statuses governing how a character's turn order is computed.
        /// </summary>
        public enum SpeedStatuses
        {
            /// <summary>
            /// Combat order is determined by character speed.
            /// </summary>
            Normal,

            /// <summary>
            /// Characters with this status go simultaneously before any other
            /// character without this status.
            /// </summary>
            AlwaysFirst,

            /// <summary>
            /// Characters with this status go simultaneously after any other
            /// character without this status.
            /// </summary>
            AlwaysLast
        }
    }
}
