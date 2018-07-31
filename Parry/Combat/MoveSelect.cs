using System;
using System.Collections.Generic;
using System.Linq;

namespace Parry.Combat
{
    /// <summary>
    /// Handles motive and move selection logic from a list of moves.
    /// </summary>
    public class MoveSelector
    {
        #region Variables
        /// <summary>
        /// When set, this function takes the combat history and returns the
        /// motive to use. Don't set when overriding Motive.
        /// </summary>
        public Func<List<List<Combatant>>, Constants.Motives> GetMotive
        {
            get;
            set;
        }

        /// <summary>
        /// Takes the list of available moves for a chosen or calculated motive
        /// with the combat history and returns the move to use.
        /// </summary>
        public Func<List<List<Combatant>>, List<Move>, Move> GetMove
        {
            get;
            set;
        }

        /// <summary>
        /// The character's motive. Moves that don't match the motive aren't
        /// selected.
        /// </summary>
        public Constants.Motives Motive
        {
            get;
            set;
        }

        /// <summary>
        /// A list of all available moves.
        /// </summary>
        public List<Move> Moves
        {
            get;
            set;
        }

        /// <summary>
        /// A value from 0 to 1, where 1 is a full turn and 0 is none left.
        /// Default value is 1.
        /// </summary>
        public float TurnFractionLeft
        {
            get;
            set;
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new move selection object.
        /// </summary>
        public MoveSelector()
        {
            GetMotive = null;
            GetMove = null;
            Motive = Constants.Motives.DamageHealth;
            Moves = new List<Move>();
            TurnFractionLeft = 1;
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        public MoveSelector(MoveSelector other)
        {
            GetMotive = other.GetMotive;
            GetMove = other.GetMove;
            Motive = other.Motive;
            Moves = new List<Move>(other.Moves);
            TurnFractionLeft = other.TurnFractionLeft;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Computes the motive if set, filters invalid moves, and selects
        /// a move based on combat history. Returns null if no moves are
        /// available, else returns the move and changes turn fraction left.
        /// </summary>
        /// <param name="combatHistory">
        /// The list of all combatants.
        /// </param>
        /// <param name="moves">
        /// The list of all moves for the combatant.
        /// </param>
        public Move Perform(List<List<Combatant>> combatHistory)
        {
            //Gets the motive.
            if (GetMotive != null)
            {
                Motive = GetMotive(combatHistory);
            }

            throw new NotImplementedException("Get moves from items and such."); //TODO

            //Filters out non-matching moves.
            List<Move> availableMoves = Moves
                .Where((move) => {
                    return (move.Motives.Contains(Motive) &&
                        move.IsMoveEnabled &&
                        move.Cooldown == 0 &&
                        move.UsesPerTurnProgress < move.UsesPerTurn);
                })
                .ToList();

            //Gets the move.
            if (GetMove != null)
            {
                return GetMove(combatHistory, availableMoves);
            }

            return null;
        }
        #endregion
    }
}
