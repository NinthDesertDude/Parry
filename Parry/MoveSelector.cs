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
        /// Usually, moves can only be charged if their turn fraction minus
        /// their charge-up progress is greater than 1. When enabled, all moves
        /// can be charged.
        /// 
        /// Example: If Tickle takes 1/2 a turn and Punch takes 1 turn, you
        /// can use Tickle and fill up the remaining turn by charging Punch.
        /// Next turn, you can use both Tickle and Punch, since both cost 1/2
        /// a turn.
        /// 
        /// False by default.
        /// </summary>
        public bool ChargePartialMoves;

        /// <summary>
        /// Usually, all turnChargeProgress is removed from a move when it's
        /// selected. When enabled, if a move's turnChargeProgress is larger
        /// than its turn fraction, the remainder is preserved rather than
        /// setting the charge back to 0.
        /// 
        /// Example: If Tickle takes 1/2 a turn and is charged up for 2 turns,
        /// it would be charged up for 1.5 turns after use.
        /// 
        /// False by default.
        /// </summary>
        public bool PreserveRemainingChargeWhenMoveUsed;

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
        /// Takes the list of performable moves for a chosen or calculated motive
        /// with the combat history and returns the moves to use. Invalid moves
        /// are removed later and only as many moves as can fit in one turn are
        /// used, starting from the first move in the list.
        /// First argument: The combat history where index 0 is most current.
        /// Second argument: A list of MotiveWithPriority.
        /// Third argument: A list of available moves (excludes disabled,
        /// moves cooling down, etc.).
        /// Returns: A list of moves to perform.
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
        /// Default empty list.
        /// </summary>
        public List<Move> ChosenMoves
        {
            private set;
            get;
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
            ChargePartialMoves = false;
            ChosenMoves = new List<Move>();
            GetMotives = null;
            GetMoves = new Func<List<List<Character>>, List<MotiveWithPriority>, List<Move>, List<Move>>(
                (combatHistory, motives, moves) =>
            {
                if (moves == null || moves.Count == 0)
                {
                    return new List<Move>();
                }

                return new List<Move>() { moves.FirstOrDefault() };
            });
            Motives = new List<MotiveWithPriority>() {
                new MotiveWithPriority() { motive = Constants.Motives.DamageHealth, priority = 100 }
            };
            MovementAfterBehavior = null;
            MovementBeforeBehavior = null;
            Moves = new List<Move>();
            PreserveRemainingChargeWhenMoveUsed = false;
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
            ChargePartialMoves = false;
            ChosenMoves = new List<Move>();
            GetMotives = null;
            GetMoves = getMoves;
            Motives = new List<MotiveWithPriority>() {
                new MotiveWithPriority() { motive = Constants.Motives.DamageHealth, priority = 100 }
            };
            MovementAfterBehavior = null;
            MovementBeforeBehavior = null;
            Moves = moves;
            PreserveRemainingChargeWhenMoveUsed = false;
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        public MoveSelector(MoveSelector other)
        {
            ChargePartialMoves = other.ChargePartialMoves;
            ChosenMoves = new List<Move>(other.ChosenMoves);
            GetMotives = other.GetMotives;
            GetMoves = other.GetMoves;
            Motives = new List<MotiveWithPriority>(other.Motives);
            MovementAfterBehavior = other.MovementAfterBehavior;
            MovementBeforeBehavior = other.MovementBeforeBehavior;
            Moves = new List<Move>(other.Moves);
            PreserveRemainingChargeWhenMoveUsed = other.PreserveRemainingChargeWhenMoveUsed;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Computes the motive, filters invalid moves, and selects moves
        /// based on combat history. All moves to be charged charge now, and
        /// all moves to be performed are returned.
        /// </summary>
        /// <param name="combatHistory">
        /// The list of all characters.
        /// </param>
        /// <param name="moves">
        /// The list of all moves for the character.
        /// </param>
        public List<Move> Perform(List<List<Character>> combatHistory)
        {
            List<Move> filteredMoves = Moves.Where((move) => move.CanPerform()).ToList();

            // Gets the motive.
            if (GetMotives != null)
            {
                Motives = GetMotives(combatHistory);
            }

            ChosenMoves = (GetMoves != null)
                ? GetMoves(combatHistory, Motives, filteredMoves)
                : new List<Move>();

            float fractionOfTurnLeft = 1;
            List<Move> excludedMoves = new List<Move>();

            for (int i = 0; i < ChosenMoves.Count; i++)
            {
                Move move = ChosenMoves[i];

                if (move == null)
                {
                    excludedMoves.Add(move);
                    continue;
                }

                // Charging moves.
                if (move.OnlyCharge)
                {
                    // Charging moves in the same round.
                    if (fractionOfTurnLeft >= move.TurnFraction)
                    {
                        move.TurnChargeFraction = (float)Math.Round(
                            move.TurnChargeFraction + move.TurnFraction, 6);
                        fractionOfTurnLeft -= move.TurnFraction;
                    }

                    // Charging moves partially in the same round.
                    else if (ChargePartialMoves)
                    {
                        move.TurnChargeFraction = (float)Math.Round(
                            move.TurnChargeFraction + fractionOfTurnLeft, 6);
                        fractionOfTurnLeft = 0;
                    }

                    excludedMoves.Add(move);
                }

                // Performing moves.
                else
                {
                    float chargedCost = Math.Max(
                        move.TurnFraction - move.TurnChargeFraction, 0);

                    // Performing moves in the same round.
                    if (fractionOfTurnLeft >= chargedCost)
                    {
                        if (!PreserveRemainingChargeWhenMoveUsed || chargedCost > 0)
                        {
                            move.TurnChargeFraction = 0;
                        }
                        else
                        {
                            move.TurnChargeFraction -= move.TurnFraction;
                        }

                        fractionOfTurnLeft = (!move.UsesRemainingTurn)
                            ? fractionOfTurnLeft - chargedCost : 0;
                    }

                    // Charging moves partially in the same round.
                    else
                    {
                        if (ChargePartialMoves)
                        {
                            move.TurnChargeFraction = (float)Math.Round(
                                move.TurnChargeFraction + fractionOfTurnLeft, 6);
                            fractionOfTurnLeft = 0;
                        }

                        excludedMoves.Add(move);
                    }
                }
            }

            ChosenMoves = ChosenMoves
                .Except(excludedMoves)
                .ToList();

            return ChosenMoves;
        }
        #endregion
    }
}
