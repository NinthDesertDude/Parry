namespace Parry.Combat
{
    /// <summary>
    /// Characters in combat are created 
    /// </summary>
    public class Combatant
    {
        #region Variables
        /// <summary>
        /// The speed calculated from move speed, character move speed, and
        /// the accumulated speed of previous rounds.
        /// </summary>
        public int Speed
        {
            get;
            set;
        }

        /// <summary>
        /// The combatant's speed accumulated from previous rounds, used
        /// when speed carryover is enabled.
        /// </summary>
        public int AccumulatedSpeed
        {
            get;
            set;
        }

        /// <summary>
        /// The combatant's current health value.
        /// </summary>
        public Stat<int> CurrentHealth
        {
            get;
            set;
        }

        /// <summary>
        /// Returns the character used to create the combatant object.
        /// </summary>
        public Character WrappedChar
        {
            get;
            private set;
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new combatant, wrapping a character.
        /// </summary>
        public Combatant(Character character)
        {
            Speed = 0;
            AccumulatedSpeed = 0;
            CurrentHealth = character.Health;
            WrappedChar = character;
        }

        /// <summary>
        /// Copy constructor. If isDeepCopy is true, creates a deep copy of
        /// the underlying character, keeping the same character id.
        /// </summary>
        public Combatant(Combatant other, bool isDeepCopy = false)
        {
            Speed = other.Speed;
            AccumulatedSpeed = other.Speed;
            CurrentHealth = new Stat<int>(other.CurrentHealth.RawData);

            if (isDeepCopy)
            {
                WrappedChar = new Character(other.WrappedChar, true);
            }
            else
            {
                WrappedChar = new Character(other.WrappedChar);
            }
        }
        #endregion
    }
}
