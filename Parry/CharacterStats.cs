using System;

namespace Parry
{
    /// <summary>
    /// Represents stats related to a character's state in combat, such as
    /// health and location.
    /// </summary>
    public class CharacterStats
    {
        /// <summary>
        /// In combat, this is the character's total possible health.
        /// Default value is 100.
        /// </summary>
        public Stat<int> MaxHealth
        {
            get;
            set;
        }

        /// <summary>
        /// The character's current health value.
        /// </summary>
        public Stat<int> Health
        {
            get;
            set;
        }

        /// <summary>
        /// The character's position in combat, if you make use of
        /// distances.
        /// Default location is 0, 0.
        /// </summary>
        public Stat<Tuple<float, float>> Location
        {
            get;
            set;
        }

        /// <summary>
        /// The delay calculated from move speed delay, character move speed
        /// delay, and the accumulated speed delay of previous rounds.
        /// </summary>
        public int SpeedDelay
        {
            get;
            set;
        }

        /// <summary>
        /// The character's speed accumulated from previous rounds, used
        /// when speed carryover is enabled.
        /// </summary>
        public int AccumulatedSpeed
        {
            get;
            set;
        }

        #region Constructors
        /// <summary>
        /// Creates a new instance of CharacterStats.
        /// </summary>
        public CharacterStats()
        {
            AccumulatedSpeed = 0;
            Health = new Stat<int>(100);
            Location = new Stat<Tuple<float, float>>(new Tuple<float, float>(0, 0));
            MaxHealth = new Stat<int>(100);
            SpeedDelay = 0;
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        public CharacterStats(CharacterStats other)
        {
            AccumulatedSpeed = other.AccumulatedSpeed;
            Health = new Stat<int>(other.Health.RawData);
            Location = new Stat<Tuple<float, float>>(other.Location.RawData);
            MaxHealth = new Stat<int>(other.MaxHealth.RawData);
            SpeedDelay = other.SpeedDelay;
        }
        #endregion
    }
}
