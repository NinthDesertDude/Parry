using System.Collections.Generic;
using System.Linq;

namespace Parry.Combat
{
    /// <summary>
    /// Represents stats in combat such as damage or critical hit chance.
    /// </summary>
    public class Stats
    {
        /// <summary>
        /// Equal to 20. Values that care about types of damage are initialized
        /// with a default number of slots depicting each kind of damage. E.g.
        /// minimum damage is an array with this many slots to assign types of
        /// damage as needed.
        /// </summary>
        public static readonly int NUM_TYPES_DAMAGE = 20;

        #region Members
        /// <summary>
        /// After receiving damage, this much knockback damage is dealt to
        /// each attacker. This does not trigger their own knockback.
        /// Default value is 0.
        /// </summary>
        public Stat<int> ConstantKnockback
        {
            get;
            set;
        }

        /// <summary>
        /// On a successful critical attack, this is the amount that the
        /// type of damage is multiplied. By default, this value is
        /// 1.5 times the damage dealt. There are <see cref="NUM_TYPES_DAMAGE"/> slots initialized by
        /// default, each of which represents a 'type' of damage. Slot 0 is
        /// intended to be physical damage and other slots may be used as
        /// needed, e.g. for "fire damage".
        /// Default values are 1.0.
        /// </summary>
        public Stat<float[]> CritDamageMultiplier
        {
            get;
            set;
        }

        /// <summary>
        /// Governs how the character receives critical hit damage.
        /// Default value is Constants.CombatCriticalStatuses.Normal.
        /// </summary>
        public Stat<Constants.CriticalStatuses> CriticalStatus
        {
            get;
            set;
        }

        /// <summary>
        /// This is a list of custom stats. Add values here that multiple
        /// characters might possess at once, which can be set or modified by
        /// items, skills, or spells.
        /// </summary>
        public Dictionary<string, object> CustomStats
        {
            get;
            set;
        }

        /// <summary>
        /// After being successfully attacked, the damage is reduced by this
        /// value. There are <see cref="NUM_TYPES_DAMAGE"/> slots initialized
        /// by default, each of which represents a 'type' of damage. Slot 0 is
        /// intended to be physical damage and other slots may be used as
        /// needed, e.g. for "fire damage".
        /// Default values are 0.
        /// </summary>
        public Stat<int[]> DamageReduction
        {
            get;
            set;
        }

        /// <summary>
        /// After being successfully attacked, the damage is reduced by this
        /// percent after damage reduction is applied. If negative, the
        /// absolute difference is added on top of normal damage. -100% would
        /// be 2x damage, for example. There are <see cref="NUM_TYPES_DAMAGE"/>
        /// slots initialized by default, each of which represents a 'type' of
        /// damage. Slot 0 is intended to be physical damage and other slots
        /// may be used as needed, e.g. for "fire damage".
        /// Default values are 0.0.
        /// </summary>
        public Stat<float[]> DamageResistance
        {
            get;
            set;
        }

        /// <summary>
        /// Governs how the character handles health being at or below 0.
        /// </summary>
        public Stat<Constants.HealthStatuses> HealthStatus
        {
            get;
            set;
        }

        /// <summary>
        /// Governs how the character's ability to hit is computed.
        /// Default is Constants.HitStatuses.Normal.
        /// </summary>
        public Stat<Constants.HitStatuses> HitStatus
        {
            get;
            set;
        }

        /// <summary>
        /// On a successful attack, this is the maximum damage possible that
        /// can be dealt before resistances and reductions are computed. There
        /// are <see cref="NUM_TYPES_DAMAGE"/> slots initialized by default,
        /// each of which represents a 'type' of damage. Slot 0 is intended to
        /// be physical damage and other slots may be used as needed, e.g. for
        /// "fire damage".
        /// Default values are 0.
        /// </summary>
        public Stat<int[]> MaxDamage
        {
            get;
            set;
        }

        /// <summary>
        /// Targets further than this distance can't be attacked. Negative
        /// values use no max range. Default value is -1.
        /// </summary>
        public Stat<int> MaxRangeAllowed
        {
            get;
            set;
        }

        /// <summary>
        /// Multiplies calculated damage after damage and critical hits are
        /// computed, before any damage reductions, for attacks made at the
        /// maximum distance. The value is interpolated between the
        /// MinRangeMultiplier and MaxRangeMultiplier across the min and max
        /// ranges set.
        /// Default value is 1.0.
        /// </summary>
        public Stat<float> MaxRangeMultiplier
        {
            get;
            set;
        }

        /// <summary>
        /// Targets are moved backwards by up to this much on a successful
        /// attack.
        /// Default value is 0.
        /// </summary>
        public Stat<float> MaxRecoil
        {
            get;
            set;
        }

        /// <summary>
        /// On a successful attack, this is the minimum damage possible that
        /// can be dealt before resistances and reductions are computed. There
        /// are <see cref="NUM_TYPES_DAMAGE"/> slots initialized by default,
        /// each of which represents a 'type' of damage. Slot 0 is intended to
        /// be physical damage and other slots may be used as needed, e.g. for
        /// "fire damage".
        /// Default values are 0.
        /// </summary>
        public Stat<int[]> MinDamage
        {
            get;
            set;
        }

        /// <summary>
        /// Multiplies calculated damage after damage and critical hits are
        /// computed, before any damage reductions, for attacks made at the
        /// minimum distance. The value is interpolated between the
        /// MinRangeMultiplier and MaxRangeMultiplier across the min and max
        /// ranges set.
        /// Default value is 1.0.
        /// </summary>
        public Stat<float> MinRangeMultiplier
        {
            get;
            set;
        }

        /// <summary>
        /// Targets closer than this distance can't be attacked.
        /// Default value is 0.
        /// </summary>
        public Stat<int> MinRangeRequired
        {
            get;
            set;
        }

        /// <summary>
        /// Targets are moved backwards by at least this much on a successful
        /// attack.
        /// Default value is 0.
        /// </summary>
        public Stat<float> MinRecoil
        {
            get;
            set;
        }

        /// <summary>
        /// Describes how far you can move in a turn.
        /// Default movement rate is 1.
        /// </summary>
        public Stat<float> MovementRate
        {
            get;
            set;
        }

        /// <summary>
        /// The base speed added to any move performed by this character.
        /// Default value is 0.
        /// </summary>
        public Stat<int> MoveSpeed
        {
            get;
            set;
        }

        /// <summary>
        /// After receiving damage, a percent of that damage is dealt in its
        /// original damage types (based on index) back to the attacker. This
        /// does not trigger their own knockback. A value of 100 is 100%.
        /// Default value is 0.
        /// </summary>
        public Stat<float> PercentKnockback
        {
            get;
            set;
        }

        /// <summary>
        /// On a successful attack, this is the percent chance that the attack
        /// will be a critical hit and use max damage multiplied by damage
        /// dealt. There are <see cref="NUM_TYPES_DAMAGE"/> slots initialized
        /// by default, each of which represents a 'type' of damage. Slot 0 is
        /// intended to be physical damage and other slots may be used as
        /// needed, e.g. for "fire damage". A value of 100 is 100%.
        /// Default values are 0.
        /// </summary>
        public Stat<int[]> PercentToCritHit
        {
            get;
            set;
        }

        /// <summary>
        /// The percent chance to successfully dodge any attack. 100 is 100%.
        /// Default value is 0.
        /// </summary>
        public Stat<float> PercentToDodge
        {
            get;
            set;
        }

        /// <summary>
        /// On an attack, this is the percent chance that the attack will be
        /// successful (that it will be considered a hit against the target
        /// and damage will be calculated).
        /// Default value is 100, meaning 100%.
        /// </summary>
        public Stat<float> PercentToHit
        {
            get;
            set;
        }

        /// <summary>
        /// Governs how the character's turn order is computed.
        /// Default is Constants.CombatSpeedStatuses.Normal.
        /// </summary>
        public Stat<Constants.SpeedStatuses> SpeedStatus
        {
            get;
            set;
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Sets defaults for all stats.
        /// </summary>
        public Stats()
        {
            ConstantKnockback = new Stat<int>(0);
            CritDamageMultiplier = new Stat<float[]>(Enumerable.Repeat(1.0f, NUM_TYPES_DAMAGE).ToArray());
            CriticalStatus = new Stat<Constants.CriticalStatuses>(Constants.CriticalStatuses.Normal);
            CustomStats = new Dictionary<string, object>();
            DamageReduction = new Stat<int[]>(Enumerable.Repeat(0, NUM_TYPES_DAMAGE).ToArray());
            DamageResistance = new Stat<float[]>(Enumerable.Repeat(0.0f, NUM_TYPES_DAMAGE).ToArray());
            HitStatus = new Stat<Constants.HitStatuses>(Constants.HitStatuses.Normal);
            HealthStatus = new Stat<Constants.HealthStatuses>(Constants.HealthStatuses.RemoveAtZero);
            MaxDamage = new Stat<int[]>(Enumerable.Repeat(0, NUM_TYPES_DAMAGE).ToArray());
            MaxRangeAllowed = new Stat<int>(-1);
            MaxRangeMultiplier = new Stat<float>(1.0f);
            MaxRecoil = new Stat<float>(0);
            MinDamage = new Stat<int[]>(Enumerable.Repeat(0, NUM_TYPES_DAMAGE).ToArray());
            MinRangeMultiplier = new Stat<float>(1.0f);
            MinRangeRequired = new Stat<int>(0);
            MinRecoil = new Stat<float>(0);
            MovementRate = new Stat<float>(1);
            MoveSpeed = new Stat<int>(0);
            PercentKnockback = new Stat<float>(0);
            PercentToCritHit = new Stat<int[]>(Enumerable.Repeat(0, NUM_TYPES_DAMAGE).ToArray());
            PercentToDodge = new Stat<float>(0);
            PercentToHit = new Stat<float>(100);
            SpeedStatus = new Stat<Constants.SpeedStatuses>(Constants.SpeedStatuses.Normal);
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        public Stats(Stats other)
        {
            ConstantKnockback = other.ConstantKnockback;
            CritDamageMultiplier = other.CritDamageMultiplier;
            CriticalStatus = other.CriticalStatus;
            DamageReduction = other.DamageReduction;
            DamageResistance = other.DamageResistance;
            HealthStatus = other.HealthStatus;
            HitStatus = other.HitStatus;
            MaxDamage = other.MaxDamage;
            MaxRangeAllowed = other.MaxRangeAllowed;
            MaxRangeMultiplier = other.MaxRangeMultiplier;
            MaxRecoil = other.MaxRecoil;
            MinDamage = other.MinDamage;
            MinRangeMultiplier = other.MinRangeMultiplier;
            MinRangeRequired = other.MinRangeRequired;
            MinRecoil = other.MinRecoil;
            MovementRate = other.MovementRate;
            MoveSpeed = other.MoveSpeed;
            PercentKnockback = other.PercentKnockback;
            PercentToCritHit = other.PercentToCritHit;
            PercentToDodge = other.PercentToDodge;
            PercentToHit = other.PercentToHit;
            SpeedStatus = other.SpeedStatus;
        }
        #endregion
    }
}
