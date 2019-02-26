using System;
using System.Collections.Generic;
using System.Linq;

namespace Parry
{
    /// <summary>
    /// Handles motive and move selection logic from a list of moves.
    /// </summary>
    public class MoveSelector
    {
        #region Variables
        /// <summary>
        /// When set, this function takes the combat history and returns a list
        /// of motives with associated weight. Don't set when overriding Motive.
        /// First argument: The combat history where index 0 is most current.
        /// Returns a list of MotiveWithPriority.
        /// </summary>
        public Func<List<List<Character>>, List<MotiveWithPriority>> GetMotives
        {
            get;
            set;
        }

        /// <summary>
        /// Takes the list of available moves for a chosen or calculated motive
        /// with the combat history and returns the move to use.
        /// First argument: The combat history where index 0 is most current.
        /// Second argument: A list of MotiveWithPriority.
        /// Third argument: A list of available moves (excludes disabled,
        /// moves cooling down, etc.).
        /// Returns: A list of moves to perform, in order if supported by the
        /// active round execution mechanism.
        /// </summary>
        public Func<List<List<Character>>, List<MotiveWithPriority>, List<Move>, List<Move>> GetMoves
        {
            get;
            set;
        }

        /// <summary>
        /// The character's motives with associated priority. Moves that don't
        /// support a motive with high priority will generally be picked last.
        /// </summary>
        public List<MotiveWithPriority> Motives
        {
            get;
            set;
        }

        /// <summary>
        /// If non-null, resolves to the movement behavior to use instead of
        /// the character's default post-move behavior.
        /// Default value is null.
        /// Argument 1: The list of chosen moves, in case they should influence the chosen behavior.
        /// Returns: The movement behavior object to use.
        /// </summary>
        public Func<List<Move>, MovementBehavior> MovementAfterBehavior
        {
            get;
            set;
        }

        /// <summary>
        /// If non-null, resolves to the movement behavior to use instead of
        /// the character's default pre-move behavior.
        /// Default value is null.
        /// Argument 1: The list of chosen moves, in case they should influence the chosen behavior.
        /// Returns: The movement behavior object to use.
        /// </summary>
        public Func<List<Move>, MovementBehavior> MovementBeforeBehavior
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
        /// The last move chosen by performing move selection.
        /// Default null.
        /// </summary>
        public List<Move> ChosenMoves
        {
            private set;
            get;
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
        /// Creates a new move selector. GetMoves is set to return the first
        /// move in the list, and Motives takes the DamageHealth motive with
        /// priority 100 by default.
        /// </summary>
        public MoveSelector()
        {
            GetMotives = null;
            GetMoves = new Func<List<List<Character>>, List<MotiveWithPriority>, List<Move>, List<Move>>(
                (combatHistory, motives, moves) =>
            {
                return new List<Move>() { moves.FirstOrDefault() };
            });
            Motives = new List<MotiveWithPriority>() {
                new MotiveWithPriority() { motive = Constants.Motives.DamageHealth, priority = 100 }
            };
            MovementAfterBehavior = null;
            MovementBeforeBehavior = null;
            Moves = new List<Move>();
            TurnFractionLeft = 1;
            ChosenMoves = new List<Move>();
        }

        /// <summary>
        /// Creates a new move selector with the given list of moves and
        /// initializes GetMove to select the first one.
        /// </summary>
        public MoveSelector(
            List<Move> moves,
            Func<List<List<Character>>,
                List<MotiveWithPriority>,
                List<Move>,
                List<Move>> getMoves)
        {
            GetMotives = null;
            GetMoves = getMoves;
            Motives = new List<MotiveWithPriority>() {
                new MotiveWithPriority() { motive = Constants.Motives.DamageHealth, priority = 100 }
            };
            MovementAfterBehavior = null;
            MovementBeforeBehavior = null;
            Moves = moves;
            TurnFractionLeft = 1;
            ChosenMoves = new List<Move>();
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        public MoveSelector(MoveSelector other)
        {
            GetMotives = other.GetMotives;
            GetMoves = other.GetMoves;
            Motives = new List<MotiveWithPriority>(other.Motives);
            MovementAfterBehavior = other.MovementAfterBehavior;
            MovementBeforeBehavior = other.MovementBeforeBehavior;
            Moves = new List<Move>(other.Moves);
            TurnFractionLeft = other.TurnFractionLeft;
            ChosenMoves = new List<Move>(other.ChosenMoves);
        }
        #endregion

        #region Methods
        /// <summary>
        /// Indicates that a turn has passed and resets the value of
        /// <see cref="TurnFractionLeft"/>.
        /// </summary>
        public void NextTurn()
        {
            TurnFractionLeft = 1;
        }

        /// <summary>
        /// Computes the motive if set, filters invalid moves, and selects
        /// a move based on combat history. Returns null if no moves are
        /// available, else returns the move and changes turn fraction left.
        /// </summary>
        /// <param name="combatHistory">
        /// The list of all characters.
        /// </param>
        /// <param name="moves">
        /// The list of all moves for the character.
        /// </param>
        public List<Move> Perform(List<List<Character>> combatHistory)
        {
            List<Move> availableMoves = Moves;

            // Gets the motive.
            if (GetMotives != null)
            {
                Motives = GetMotives(combatHistory);

                // Filters out invalid moves.
                availableMoves = Moves.Where((move) =>
                {
                    return move.IsMoveEnabled &&
                        move.Cooldown == 0 &&
                        move.UsesPerTurnProgress < move.UsesPerTurn;
                })
                .ToList();
            }

            // Gets the move.
            if (GetMoves != null)
            {
                ChosenMoves = GetMoves(combatHistory, Motives, availableMoves);
            }
            else
            {
                ChosenMoves = new List<Move>();
            }

            return ChosenMoves;
        }
        #endregion
    }
}
