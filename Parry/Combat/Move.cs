using System;
using System.Collections.Generic;

namespace Parry.Combat
{
    /// <summary>
    /// A move in combat uses intentions to tell AI what it does.
    /// </summary>
    public class Move
    {
        #region Variables
        /// <summary>
        /// After using a move, a cooldown prevents using the move again until
        /// that many turns have passed.
        /// Default value is 0.
        /// </summary>
        public byte Cooldown
        {
            get;
            set;
        }

        /// <summary>
        /// After using a move with a cooldown, this is set to the cooldown
        /// value and it falls by one each turn. At 0, the move is ready
        /// for use again.
        /// </summary>
        public byte CooldownProgress
        {
            get;
            set;
        }

        /// <summary>
        /// True if the move can be selected.
        /// True by default.
        /// </summary>
        public bool IsMoveEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// The base speed of the move. Higher speeds will cause the move to
        /// take precedence in turn order. Default value is 0.
        /// </summary>
        public int MoveSpeed
        {
            get;
            set;
        }

        /// <summary>
        /// Contains a list of motives associated with the move.
        /// </summary>
        public List<Constants.Motives> Motives
        {
            get;
            set;
        }

        /// <summary>
        /// If non-null, this targeting behavior is used instead of the
        /// combatant's default behavior.
        /// Default value is null.
        /// </summary>
        public TargetBehavior TargetBehavior
        {
            get;
            set;
        }

        /// <summary>
        /// If non-null, this movement behavior is used instead of the
        /// combatant's default behavior.
        /// Default value is null.
        /// </summary>
        public MovementBehavior MovementBehavior
        {
            get;
            set;
        }

        /// <summary>
        /// How many turns it takes to use the move, default 1. Any number
        /// of moves adding up to 1 turn fraction can be performed each turn,
        /// or a character can forego their action to save up for a move that
        /// requires more than 1 turn.
        /// Default value is 1.
        /// </summary>
        public float TurnFraction
        {
            get;
            set;
        }

        /// <summary>
        /// After using a move with a turn fraction greater than 1, this is set
        /// to the turn fraction and it falls by one each turn. At 0, the move
        /// is performed.
        /// </summary>
        public float TurnFractionProgress
        {
            get;
            set;
        }

        /// <summary>
        /// How many times a move can be used in a turn for moves that take
        /// less than a full turn to execute.
        /// Default is 1.
        /// </summary>
        public int UsesPerTurn
        {
            get;
            set;
        }

        /// <summary>
        /// At the start of a turn, this is set to the uses per turn value. It
        /// decreases by 1 for each usage in the same turn. At 0, the move
        /// cannot be performed.
        /// </summary>
        public int UsesPerTurnProgress
        {
            get;
            set;
        }

        /// <summary>
        /// If true, using this move ends the turn afterwards.
        /// False by default.
        /// </summary>
        public bool UsesRemainingTurn
        {
            get;
            set;
        }

        /// <summary>
        /// The action to perform when the move is executed. This is handled
        /// externally, outside of the Move class.
        /// Takes a list of all combatants, followed by a list of the
        /// targeted combatants.
        /// </summary>
        public Action<List<Combatant>, List<Combatant>> PerformAction
        {
            get;
            set;
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Constructs a move with defaults.
        /// </summary>
        public Move()
        {
            Cooldown = 0;
            CooldownProgress = 0;
            IsMoveEnabled = true;
            MoveSpeed = 0;
            Motives = new List<Constants.Motives>();
            TargetBehavior = null;
            MovementBehavior = null;
            TurnFraction = 1;
            TurnFractionProgress = 0;
            UsesPerTurn = 1;
            UsesPerTurnProgress = 0;
            UsesRemainingTurn = false;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Returns true if the move can be performed.
        /// </summary>
        public bool CanPerform()
        {
            return (IsMoveEnabled &&
                CooldownProgress == 0 &&
                TurnFractionProgress == 0 &&
                UsesPerTurnProgress > 0);
        }

        /// <summary>
        /// Resets and adjusts values in the move to indicate it's the next
        /// turn.
        /// </summary>
        public void NextTurn()
        {
            UsesPerTurnProgress = 0;

            if (CooldownProgress > 0)
            {
                CooldownProgress -= 1;
            }
            if (TurnFractionProgress > 0)
            {
                TurnFractionProgress -= 1;
            }
        }

        /// <summary>
        /// Performs the move with the given list of combatants, targets, and
        /// action. Returns whether the move could be performed or not.
        /// </summary>
        /// <param name="combatants">
        /// A list of all combatants.
        /// </param>
        /// <param name="action">
        /// The action to perform, which takes a list of all combatants,
        /// followed by a list of the targeted combatants.
        /// </param>
        public bool Perform(
            List<Combatant> combatants,
            List<Combatant> targets,
            Action<List<Combatant>, List<Combatant>> action)
        {
            //Manages charge-up moves.
            if (TurnFraction > 1 && TurnFractionProgress == 0)
            {
                TurnFractionProgress = TurnFraction + 1;
            }

            if (CanPerform())
            {
                UsesPerTurnProgress += 1;
                action(combatants, targets);

                //Starts the cooldown period if nonzero.
                if (Cooldown != 0 && CooldownProgress == 0)
                {
                    CooldownProgress = Cooldown;
                }

                return true;
            }

            return false;
        }
        #endregion
    }
}
