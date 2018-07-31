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
        /// Whether moving towards or away from the target.
        /// </summary>
        public bool doMoveTowards;

        /// <summary>
        /// If true, the location to move to or away from is computed as
        /// the average of all targets rather than the 1st target's location.
        /// </summary>
        public bool doUseAllTargets;
        #endregion

        #region Constructors
        /// <summary>
        /// Sets defaults for tendencies and all factors and bonuses to 0.
        /// </summary>
        public MovementBehavior(
            bool doMoveTowards,
            bool doUseAllTargets)
        {
            this.doMoveTowards = doMoveTowards;
            this.doUseAllTargets = doUseAllTargets;
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="other">
        /// The instance to copy all values from.
        /// </param>
        public MovementBehavior(MovementBehavior other)
        {
            doMoveTowards = other.doMoveTowards;
            doUseAllTargets = other.doUseAllTargets;
        }
        #endregion
    }
}
