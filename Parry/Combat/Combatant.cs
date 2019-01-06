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
            AccumulatedSpeed = 0;
            CurrentHealth = character.Health;
            WrappedChar = character;
        }
        #endregion
    }
}
